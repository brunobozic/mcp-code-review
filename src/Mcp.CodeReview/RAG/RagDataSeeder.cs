using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Mcp.CodeReview.RAG
{
    /// <summary>
    /// Seeds the RAG system with initial data for 80/20 implementation
    /// Provides essential coding standards, patterns, and common issues
    /// </summary>
    public class RagDataSeeder
    {
        private readonly IVectorSearchService _vectorSearchService;
        private readonly ILogger<RagDataSeeder> _logger;

        public RagDataSeeder(
            IVectorSearchService vectorSearchService,
            ILogger<RagDataSeeder> logger)
        {
            _vectorSearchService = vectorSearchService ?? throw new ArgumentNullException(nameof(vectorSearchService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Seeds the RAG system with essential data for 80/20 implementation
        /// </summary>
        public async Task SeedEssentialDataAsync(string defaultProjectId = "default", CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting RAG data seeding for 80/20 implementation");

            try
            {
                // Initialize collections first
                await _vectorSearchService.InitializeCollectionsAsync(cancellationToken);

                // Seed essential data
                await SeedCodingStandardsAsync(cancellationToken);
                await SeedHistoricalIssuesAsync(defaultProjectId, cancellationToken);
                await SeedTeamPatternsAsync(defaultProjectId, cancellationToken);
                await SeedCodePatternsAsync(defaultProjectId, cancellationToken);

                _logger.LogInformation("RAG data seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed RAG data");
                throw;
            }
        }

        /// <summary>
        /// Seeds essential coding standards and guidelines
        /// </summary>
        private async Task SeedCodingStandardsAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Seeding coding standards");

            var standards = new[]
            {
                new RAGDocument
                {
                    Id = "cs-security-001",
                    Collection = "coding_standards",
                    Content = @"
Authentication Security Standards:
- Always use JWT tokens with short expiry (15 minutes maximum)
- Implement refresh token rotation for security
- Use BCrypt or Argon2 for password hashing with minimum 12 salt rounds
- Implement constant-time comparison to prevent timing attacks
- Log all authentication attempts for monitoring
- Implement rate limiting: maximum 5 login attempts per minute per IP
- Never store passwords in plain text or reversible encryption
                    ",
                    Metadata = new Dictionary<string, object>
                    {
                        ["category"] = "security",
                        ["priority"] = "critical",
                        ["type"] = "authentication"
                    }
                },

                new RAGDocument
                {
                    Id = "cs-performance-001",
                    Collection = "coding_standards",
                    Content = @"
Performance Best Practices:
- Always use async/await for I/O operations
- Implement database query optimization (avoid N+1 queries)
- Use pagination for large data sets (max 100 items per page)
- Implement caching for frequently accessed data
- Use connection pooling for database connections
- Avoid blocking operations in async methods
- Implement proper disposal patterns for resources
- Use StringBuilder for multiple string concatenations
                    ",
                    Metadata = new Dictionary<string, object>
                    {
                        ["category"] = "performance",
                        ["priority"] = "high",
                        ["type"] = "optimization"
                    }
                },

                new RAGDocument
                {
                    Id = "cs-architecture-001",
                    Collection = "coding_standards",
                    Content = @"
Architecture Guidelines:
- Follow SOLID principles in class design
- Use dependency injection for loose coupling
- Implement Repository pattern with Unit of Work for data access
- Separate business logic from presentation logic
- Use MediatR pattern for complex business operations
- Implement proper error handling with custom exceptions
- Use AutoMapper for object-to-object mapping
- Follow Clean Architecture principles
                    ",
                    Metadata = new Dictionary<string, object>
                    {
                        ["category"] = "architecture",
                        ["priority"] = "high",
                        ["type"] = "design-patterns"
                    }
                },

                new RAGDocument
                {
                    Id = "cs-testing-001",
                    Collection = "coding_standards",
                    Content = @"
Testing Standards:
- Maintain minimum 80% code coverage
- Write unit tests for all business logic
- Use integration tests for API endpoints
- Mock external dependencies in unit tests
- Follow AAA pattern (Arrange, Act, Assert)
- Use meaningful test names that describe the scenario
- Test both positive and negative scenarios
- Implement performance tests for critical paths
                    ",
                    Metadata = new Dictionary<string, object>
                    {
                        ["category"] = "testing",
                        ["priority"] = "medium",
                        ["type"] = "quality-assurance"
                    }
                }
            };

            foreach (var standard in standards)
            {
                await _vectorSearchService.StoreDocumentAsync(standard, cancellationToken);
            }

            _logger.LogDebug("Seeded {Count} coding standards", standards.Length);
        }

        /// <summary>
        /// Seeds common historical issues and their resolutions
        /// </summary>
        private async Task SeedHistoricalIssuesAsync(string projectId, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Seeding historical issues");

            var issues = new[]
            {
                new RAGDocument
                {
                    Id = "hi-001",
                    Collection = "historical_issues",
                    Content = @"
Issue: SQL Injection in User Search
Description: User search functionality was vulnerable to SQL injection attacks due to string concatenation in LINQ queries.
Root Cause: Direct string concatenation instead of parameterized queries.
Solution: Replaced string concatenation with parameterized Entity Framework queries.
Impact: Critical security vulnerability resolved.
Prevention: Always use parameterized queries and input validation.
                    ",
                    Metadata = new Dictionary<string, object>
                    {
                        ["project_id"] = projectId,
                        ["category"] = "security",
                        ["severity"] = "critical",
                        ["resolved_date"] = "2024-01-15",
                        ["issue_type"] = "sql-injection"
                    }
                },

                new RAGDocument
                {
                    Id = "hi-002",
                    Collection = "historical_issues",
                    Content = @"
Issue: Memory Leak in Data Processing
Description: Application was experiencing memory leaks during large data processing operations.
Root Cause: Large objects were not being disposed properly and were accumulating in memory.
Solution: Implemented proper disposal patterns using 'using' statements and IDisposable.
Impact: Reduced memory usage by 60% and eliminated OutOfMemoryExceptions.
Prevention: Always implement IDisposable for resource-heavy classes and use using statements.
                    ",
                    Metadata = new Dictionary<string, object>
                    {
                        ["project_id"] = projectId,
                        ["category"] = "performance",
                        ["severity"] = "high",
                        ["resolved_date"] = "2024-02-03",
                        ["issue_type"] = "memory-leak"
                    }
                },

                new RAGDocument
                {
                    Id = "hi-003",
                    Collection = "historical_issues",
                    Content = @"
Issue: Authentication Timing Attack
Description: Login functionality was vulnerable to timing attacks allowing username enumeration.
Root Cause: Different response times for valid vs invalid usernames.
Solution: Implemented constant-time authentication with consistent delays.
Impact: Eliminated timing-based username enumeration attack vector.
Prevention: Always use constant-time operations for authentication and implement consistent response patterns.
                    ",
                    Metadata = new Dictionary<string, object>
                    {
                        ["project_id"] = projectId,
                        ["category"] = "security",
                        ["severity"] = "medium",
                        ["resolved_date"] = "2024-01-28",
                        ["issue_type"] = "timing-attack"
                    }
                },

                new RAGDocument
                {
                    Id = "hi-004",
                    Collection = "historical_issues",
                    Content = @"
Issue: N+1 Query Performance Problem
Description: User profile page was making hundreds of database queries causing severe performance issues.
Root Cause: Lazy loading in Entity Framework causing N+1 query pattern.
Solution: Used Include() statements for eager loading and implemented projection queries.
Impact: Reduced page load time from 5 seconds to 200ms.
Prevention: Always review Entity Framework queries and use Include() for related data or implement projection queries.
                    ",
                    Metadata = new Dictionary<string, object>
                    {
                        ["project_id"] = projectId,
                        ["category"] = "performance",
                        ["severity"] = "high",
                        ["resolved_date"] = "2024-02-15",
                        ["issue_type"] = "n-plus-one"
                    }
                }
            };

            foreach (var issue in issues)
            {
                await _vectorSearchService.StoreDocumentAsync(issue, cancellationToken);
            }

            _logger.LogDebug("Seeded {Count} historical issues", issues.Length);
        }

        /// <summary>
        /// Seeds team patterns and preferences
        /// </summary>
        private async Task SeedTeamPatternsAsync(string projectId, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Seeding team patterns");

            var patterns = new[]
            {
                new RAGDocument
                {
                    Id = "tp-001",
                    Collection = "team_patterns",
                    Content = @"
Team Pattern: Repository with Unit of Work
Description: Team consistently uses Repository pattern combined with Unit of Work for data access.
Usage: All data access goes through repository interfaces with dependency injection.
Benefits: Improved testability, separation of concerns, and consistent data access patterns.
Implementation: Use generic repository base class with specific implementations for entities.
                    ",
                    Metadata = new Dictionary<string, object>
                    {
                        ["project_id"] = projectId,
                        ["pattern_type"] = "data-access",
                        ["confidence"] = "high",
                        ["usage_frequency"] = "always"
                    }
                },

                new RAGDocument
                {
                    Id = "tp-002",
                    Collection = "team_patterns",
                    Content = @"
Team Preference: Composition over Inheritance
Description: Team prefers composition over inheritance for code reuse and flexibility.
Reasoning: Composition provides better flexibility and easier testing compared to inheritance hierarchies.
Implementation: Use dependency injection and interfaces instead of base classes where possible.
Exception: Only use inheritance for true 'is-a' relationships, prefer composition for 'has-a' relationships.
                    ",
                    Metadata = new Dictionary<string, object>
                    {
                        ["project_id"] = projectId,
                        ["pattern_type"] = "design-principle",
                        ["confidence"] = "high",
                        ["usage_frequency"] = "preferred"
                    }
                },

                new RAGDocument
                {
                    Id = "tp-003",
                    Collection = "team_patterns",
                    Content = @"
Team Standard: Explicit Error Handling
Description: Team requires explicit error handling with custom exception types instead of generic exceptions.
Implementation: Create specific exception classes for different error scenarios.
Logging: Always log exceptions with correlation IDs for traceability.
User Experience: Provide meaningful error messages to users while logging technical details.
                    ",
                    Metadata = new Dictionary<string, object>
                    {
                        ["project_id"] = projectId,
                        ["pattern_type"] = "error-handling",
                        ["confidence"] = "high",
                        ["usage_frequency"] = "required"
                    }
                }
            };

            foreach (var pattern in patterns)
            {
                await _vectorSearchService.StoreDocumentAsync(pattern, cancellationToken);
            }

            _logger.LogDebug("Seeded {Count} team patterns", patterns.Length);
        }

        /// <summary>
        /// Seeds common code patterns and examples
        /// </summary>
        private async Task SeedCodePatternsAsync(string projectId, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Seeding code patterns");

            var patterns = new[]
            {
                new RAGDocument
                {
                    Id = "cp-001",
                    Collection = "code_patterns",
                    Content = @"
Authentication Controller Pattern:
This pattern shows the team's preferred way to implement authentication endpoints.

[ApiController]
[Route(""api/[controller]"")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost(""login"")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.AuthenticateAsync(request.Email, request.Password);
        
        if (result.IsSuccess)
        {
            return Ok(new { Token = result.Token, RefreshToken = result.RefreshToken });
        }
        
        return Unauthorized(new { Message = ""Invalid credentials"" });
    }
}
                    ",
                    Metadata = new Dictionary<string, object>
                    {
                        ["project_id"] = projectId,
                        ["pattern_type"] = "controller",
                        ["language"] = "csharp",
                        ["category"] = "authentication"
                    }
                },

                new RAGDocument
                {
                    Id = "cp-002",
                    Collection = "code_patterns",
                    Content = @"
Service Layer Pattern:
This shows the team's standard service implementation pattern.

public interface IUserService
{
    Task<UserDto> GetUserByIdAsync(int userId);
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;
    
    public UserService(IUserRepository userRepository, IMapper mapper, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<UserDto> GetUserByIdAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new UserNotFoundException($""User with ID {userId} not found"");
            }
            
            return _mapper.Map<UserDto>(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ""Error retrieving user {UserId}"", userId);
            throw;
        }
    }
}
                    ",
                    Metadata = new Dictionary<string, object>
                    {
                        ["project_id"] = projectId,
                        ["pattern_type"] = "service",
                        ["language"] = "csharp",
                        ["category"] = "business-logic"
                    }
                }
            };

            foreach (var pattern in patterns)
            {
                await _vectorSearchService.StoreDocumentAsync(pattern, cancellationToken);
            }

            _logger.LogDebug("Seeded {Count} code patterns", patterns.Length);
        }
    }
}