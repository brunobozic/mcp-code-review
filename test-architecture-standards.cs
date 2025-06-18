using System;
using System.Collections.Generic;

namespace TestProject.Services
{
    // Legacy-style service without proper patterns
    public class LegacyUserService
    {
        public void CreateUser(string name, string password)
        {
            // Missing validation
            var user = new User { Name = name, Password = password };
            // Direct database call without repository pattern
            Database.Insert(user);
        }
        
        public User GetUser(int id)
        {
            return Database.GetById(id);
        }
    }
    
    // Modern service with proper patterns
    public class ModernUserService : BaseService, IUserService
    {
        #region Constructor
        
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ModernUserService> _logger;
        
        /// <summary>
        /// Initializes a new instance of the ModernUserService class
        /// </summary>
        /// <param name="userRepository">Repository for user data access</param>
        /// <param name="unitOfWork">Unit of work for transaction management</param>
        /// <param name="logger">Logger instance for structured logging</param>
        public ModernUserService(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ILogger<ModernUserService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Creates a new user with validation and structured logging
        /// </summary>
        /// <param name="request">User creation request with validation</param>
        /// <returns>Response containing the created user or error details</returns>
        public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = request.CorrelationId,
                ["Operation"] = "CreateUser"
            });
            
            try
            {
                _logger.LogInformation("Creating user with name {UserName}", request.Name);
                
                // Validate request
                var validationResult = ValidateCreateUserRequest(request);
                if (!validationResult.IsValid)
                {
                    return CreateUserResponse.Failure(validationResult.Errors);
                }
                
                // Create user entity
                var user = new User
                {
                    Name = request.Name,
                    Email = request.Email,
                    Password = HashPassword(request.Password),
                    CreatedAt = DateTimeOffset.UtcNow
                };
                
                // Save using repository pattern
                await _userRepository.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Successfully created user {UserId}", user.Id);
                
                return CreateUserResponse.Success(new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    _links = GenerateHateoasLinks(user.Id)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user: {ErrorMessage}. Inner exception: {InnerException}. Stack trace: {StackTrace}", 
                    ex.Message, ex.InnerException?.Message, ex.StackTrace);
                
                return CreateUserResponse.Failure("User creation failed");
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private ValidationResult ValidateCreateUserRequest(CreateUserRequest request)
        {
            var result = new ValidationResult { IsValid = true };
            
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                result.IsValid = false;
                result.Errors.Add("Name is required");
            }
            
            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12)
            {
                result.IsValid = false;
                result.Errors.Add("Password must be at least 12 characters long");
            }
            
            return result;
        }
        
        private string HashPassword(string password)
        {
            // Secure password hashing implementation
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        
        private Dictionary<string, string> GenerateHateoasLinks(int userId)
        {
            return new Dictionary<string, string>
            {
                ["self"] = $"/api/users/{userId}",
                ["edit"] = $"/api/users/{userId}",
                ["delete"] = $"/api/users/{userId}"
            };
        }
        
        #endregion
    }
    
    // Request/Response patterns
    public class CreateUserRequest : BaseRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    
    public class CreateUserResponse : BaseResponse
    {
        public UserDto? User { get; set; }
        public Dictionary<string, string> _links { get; set; } = new();
        
        public static CreateUserResponse Success(UserDto user) => new()
        {
            Success = true,
            User = user
        };
        
        public static CreateUserResponse Failure(List<string> errors) => new()
        {
            Success = false,
            Errors = errors
        };
    }
}