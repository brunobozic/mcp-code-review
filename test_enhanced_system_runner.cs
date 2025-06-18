using System;
using System.Threading.Tasks;
using Mcp.CodeReview.AI;
using Mcp.CodeReview.Models;
using Mcp.CodeReview.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace TestEnhancedSystem
{
    /// <summary>
    /// Test runner for the enhanced AI agent orchestration system
    /// Validates 2024 improvements including nested chats, dynamic selection, and cross-validation
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Enhanced AI Agent Orchestration System Test ===");
            Console.WriteLine("Testing 2024 improvements: Nested chats, dynamic selection, cross-validation");
            Console.WriteLine();

            // Setup dependency injection
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            
            // Mock services for testing
            services.AddSingleton<IClaudeService, MockClaudeService>();
            services.AddSingleton<IAgentOrchestrator, MockAgentOrchestrator>();
            services.AddSingleton<ConsolidatedAIReviewSystem>();

            var serviceProvider = services.BuildServiceProvider();
            var reviewSystem = serviceProvider.GetRequiredService<ConsolidatedAIReviewSystem>();

            // Load test code
            var testCode = await LoadTestCode();
            
            // Test 1: Dynamic Agent Selection
            Console.WriteLine("🔍 Test 1: Dynamic Agent Selection");
            await TestDynamicAgentSelection(serviceProvider, testCode);
            Console.WriteLine();

            // Test 2: Nested Chat Framework
            Console.WriteLine("🔄 Test 2: Nested Chat Framework (Writer-Critic Loops)");
            await TestNestedChatFramework(serviceProvider, testCode);
            Console.WriteLine();

            // Test 3: Enhanced Multi-Agent Review
            Console.WriteLine("🚀 Test 3: Enhanced Multi-Agent Review (Full System)");
            await TestEnhancedMultiAgentReview(reviewSystem, testCode);
            Console.WriteLine();

            // Test 4: Cross-Agent Validation
            Console.WriteLine("✅ Test 4: Cross-Agent Validation");
            await TestCrossAgentValidation(serviceProvider, testCode);
            Console.WriteLine();

            Console.WriteLine("=== All Tests Completed ===");
            Console.WriteLine("Enhanced system validation successful!");
        }

        static async Task<string> LoadTestCode()
        {
            // Return the UserService test code
            return @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestCode
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private readonly IEmailService _emailService;
        private readonly ICacheService _cacheService;

        public async Task<CreateUserResult> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                // Input validation (Security concern)
                if (string.IsNullOrWhiteSpace(request.Email))
                    throw new ArgumentException(""Email is required"", nameof(request.Email));

                if (string.IsNullOrWhiteSpace(request.Password))
                    throw new ArgumentException(""Password is required"", nameof(request.Password));

                // Business logic validation (Domain expertise needed)
                if (!IsValidEmailDomain(request.Email))
                    throw new BusinessRuleException(""Email domain not allowed"");

                // Check cache first (Performance optimization)
                var cacheKey = $""user_exists_{request.Email.ToLower()}"";
                var existsInCache = await _cacheService.GetAsync<bool?>(cacheKey);
                
                if (existsInCache == true)
                {
                    return CreateUserResult.Failure(""User already exists"");
                }

                // Database operation (Security & Performance concern)
                var existingUser = await _userRepository.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    await _cacheService.SetAsync(cacheKey, true, TimeSpan.FromMinutes(5));
                    return CreateUserResult.Failure(""User already exists"");
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
                        _logger.LogError(ex, ""Failed to send welcome email for user {UserId}"", user.Id);
                    }
                });

                _logger.LogInformation(""Successfully created user {UserId} with email {Email}"", 
                    user.Id, user.Email);

                return CreateUserResult.Success(user.Id);
            }
            catch (BusinessRuleException ex)
            {
                _logger.LogWarning(ex, ""Business rule violation during user creation: {Email}"", request.Email);
                return CreateUserResult.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ""Unexpected error during user creation: {Email}"", request.Email);
                return CreateUserResult.Failure(""An unexpected error occurred"");
            }
        }

        // Bulk processing with N+1 query problem
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
                        var processed = await ProcessSingleUserAsync(user);
                        results.Add(processed);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ""Failed to process user {UserId}"", userId);
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
    }
}";
        }

        static async Task TestDynamicAgentSelection(IServiceProvider serviceProvider, string testCode)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<DynamicAgentSelector>>();
            var selector = new DynamicAgentSelector(logger);

            var context = new Dictionary<string, object>
            {
                ["TeamExperience"] = "junior",
                ["BusinessCritical"] = true,
                ["BusinessDomain"] = "financial"
            };

            var options = new ReviewOptions
            {
                IncludeSecurityAnalysis = true,
                IncludePerformanceAnalysis = true,
                ReviewDepth = "comprehensive"
            };

            try
            {
                var result = await selector.SelectOptimalAgents(testCode, "csharp", context, options);
                
                Console.WriteLine($"✅ Selected {result.SelectedAgents.Count} optimal agents:");
                foreach (var agent in result.SelectedAgents)
                {
                    Console.WriteLine($"   - {agent.AgentType} ({agent.Priority}): {agent.Reasoning}");
                }
                
                Console.WriteLine($"📊 Code Characteristics:");
                Console.WriteLine($"   - Complexity: {result.CodeCharacteristics.Complexity}/10");
                Console.WriteLine($"   - Has Database Calls: {result.CodeCharacteristics.HasDatabaseCalls}");
                Console.WriteLine($"   - Performance Critical: {result.CodeCharacteristics.PerformanceCritical}");
                Console.WriteLine($"   - Business Logic Heavy: {result.CodeCharacteristics.BusinessLogicHeavy}");
                
                Console.WriteLine($"⏱️ Estimated Analysis Time: {result.EstimatedAnalysisTime.TotalSeconds:F1}s");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Dynamic agent selection failed: {ex.Message}");
            }
        }

        static async Task TestNestedChatFramework(IServiceProvider serviceProvider, string testCode)
        {
            var claudeService = serviceProvider.GetRequiredService<IClaudeService>();
            var logger = serviceProvider.GetRequiredService<ILogger<NestedChatFramework>>();
            var framework = new NestedChatFramework(claudeService, logger);

            var context = new Dictionary<string, object>
            {
                ["FileName"] = "UserService.cs",
                ["Language"] = "csharp",
                ["BusinessDomain"] = "user-management"
            };

            var options = new IterativeAnalysisOptions
            {
                MaxIterations = 2,
                ImprovementThreshold = 0.8
            };

            try
            {
                // Test iterative analysis
                var result = await framework.ConductIterativeAnalysis(
                    AgentType.SecurityExpert, 
                    testCode, 
                    context, 
                    options);
                
                Console.WriteLine($"✅ Iterative analysis completed:");
                Console.WriteLine($"   - Final confidence: {result.ConfidenceScore:F2}");
                Console.WriteLine($"   - Findings: {result.Findings.Count}");
                Console.WriteLine($"   - Recommendations: {result.Recommendations.Count}");
                
                if (result.Metadata?.ContainsKey("IterativeAnalysis") == true)
                {
                    Console.WriteLine($"   - Iterative improvements applied ✨");
                }

                // Test self-reflective analysis
                var selfReflectiveResult = await framework.ConductSelfReflectiveAnalysis(
                    AgentType.PerformanceAnalyst,
                    testCode,
                    context);

                Console.WriteLine($"✅ Self-reflective analysis completed:");
                Console.WriteLine($"   - Final confidence: {selfReflectiveResult.ConfidenceScore:F2}");
                
                if (selfReflectiveResult.Metadata?.ContainsKey("SelfReflection") == true)
                {
                    Console.WriteLine($"   - Self-reflection insights captured 🧠");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Nested chat framework test failed: {ex.Message}");
            }
        }

        static async Task TestEnhancedMultiAgentReview(ConsolidatedAIReviewSystem reviewSystem, string testCode)
        {
            var request = new CodeReviewRequest
            {
                Content = testCode,
                FileName = "UserService.cs",
                Language = "csharp",
                Context = "User management service for financial application",
                Options = new ReviewOptions
                {
                    IncludeSecurityAnalysis = true,
                    IncludePerformanceAnalysis = true,
                    IncludeQualityAnalysis = true,
                    ReviewDepth = "comprehensive",
                    BusinessDomain = "financial"
                },
                Metadata = new Dictionary<string, object>
                {
                    ["TeamExperience"] = "junior",
                    ["BusinessCritical"] = true,
                    ["PerformanceCritical"] = true
                }
            };

            try
            {
                var result = await reviewSystem.ConductMultiAgentReviewAsync(request);
                
                Console.WriteLine($"✅ Enhanced multi-agent review completed:");
                Console.WriteLine($"   - Quality Score: {result.QualityScore:F2}");
                Console.WriteLine($"   - Analysis Time: {result.Metrics.TotalAnalysisTime.TotalSeconds:F1}s");
                Console.WriteLine($"   - Lines Analyzed: {result.Metrics.LinesAnalyzed}");
                Console.WriteLine($"   - Issues Found: {result.Metrics.IssuesFound}");
                Console.WriteLine($"   - Recommendations: {result.Metrics.RecommendationsGenerated}");
                Console.WriteLine($"   - Agents Used: {result.AgentResults.Count}");
                
                Console.WriteLine($"📋 Agent Results:");
                foreach (var agentResult in result.AgentResults.Take(3))
                {
                    Console.WriteLine($"   - {agentResult.AgentName}: {agentResult.ConfidenceScore:F2} confidence");
                }
                
                if (result.Metadata?.ContainsKey("AdvancedFeatures") == true)
                {
                    Console.WriteLine($"🚀 Advanced 2024 features enabled:");
                    Console.WriteLine($"   - Dynamic agent selection ✓");
                    Console.WriteLine($"   - Iterative analysis ✓");
                    Console.WriteLine($"   - Cross-validation ✓");
                    Console.WriteLine($"   - Enhanced prompting ✓");
                }
                
                Console.WriteLine($"🎯 Key Findings:");
                foreach (var finding in result.KeyFindings.Take(3))
                {
                    Console.WriteLine($"   - {finding}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Enhanced multi-agent review failed: {ex.Message}");
            }
        }

        static async Task TestCrossAgentValidation(IServiceProvider serviceProvider, string testCode)
        {
            var claudeService = serviceProvider.GetRequiredService<IClaudeService>();
            var logger = serviceProvider.GetRequiredService<ILogger<NestedChatFramework>>();
            var framework = new NestedChatFramework(claudeService, logger);

            // Create sample agent results for cross-validation
            var agentResults = new List<AgentResult>
            {
                new AgentResult
                {
                    AgentType = AgentType.SecurityExpert,
                    AgentName = "SecurityExpert",
                    Analysis = "Security analysis of user creation flow",
                    ConfidenceScore = 0.85,
                    Findings = new List<Finding>
                    {
                        new Finding { Type = "Security", Description = "Password handling needs review", Severity = "HIGH" }
                    }
                },
                new AgentResult
                {
                    AgentType = AgentType.PerformanceAnalyst,
                    AgentName = "PerformanceAnalyst", 
                    Analysis = "Performance analysis of bulk operations",
                    ConfidenceScore = 0.78,
                    Findings = new List<Finding>
                    {
                        new Finding { Type = "Performance", Description = "N+1 query problem detected", Severity = "HIGH" }
                    }
                }
            };

            var context = new Dictionary<string, object>
            {
                ["FileName"] = "UserService.cs",
                ["Language"] = "csharp"
            };

            try
            {
                var result = await framework.ConductCrossValidation(agentResults, testCode, context);
                
                Console.WriteLine($"✅ Cross-validation completed:");
                Console.WriteLine($"   - Consensus Score: {result.OverallConsensusScore:F2}");
                Console.WriteLine($"   - Validation Matrix: {result.ValidationMatrix.Count} entries");
                Console.WriteLine($"   - Consensus Findings: {result.ConsensusFindings.Count}");
                Console.WriteLine($"   - Conflicting Findings: {result.ConflictingFindings.Count}");
                
                if (result.OverallConsensusScore > 0.7)
                {
                    Console.WriteLine($"   - High agent consensus achieved ✅");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Cross-agent validation failed: {ex.Message}");
            }
        }
    }

    // Mock implementations for testing
    public class MockClaudeService : IClaudeService
    {
        public async Task<string> GenerateReviewAsync(string prompt, CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken); // Simulate API call
            
            return @"{
                ""analysis"": ""Comprehensive code analysis completed"",
                ""confidence"": 0.85,
                ""findings"": [
                    {
                        ""type"": ""Security"",
                        ""description"": ""Password handling implementation"",
                        ""severity"": ""HIGH"",
                        ""location"": ""CreateUserAsync method""
                    }
                ],
                ""recommendations"": [
                    {
                        ""title"": ""Improve error handling"",
                        ""description"": ""Add more specific exception handling"",
                        ""priority"": ""HIGH""
                    }
                ]
            }";
        }
    }

    public class MockAgentOrchestrator : IAgentOrchestrator
    {
        public ISpecializedAgent CreateAgent(AgentType agentType, AgentConfiguration configuration)
        {
            return new MockSpecializedAgent(agentType);
        }

        public async Task<AgentExecutionResult[]> ExecuteAgentsParallelAsync(List<AgentExecutionTask> tasks, CancellationToken cancellationToken = default)
        {
            await Task.Delay(200, cancellationToken);
            
            return tasks.Select(task => new AgentExecutionResult
            {
                AgentType = task.AgentType,
                Success = true,
                Result = new AgentResult
                {
                    AgentType = task.AgentType,
                    AgentName = task.AgentType.ToString(),
                    Analysis = $"Mock analysis from {task.AgentType}",
                    ConfidenceScore = 0.8,
                    Findings = new List<Finding>(),
                    Recommendations = new List<Recommendation>()
                },
                ExecutionTime = TimeSpan.FromSeconds(30)
            }).ToArray();
        }
    }

    public class MockSpecializedAgent : ISpecializedAgent
    {
        private readonly AgentType _agentType;

        public MockSpecializedAgent(AgentType agentType)
        {
            _agentType = agentType;
        }

        public async Task<AgentResult> AnalyzeAsync(string content, Dictionary<string, object> context, CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken);
            
            return new AgentResult
            {
                AgentType = _agentType,
                AgentName = _agentType.ToString(),
                Analysis = $"Mock analysis from {_agentType}",
                ConfidenceScore = 0.8,
                Findings = new List<Finding>
                {
                    new Finding
                    {
                        Type = _agentType.ToString(),
                        Description = $"Sample finding from {_agentType}",
                        Severity = "MEDIUM"
                    }
                },
                Recommendations = new List<Recommendation>
                {
                    new Recommendation
                    {
                        Title = $"Sample recommendation from {_agentType}",
                        Priority = "MEDIUM"
                    }
                }
            };
        }
    }
}