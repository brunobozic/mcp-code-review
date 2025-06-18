using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.AI
{
    /// <summary>
    /// Specialized agent for enforcing enterprise architecture patterns and coding standards
    /// Intelligently adapts requirements based on project maturity and existing patterns
    /// </summary>
    public class ArchitectureStandardsAgent
    {
        private readonly ILogger<ArchitectureStandardsAgent> _logger;
        private readonly IClaudeService _claudeService;

        public ArchitectureStandardsAgent(
            ILogger<ArchitectureStandardsAgent> logger,
            IClaudeService claudeService)
        {
            _logger = logger;
            _claudeService = claudeService;
        }

        /// <summary>
        /// Analyzes code for enterprise architecture patterns and standards compliance
        /// </summary>
        public async Task<ArchitectureAnalysisResult> AnalyzeArchitectureAsync(ArchitectureCodeReviewRequest request)
        {
            _logger.LogInformation("Starting architecture standards analysis for {ProjectId}", request.ProjectId);

            var result = new ArchitectureAnalysisResult
            {
                ProjectId = request.ProjectId,
                MergeRequestIid = request.MergeRequestIid,
                AnalysisTimestamp = DateTimeOffset.UtcNow
            };

            try
            {
                // Step 1: Assess project maturity and existing patterns
                var maturityAssessment = await AssessProjectMaturityAsync(request);
                result.ProjectMaturity = maturityAssessment;

                // Step 2: Analyze based on project type and maturity
                if (maturityAssessment.IsLegacyProject)
                {
                    result.Recommendations = await AnalyzeLegacyProjectAsync(request, maturityAssessment);
                }
                else if (maturityAssessment.IsGreenfield || maturityAssessment.HasModernPatterns)
                {
                    result.Requirements = await AnalyzeModernProjectAsync(request, maturityAssessment);
                }
                else
                {
                    result.GradualMigrationPlan = await CreateMigrationPlanAsync(request, maturityAssessment);
                }

                // Step 3: Apply context-aware standards
                await ApplyContextAwareStandardsAsync(request, result);

                result.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during architecture analysis");
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Assesses project maturity and existing architectural patterns
        /// </summary>
        private async Task<ProjectMaturityAssessment> AssessProjectMaturityAsync(ArchitectureCodeReviewRequest request)
        {
            var assessment = new ProjectMaturityAssessment();
            var allCode = string.Join("\n", request.Changes.Select(c => c.NewContent ?? c.OldContent ?? ""));

            // Check for existing modern patterns
            assessment.HasServicePattern = HasServicePattern(allCode);
            assessment.HasRepositoryPattern = HasRepositoryPattern(allCode);
            assessment.HasUnitOfWork = HasUnitOfWorkPattern(allCode);
            assessment.HasRequestResponsePattern = HasRequestResponsePattern(allCode);
            assessment.HasMinimalApis = HasMinimalApis(allCode);
            assessment.HasStructuredLogging = HasStructuredLogging(allCode);
            assessment.HasOpenApiAnnotations = HasOpenApiAnnotations(allCode);
            assessment.HasRegionOrganization = HasRegionOrganization(allCode);
            assessment.HasXmlDocumentation = HasXmlDocumentation(allCode);
            assessment.HasHateoas = HasHateoasPattern(allCode);
            assessment.HasFeatureSlices = HasFeatureSlices(request);

            // Determine project type
            assessment.IsLegacyProject = DetermineLegacyProject(assessment);
            assessment.IsGreenfield = DetermineGreenfieldProject(request);
            assessment.HasModernPatterns = HasAnyModernPatterns(assessment);

            // Calculate architecture maturity score
            assessment.ArchitectureMaturityScore = CalculateMaturityScore(assessment);

            return assessment;
        }

        /// <summary>
        /// Analyzes legacy projects with gentle recommendations
        /// </summary>
        private async Task<List<ArchitectureRecommendation>> AnalyzeLegacyProjectAsync(
            ArchitectureCodeReviewRequest request, ProjectMaturityAssessment maturity)
        {
            var prompt = CreateLegacyProjectPrompt(request, maturity);
            var response = await _claudeService.GenerateReviewAsync(prompt);

            return ParseRecommendations(response, RecommendationType.Gentle);
        }

        /// <summary>
        /// Analyzes modern projects with strict requirements
        /// </summary>
        private async Task<List<ArchitectureRequirement>> AnalyzeModernProjectAsync(
            ArchitectureCodeReviewRequest request, ProjectMaturityAssessment maturity)
        {
            var prompt = CreateModernProjectPrompt(request, maturity);
            var response = await _claudeService.GenerateReviewAsync(prompt);

            return ParseRequirements(response);
        }

        /// <summary>
        /// Creates gradual migration plan for transitional projects
        /// </summary>
        private async Task<GradualMigrationPlan> CreateMigrationPlanAsync(
            ArchitectureCodeReviewRequest request, ProjectMaturityAssessment maturity)
        {
            var prompt = CreateMigrationPrompt(request, maturity);
            var response = await _claudeService.GenerateReviewAsync(prompt);

            return ParseMigrationPlan(response);
        }

        /// <summary>
        /// Applies context-aware coding standards based on project state
        /// </summary>
        private async Task ApplyContextAwareStandardsAsync(ArchitectureCodeReviewRequest request, ArchitectureAnalysisResult result)
        {
            var standards = new List<StandardsViolation>();

            foreach (var change in request.Changes)
            {
                if (string.IsNullOrEmpty(change.NewContent)) continue;

                // Always apply these standards (regardless of project maturity)
                await CheckUniversalStandards(change, standards, result.ProjectMaturity);

                // Apply progressive standards based on project maturity
                if (result.ProjectMaturity.ArchitectureMaturityScore > 0.3)
                {
                    await CheckIntermediateStandards(change, standards, result.ProjectMaturity);
                }

                if (result.ProjectMaturity.ArchitectureMaturityScore > 0.7)
                {
                    await CheckAdvancedStandards(change, standards, result.ProjectMaturity);
                }
            }

            result.StandardsViolations = standards;
        }

        #region Pattern Detection Methods

        private bool HasServicePattern(string code)
        {
            return Regex.IsMatch(code, @"class\s+\w+Service\s*:\s*I\w+Service") ||
                   Regex.IsMatch(code, @"class\s+\w+Service\s*:\s*BaseService") ||
                   code.Contains("IServiceCollection") && code.Contains("AddScoped");
        }

        private bool HasRepositoryPattern(string code)
        {
            return Regex.IsMatch(code, @"interface\s+I\w+Repository") ||
                   Regex.IsMatch(code, @"class\s+\w+Repository\s*:\s*I\w+Repository") ||
                   Regex.IsMatch(code, @"class\s+\w+Repository\s*:\s*BaseRepository");
        }

        private bool HasUnitOfWorkPattern(string code)
        {
            return code.Contains("IUnitOfWork") ||
                   Regex.IsMatch(code, @"class\s+UnitOfWork\s*:\s*IUnitOfWork");
        }

        private bool HasRequestResponsePattern(string code)
        {
            return Regex.IsMatch(code, @"class\s+\w+Request\s*:\s*BaseRequest") ||
                   Regex.IsMatch(code, @"class\s+\w+Response\s*:\s*BaseResponse") ||
                   code.Contains("PagedRequest") || code.Contains("PagedResponse");
        }

        private bool HasMinimalApis(string code)
        {
            return code.Contains("app.MapGet") || code.Contains("app.MapPost") ||
                   code.Contains("app.MapPut") || code.Contains("app.MapDelete") ||
                   code.Contains("WebApplication.CreateBuilder");
        }

        private bool HasStructuredLogging(string code)
        {
            return code.Contains("ILogger") && (
                code.Contains("LogInformation") || code.Contains("LogError") ||
                code.Contains("LogWarning") || code.Contains("Serilog")) ||
                code.Contains("OpenTelemetry") || code.Contains("CorrelationId");
        }

        private bool HasOpenApiAnnotations(string code)
        {
            return code.Contains("[ProducesResponseType") ||
                   code.Contains("[SwaggerOperation") ||
                   code.Contains("OpenApiOperation") ||
                   code.Contains("[Tags(");
        }

        private bool HasRegionOrganization(string code)
        {
            return code.Contains("#region") && (
                code.Contains("#region Constructor") ||
                code.Contains("#region Public") ||
                code.Contains("#region Private"));
        }

        private bool HasXmlDocumentation(string code)
        {
            return Regex.IsMatch(code, @"///\s*<summary>") &&
                   Regex.IsMatch(code, @"///\s*<param\s+name=");
        }

        private bool HasHateoasPattern(string code)
        {
            return code.Contains("_links") || code.Contains("HateoasResponse") ||
                   code.Contains("LinkGenerator") || code.Contains("IUrlHelper");
        }

        private bool HasFeatureSlices(ArchitectureCodeReviewRequest request)
        {
            var filePaths = request.Changes.Select(c => c.FilePath).ToList();
            return filePaths.Any(path => 
                path.Contains("/Features/") || 
                path.Contains("/Slices/") ||
                (path.Contains("/Commands/") && path.Contains("/Queries/")));
        }

        #endregion

        #region Standards Checking Methods

        private async Task CheckUniversalStandards(FileChange change, List<StandardsViolation> violations, ProjectMaturityAssessment maturity)
        {
            var code = change.NewContent ?? "";
            var fileName = change.FilePath ?? "";

            // 1. Basic XML Documentation (always required for public APIs)
            if (IsPublicApiFile(fileName) && !HasAdequateXmlDocs(code))
            {
                violations.Add(new StandardsViolation
                {
                    Type = ViolationType.Documentation,
                    Severity = maturity.IsLegacyProject ? "medium" : "high",
                    File = fileName,
                    Message = "Public APIs should have comprehensive XML documentation",
                    Suggestion = "Add XML comments with <summary>, <param>, and <returns> tags"
                });
            }

            // 2. Error Handling in Development
            if (IsLocalOrDevelopmentCode(code) && HasPoorErrorHandling(code))
            {
                violations.Add(new StandardsViolation
                {
                    Type = ViolationType.ErrorHandling,
                    Severity = "medium",
                    File = fileName,
                    Message = "Development environments should include full error details with stack traces",
                    Suggestion = "Add detailed exception handling with inner exceptions and stack traces for development"
                });
            }

            // 3. Basic Logging Structure
            if (HasLoggingUsage(code) && !HasStructuredLoggingUsage(code))
            {
                violations.Add(new StandardsViolation
                {
                    Type = ViolationType.Logging,
                    Severity = maturity.IsLegacyProject ? "low" : "medium",
                    File = fileName,
                    Message = "Consider structured logging with Serilog and correlation IDs",
                    Suggestion = maturity.IsLegacyProject 
                        ? "Future enhancement: Migrate to structured logging when modernizing"
                        : "Implement structured logging with correlation IDs for better observability"
                });
            }
        }

        private async Task CheckIntermediateStandards(FileChange change, List<StandardsViolation> violations, ProjectMaturityAssessment maturity)
        {
            var code = change.NewContent ?? "";
            var fileName = change.FilePath ?? "";

            // 4. Region Organization
            if (IsClassFile(fileName) && !HasProperRegions(code))
            {
                violations.Add(new StandardsViolation
                {
                    Type = ViolationType.Organization,
                    Severity = "medium",
                    File = fileName,
                    Message = "Classes should be organized with regions (Constructor, Public Methods, Private Methods)",
                    Suggestion = "Add #region directives to organize code sections"
                });
            }

            // 5. Service Pattern Usage
            if (IsServiceFile(fileName) && !FollowsServicePattern(code))
            {
                violations.Add(new StandardsViolation
                {
                    Type = ViolationType.Architecture,
                    Severity = "high",
                    File = fileName,
                    Message = "Services should follow the service pattern with interface and base class",
                    Suggestion = "Implement IServiceName interface and inherit from BaseService if available"
                });
            }

            // 6. Repository Pattern
            if (IsRepositoryFile(fileName) && !FollowsRepositoryPattern(code))
            {
                violations.Add(new StandardsViolation
                {
                    Type = ViolationType.Architecture,
                    Severity = "high",
                    File = fileName,
                    Message = "Repositories should implement generic repository pattern with Unit of Work",
                    Suggestion = "Use IRepository<T> interface and UnitOfWork pattern"
                });
            }
        }

        private async Task CheckAdvancedStandards(FileChange change, List<StandardsViolation> violations, ProjectMaturityAssessment maturity)
        {
            var code = change.NewContent ?? "";
            var fileName = change.FilePath ?? "";

            // 7. Request/Response Pattern
            if (IsApiFile(fileName) && !UsesRequestResponsePattern(code))
            {
                violations.Add(new StandardsViolation
                {
                    Type = ViolationType.Architecture,
                    Severity = "high",
                    File = fileName,
                    Message = "APIs should use Request/Response pattern with BaseRequest/BaseResponse",
                    Suggestion = "Implement typed request/response objects with pagination and HATEOAS support"
                });
            }

            // 8. Minimal APIs with OpenAPI
            if (IsApiFile(fileName) && !HasDetailedOpenApiAnnotations(code))
            {
                violations.Add(new StandardsViolation
                {
                    Type = ViolationType.Documentation,
                    Severity = "high",
                    File = fileName,
                    Message = "Minimal APIs require detailed OpenAPI annotations",
                    Suggestion = "Add [ProducesResponseType], [SwaggerOperation], and [Tags] attributes"
                });
            }

            // 9. Feature Slices Architecture
            if (IsFeatureFile(fileName) && !FollowsFeatureSlicePattern(fileName, code))
            {
                violations.Add(new StandardsViolation
                {
                    Type = ViolationType.Architecture,
                    Severity = "medium",
                    File = fileName,
                    Message = "Consider organizing code using Feature Slices pattern",
                    Suggestion = "Group related commands, queries, and handlers in feature-based folders"
                });
            }

            // 10. HATEOAS Implementation
            if (IsApiResponseFile(fileName) && !ImplementsHateoas(code))
            {
                violations.Add(new StandardsViolation
                {
                    Type = ViolationType.Architecture,
                    Severity = "medium",
                    File = fileName,
                    Message = "API responses should include HATEOAS links for better REST compliance",
                    Suggestion = "Add _links property with navigation links to related resources"
                });
            }
        }

        #endregion

        #region Helper Methods

        private bool DetermineLegacyProject(ProjectMaturityAssessment assessment)
        {
            // Project is legacy if it has very few modern patterns
            var modernPatternCount = new[]
            {
                assessment.HasServicePattern,
                assessment.HasRepositoryPattern,
                assessment.HasUnitOfWork,
                assessment.HasRequestResponsePattern,
                assessment.HasMinimalApis,
                assessment.HasStructuredLogging
            }.Count(x => x);

            return modernPatternCount <= 1;
        }

        private bool DetermineGreenfieldProject(ArchitectureCodeReviewRequest request)
        {
            // New project if few files and modern naming conventions
            return request.Changes.Count <= 5 && 
                   request.Changes.Any(c => c.FilePath?.Contains("Program.cs") == true);
        }

        private bool HasAnyModernPatterns(ProjectMaturityAssessment assessment)
        {
            return assessment.HasServicePattern || assessment.HasRepositoryPattern ||
                   assessment.HasMinimalApis || assessment.HasStructuredLogging;
        }

        private double CalculateMaturityScore(ProjectMaturityAssessment assessment)
        {
            var patterns = new[]
            {
                assessment.HasServicePattern,
                assessment.HasRepositoryPattern,
                assessment.HasUnitOfWork,
                assessment.HasRequestResponsePattern,
                assessment.HasMinimalApis,
                assessment.HasStructuredLogging,
                assessment.HasOpenApiAnnotations,
                assessment.HasRegionOrganization,
                assessment.HasXmlDocumentation,
                assessment.HasHateoas,
                assessment.HasFeatureSlices
            };

            return (double)patterns.Count(x => x) / patterns.Length;
        }

        // File type detection helpers
        private bool IsPublicApiFile(string fileName) => 
            fileName.Contains("Controller") || fileName.Contains("Endpoint") || fileName.Contains("Api");
        
        private bool IsServiceFile(string fileName) => 
            fileName.Contains("Service") && !fileName.Contains("Test");
        
        private bool IsRepositoryFile(string fileName) => 
            fileName.Contains("Repository") && !fileName.Contains("Test");
        
        private bool IsApiFile(string fileName) => 
            fileName.Contains("Controller") || fileName.Contains("Endpoint") || fileName.Contains("Api");
        
        private bool IsClassFile(string fileName) => 
            fileName.EndsWith(".cs") && !fileName.Contains("Interface");
        
        private bool IsFeatureFile(string fileName) => 
            fileName.Contains("/Features/") || fileName.Contains("/Commands/") || fileName.Contains("/Queries/");
        
        private bool IsApiResponseFile(string fileName) => 
            fileName.Contains("Response") || fileName.Contains("Result");

        // Code pattern detection helpers
        private bool HasAdequateXmlDocs(string code) =>
            Regex.Matches(code, @"///\s*<summary>").Count >= Regex.Matches(code, @"public\s+\w+").Count / 2;

        private bool IsLocalOrDevelopmentCode(string code) =>
            code.Contains("IsDevelopment") || code.Contains("env.IsDevelopment") || code.Contains("ASPNETCORE_ENVIRONMENT");

        private bool HasPoorErrorHandling(string code) =>
            code.Contains("try") && !code.Contains("InnerException") && !code.Contains("StackTrace");

        private bool HasLoggingUsage(string code) =>
            code.Contains("ILogger") || code.Contains("Log.");

        private bool HasStructuredLoggingUsage(string code) =>
            code.Contains("LogInformation(") || code.Contains("LogError(") || code.Contains("Serilog");

        private bool HasProperRegions(string code) =>
            code.Contains("#region Constructor") || code.Contains("#region Public") || code.Contains("#region Private");

        private bool FollowsServicePattern(string code) =>
            Regex.IsMatch(code, @"class\s+\w+Service\s*:\s*I\w+Service");

        private bool FollowsRepositoryPattern(string code) =>
            code.Contains("IRepository") || code.Contains("BaseRepository");

        private bool UsesRequestResponsePattern(string code) =>
            code.Contains("Request") && code.Contains("Response") && (code.Contains("BaseRequest") || code.Contains("BaseResponse"));

        private bool HasDetailedOpenApiAnnotations(string code) =>
            code.Contains("[ProducesResponseType") && code.Contains("[SwaggerOperation");

        private bool FollowsFeatureSlicePattern(string fileName, string code) =>
            fileName.Contains("/Features/") && (code.Contains("Handler") || code.Contains("Command") || code.Contains("Query"));

        private bool ImplementsHateoas(string code) =>
            code.Contains("_links") || code.Contains("HateoasResponse");

        #endregion

        #region Prompt Creation Methods

        private string CreateLegacyProjectPrompt(ArchitectureCodeReviewRequest request, ProjectMaturityAssessment maturity)
        {
            return $@"
You are analyzing a LEGACY .NET project that lacks modern architectural patterns. 
Your role is to provide GENTLE, NON-BLOCKING recommendations that won't fail the PR/MR.

Project Context:
- Architecture Maturity Score: {maturity.ArchitectureMaturityScore:F2}
- Has Service Pattern: {maturity.HasServicePattern}
- Has Repository Pattern: {maturity.HasRepositoryPattern}
- Has Modern Logging: {maturity.HasStructuredLogging}

IMPORTANT GUIDELINES:
1. DO NOT require implementation of missing infrastructure
2. Provide suggestions as ""future enhancements""
3. Focus on incremental improvements
4. Acknowledge existing code style and patterns
5. Only flag genuine code quality issues, not architectural gaps

Code Changes:
{string.Join("\n", request.Changes.Select(c => $"File: {c.FilePath}\n{c.NewContent}"))}

Provide gentle recommendations that respect the existing codebase maturity.
";
        }

        private string CreateModernProjectPrompt(ArchitectureCodeReviewRequest request, ProjectMaturityAssessment maturity)
        {
            return $@"
You are analyzing a MODERN .NET project that should follow enterprise architectural patterns.
Apply strict standards as this project has the infrastructure to support them.

Project Context:
- Architecture Maturity Score: {maturity.ArchitectureMaturityScore:F2}
- Has Modern Patterns: {maturity.HasModernPatterns}
- Project Type: Greenfield or Modern

REQUIRED STANDARDS:
1. Service Pattern with BaseService and IService interfaces
2. Generic Repository Pattern with Unit of Work
3. Request/Response pattern with BaseRequest/BaseResponse
4. Feature Slices architecture
5. Comprehensive XML documentation
6. Structured logging with correlation IDs
7. Minimal APIs with detailed OpenAPI annotations
8. Code organization with regions
9. HATEOAS implementation for APIs
10. Full error details in development environments

Code Changes:
{string.Join("\n", request.Changes.Select(c => $"File: {c.FilePath}\n{c.NewContent}"))}

Enforce high standards and flag violations that should block the PR/MR if not addressed.
";
        }

        private string CreateMigrationPrompt(ArchitectureCodeReviewRequest request, ProjectMaturityAssessment maturity)
        {
            return $@"
You are analyzing a project IN TRANSITION between legacy and modern patterns.
Create a gradual migration plan that improves the codebase without breaking existing functionality.

Current Patterns Found:
- Service Pattern: {maturity.HasServicePattern}
- Repository Pattern: {maturity.HasRepositoryPattern}
- Unit of Work: {maturity.HasUnitOfWork}
- Request/Response: {maturity.HasRequestResponsePattern}
- Structured Logging: {maturity.HasStructuredLogging}

Create a migration strategy that:
1. Builds on existing patterns
2. Introduces new patterns gradually
3. Provides clear migration steps
4. Identifies quick wins vs long-term goals

Code Changes:
{string.Join("\n", request.Changes.Select(c => $"File: {c.FilePath}\n{c.NewContent}"))}

Focus on practical migration steps that can be implemented incrementally.
";
        }

        #endregion

        #region Response Parsing Methods

        private List<ArchitectureRecommendation> ParseRecommendations(string response, RecommendationType type)
        {
            // Parse AI response into structured recommendations
            // Implementation depends on response format
            return new List<ArchitectureRecommendation>();
        }

        private List<ArchitectureRequirement> ParseRequirements(string response)
        {
            // Parse AI response into structured requirements
            return new List<ArchitectureRequirement>();
        }

        private GradualMigrationPlan ParseMigrationPlan(string response)
        {
            // Parse AI response into migration plan
            return new GradualMigrationPlan();
        }

        #endregion
    }
}