using System;
using System.Collections.Generic;
using System.Text;

namespace TestCode
{
    /// <summary>
    /// Sample code with various complexity patterns for testing the enhanced AI system
    /// This class demonstrates multiple code characteristics that should trigger different agents
    /// </summary>
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private readonly IEmailService _emailService;
        private readonly ICacheService _cacheService;

        public UserService(
            IUserRepository userRepository, 
            ILogger<UserService> logger,
            IEmailService emailService,
            ICacheService cacheService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        /// <summary>
        /// Creates a new user with complex business logic
        /// This method should trigger multiple agent types due to its characteristics
        /// </summary>
        public async Task<CreateUserResult> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                // Input validation (Security concern)
                if (string.IsNullOrWhiteSpace(request.Email))
                    throw new ArgumentException("Email is required", nameof(request.Email));

                if (string.IsNullOrWhiteSpace(request.Password))
                    throw new ArgumentException("Password is required", nameof(request.Password));

                // Business logic validation (Domain expertise needed)
                if (!IsValidEmailDomain(request.Email))
                    throw new BusinessRuleException("Email domain not allowed");

                // Check cache first (Performance optimization)
                var cacheKey = $"user_exists_{request.Email.ToLower()}";
                var existsInCache = await _cacheService.GetAsync<bool?>(cacheKey);
                
                if (existsInCache == true)
                {
                    return CreateUserResult.Failure("User already exists");
                }

                // Database operation (Security & Performance concern)
                var existingUser = await _userRepository.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    await _cacheService.SetAsync(cacheKey, true, TimeSpan.FromMinutes(5));
                    return CreateUserResult.Failure("User already exists");
                }

                // Password hashing (Security critical)
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);

                // Create user entity (Domain modeling)
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email.ToLower().Trim(),
                    PasswordHash = hashedPassword,
                    FirstName = request.FirstName?.Trim(),
                    LastName = request.LastName?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    EmailVerified = false,
                    SubscriptionTier = DetermineInitialSubscriptionTier(request)
                };

                // Save to database (Potential performance bottleneck)
                await _userRepository.CreateAsync(user);
                
                // Clear cache
                await _cacheService.RemoveAsync(cacheKey);

                // Send welcome email (Async operation)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send welcome email for user {UserId}", user.Id);
                    }
                });

                _logger.LogInformation("Successfully created user {UserId} with email {Email}", 
                    user.Id, user.Email);

                return CreateUserResult.Success(user.Id);
            }
            catch (BusinessRuleException ex)
            {
                _logger.LogWarning(ex, "Business rule violation during user creation: {Email}", request.Email);
                return CreateUserResult.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during user creation: {Email}", request.Email);
                return CreateUserResult.Failure("An unexpected error occurred");
            }
        }

        /// <summary>
        /// Retrieves user with caching and complex business logic
        /// Performance-critical method with multiple optimization opportunities
        /// </summary>
        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            // TODO: Add rate limiting here
            
            var cacheKey = $"user_{userId}";
            
            // Try cache first (Performance optimization)
            var cachedUser = await _cacheService.GetAsync<User>(cacheKey);
            if (cachedUser != null)
            {
                return cachedUser;
            }

            // Database query (N+1 potential issue)
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            // Cache the result (but what about cache invalidation?)
            await _cacheService.SetAsync(cacheKey, user, TimeSpan.FromMinutes(30));
            
            return user;
        }

        /// <summary>
        /// Bulk user processing with potential performance issues
        /// This method has algorithmic complexity concerns
        /// </summary>
        public async Task<BulkProcessResult> ProcessUsersInBulkAsync(List<Guid> userIds)
        {
            var results = new List<UserProcessResult>();
            
            // PERFORMANCE ISSUE: N+1 query problem
            foreach (var userId in userIds)
            {
                try
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user != null)
                    {
                        // Complex business logic
                        var processed = await ProcessSingleUserAsync(user);
                        results.Add(processed);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process user {UserId}", userId);
                    results.Add(UserProcessResult.Failed(userId, ex.Message));
                }
            }

            return new BulkProcessResult
            {
                TotalProcessed = results.Count,
                SuccessCount = results.Count(r => r.Success),
                FailureCount = results.Count(r => !r.Success),
                Results = results
            };
        }

        // Private helper methods
        private bool IsValidEmailDomain(string email)
        {
            var domain = email.Split('@').LastOrDefault()?.ToLower();
            var blockedDomains = new[] { "tempmail.com", "guerrillamail.com", "10minutemail.com" };
            
            return !string.IsNullOrEmpty(domain) && !blockedDomains.Contains(domain);
        }

        private SubscriptionTier DetermineInitialSubscriptionTier(CreateUserRequest request)
        {
            // Business logic for determining subscription tier
            if (request.Email.EndsWith("@enterprise.com"))
                return SubscriptionTier.Enterprise;
            
            if (request.ReferralCode?.StartsWith("PREMIUM") == true)
                return SubscriptionTier.Premium;
            
            return SubscriptionTier.Basic;
        }

        private async Task<UserProcessResult> ProcessSingleUserAsync(User user)
        {
            // Simulate complex processing
            await Task.Delay(100); // This could be optimized
            
            // More business logic here
            user.LastProcessedAt = DateTime.UtcNow;
            
            await _userRepository.UpdateAsync(user);
            
            return UserProcessResult.Success(user.Id);
        }
    }

    // Supporting classes and interfaces
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string email);
        Task<User?> GetByIdAsync(Guid id);
        Task CreateAsync(User user);
        Task UpdateAsync(User user);
    }

    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string email, string firstName);
    }

    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan expiration);
        Task RemoveAsync(string key);
    }

    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastProcessedAt { get; set; }
        public bool IsActive { get; set; }
        public bool EmailVerified { get; set; }
        public SubscriptionTier SubscriptionTier { get; set; }
    }

    public class CreateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ReferralCode { get; set; }
    }

    public class CreateUserResult
    {
        public bool Success { get; set; }
        public Guid? UserId { get; set; }
        public string? ErrorMessage { get; set; }

        public static CreateUserResult Success(Guid userId) => new() { Success = true, UserId = userId };
        public static CreateUserResult Failure(string error) => new() { Success = false, ErrorMessage = error };
    }

    public class BulkProcessResult
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<UserProcessResult> Results { get; set; } = new();
    }

    public class UserProcessResult
    {
        public Guid UserId { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public static UserProcessResult Success(Guid userId) => new() { UserId = userId, Success = true };
        public static UserProcessResult Failed(Guid userId, string error) => new() { UserId = userId, Success = false, ErrorMessage = error };
    }

    public enum SubscriptionTier
    {
        Basic,
        Premium,
        Enterprise
    }

    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message) { }
    }
}