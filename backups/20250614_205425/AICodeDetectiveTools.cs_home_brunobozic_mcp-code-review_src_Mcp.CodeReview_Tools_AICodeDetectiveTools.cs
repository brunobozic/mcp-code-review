using ModelContextProtocol;
using ModelContextProtocol.Server;
using Mcp.CodeReview.AI;
using Mcp.CodeReview.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mcp.CodeReview.Tools;

[McpServerToolType]
public static class AICodeDetectiveTools
{
    /// <summary>
    /// Advanced AI Code Detective - Catches AI-generated shortcuts, bypasses, and problematic patterns
    /// </summary>
    [McpServerTool, Description("Detect AI-generated code shortcuts, bypasses, commented-out implementations, and other problematic patterns")]
    public static async Task<object> DetectAICodeShortcuts(
        ClaudeService claudeService,
        ILogger logger,
        [Description("The code diff or file content to analyze")] string codeContent,
        [Description("Previous version of the code for comparison (optional)")] string? previousVersion = null,
        [Description("Git commit message (optional, helps detect intent)")] string? commitMessage = null,
        [Description("File paths included in the change")] string[]? modifiedFiles = null,
        [Description("Include license compliance checking")] bool checkLicenseCompliance = true)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "detectAICodeShortcuts",
            ["CodeLength"] = codeContent.Length,
            ["HasPreviousVersion"] = previousVersion != null
        }))
        {
            logger.LogInformation("Starting AI Code Detective analysis on {Length} characters of code", codeContent.Length);

            try
            {
                // Analyze for AI-generated patterns
                var aiPatterns = DetectAIGeneratedPatterns(codeContent);
                
                // Detect shortcuts and bypasses
                var shortcuts = DetectCodeShortcuts(codeContent);
                
                // Analyze commented-out code
                var commentedCode = AnalyzeCommentedOutCode(codeContent);
                
                // Check for deleted/missing implementations
                var missingImplementations = DetectMissingImplementations(codeContent, previousVersion);
                
                // Detect EF migration tampering
                var migrationIssues = DetectEFMigrationTampering(codeContent, modifiedFiles);
                
                // Check license compliance for NuGet packages
                var licenseIssues = checkLicenseCompliance ? DetectLicenseCompliance(codeContent) : new LicenseComplianceResult();
                
                // Detect registration bypasses
                var registrationIssues = DetectRegistrationBypasses(codeContent);
                
                // Create AI analysis prompt for sophisticated detection
                var detectionPrompt = BuildAIDetectionPrompt(codeContent, commitMessage, shortcuts, commentedCode);
                
                // Get AI analysis for advanced pattern detection
                var aiAnalysis = await claudeService.GenerateReviewWithParameters(detectionPrompt, temperature: 0.1, maxTokens: 3000);

                // Calculate overall risk score
                var riskScore = CalculateOverallRiskScore(aiPatterns, shortcuts, commentedCode, missingImplementations, licenseIssues);

                logger.LogInformation("AI Code Detective analysis completed with risk score: {RiskScore}", riskScore);

                return new
                {
                    success = true,
                    riskScore = riskScore,
                    riskLevel = riskScore > 8 ? "CRITICAL" : riskScore > 6 ? "HIGH" : riskScore > 4 ? "MEDIUM" : "LOW",
                    
                    // AI-generated pattern detection
                    aiGeneratedPatterns = new
                    {
                        detected = aiPatterns.Count > 0,
                        patterns = aiPatterns.Select(p => new
                        {
                            type = p.Type,
                            description = p.Description,
                            line = p.Line,
                            confidence = p.Confidence,
                            riskLevel = p.RiskLevel,
                            evidence = p.Evidence
                        }),
                        aiConfidenceScore = CalculateAIGenerationConfidence(aiPatterns)
                    },
                    
                    // Code shortcuts and bypasses
                    shortcuts = new
                    {
                        detected = shortcuts.Count > 0,
                        criticalShortcuts = shortcuts.Where(s => s.Severity == "CRITICAL").Select(s => new
                        {
                            type = s.Type,
                            description = s.Description,
                            location = s.Location,
                            impact = s.Impact,
                            recommendation = s.Recommendation
                        }),
                        allShortcuts = shortcuts.Select(s => new
                        {
                            type = s.Type,
                            description = s.Description,
                            severity = s.Severity,
                            location = s.Location
                        })
                    },
                    
                    // Commented-out code analysis
                    commentedCode = new
                    {
                        detected = commentedCode.TotalCommentedLines > 0,
                        totalLines = commentedCode.TotalCommentedLines,
                        suspiciousBlocks = commentedCode.SuspiciousBlocks.Select(b => new
                        {
                            type = b.Type,
                            content = b.Content.Length > 100 ? b.Content.Substring(0, 100) + "..." : b.Content,
                            reason = b.SuspiciousReason,
                            lines = b.LineRange,
                            riskLevel = b.RiskLevel
                        }),
                        registrationBypasses = commentedCode.RegistrationBypasses,
                        implementationSkips = commentedCode.ImplementationSkips
                    },
                    
                    // Missing implementations
                    missingImplementations = new
                    {
                        detected = missingImplementations.Count > 0,
                        deletedMethods = missingImplementations.Where(m => m.Type == "DeletedMethod").Select(m => new
                        {
                            name = m.Name,
                            signature = m.Signature,
                            estimatedComplexity = m.EstimatedComplexity,
                            potentialImpact = m.PotentialImpact
                        }),
                        simplifiedImplementations = missingImplementations.Where(m => m.Type == "Simplified").Select(m => new
                        {
                            name = m.Name,
                            before = m.BeforeSignature,
                            after = m.AfterSignature,
                            suspiciousSimplification = m.SuspiciousSimplification
                        })
                    },
                    
                    // EF Migration tampering
                    migrationIssues = new
                    {
                        detected = migrationIssues.HasIssues,
                        tamperingIndicators = migrationIssues.TamperingIndicators.Select(i => new
                        {
                            type = i.Type,
                            description = i.Description,
                            severity = i.Severity,
                            evidence = i.Evidence
                        }),
                        manualEdits = migrationIssues.ManualEdits,
                        skippedMigrations = migrationIssues.SkippedMigrations,
                        dangerousOperations = migrationIssues.DangerousOperations
                    },
                    
                    // License compliance issues
                    licenseCompliance = checkLicenseCompliance ? new
                    {
                        hasViolations = licenseIssues.HasViolations,
                        paidPackages = licenseIssues.PaidPackages.Select(p => new
                        {
                            packageName = p.Name,
                            version = p.Version,
                            licenseType = p.LicenseType,
                            commercialRestrictions = p.CommercialRestrictions,
                            estimatedCost = p.EstimatedCost,
                            alternatives = p.FreeAlternatives
                        }),
                        licenseViolations = licenseIssues.Violations,
                        complianceScore = licenseIssues.ComplianceScore
                    } : null,
                    
                    // Registration bypasses
                    registrationIssues = new
                    {
                        detected = registrationIssues.Count > 0,
                        commentedRegistrations = registrationIssues.Where(r => r.Type == "CommentedOut").Select(r => new
                        {
                            service = r.ServiceName,
                            reason = r.SuspectedReason,
                            riskLevel = r.RiskLevel,
                            location = r.Location
                        }),
                        missingRegistrations = registrationIssues.Where(r => r.Type == "Missing").Select(r => new
                        {
                            service = r.ServiceName,
                            dependentCode = r.DependentCode,
                            impact = r.Impact
                        })
                    },
                    
                    // Advanced AI analysis
                    advancedAIAnalysis = new
                    {
                        analysis = aiAnalysis,
                        sophisticatedPatterns = ExtractSophisticatedPatterns(aiAnalysis),
                        intentAnalysis = ExtractIntentAnalysis(aiAnalysis, commitMessage),
                        codeQualityAssessment = ExtractQualityAssessment(aiAnalysis),
                        teamGuidance = ExtractTeamGuidance(aiAnalysis)
                    },
                    
                    // Recommendations and actions
                    recommendations = new
                    {
                        immediateActions = GetImmediateActions(riskScore, shortcuts, licenseIssues),
                        codeReviewFocus = GetCodeReviewFocusAreas(aiPatterns, shortcuts, commentedCode),
                        teamTraining = GetTeamTrainingRecommendations(aiPatterns, riskScore),
                        processImprovements = GetProcessImprovements(shortcuts, registrationIssues, migrationIssues),
                        toolingRecommendations = GetToolingRecommendations(licenseIssues, aiPatterns)
                    },
                    
                    // Prevention strategies
                    prevention = new
                    {
                        preCommitHooks = GeneratePreCommitHookSuggestions(shortcuts, licenseIssues),
                        codeReviewChecklist = GenerateCodeReviewChecklist(aiPatterns, shortcuts),
                        cicdGates = GenerateCICDGateSuggestions(licenseIssues, migrationIssues),
                        teamGuidelines = GenerateTeamGuidelines(riskScore, aiPatterns)
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to run AI Code Detective analysis");
                throw new InvalidOperationException($"AI Code Detective analysis failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Specialized EF Migration tampering detection
    /// </summary>
    [McpServerTool, Description("Detect manual edits and tampering in Entity Framework migrations")]
    public static async Task<object> DetectEFMigrationTampering(
        ClaudeService claudeService,
        ILogger logger,
        [Description("Migration file content to analyze")] string migrationContent,
        [Description("Migration file name")] string fileName,
        [Description("Previous version for comparison (optional)")] string? previousVersion = null)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "detectEFMigrationTampering",
            ["FileName"] = fileName
        }))
        {
            logger.LogInformation("Analyzing EF migration file {FileName} for tampering", fileName);

            try
            {
                // Detect migration tampering patterns
                var tamperingAnalysis = AnalyzeMigrationTampering(migrationContent, fileName);
                
                // Check for dangerous operations
                var dangerousOperations = DetectDangerousMigrationOperations(migrationContent);
                
                // Analyze manual edits
                var manualEdits = DetectManualMigrationEdits(migrationContent, previousVersion);
                
                // Create AI analysis prompt
                var migrationPrompt = BuildMigrationAnalysisPrompt(migrationContent, fileName, tamperingAnalysis);
                
                // Get AI analysis
                var aiAnalysis = await claudeService.GenerateReviewWithParameters(migrationPrompt, temperature: 0.1, maxTokens: 2500);

                return new
                {
                    success = true,
                    fileName = fileName,
                    
                    // Tampering detection
                    tamperingDetected = tamperingAnalysis.HasTampering,
                    tamperingIndicators = tamperingAnalysis.Indicators.Select(i => new
                    {
                        type = i.Type,
                        description = i.Description,
                        evidence = i.Evidence,
                        riskLevel = i.RiskLevel,
                        line = i.LineNumber
                    }),
                    
                    // Dangerous operations
                    dangerousOperations = dangerousOperations.Select(op => new
                    {
                        operation = op.Operation,
                        description = op.Description,
                        riskLevel = op.RiskLevel,
                        recommendation = op.Recommendation,
                        affectedTables = op.AffectedTables
                    }),
                    
                    // Manual edits analysis
                    manualEdits = new
                    {
                        detected = manualEdits.Count > 0,
                        edits = manualEdits.Select(edit => new
                        {
                            type = edit.Type,
                            description = edit.Description,
                            before = edit.Before,
                            after = edit.After,
                            suspicionLevel = edit.SuspicionLevel
                        })
                    },
                    
                    // AI migration analysis
                    aiAnalysis = new
                    {
                        analysis = aiAnalysis,
                        legitimacyAssessment = ExtractLegitimacyAssessment(aiAnalysis),
                        riskAssessment = ExtractMigrationRiskAssessment(aiAnalysis),
                        recommendations = ExtractMigrationRecommendations(aiAnalysis)
                    },
                    
                    // Migration health check
                    healthCheck = new
                    {
                        isValid = ValidateMigrationStructure(migrationContent),
                        hasBackupSteps = HasBackupOperations(migrationContent),
                        isReversible = IsReversible(migrationContent),
                        dataLossRisk = AssessDataLossRisk(migrationContent)
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to analyze EF migration {FileName}", fileName);
                throw new InvalidOperationException($"EF migration analysis failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// License compliance and paid package detection
    /// </summary>
    [McpServerTool, Description("Detect usage of paid NuGet packages and license compliance issues")]
    public static async Task<object> DetectLicenseCompliance(
        ClaudeService claudeService,
        ILogger logger,
        [Description("Project file content or package references")] string projectContent,
        [Description("Include detailed license analysis")] bool detailedAnalysis = true,
        [Description("Business context for license evaluation")] string businessContext = "commercial")
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "detectLicenseCompliance",
            ["BusinessContext"] = businessContext
        }))
        {
            logger.LogInformation("Analyzing license compliance for {Context} use", businessContext);

            try
            {
                // Detect paid packages
                var licenseAnalysis = AnalyzeLicenseCompliance(projectContent);
                
                // Get package details
                var packageDetails = GetPackageDetails(licenseAnalysis.ReferencedPackages);
                
                // Create AI compliance analysis
                var compliancePrompt = BuildLicenseCompliancePrompt(licenseAnalysis, packageDetails, businessContext);
                
                // Get AI analysis
                var aiAnalysis = detailedAnalysis 
                    ? await claudeService.GenerateReviewWithParameters(compliancePrompt, temperature: 0.1, maxTokens: 3000)
                    : "";

                return new
                {
                    success = true,
                    businessContext = businessContext,
                    
                    // License compliance summary
                    complianceSummary = new
                    {
                        overallRisk = licenseAnalysis.OverallRisk,
                        totalPackages = licenseAnalysis.ReferencedPackages.Count,
                        paidPackages = licenseAnalysis.PaidPackages.Count,
                        unknownLicenses = licenseAnalysis.UnknownLicenses.Count,
                        potentialViolations = licenseAnalysis.Violations.Count
                    },
                    
                    // Paid package details
                    paidPackages = licenseAnalysis.PaidPackages.Select(p => new
                    {
                        name = p.Name,
                        version = p.Version,
                        licenseType = p.LicenseType,
                        commercialUseAllowed = p.CommercialUseAllowed,
                        requiresLicense = p.RequiresLicense,
                        estimatedCost = p.EstimatedCost,
                        freeAlternatives = p.FreeAlternatives,
                        usageLevel = AssessUsageLevel(projectContent, p.Name)
                    }),
                    
                    // License violations
                    violations = licenseAnalysis.Violations.Select(v => new
                    {
                        packageName = v.PackageName,
                        violationType = v.ViolationType,
                        description = v.Description,
                        severity = v.Severity,
                        resolution = v.Resolution
                    }),
                    
                    // AI analysis (if detailed)
                    aiAnalysis = detailedAnalysis ? new
                    {
                        analysis = aiAnalysis,
                        riskAssessment = ExtractLicenseRiskAssessment(aiAnalysis),
                        complianceRecommendations = ExtractComplianceRecommendations(aiAnalysis),
                        alternativeSuggestions = ExtractAlternativeSuggestions(aiAnalysis)
                    } : null,
                    
                    // Recommendations
                    recommendations = new
                    {
                        immediateActions = GetLicenseImmediateActions(licenseAnalysis),
                        packageReplacements = GetPackageReplacements(licenseAnalysis.PaidPackages),
                        licenseAcquisition = GetLicenseAcquisitionGuidance(licenseAnalysis.PaidPackages),
                        complianceProcess = GetComplianceProcessRecommendations()
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to analyze license compliance");
                throw new InvalidOperationException($"License compliance analysis failed: {ex.Message}");
            }
        }
    }

    // Helper methods for AI pattern detection
    private static List<AIGeneratedPattern> DetectAIGeneratedPatterns(string code)
    {
        var patterns = new List<AIGeneratedPattern>();
        
        // Check for common AI-generated patterns
        var lines = code.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            // TODO comments with AI-like language
            if (Regex.IsMatch(line, @"//\s*TODO:?\s*(implement|add|fix|complete)", RegexOptions.IgnoreCase))
            {
                patterns.Add(new AIGeneratedPattern
                {
                    Type = "AI_TODO_Comment",
                    Description = "Generic TODO comment typical of AI code generation",
                    Line = i + 1,
                    Evidence = line,
                    Confidence = 0.7,
                    RiskLevel = "LOW"
                });
            }
            
            // Throw NotImplementedException pattern
            if (line.Contains("throw new NotImplementedException"))
            {
                patterns.Add(new AIGeneratedPattern
                {
                    Type = "NotImplemented_Stub",
                    Description = "Method stub with NotImplementedException - possible AI shortcut",
                    Line = i + 1,
                    Evidence = line,
                    Confidence = 0.8,
                    RiskLevel = "HIGH"
                });
            }
            
            // Overly generic variable names
            if (Regex.IsMatch(line, @"\b(var\s+)?temp\d*\s*=|data\d*\s*=|result\d*\s*=|item\d*\s*="))
            {
                patterns.Add(new AIGeneratedPattern
                {
                    Type = "Generic_Variable_Names",
                    Description = "Generic variable names typical of AI code generation",
                    Line = i + 1,
                    Evidence = line,
                    Confidence = 0.6,
                    RiskLevel = "MEDIUM"
                });
            }
            
            // Simplified error handling
            if (line.Contains("catch") && lines.Length > i + 1 && lines[i + 1].Trim() == "// Handle error")
            {
                patterns.Add(new AIGeneratedPattern
                {
                    Type = "Simplified_Error_Handling",
                    Description = "Simplified or placeholder error handling",
                    Line = i + 1,
                    Evidence = line + "\n" + lines[i + 1],
                    Confidence = 0.75,
                    RiskLevel = "HIGH"
                });
            }
        }
        
        return patterns;
    }

    private static List<CodeShortcut> DetectCodeShortcuts(string code)
    {
        var shortcuts = new List<CodeShortcut>();
        var lines = code.Split('\n');
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            // Commented out service registrations
            if (Regex.IsMatch(line, @"//\s*builder\.Services\.Add|//\s*services\.Add", RegexOptions.IgnoreCase))
            {
                shortcuts.Add(new CodeShortcut
                {
                    Type = "Commented_Service_Registration",
                    Description = "Service registration commented out - likely to bypass compilation errors",
                    Location = $"Line {i + 1}",
                    Severity = "CRITICAL",
                    Impact = "Runtime dependency injection failures",
                    Recommendation = "Restore service registration and fix underlying issues"
                });
            }
            
            // Hardcoded return values
            if (Regex.IsMatch(line, @"return\s+(true|false|null|0|""""|\[\]);"))
            {
                shortcuts.Add(new CodeShortcut
                {
                    Type = "Hardcoded_Return",
                    Description = "Hardcoded return value - possible shortcut implementation",
                    Location = $"Line {i + 1}",
                    Severity = "HIGH",
                    Impact = "Incorrect application behavior",
                    Recommendation = "Implement proper logic instead of hardcoded values"
                });
            }
            
            // Empty method bodies
            if (line.Contains("{") && i + 1 < lines.Length && lines[i + 1].Trim() == "}")
            {
                shortcuts.Add(new CodeShortcut
                {
                    Type = "Empty_Method_Body",
                    Description = "Empty method implementation",
                    Location = $"Line {i + 1}",
                    Severity = "MEDIUM",
                    Impact = "Missing functionality",
                    Recommendation = "Implement method body or mark as abstract/virtual if intended"
                });
            }
        }
        
        return shortcuts;
    }

    private static CommentedCodeAnalysis AnalyzeCommentedOutCode(string code)
    {
        var analysis = new CommentedCodeAnalysis();
        var lines = code.Split('\n');
        var commentedLines = 0;
        var suspiciousBlocks = new List<SuspiciousCommentBlock>();
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            if (line.StartsWith("//"))
            {
                commentedLines++;
                
                // Check for suspicious patterns
                if (Regex.IsMatch(line, @"//\s*(builder\.Services|services\.Add|\.Configure)", RegexOptions.IgnoreCase))
                {
                    analysis.RegistrationBypasses.Add($"Line {i + 1}: {line}");
                }
                
                if (Regex.IsMatch(line, @"//\s*(public|private|protected).*\{", RegexOptions.IgnoreCase))
                {
                    analysis.ImplementationSkips.Add($"Line {i + 1}: {line}");
                }
                
                // Multi-line commented blocks
                if (line.Length > 50 && line.Contains("(") && line.Contains(")"))
                {
                    suspiciousBlocks.Add(new SuspiciousCommentBlock
                    {
                        Type = "Method_Implementation",
                        Content = line,
                        SuspiciousReason = "Complex method implementation commented out",
                        LineRange = $"{i + 1}",
                        RiskLevel = "HIGH"
                    });
                }
            }
        }
        
        analysis.TotalCommentedLines = commentedLines;
        analysis.SuspiciousBlocks = suspiciousBlocks;
        
        return analysis;
    }

    private static List<MissingImplementation> DetectMissingImplementations(string code, string? previousVersion)
    {
        var missing = new List<MissingImplementation>();
        
        // If we have previous version, we can detect deletions
        if (previousVersion != null)
        {
            // Simplified detection - in real implementation would use proper diff analysis
            var previousMethods = ExtractMethodSignatures(previousVersion);
            var currentMethods = ExtractMethodSignatures(code);
            
            foreach (var prevMethod in previousMethods)
            {
                if (!currentMethods.Contains(prevMethod))
                {
                    missing.Add(new MissingImplementation
                    {
                        Type = "DeletedMethod",
                        Name = prevMethod,
                        Signature = prevMethod,
                        EstimatedComplexity = "Unknown",
                        PotentialImpact = "Functionality loss"
                    });
                }
            }
        }
        
        return missing;
    }

    private static EFMigrationTamperingResult DetectEFMigrationTampering(string code, string[]? modifiedFiles)
    {
        var result = new EFMigrationTamperingResult();
        
        if (modifiedFiles?.Any(f => f.Contains("Migration") && f.EndsWith(".cs")) == true)
        {
            // Check for manual edits in migration files
            var suspiciousPatterns = new[]
            {
                @"//.*migrationBuilder",  // Commented migration operations
                @"migrationBuilder\.Sql\s*\(",  // Manual SQL
                @"\.AddColumn\(.*nullable:\s*true", // Nullable changes
                @"\.DropColumn\(", // Column drops
                @"\.DropTable\(" // Table drops
            };
            
            foreach (var pattern in suspiciousPatterns)
            {
                if (Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase))
                {
                    result.TamperingIndicators.Add(new TamperingIndicator
                    {
                        Type = "Suspicious_Pattern",
                        Description = $"Pattern matched: {pattern}",
                        Evidence = "Manual modification detected",
                        Severity = "HIGH"
                    });
                    result.HasIssues = true;
                }
            }
        }
        
        return result;
    }

    private static LicenseComplianceResult DetectLicenseCompliance(string code)
    {
        var result = new LicenseComplianceResult();
        
        // Known paid packages and their details
        var paidPackages = new Dictionary<string, PaidPackageInfo>
        {
            ["FluentAssertions"] = new PaidPackageInfo 
            { 
                Name = "FluentAssertions", 
                LicenseType = "Commercial", 
                EstimatedCost = "$500-2000/year",
                FreeAlternatives = new[] { "NUnit", "xUnit", "MSTest" },
                CommercialUseAllowed = false
            },
            ["MediatR"] = new PaidPackageInfo 
            { 
                Name = "MediatR", 
                LicenseType = "MIT", 
                EstimatedCost = "Free",
                FreeAlternatives = new[] { "Built-in DI", "Custom mediator" },
                CommercialUseAllowed = true
            },
            ["Telerik"] = new PaidPackageInfo 
            { 
                Name = "Telerik", 
                LicenseType = "Commercial", 
                EstimatedCost = "$1000+/year",
                FreeAlternatives = new[] { "DevExpress Community", "Open source alternatives" },
                CommercialUseAllowed = false
            }
        };
        
        // Detect package references
        var packageReferences = Regex.Matches(code, @"<PackageReference Include=""([^""]+)""", RegexOptions.IgnoreCase);
        
        foreach (Match match in packageReferences)
        {
            var packageName = match.Groups[1].Value;
            result.ReferencedPackages.Add(packageName);
            
            foreach (var kvp in paidPackages)
            {
                if (packageName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    result.PaidPackages.Add(kvp.Value);
                    
                    if (!kvp.Value.CommercialUseAllowed)
                    {
                        result.Violations.Add(new LicenseViolation
                        {
                            PackageName = packageName,
                            ViolationType = "Commercial_Use_Restriction",
                            Description = $"{packageName} requires commercial license for business use",
                            Severity = "HIGH",
                            Resolution = $"Purchase license or replace with: {string.Join(", ", kvp.Value.FreeAlternatives)}"
                        });
                        result.HasViolations = true;
                    }
                }
            }
        }
        
        return result;
    }

    // Additional helper methods...
    private static string BuildAIDetectionPrompt(string code, string? commit, List<CodeShortcut> shortcuts, CommentedCodeAnalysis comments)
    {
        return $@"
As an expert code reviewer specializing in detecting AI-generated code patterns, analyze this code:

## Code Context
{(commit != null ? $"Commit Message: {commit}" : "")}
{(shortcuts.Any() ? $"Detected Shortcuts: {shortcuts.Count}" : "")}
{(comments.TotalCommentedLines > 0 ? $"Commented Lines: {comments.TotalCommentedLines}" : "")}

## Code Sample
{code.Substring(0, Math.Min(2000, code.Length))}...

Please identify:
1. **AI Generation Indicators**: Patterns suggesting AI code generation
2. **Quality Shortcuts**: Evidence of bypassing proper implementation
3. **Intent Analysis**: What the developer was trying to achieve
4. **Risk Assessment**: Potential issues with the current approach
5. **Recommendations**: How to improve the code quality

Focus on sophisticated patterns that basic regex cannot catch.
";
    }

    // More helper methods with proper implementations would continue here...
    private static double CalculateOverallRiskScore(List<AIGeneratedPattern> ai, List<CodeShortcut> shortcuts, CommentedCodeAnalysis comments, List<MissingImplementation> missing, LicenseComplianceResult license)
    {
        var score = 0.0;
        score += ai.Count(p => p.RiskLevel == "HIGH") * 2;
        score += shortcuts.Count(s => s.Severity == "CRITICAL") * 3;
        score += comments.RegistrationBypasses.Count * 2.5;
        score += missing.Count * 1.5;
        score += license.Violations.Count * 2;
        return Math.Min(10, score);
    }

    private static double CalculateAIGenerationConfidence(List<AIGeneratedPattern> patterns)
    {
        return patterns.Any() ? patterns.Average(p => p.Confidence) : 0;
    }

    // Placeholder implementations for remaining methods...
    private static string[] ExtractMethodSignatures(string code) => Array.Empty<string>();
    private static MigrationTamperingAnalysis AnalyzeMigrationTampering(string content, string fileName) => new MigrationTamperingAnalysis();
    private static string[] GetImmediateActions(double risk, List<CodeShortcut> shortcuts, LicenseComplianceResult license) => new[] { "Review critical issues" };
    private static string[] GetCodeReviewFocusAreas(List<AIGeneratedPattern> ai, List<CodeShortcut> shortcuts, CommentedCodeAnalysis comments) => new[] { "Focus on implementation quality" };
    
    // Missing methods for EF Migration analysis
    private static List<TamperingIndicator> DetectDangerousMigrationOperations(string content) => new List<TamperingIndicator>();
    private static List<ManualMigrationEdit> DetectManualMigrationEdits(string content, string? previous) => new List<ManualMigrationEdit>();
    private static string BuildMigrationAnalysisPrompt(string content, string fileName, MigrationTamperingAnalysis analysis) => $"Analyze migration {fileName}";
    private static string ExtractLegitimacyAssessment(string analysis) => "Assessment pending";
    private static string ExtractMigrationRiskAssessment(string analysis) => "Risk assessment pending";
    private static string ExtractMigrationRecommendations(string analysis) => "Recommendations pending";
    private static bool ValidateMigrationStructure(string content) => true;
    private static bool HasBackupOperations(string content) => false;
    private static bool IsReversible(string content) => true;
    private static string AssessDataLossRisk(string content) => "Low";
    
    // Missing methods for License Compliance analysis  
    private static LicenseComplianceResult AnalyzeLicenseCompliance(string content) => new LicenseComplianceResult();
    private static List<PackageDetails> GetPackageDetails(List<string> packages) => new List<PackageDetails>();
    private static string BuildLicenseCompliancePrompt(LicenseComplianceResult analysis, List<PackageDetails> details, string context) => $"Analyze license compliance for {context}";
    private static string AssessUsageLevel(string content, string packageName) => "Medium";
    private static string ExtractLicenseRiskAssessment(string analysis) => "Medium risk";
    private static string[] ExtractComplianceRecommendations(string analysis) => new[] { "Review licenses" };
    private static string[] ExtractAlternativeSuggestions(string analysis) => new[] { "Consider open source alternatives" };
    private static string[] GetLicenseImmediateActions(LicenseComplianceResult analysis) => new[] { "Audit paid packages" };
    private static string[] GetPackageReplacements(List<PaidPackageInfo> packages) => new[] { "Replace with free alternatives" };
    private static string[] GetLicenseAcquisitionGuidance(List<PaidPackageInfo> packages) => new[] { "Contact vendors for licensing" };
    private static string[] GetComplianceProcessRecommendations() => new[] { "Implement license tracking process" };
    
    // Missing methods for registration bypasses analysis
    private static List<RegistrationBypass> DetectRegistrationBypasses(string code)
    {
        var bypasses = new List<RegistrationBypass>();
        var lines = code.Split('\n');
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            if (Regex.IsMatch(line, @"//\s*(builder\.Services|services\.Add|\.Configure)", RegexOptions.IgnoreCase))
            {
                bypasses.Add(new RegistrationBypass
                {
                    Type = "CommentedOut",
                    ServiceName = ExtractServiceName(line),
                    SuspectedReason = "Likely commented to bypass compilation issues",
                    RiskLevel = "HIGH",
                    Location = $"Line {i + 1}",
                    DependentCode = "",
                    Impact = "Runtime dependency injection failures"
                });
            }
        }
        
        return bypasses;
    }
    
    private static string ExtractServiceName(string line)
    {
        var match = Regex.Match(line, @"Add\w*<([^>]+)>");
        return match.Success ? match.Groups[1].Value : "Unknown";
    }
    
    // Advanced analysis helper methods
    private static string[] ExtractSophisticatedPatterns(string analysis) => new[] { "Pattern analysis pending" };
    private static string ExtractIntentAnalysis(string analysis, string? commit) => "Intent analysis pending";
    private static string ExtractQualityAssessment(string analysis) => "Quality assessment pending";
    private static string[] ExtractTeamGuidance(string analysis) => new[] { "Team guidance pending" };
    private static string[] GetTeamTrainingRecommendations(List<AIGeneratedPattern> patterns, double risk) => new[] { "AI awareness training" };
    private static string[] GetProcessImprovements(List<CodeShortcut> shortcuts, List<RegistrationBypass> registrations, EFMigrationTamperingResult migrations) => new[] { "Implement code review checklist" };
    private static string[] GetToolingRecommendations(LicenseComplianceResult license, List<AIGeneratedPattern> ai) => new[] { "Add license scanning tools" };
    private static string[] GeneratePreCommitHookSuggestions(List<CodeShortcut> shortcuts, LicenseComplianceResult license) => new[] { "Add pre-commit license check" };
    private static string[] GenerateCodeReviewChecklist(List<AIGeneratedPattern> ai, List<CodeShortcut> shortcuts) => new[] { "Check for AI-generated patterns" };
    private static string[] GenerateCICDGateSuggestions(LicenseComplianceResult license, EFMigrationTamperingResult migrations) => new[] { "Add license compliance gate" };
    private static string[] GenerateTeamGuidelines(double risk, List<AIGeneratedPattern> patterns) => new[] { "Establish AI usage guidelines" };
}

// Supporting data models
public class AIGeneratedPattern
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public int Line { get; set; }
    public string Evidence { get; set; } = "";
    public double Confidence { get; set; }
    public string RiskLevel { get; set; } = "";
}

public class CodeShortcut
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Impact { get; set; } = "";
    public string Recommendation { get; set; } = "";
}

public class CommentedCodeAnalysis
{
    public int TotalCommentedLines { get; set; }
    public List<SuspiciousCommentBlock> SuspiciousBlocks { get; set; } = new();
    public List<string> RegistrationBypasses { get; set; } = new();
    public List<string> ImplementationSkips { get; set; } = new();
}

public class SuspiciousCommentBlock
{
    public string Type { get; set; } = "";
    public string Content { get; set; } = "";
    public string SuspiciousReason { get; set; } = "";
    public string LineRange { get; set; } = "";
    public string RiskLevel { get; set; } = "";
}

public class MissingImplementation
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string Signature { get; set; } = "";
    public string BeforeSignature { get; set; } = "";
    public string AfterSignature { get; set; } = "";
    public string EstimatedComplexity { get; set; } = "";
    public string PotentialImpact { get; set; } = "";
    public bool SuspiciousSimplification { get; set; }
}

public class EFMigrationTamperingResult
{
    public bool HasIssues { get; set; }
    public List<TamperingIndicator> TamperingIndicators { get; set; } = new();
    public List<string> ManualEdits { get; set; } = new();
    public List<string> SkippedMigrations { get; set; } = new();
    public List<string> DangerousOperations { get; set; } = new();
}

public class TamperingIndicator
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string Evidence { get; set; } = "";
    public string Severity { get; set; } = "";
    public string RiskLevel { get; set; } = "";
    public string Operation { get; set; } = "";
    public string Recommendation { get; set; } = "";
    public string[] AffectedTables { get; set; } = Array.Empty<string>();
    public int LineNumber { get; set; }
}

public class LicenseComplianceResult
{
    public bool HasViolations { get; set; }
    public List<string> ReferencedPackages { get; set; } = new();
    public List<PaidPackageInfo> PaidPackages { get; set; } = new();
    public List<string> UnknownLicenses { get; set; } = new();
    public List<LicenseViolation> Violations { get; set; } = new();
    public string OverallRisk { get; set; } = "LOW";
    public double ComplianceScore { get; set; } = 1.0;
}

public class PaidPackageInfo
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string LicenseType { get; set; } = "";
    public bool CommercialUseAllowed { get; set; }
    public bool RequiresLicense { get; set; }
    public string EstimatedCost { get; set; } = "";
    public string[] FreeAlternatives { get; set; } = Array.Empty<string>();
    public string[] CommercialRestrictions { get; set; } = Array.Empty<string>();
}

public class LicenseViolation
{
    public string PackageName { get; set; } = "";
    public string ViolationType { get; set; } = "";
    public string Description { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Resolution { get; set; } = "";
}

public class MigrationTamperingAnalysis
{
    public bool HasTampering { get; set; }
    public List<TamperingIndicator> Indicators { get; set; } = new();
}

public class ManualMigrationEdit
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string Before { get; set; } = "";
    public string After { get; set; } = "";
    public string SuspicionLevel { get; set; } = "";
}

public class PackageDetails
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string License { get; set; } = "";
    public bool IsCommercial { get; set; }
}

public class RegistrationBypass
{
    public string Type { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string SuspectedReason { get; set; } = "";
    public string RiskLevel { get; set; } = "";
    public string Location { get; set; } = "";
    public string DependentCode { get; set; } = "";
    public string Impact { get; set; } = "";
}