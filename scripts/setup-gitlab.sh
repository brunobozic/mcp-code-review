#!/bin/bash
set -euo pipefail

# GitLab Setup Script with Best Practices
# This script configures GitLab with proper security settings and creates necessary users/tokens

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
GITLAB_URL="http://localhost:8080"
GITLAB_ROOT_PASSWORD="SecurePassword123!"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

wait_for_gitlab() {
    log_info "Waiting for GitLab to be ready..."
    local max_attempts=60
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if curl -f -s "$GITLAB_URL/users/sign_in" > /dev/null 2>&1; then
            log_success "GitLab is ready!"
            return 0
        fi
        
        log_info "Attempt $attempt/$max_attempts - GitLab not ready yet, waiting 10 seconds..."
        sleep 10
        attempt=$((attempt + 1))
    done
    
    log_error "GitLab failed to start within expected time"
    return 1
}

get_root_token() {
    log_info "Getting root user token..."
    
    # Create a personal access token for root user
    local response=$(curl -s -X POST "$GITLAB_URL/oauth/token" \
        -H "Content-Type: application/json" \
        -d '{
            "grant_type": "password",
            "username": "root",
            "password": "'$GITLAB_ROOT_PASSWORD'"
        }' || echo "")
    
    if [ -z "$response" ]; then
        log_warning "OAuth token creation failed, trying alternative method..."
        
        # Alternative: Create personal access token via Rails console
        docker exec -it mcp-gitlab gitlab-rails runner "
            user = User.find_by(username: 'root')
            token = user.personal_access_tokens.create(
                name: 'mcp-automation-token',
                scopes: ['api', 'read_user', 'read_repository', 'write_repository', 'read_registry', 'write_registry']
            )
            puts token.token
        " 2>/dev/null | tail -1
    else
        echo "$response" | jq -r '.access_token' 2>/dev/null || echo ""
    fi
}

create_mcp_user() {
    local token=$1
    log_info "Creating MCP service user..."
    
    local user_data='{
        "name": "MCP Code Review Service",
        "username": "mcp-service",
        "email": "mcp-service@localhost",
        "password": "McpServicePassword123!",
        "skip_confirmation": true,
        "admin": false,
        "can_create_group": true,
        "projects_limit": 100
    }'
    
    local response=$(curl -s -X POST "$GITLAB_URL/api/v4/users" \
        -H "Authorization: Bearer $token" \
        -H "Content-Type: application/json" \
        -d "$user_data")
    
    if echo "$response" | jq -e '.id' > /dev/null 2>&1; then
        local user_id=$(echo "$response" | jq -r '.id')
        log_success "Created MCP service user with ID: $user_id"
        echo "$user_id"
    else
        log_error "Failed to create MCP service user: $response"
        return 1
    fi
}

create_mcp_token() {
    local user_id=$1
    log_info "Creating personal access token for MCP service user..."
    
    # Create token via Rails console since API requires admin privileges
    local token=$(docker exec -it mcp-gitlab gitlab-rails runner "
        user = User.find($user_id)
        token = user.personal_access_tokens.create(
            name: 'mcp-code-review-token',
            scopes: ['api', 'read_user', 'read_repository', 'write_repository', 'read_registry']
        )
        puts token.token
    " 2>/dev/null | tail -1 | tr -d '\r\n')
    
    if [ -n "$token" ] && [ "$token" != "null" ]; then
        log_success "Created personal access token for MCP service user"
        echo "$token"
    else
        log_error "Failed to create personal access token"
        return 1
    fi
}

create_test_project() {
    local token=$1
    log_info "Creating test repository for MCP testing..."
    
    local project_data='{
        "name": "mcp-test-repository",
        "path": "mcp-test-repository",
        "description": "Test repository for MCP Code Review system",
        "visibility": "private",
        "default_branch": "main",
        "initialize_with_readme": true,
        "issues_enabled": true,
        "merge_requests_enabled": true,
        "wiki_enabled": false,
        "builds_enabled": true
    }'
    
    local response=$(curl -s -X POST "$GITLAB_URL/api/v4/projects" \
        -H "Authorization: Bearer $token" \
        -H "Content-Type: application/json" \
        -d "$project_data")
    
    if echo "$response" | jq -e '.id' > /dev/null 2>&1; then
        local project_id=$(echo "$response" | jq -r '.id')
        local project_url=$(echo "$response" | jq -r '.web_url')
        log_success "Created test project: $project_url"
        echo "$project_id"
    else
        log_error "Failed to create test project: $response"
        return 1
    fi
}

add_sample_code() {
    local token=$1
    local project_id=$2
    log_info "Adding sample C# code to test repository..."
    
    # Read the test code we created earlier
    local sample_code=$(cat << 'EOF'
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace TestCode
{
    /// <summary>
    /// Sample service with various code quality issues for testing
    /// This class demonstrates security, performance, and design issues
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
        /// Creates a new user - has security and performance issues
        /// </summary>
        public async Task<CreateUserResult> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                // SECURITY ISSUE: Weak input validation
                if (string.IsNullOrWhiteSpace(request.Email))
                    throw new ArgumentException("Email is required", nameof(request.Email));

                // SECURITY ISSUE: No password strength validation
                if (string.IsNullOrWhiteSpace(request.Password))
                    throw new ArgumentException("Password is required", nameof(request.Password));

                // PERFORMANCE ISSUE: No caching of email domain validation
                if (!IsValidEmailDomain(request.Email))
                    throw new BusinessRuleException("Email domain not allowed");

                // PERFORMANCE ISSUE: Multiple database calls
                var existingUser = await _userRepository.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return CreateUserResult.Failure("User already exists");
                }

                // SECURITY ISSUE: Password hashing should use stronger settings
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email.ToLower().Trim(),
                    PasswordHash = hashedPassword,
                    FirstName = request.FirstName?.Trim(),
                    LastName = request.LastName?.Trim(),
                    CreatedAt = DateTime.UtcNow, // ISSUE: Should use DateTimeOffset
                    IsActive = true,
                    EmailVerified = false,
                    SubscriptionTier = DetermineInitialSubscriptionTier(request)
                };

                await _userRepository.CreateAsync(user);
                
                // ISSUE: Fire-and-forget without proper error handling
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
        /// PERFORMANCE ISSUE: N+1 query problem
        /// </summary>
        public async Task<BulkProcessResult> ProcessUsersInBulkAsync(List<Guid> userIds)
        {
            var results = new List<UserProcessResult>();
            
            // MAJOR PERFORMANCE ISSUE: N+1 queries
            foreach (var userId in userIds)
            {
                try
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user != null)
                    {
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

        private bool IsValidEmailDomain(string email)
        {
            // ISSUE: Hard-coded domain list should be configurable
            var domain = email.Split('@').LastOrDefault()?.ToLower();
            var blockedDomains = new[] { "tempmail.com", "guerrillamail.com", "10minutemail.com" };
            
            return !string.IsNullOrEmpty(domain) && !blockedDomains.Contains(domain);
        }

        private SubscriptionTier DetermineInitialSubscriptionTier(CreateUserRequest request)
        {
            // ISSUE: Business logic should be externalized
            if (request.Email.EndsWith("@enterprise.com"))
                return SubscriptionTier.Enterprise;
            
            if (request.ReferralCode?.StartsWith("PREMIUM") == true)
                return SubscriptionTier.Premium;
            
            return SubscriptionTier.Basic;
        }

        private async Task<UserProcessResult> ProcessSingleUserAsync(User user)
        {
            // PERFORMANCE ISSUE: Artificial delay
            await Task.Delay(100);
            
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
EOF
)

    # Create the file via GitLab API
    local file_content=$(echo "$sample_code" | base64 -w 0)
    local commit_data='{
        "branch": "main",
        "commit_message": "Add sample UserService with code quality issues for testing",
        "actions": [
            {
                "action": "create",
                "file_path": "src/Services/UserService.cs",
                "content": "'"$file_content"'",
                "encoding": "base64"
            }
        ]
    }'
    
    local response=$(curl -s -X POST "$GITLAB_URL/api/v4/projects/$project_id/repository/commits" \
        -H "Authorization: Bearer $token" \
        -H "Content-Type: application/json" \
        -d "$commit_data")
    
    if echo "$response" | jq -e '.id' > /dev/null 2>&1; then
        log_success "Added sample C# code to repository"
    else
        log_warning "Failed to add sample code (repository might already exist): $response"
    fi
}

configure_webhooks() {
    local token=$1
    local project_id=$2
    log_info "Configuring GitLab webhooks for MCP integration..."
    
    local webhook_data='{
        "url": "http://mcp-server:5000/webhooks/gitlab",
        "push_events": true,
        "merge_requests_events": true,
        "issues_events": false,
        "tag_push_events": false,
        "note_events": true,
        "pipeline_events": false,
        "wiki_page_events": false,
        "deployment_events": false,
        "job_events": false,
        "releases_events": false,
        "subgroup_events": false,
        "enable_ssl_verification": false,
        "token": "webhook_secret_token_123",
        "push_events_branch_filter": "main"
    }'
    
    local response=$(curl -s -X POST "$GITLAB_URL/api/v4/projects/$project_id/hooks" \
        -H "Authorization: Bearer $token" \
        -H "Content-Type: application/json" \
        -d "$webhook_data")
    
    if echo "$response" | jq -e '.id' > /dev/null 2>&1; then
        log_success "Configured GitLab webhook for MCP integration"
    else
        log_warning "Failed to configure webhook: $response"
    fi
}

update_env_file() {
    local gitlab_token=$1
    local project_id=$2
    log_info "Updating .env file with GitLab configuration..."
    
    # Update the .env file with the new token
    if [ -f "$PROJECT_ROOT/.env" ]; then
        sed -i "s/^GITLAB_TOKEN=.*/GITLAB_TOKEN=$gitlab_token/" "$PROJECT_ROOT/.env"
        echo "GITLAB_PROJECT_ID=$project_id" >> "$PROJECT_ROOT/.env"
        log_success "Updated .env file with GitLab token and project ID"
    else
        log_error ".env file not found"
        return 1
    fi
}

main() {
    log_info "Starting GitLab setup with best practices..."
    
    # Wait for GitLab to be ready
    if ! wait_for_gitlab; then
        log_error "GitLab setup failed - GitLab is not accessible"
        exit 1
    fi
    
    # Get root token
    log_info "Getting GitLab root token..."
    local root_token=$(get_root_token)
    if [ -z "$root_token" ] || [ "$root_token" = "null" ]; then
        log_error "Failed to get root token"
        exit 1
    fi
    log_success "Got GitLab root token"
    
    # Create MCP service user
    local mcp_user_id=$(create_mcp_user "$root_token")
    if [ -z "$mcp_user_id" ]; then
        log_error "Failed to create MCP service user"
        exit 1
    fi
    
    # Create token for MCP service user
    local mcp_token=$(create_mcp_token "$mcp_user_id")
    if [ -z "$mcp_token" ]; then
        log_error "Failed to create MCP service token"
        exit 1
    fi
    
    # Create test project
    local project_id=$(create_test_project "$mcp_token")
    if [ -z "$project_id" ]; then
        log_error "Failed to create test project"
        exit 1
    fi
    
    # Add sample code to the project
    add_sample_code "$mcp_token" "$project_id"
    
    # Configure webhooks
    configure_webhooks "$mcp_token" "$project_id"
    
    # Update .env file
    update_env_file "$mcp_token" "$project_id"
    
    # Print summary
    echo
    log_success "GitLab setup completed successfully!"
    echo
    echo "=== GitLab Configuration Summary ==="
    echo "GitLab URL: $GITLAB_URL"
    echo "Root user: root"
    echo "Root password: $GITLAB_ROOT_PASSWORD"
    echo "MCP Service User: mcp-service"
    echo "MCP Token: $mcp_token"
    echo "Test Project ID: $project_id"
    echo "Test Project URL: $GITLAB_URL/mcp-service/mcp-test-repository"
    echo
    echo "Next steps:"
    echo "1. Access GitLab at: $GITLAB_URL"
    echo "2. Login as root or mcp-service user"
    echo "3. Create a merge request to test the MCP workflow"
    echo "4. Monitor logs with: docker logs mcp-code-review -f"
    echo
}

# Run the main function
main "$@"