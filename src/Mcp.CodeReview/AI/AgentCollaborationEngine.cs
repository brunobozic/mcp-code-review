using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;
using System.Text.Json;

namespace Mcp.CodeReview.AI;

/// <summary>
/// Engine that enables real agent-to-agent communication and collaborative analysis
/// </summary>
public class AgentCollaborationEngine
{
    private readonly IAIServiceProvider _aiService;
    private readonly ILogger<AgentCollaborationEngine> _logger;
    private readonly List<AgentConversation> _activeConversations = new();

    public AgentCollaborationEngine(
        IAIServiceProvider aiService,
        ILogger<AgentCollaborationEngine> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Conduct collaborative review where agents actually communicate with each other
    /// </summary>
    public async Task<CollaborativeReviewResult> ConductCollaborativeReviewAsync(
        CodeReviewRequest request,
        RepositoryContext repositoryContext,
        Models.AgentExecutionResult[] initialResults,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting collaborative agent review with {AgentCount} agents", initialResults.Length);

        var conversation = new AgentConversation
        {
            Participants = initialResults.Select(r => r.AgentType).ToList()
        };

        try
        {
            // Phase 1: Initial findings presentation
            await PresentInitialFindings(conversation, initialResults, repositoryContext);
            
            // Phase 2: Agent questioning and clarification
            await ConductQuestioningRound(conversation, repositoryContext, cancellationToken);
            
            // Phase 3: Challenge and debate
            await ConductChallengeRound(conversation, repositoryContext, cancellationToken);
            
            // Phase 4: Evidence gathering
            await GatherSupportingEvidence(conversation, repositoryContext, cancellationToken);
            
            // Phase 5: Consensus building
            var consensus = await BuildConsensus(conversation, repositoryContext, cancellationToken);
            
            // Phase 6: Final synthesis
            var collaborativeResult = await SynthesizeCollaborativeResult(conversation, consensus, cancellationToken);
            
            conversation.Status = ConversationStatus.Completed;
            conversation.CompletedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Collaborative review completed with consensus score: {Score}", 
                consensus.OverallConfidence);

            return collaborativeResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to conduct collaborative agent review");
            conversation.Status = ConversationStatus.DisputeUnresolved;
            throw;
        }
        finally
        {
            _activeConversations.Add(conversation);
        }
    }

    private async Task PresentInitialFindings(
        AgentConversation conversation, 
        Models.AgentExecutionResult[] initialResults,
        RepositoryContext repositoryContext)
    {
        _logger.LogInformation("Phase 1: Agents presenting initial findings");

        foreach (var result in initialResults.Where(r => r.Success))
        {
            var message = new AgentMessage
            {
                FromAgent = result.AgentType,
                ToAgents = new List<AgentType>(), // Broadcast to all
                Type = MessageType.InitialAnalysis,
                Content = FormatInitialFinding(result, repositoryContext)
            };

            conversation.Messages.Add(message);
            _logger.LogDebug("Agent {Agent} presented initial findings", result.AgentType);
        }
    }

    private string FormatInitialFinding(Models.AgentExecutionResult result, RepositoryContext context)
    {
        return $@"
## {result.AgentType} Initial Analysis

**Context**: Analyzing code in project '{context.ProjectName}' with {context.Structure.ArchitecturePattern} architecture.
**Dependencies**: {context.Dependencies.Count} identified dependencies.
**Related Files**: {context.RelatedFiles.Count} related files analyzed.

**Key Findings**:
{string.Join("\n", result.KeyFindings.Select(f => $"- {f}"))}

**Recommendations**:
{string.Join("\n", result.Recommendations.Select(r => $"- {r}"))}

**Confidence Level**: {result.ConfidenceScore:F2}

**Historical Context**: Based on {context.HistoricalPatterns.Count} similar patterns in codebase history.
";
    }

    private async Task ConductQuestioningRound(
        AgentConversation conversation, 
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Phase 2: Agent questioning and clarification");

        var initialMessages = conversation.Messages.Where(m => m.Type == MessageType.InitialAnalysis).ToList();
        
        foreach (var questioner in conversation.Participants)
        {
            foreach (var targetMessage in initialMessages.Where(m => m.FromAgent != questioner))
            {
                var questionPrompt = BuildQuestionPrompt(questioner, targetMessage, repositoryContext);
                var questionResponse = await _aiService.GenerateReviewAsync(questionPrompt, cancellationToken);
                
                if (!string.IsNullOrWhiteSpace(questionResponse))
                {
                    var questionMessage = new AgentMessage
                    {
                        FromAgent = questioner,
                        ToAgents = new List<AgentType> { targetMessage.FromAgent },
                        Type = MessageType.Question,
                        Content = questionResponse,
                        ReplyToMessageId = targetMessage.MessageId
                    };
                    
                    conversation.Messages.Add(questionMessage);
                    
                    // Get response from target agent
                    await GetAgentResponse(conversation, questionMessage, repositoryContext, cancellationToken);
                }
                
                await Task.Delay(100, cancellationToken); // Small delay to prevent API rate limiting
            }
        }
    }

    private string BuildQuestionPrompt(AgentType questioner, AgentMessage targetMessage, RepositoryContext context)
    {
        return $@"
You are the {questioner} agent reviewing code in a collaborative session.

Another agent ({targetMessage.FromAgent}) has made the following analysis:
{targetMessage.Content}

Context about the codebase:
- Project: {context.ProjectName}
- Architecture: {context.Structure.ArchitecturePattern}
- Key Dependencies: {string.Join(", ", context.Dependencies.Keys.Take(5))}
- Historical Patterns: {context.HistoricalPatterns.Count} similar cases found

Your role is to ask thoughtful, technical questions about their analysis from your {questioner} perspective.
Focus on:
1. Clarifying assumptions they made
2. Questioning methodology or evidence
3. Identifying potential gaps in their analysis
4. Asking about alternative approaches they considered

Generate 1-2 specific, technical questions. Be professional and constructive.
Format your response as clear questions, not statements.
";
    }

    private async Task GetAgentResponse(
        AgentConversation conversation,
        AgentMessage questionMessage,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        var responsePrompt = BuildResponsePrompt(questionMessage, repositoryContext);
        var responseContent = await _aiService.GenerateReviewAsync(responsePrompt, cancellationToken);
        
        if (!string.IsNullOrWhiteSpace(responseContent))
        {
            var responseMessage = new AgentMessage
            {
                FromAgent = questionMessage.ToAgents.First(),
                ToAgents = new List<AgentType> { questionMessage.FromAgent },
                Type = MessageType.Response,
                Content = responseContent,
                ReplyToMessageId = questionMessage.MessageId
            };
            
            conversation.Messages.Add(responseMessage);
        }
    }

    private string BuildResponsePrompt(AgentMessage questionMessage, RepositoryContext context)
    {
        var targetAgent = questionMessage.ToAgents.First();
        
        return $@"
You are the {targetAgent} agent in a collaborative code review session.

Another agent ({questionMessage.FromAgent}) has asked you this question:
{questionMessage.Content}

Context about the codebase:
- Project: {context.ProjectName}
- Architecture: {context.Structure.ArchitecturePattern}
- Available Historical Data: {context.HistoricalPatterns.Count} patterns
- Team Preferences: {context.TeamPatterns.PreferredPatterns.Count} known patterns

Respond as the {targetAgent} agent would, addressing their questions directly.
Provide:
1. Clear answers to their specific questions
2. Supporting evidence from the codebase context
3. Acknowledgment if they identified valid concerns
4. Additional details that strengthen your original analysis

Be technical, specific, and collaborative. If they raised valid points, acknowledge them.
";
    }

    private async Task ConductChallengeRound(
        AgentConversation conversation, 
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Phase 3: Agent challenge and debate");

        var challenges = new List<AgentChallenge>();
        
        // Identify conflicting findings for challenges
        var conflictingFindings = IdentifyConflictingFindings(conversation.Messages);
        
        foreach (var conflict in conflictingFindings)
        {
            var challengePrompt = BuildChallengePrompt(conflict.Item1, conflict.Item2, repositoryContext);
            var challengeContent = await _aiService.GenerateReviewAsync(challengePrompt, cancellationToken);
            
            if (!string.IsNullOrWhiteSpace(challengeContent))
            {
                var challenge = new AgentChallenge
                {
                    ChallengingAgent = conflict.Item1.FromAgent,
                    ChallengedAgent = conflict.Item2.FromAgent,
                    OriginalFinding = conflict.Item2.Content,
                    Challenge = challengeContent,
                    ConfidenceScore = 0.8 // Will be refined based on response
                };
                
                challenges.Add(challenge);
                
                // Get response to challenge
                await GetChallengeResponse(challenge, repositoryContext, cancellationToken);
                
                // Add challenge discussion to conversation
                var challengeMessage = new AgentMessage
                {
                    FromAgent = challenge.ChallengingAgent,
                    ToAgents = new List<AgentType> { challenge.ChallengedAgent },
                    Type = MessageType.Challenge,
                    Content = $"CHALLENGE: {challenge.Challenge}\n\nRESPONSE: {challenge.Response}",
                    ReplyToMessageId = conflict.Item2.MessageId
                };
                
                conversation.Messages.Add(challengeMessage);
            }
            
            await Task.Delay(200, cancellationToken);
        }
    }

    private List<(AgentMessage, AgentMessage)> IdentifyConflictingFindings(List<AgentMessage> messages)
    {
        var conflicts = new List<(AgentMessage, AgentMessage)>();
        var findings = messages.Where(m => m.Type == MessageType.InitialAnalysis).ToList();
        
        // Simple conflict detection based on contrasting keywords
        var conflictKeywords = new[]
        {
            ("secure", "vulnerable"),
            ("performance", "inefficient"),
            ("good", "poor"),
            ("recommended", "avoid"),
            ("optimal", "suboptimal")
        };
        
        for (int i = 0; i < findings.Count; i++)
        {
            for (int j = i + 1; j < findings.Count; j++)
            {
                foreach (var (positive, negative) in conflictKeywords)
                {
                    if ((findings[i].Content.Contains(positive, StringComparison.OrdinalIgnoreCase) &&
                         findings[j].Content.Contains(negative, StringComparison.OrdinalIgnoreCase)) ||
                        (findings[i].Content.Contains(negative, StringComparison.OrdinalIgnoreCase) &&
                         findings[j].Content.Contains(positive, StringComparison.OrdinalIgnoreCase)))
                    {
                        conflicts.Add((findings[i], findings[j]));
                        break;
                    }
                }
            }
        }
        
        return conflicts.Take(3).ToList(); // Limit to prevent excessive back-and-forth
    }

    private string BuildChallengePrompt(AgentMessage challenger, AgentMessage challenged, RepositoryContext context)
    {
        return $@"
You are the {challenger.FromAgent} agent in a collaborative code review.

Another agent ({challenged.FromAgent}) made this analysis:
{challenged.Content}

Your analysis was:
{challenger.Content}

Context:
- Project: {context.ProjectName} 
- Architecture: {context.Structure.ArchitecturePattern}
- Historical Patterns: {context.HistoricalPatterns.Count} available
- Team Standards: {context.ProjectStandards.Count} applicable standards

You notice a potential disagreement or different perspective. Challenge their analysis constructively:
1. Point out specific technical disagreements
2. Present alternative interpretations
3. Ask for additional evidence
4. Reference historical patterns or standards that support your view

Be technical and specific. Focus on the technical merits, not just opinions.
Format as a professional challenge that advances the discussion.
";
    }

    private async Task GetChallengeResponse(
        AgentChallenge challenge, 
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        var responsePrompt = $@"
You are the {challenge.ChallengedAgent} agent responding to a challenge.

Another agent ({challenge.ChallengingAgent}) has challenged your analysis:
{challenge.Challenge}

Your original finding was:
{challenge.OriginalFinding}

Context:
- Project: {repositoryContext.ProjectName}
- Architecture: {repositoryContext.Structure.ArchitecturePattern}
- Available Evidence: Historical patterns, team standards, dependency analysis

Respond to their challenge:
1. Address their specific technical concerns
2. Provide additional evidence supporting your view OR acknowledge valid points
3. Clarify any misunderstandings
4. If appropriate, modify your position based on their input

Be open to valid criticism while defending sound technical positions.
";

        challenge.Response = await _aiService.GenerateReviewAsync(responsePrompt, cancellationToken);
        challenge.Status = ChallengeStatus.Responded;
    }

    private async Task GatherSupportingEvidence(
        AgentConversation conversation,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Phase 4: Gathering supporting evidence");

        foreach (var agent in conversation.Participants)
        {
            var evidencePrompt = BuildEvidencePrompt(agent, conversation, repositoryContext);
            var evidence = await _aiService.GenerateReviewAsync(evidencePrompt, cancellationToken);
            
            if (!string.IsNullOrWhiteSpace(evidence))
            {
                var evidenceMessage = new AgentMessage
                {
                    FromAgent = agent,
                    ToAgents = new List<AgentType>(), // Broadcast
                    Type = MessageType.Evidence,
                    Content = evidence
                };
                
                conversation.Messages.Add(evidenceMessage);
            }
            
            await Task.Delay(100, cancellationToken);
        }
    }

    private string BuildEvidencePrompt(AgentType agent, AgentConversation conversation, RepositoryContext context)
    {
        var agentMessages = conversation.Messages.Where(m => m.FromAgent == agent).ToList();
        var recentDiscussion = string.Join("\n\n", conversation.Messages.TakeLast(10).Select(m => 
            $"{m.FromAgent}: {m.Content.Substring(0, Math.Min(200, m.Content.Length))}..."));
        
        return $@"
You are the {agent} agent in the final evidence-gathering phase.

Recent discussion summary:
{recentDiscussion}

Available contextual evidence:
- Project Structure: {context.Structure.ArchitecturePattern} with {context.Structure.FilesByType.Count} file types
- Dependencies: {string.Join(", ", context.Dependencies.Keys.Take(5))}
- Historical Patterns: {context.HistoricalPatterns.Count} similar cases
- Team Standards: {context.ProjectStandards.Count} applicable standards
- Related Files: {context.RelatedFiles.Count} files analyzed

Provide concrete evidence from the codebase context that supports your position.
Focus on:
1. Specific patterns found in the historical data
2. Dependency analysis results
3. Architecture-specific considerations
4. Team standard compliance

Be specific and reference actual data points from the context.
";
    }

    private async Task<AgentConsensus> BuildConsensus(
        AgentConversation conversation,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Phase 5: Building agent consensus");

        var consensusPrompt = BuildConsensusPrompt(conversation, repositoryContext);
        var consensusContent = await _aiService.GenerateReviewAsync(consensusPrompt, cancellationToken);
        
        // Parse consensus (in a real implementation, this would be more sophisticated)
        var consensus = new AgentConsensus
        {
            Summary = consensusContent,
            OverallConfidence = CalculateOverallConfidence(conversation),
            AgreedFindings = ExtractAgreedFindings(conversation),
            DisputedFindings = ExtractDisputedFindings(conversation)
        };
        
        conversation.Consensus = consensus;
        return consensus;
    }

    private string BuildConsensusPrompt(AgentConversation conversation, RepositoryContext context)
    {
        var discussionSummary = string.Join("\n\n", conversation.Messages.Select(m => 
            $"{m.FromAgent} ({m.Type}): {m.Content.Substring(0, Math.Min(300, m.Content.Length))}..."));
        
        return $@"
Analyze the collaborative agent discussion and build consensus.

Discussion summary:
{discussionSummary}

Participants: {string.Join(", ", conversation.Participants)}
Context: {context.ProjectName} ({context.Structure.ArchitecturePattern})

Synthesize the discussion into:
1. Points where all agents agree
2. Points where there's disagreement
3. Overall confidence level (0-1)
4. Final recommendations that incorporate multiple perspectives

Focus on technical consensus and acknowledge different viewpoints where they exist.
";
    }

    private double CalculateOverallConfidence(AgentConversation conversation)
    {
        var challengeMessages = conversation.Messages.Where(m => m.Type == MessageType.Challenge).Count();
        var responseMessages = conversation.Messages.Where(m => m.Type == MessageType.Response).Count();
        var evidenceMessages = conversation.Messages.Where(m => m.Type == MessageType.Evidence).Count();
        
        // Higher confidence when there's good discussion and evidence
        var baseConfidence = 0.7;
        var discussionBonus = Math.Min(0.2, (responseMessages + evidenceMessages) * 0.05);
        var challengePenalty = Math.Min(0.1, challengeMessages * 0.02);
        
        return Math.Max(0.5, Math.Min(1.0, baseConfidence + discussionBonus - challengePenalty));
    }

    private List<ConsensusItem> ExtractAgreedFindings(AgentConversation conversation)
    {
        // Simplified extraction - in reality, this would use NLP to identify consensus
        var findings = new List<ConsensusItem>();
        
        var evidenceMessages = conversation.Messages.Where(m => m.Type == MessageType.Evidence).ToList();
        foreach (var evidence in evidenceMessages)
        {
            findings.Add(new ConsensusItem
            {
                Finding = evidence.Content.Substring(0, Math.Min(200, evidence.Content.Length)),
                AgreeingAgents = new List<AgentType> { evidence.FromAgent },
                ConfidenceScore = 0.8,
                Evidence = evidence.Content,
                Category = evidence.FromAgent.ToString()
            });
        }
        
        return findings;
    }

    private List<ConsensusItem> ExtractDisputedFindings(AgentConversation conversation)
    {
        var disputed = new List<ConsensusItem>();
        
        var challenges = conversation.Messages.Where(m => m.Type == MessageType.Challenge).ToList();
        foreach (var challenge in challenges)
        {
            disputed.Add(new ConsensusItem
            {
                Finding = challenge.Content.Substring(0, Math.Min(200, challenge.Content.Length)),
                AgreeingAgents = new List<AgentType> { challenge.FromAgent },
                DisagreeingAgents = challenge.ToAgents,
                ConfidenceScore = 0.5,
                Evidence = challenge.Content,
                Category = "Disputed"
            });
        }
        
        return disputed;
    }

    private async Task<CollaborativeReviewResult> SynthesizeCollaborativeResult(
        AgentConversation conversation,
        AgentConsensus consensus,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Phase 6: Final synthesis of collaborative results");

        var synthesisPrompt = BuildSynthesisPrompt(conversation, consensus);
        var finalSynthesis = await _aiService.GenerateReviewAsync(synthesisPrompt, cancellationToken);
        
        return new CollaborativeReviewResult
        {
            Conversation = conversation,
            Consensus = consensus,
            ValidatedFindings = ExtractValidatedFindings(consensus),
            CollaborativeRecommendations = ExtractCollaborativeRecommendations(consensus),
            AgentConfidenceScores = CalculateAgentConfidenceScores(conversation),
            CollaborationSummary = finalSynthesis
        };
    }

    private string BuildSynthesisPrompt(AgentConversation conversation, AgentConsensus consensus)
    {
        return $@"
Synthesize the collaborative agent discussion into final recommendations.

Conversation involved: {string.Join(", ", conversation.Participants)}
Total messages: {conversation.Messages.Count}
Consensus confidence: {consensus.OverallConfidence:F2}

Consensus findings:
{string.Join("\n", consensus.AgreedFindings.Select(f => $"- {f.Finding}"))}

Create a final synthesis that:
1. Highlights the most important collaborative insights
2. Presents validated findings with confidence scores
3. Provides actionable recommendations
4. Acknowledges areas of disagreement

Focus on the value added by agent collaboration vs. individual analysis.
";
    }

    private List<string> ExtractValidatedFindings(AgentConsensus consensus)
    {
        return consensus.AgreedFindings
            .Where(f => f.ConfidenceScore > 0.7)
            .Select(f => f.Finding)
            .ToList();
    }

    private List<string> ExtractCollaborativeRecommendations(AgentConsensus consensus)
    {
        return consensus.AgreedFindings
            .Where(f => f.Category.Contains("recommendation", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.Finding)
            .ToList();
    }

    /// <summary>
    /// Master method that orchestrates all advanced reasoning patterns for comprehensive analysis
    /// </summary>
    public async Task<AdvancedCollaborativeResult> ConductAdvancedMultiPatternAnalysisAsync(
        CodeReviewRequest request,
        RepositoryContext repositoryContext,
        AdvancedReasoningConfig config,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting advanced multi-pattern collaborative analysis for {FileName}", request.FileName);

        var result = new AdvancedCollaborativeResult
        {
            StartTime = DateTime.UtcNow,
            PrimaryPattern = DetermineOptimalReasoningPattern(request, repositoryContext, config)
        };

        try
        {
            // Phase 1: Standard collaborative review as baseline
            if (request.RequestedAgents.Any())
            {
                var initialResults = await GenerateInitialAgentResults(request, repositoryContext, cancellationToken);
                result.StandardCollaboration = await ConductCollaborativeReviewAsync(
                    request, repositoryContext, initialResults, cancellationToken);
                
                _logger.LogInformation("Standard collaboration completed with consensus score: {Score}", 
                    result.StandardCollaboration.Consensus.OverallConfidence);
            }

            // Phase 2: Advanced reasoning patterns based on complexity
            var complexityScore = AnalyzeRequestComplexity(request, repositoryContext);
            
            if (complexityScore >= 0.7 && config.EnableTreeOfThoughts)
            {
                _logger.LogInformation("High complexity detected ({Score:F2}), initiating Tree of Thoughts analysis", complexityScore);
                result.TreeOfThoughts = await ConductTreeOfThoughtsAnalysisAsync(
                    request, repositoryContext, request.RequestedAgents, cancellationToken);
                result.PrimaryPattern = ReasoningPattern.TreeOfThoughts;
            }
            else if (complexityScore >= 0.5 && config.EnableChainOfThought)
            {
                _logger.LogInformation("Medium complexity detected ({Score:F2}), initiating Chain of Thought analysis", complexityScore);
                var primaryAgent = SelectPrimaryAgentForChain(request.RequestedAgents);
                var collaborators = request.RequestedAgents.Where(a => a != primaryAgent).ToList();
                
                result.ChainOfThought = await ConductChainOfThoughtAnalysisAsync(
                    request, repositoryContext, primaryAgent, collaborators, cancellationToken);
                result.PrimaryPattern = ReasoningPattern.ChainOfThought;
            }

            // Phase 3: Focused nested chats for specific concerns
            if (config.EnableNestedChats)
            {
                var nestedChatTopics = IdentifyNestedChatTopics(request, repositoryContext, result);
                
                foreach (var topic in nestedChatTopics.Take(3)) // Limit to prevent excessive analysis time
                {
                    var participants = SelectParticipantsForTopic(topic, request.RequestedAgents);
                    var context = BuildTopicContext(topic, request, repositoryContext, result);
                    
                    var nestedChat = await ConductNestedChatAsync(topic, participants, repositoryContext, context, cancellationToken);
                    result.NestedChats.Add(nestedChat);
                }
                
                _logger.LogInformation("Completed {ChatCount} nested chat sessions", result.NestedChats.Count);
            }

            // Phase 4: Meta-reasoning and synthesis
            result.SynthesizedConclusion = await SynthesizeAdvancedResults(result, repositoryContext, cancellationToken);
            result.OverallConfidence = CalculateOverallConfidence(result);
            result.FinalRecommendations = ExtractFinalRecommendations(result);
            result.AgentContributions = CalculateAgentContributions(result);
            result.AdvancedMetrics = CalculateAdvancedMetrics(result);

            result.EndTime = DateTime.UtcNow;
            
            _logger.LogInformation("Advanced multi-pattern analysis completed in {Duration:F1}s with confidence {Confidence:F2}",
                result.TotalDuration.TotalSeconds, result.OverallConfidence);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to conduct advanced multi-pattern analysis");
            result.EndTime = DateTime.UtcNow;
            throw;
        }
    }

    /// <summary>
    /// Hybrid reasoning approach that combines multiple patterns dynamically
    /// </summary>
    public async Task<AdvancedCollaborativeResult> ConductHybridReasoningAnalysisAsync(
        CodeReviewRequest request,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting hybrid reasoning analysis for {FileName}", request.FileName);

        var result = new AdvancedCollaborativeResult
        {
            StartTime = DateTime.UtcNow,
            PrimaryPattern = ReasoningPattern.Hybrid
        };

        try
        {
            // Step 1: Quick Tree of Thoughts to identify key reasoning branches
            var quickToT = await ConductQuickTreeOfThoughtsAsync(request, repositoryContext, request.RequestedAgents.Take(3).ToList(), cancellationToken);
            
            // Step 2: Chain of Thought for the most promising branch
            var bestBranch = quickToT.EvaluatedPaths.OrderByDescending(p => p.Evaluation.OverallScore).First();
            var chainResult = await ConductTargetedChainOfThoughtAsync(bestBranch, repositoryContext, cancellationToken);
            
            // Step 3: Nested chats for unresolved questions
            var unresolvedQuestions = ExtractUnresolvedQuestions(chainResult);
            var nestedChats = new List<NestedChatSession>();
            
            foreach (var question in unresolvedQuestions.Take(2))
            {
                var participants = SelectExpertsForQuestion(question, request.RequestedAgents);
                var context = new Dictionary<string, object> { ["question"] = question, ["chainResult"] = chainResult };
                
                var nestedChat = await ConductNestedChatAsync(question, participants, repositoryContext, context, cancellationToken);
                nestedChats.Add(nestedChat);
            }

            // Step 4: Synthesize hybrid results
            result.TreeOfThoughts = quickToT;
            result.ChainOfThought = chainResult;
            result.NestedChats = nestedChats;
            result.SynthesizedConclusion = await SynthesizeHybridResults(result, repositoryContext, cancellationToken);
            result.OverallConfidence = CalculateHybridConfidence(result);
            result.FinalRecommendations = ExtractHybridRecommendations(result);

            result.EndTime = DateTime.UtcNow;
            
            _logger.LogInformation("Hybrid reasoning analysis completed with {BranchCount} thought branches, {StepCount} reasoning steps, and {ChatCount} focused discussions",
                quickToT.ThoughtTree.Count, chainResult.ReasoningChain.Count, nestedChats.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to conduct hybrid reasoning analysis");
            result.EndTime = DateTime.UtcNow;
            throw;
        }
    }

    // Advanced reasoning orchestration methods

    private ReasoningPattern DetermineOptimalReasoningPattern(
        CodeReviewRequest request, 
        RepositoryContext repositoryContext, 
        AdvancedReasoningConfig config)
    {
        var complexity = AnalyzeRequestComplexity(request, repositoryContext);
        var agentCount = request.RequestedAgents.Count;
        var hasSpecificConcerns = !string.IsNullOrEmpty(request.Context);

        if (complexity >= 0.8 && agentCount >= 4 && config.EnableTreeOfThoughts)
            return ReasoningPattern.TreeOfThoughts;
        
        if (complexity >= 0.6 && agentCount >= 3 && config.EnableChainOfThought)
            return ReasoningPattern.ChainOfThought;
        
        if (hasSpecificConcerns && agentCount >= 2 && config.EnableNestedChats)
            return ReasoningPattern.NestedChat;
        
        return ReasoningPattern.StandardCollaboration;
    }

    private double AnalyzeRequestComplexity(CodeReviewRequest request, RepositoryContext repositoryContext)
    {
        var complexity = 0.0;
        
        // Code size factor
        if (request.Content.Length > 5000) complexity += 0.2;
        if (request.Content.Length > 10000) complexity += 0.2;
        
        // Architecture complexity
        if (repositoryContext.Structure.ArchitecturePattern.Contains("Microservices")) complexity += 0.2;
        if (repositoryContext.Dependencies.Count > 20) complexity += 0.1;
        
        // Historical patterns complexity
        if (repositoryContext.HistoricalPatterns.Count > 10) complexity += 0.1;
        if (repositoryContext.HistoricalPatterns.Any(p => p.Similarity < 0.5)) complexity += 0.1;
        
        // Review depth
        if (request.Options.ReviewDepth == "comprehensive") complexity += 0.2;
        
        // Multiple agent types requested
        complexity += Math.Min(0.3, request.RequestedAgents.Count * 0.05);
        
        return Math.Min(1.0, complexity);
    }

    private AgentType SelectPrimaryAgentForChain(List<AgentType> agents)
    {
        // Prefer domain experts for chain initiation
        if (agents.Contains(AgentType.DomainExpert)) return AgentType.DomainExpert;
        if (agents.Contains(AgentType.ArchitectureExpert)) return AgentType.ArchitectureExpert;
        if (agents.Contains(AgentType.CodeQualityReviewer)) return AgentType.CodeQualityReviewer;
        
        return agents.FirstOrDefault();
    }

    private List<string> IdentifyNestedChatTopics(
        CodeReviewRequest request, 
        RepositoryContext repositoryContext, 
        AdvancedCollaborativeResult currentResult)
    {
        var topics = new List<string>();
        
        // Topics from standard collaboration
        if (currentResult.StandardCollaboration?.Consensus.DisputedFindings.Any() == true)
        {
            topics.Add("Resolving disputed findings from initial collaboration");
        }
        
        // Topics from Tree of Thoughts
        if (currentResult.TreeOfThoughts?.EvaluatedPaths.Any(p => p.Evaluation.Weaknesses.Any()) == true)
        {
            topics.Add("Addressing reasoning path weaknesses");
        }
        
        // Topics from Chain of Thought
        if (currentResult.ChainOfThought?.ReasoningChain.Any(s => !s.IsConclusive) == true)
        {
            topics.Add("Exploring inconclusive reasoning steps");
        }
        
        // Domain-specific topics
        if (repositoryContext.Structure.ArchitecturePattern == "Microservices")
        {
            topics.Add("Microservice-specific architectural concerns");
        }
        
        // Security-focused topic if security agent involved
        if (request.RequestedAgents.Contains(AgentType.SecurityExpert))
        {
            topics.Add("Deep security analysis and threat modeling");
        }
        
        return topics.Distinct().ToList();
    }

    private List<AgentType> SelectParticipantsForTopic(string topic, List<AgentType> availableAgents)
    {
        var participants = new List<AgentType>();
        
        if (topic.Contains("security", StringComparison.OrdinalIgnoreCase))
        {
            participants.AddRange(availableAgents.Where(a => 
                a == AgentType.SecurityExpert || a == AgentType.ArchitectureExpert));
        }
        else if (topic.Contains("performance", StringComparison.OrdinalIgnoreCase))
        {
            participants.AddRange(availableAgents.Where(a => 
                a == AgentType.PerformanceAnalyst || a == AgentType.ArchitectureExpert));
        }
        else if (topic.Contains("architecture", StringComparison.OrdinalIgnoreCase))
        {
            participants.AddRange(availableAgents.Where(a => 
                a == AgentType.ArchitectureExpert || a == AgentType.DomainExpert));
        }
        else
        {
            // Default: take first 3 available agents
            participants.AddRange(availableAgents.Take(3));
        }
        
        return participants.Any() ? participants : availableAgents.Take(2).ToList();
    }

    private Dictionary<string, object> BuildTopicContext(
        string topic, 
        CodeReviewRequest request, 
        RepositoryContext repositoryContext, 
        AdvancedCollaborativeResult currentResult)
    {
        return new Dictionary<string, object>
        {
            ["topic"] = topic,
            ["originalRequest"] = request,
            ["repositoryContext"] = repositoryContext,
            ["standardCollaboration"] = currentResult.StandardCollaboration,
            ["treeOfThoughts"] = currentResult.TreeOfThoughts,
            ["chainOfThought"] = currentResult.ChainOfThought,
            ["complexity"] = AnalyzeRequestComplexity(request, repositoryContext),
            ["timestamp"] = DateTime.UtcNow
        };
    }

    private async Task<Models.AgentExecutionResult[]> GenerateInitialAgentResults(
        CodeReviewRequest request, 
        RepositoryContext repositoryContext, 
        CancellationToken cancellationToken)
    {
        var results = new List<Models.AgentExecutionResult>();
        
        foreach (var agentType in request.RequestedAgents)
        {
            var result = new Models.AgentExecutionResult
            {
                AgentType = agentType,
                Success = true,
                ConfidenceScore = 0.8,
                KeyFindings = new List<string> { $"Initial {agentType} analysis findings" },
                Recommendations = new List<string> { $"Initial {agentType} recommendations" },
                ExecutionTime = TimeSpan.FromSeconds(2)
            };
            
            results.Add(result);
        }
        
        return results.ToArray();
    }

    // Quick Tree of Thoughts for hybrid reasoning
    private async Task<TreeOfThoughtsResult> ConductQuickTreeOfThoughtsAsync(
        CodeReviewRequest request,
        RepositoryContext repositoryContext,
        List<AgentType> agents,
        CancellationToken cancellationToken)
    {
        var result = new TreeOfThoughtsResult
        {
            RootProblem = $"Quick analysis for: {request.FileName}",
            StartTime = DateTime.UtcNow
        };

        // Generate initial thoughts (limited to 3 agents for speed)
        var initialThoughts = await GenerateInitialThoughts(request, repositoryContext, agents, cancellationToken);
        result.ThoughtTree.AddRange(initialThoughts);

        // Quick evaluation without deep expansion
        var evaluatedPaths = await EvaluateThoughtPaths(result.ThoughtTree, repositoryContext, cancellationToken);
        result.EvaluatedPaths = evaluatedPaths;

        if (evaluatedPaths.Any())
        {
            result.OptimalPath = evaluatedPaths.OrderByDescending(p => p.Evaluation.OverallScore).First();
        }

        result.EndTime = DateTime.UtcNow;
        result.Success = true;
        
        return result;
    }

    private async Task<ChainOfThoughtResult> ConductTargetedChainOfThoughtAsync(
        ThoughtPath bestBranch,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        // Create a focused chain of thought based on the best branch
        var result = new ChainOfThoughtResult
        {
            PrimaryAgent = bestBranch.RootThought.Agent,
            CollaboratingAgents = bestBranch.Nodes.Select(n => n.Agent).Distinct().Where(a => a != bestBranch.RootThought.Agent).ToList(),
            StartTime = DateTime.UtcNow
        };

        // Convert thought nodes to reasoning steps
        foreach (var node in bestBranch.Nodes.Take(5)) // Limit steps for efficiency
        {
            var reasoningStep = new ReasoningStep
            {
                StepNumber = result.ReasoningChain.Count + 1,
                Agent = node.Agent,
                Reasoning = node.Content,
                ConfidenceScore = node.ConfidenceScore,
                IsConclusive = node.ConfidenceScore > 0.8,
                Timestamp = node.Timestamp
            };
            
            result.ReasoningChain.Add(reasoningStep);
        }

        result.FinalReasoning = await SynthesizeChainOfThought(result.ReasoningChain, repositoryContext, cancellationToken);
        result.EndTime = DateTime.UtcNow;
        result.Success = true;

        return result;
    }

    private List<string> ExtractUnresolvedQuestions(ChainOfThoughtResult chainResult)
    {
        var questions = new List<string>();
        
        // Extract questions from reasoning steps that weren't conclusive
        var inconclusiveSteps = chainResult.ReasoningChain.Where(s => !s.IsConclusive && s.ConfidenceScore < 0.7);
        
        foreach (var step in inconclusiveSteps)
        {
            if (step.Reasoning.Contains("?") || step.Reasoning.Contains("unclear") || step.Reasoning.Contains("uncertain"))
            {
                questions.Add($"Clarify reasoning from {step.Agent} in step {step.StepNumber}");
            }
        }
        
        // Default questions if none extracted
        if (!questions.Any())
        {
            questions.Add("Validate final conclusions and identify any overlooked aspects");
        }
        
        return questions.Take(3).ToList();
    }

    private List<AgentType> SelectExpertsForQuestion(string question, List<AgentType> availableAgents)
    {
        if (question.Contains("security", StringComparison.OrdinalIgnoreCase))
            return availableAgents.Where(a => a == AgentType.SecurityExpert || a == AgentType.ArchitectureExpert).ToList();
        
        if (question.Contains("performance", StringComparison.OrdinalIgnoreCase))
            return availableAgents.Where(a => a == AgentType.PerformanceAnalyst || a == AgentType.ArchitectureExpert).ToList();
        
        return availableAgents.Take(2).ToList();
    }

    // Synthesis methods for advanced patterns

    private async Task<string> SynthesizeAdvancedResults(
        AdvancedCollaborativeResult result, 
        RepositoryContext repositoryContext, 
        CancellationToken cancellationToken)
    {
        var synthesisPrompt = BuildAdvancedSynthesisPrompt(result, repositoryContext);
        return await _aiService.GenerateReviewAsync(synthesisPrompt, cancellationToken);
    }

    private string BuildAdvancedSynthesisPrompt(AdvancedCollaborativeResult result, RepositoryContext context)
    {
        var components = new List<string>();
        
        if (result.StandardCollaboration != null)
            components.Add($"Standard Collaboration: {result.StandardCollaboration.CollaborationSummary}");
        
        if (result.TreeOfThoughts != null)
            components.Add($"Tree of Thoughts: {result.TreeOfThoughts.ThoughtTree.Count} thoughts evaluated");
        
        if (result.ChainOfThought != null)
            components.Add($"Chain of Thought: {result.ChainOfThought.ReasoningChain.Count} reasoning steps");
        
        if (result.NestedChats.Any())
            components.Add($"Nested Chats: {result.NestedChats.Count} focused discussions");

        return $@"
Synthesize the comprehensive multi-pattern collaborative analysis.

Components analyzed:
{string.Join("\n", components)}

Primary Pattern: {result.PrimaryPattern}
Duration: {result.TotalDuration.TotalMinutes:F1} minutes

Context:
- Project: {context.ProjectName}
- Architecture: {context.Structure.ArchitecturePattern}

Create a comprehensive synthesis that:
1. Integrates insights from all reasoning patterns
2. Highlights unique contributions of each approach
3. Identifies converging conclusions and conflicting views
4. Provides final actionable recommendations
5. Assesses the effectiveness of the multi-pattern approach

Focus on the added value of advanced collaborative reasoning.
";
    }

    private async Task<string> SynthesizeHybridResults(
        AdvancedCollaborativeResult result, 
        RepositoryContext repositoryContext, 
        CancellationToken cancellationToken)
    {
        var synthesisPrompt = BuildHybridSynthesisPrompt(result, repositoryContext);
        return await _aiService.GenerateReviewAsync(synthesisPrompt, cancellationToken);
    }

    private string BuildHybridSynthesisPrompt(AdvancedCollaborativeResult result, RepositoryContext context)
    {
        return $@"
Synthesize the hybrid reasoning analysis that combined multiple AI reasoning patterns.

Hybrid Analysis Components:
- Tree of Thoughts: {result.TreeOfThoughts?.ThoughtTree.Count ?? 0} initial thoughts explored
- Chain of Thought: {result.ChainOfThought?.ReasoningChain.Count ?? 0} reasoning steps developed
- Nested Chats: {result.NestedChats.Count} focused discussions conducted

Duration: {result.TotalDuration.TotalMinutes:F1} minutes

Context:
- Project: {context.ProjectName}
- Architecture: {context.Structure.ArchitecturePattern}

Provide a synthesis that:
1. Traces how insights evolved through different reasoning patterns
2. Shows how Tree of Thoughts informed Chain of Thought reasoning
3. Demonstrates how Nested Chats resolved specific uncertainties
4. Highlights the synergistic effect of pattern combination
5. Provides final conclusions with high confidence

Emphasize the unique value of the hybrid approach.
";
    }

    private double CalculateOverallConfidence(AdvancedCollaborativeResult result)
    {
        var confidences = new List<double>();
        
        if (result.StandardCollaboration != null)
            confidences.Add(result.StandardCollaboration.Consensus.OverallConfidence);
        
        if (result.TreeOfThoughts?.OptimalPath != null)
            confidences.Add(result.TreeOfThoughts.OptimalPath.Evaluation.OverallScore);
        
        if (result.ChainOfThought != null && result.ChainOfThought.ReasoningChain.Any())
            confidences.Add(result.ChainOfThought.ReasoningChain.Average(s => s.ConfidenceScore));
        
        if (result.NestedChats.Any())
            confidences.Add(result.NestedChats.Average(c => c.Conclusion?.FinalConfidence ?? 0.5));
        
        return confidences.Any() ? confidences.Average() : 0.5;
    }

    private double CalculateHybridConfidence(AdvancedCollaborativeResult result)
    {
        var baseConfidence = CalculateOverallConfidence(result);
        var hybridBonus = 0.1; // Bonus for using multiple patterns
        var convergenceBonus = result.NestedChats.Count > 0 ? 0.05 : 0.0;
        
        return Math.Min(1.0, baseConfidence + hybridBonus + convergenceBonus);
    }

    private List<string> ExtractFinalRecommendations(AdvancedCollaborativeResult result)
    {
        var recommendations = new List<string>();
        
        if (result.StandardCollaboration != null)
            recommendations.AddRange(result.StandardCollaboration.CollaborativeRecommendations);
        
        if (result.TreeOfThoughts != null)
            recommendations.AddRange(result.TreeOfThoughts.FinalRecommendations);
        
        if (result.ChainOfThought != null)
            recommendations.Add($"Chain of Thought conclusion: {result.ChainOfThought.FinalReasoning}");
        
        recommendations.AddRange(result.NestedChats
            .SelectMany(c => c.Conclusion?.ActionableOutcomes ?? new List<string>()));
        
        return recommendations.Distinct().Take(8).ToList();
    }

    private List<string> ExtractHybridRecommendations(AdvancedCollaborativeResult result)
    {
        var recommendations = ExtractFinalRecommendations(result);
        
        // Add hybrid-specific insights
        recommendations.Add("Leveraged multi-pattern reasoning for comprehensive analysis");
        if (result.TreeOfThoughts != null && result.ChainOfThought != null)
            recommendations.Add("Combined broad exploration with focused reasoning chains");
        
        return recommendations.Take(6).ToList();
    }

    private Dictionary<AgentType, double> CalculateAgentContributions(AdvancedCollaborativeResult result)
    {
        var contributions = new Dictionary<AgentType, double>();
        
        // Count contributions across all patterns
        var allAgents = new List<AgentType>();
        
        if (result.StandardCollaboration != null)
            allAgents.AddRange(result.StandardCollaboration.Conversation.Participants);
        
        if (result.TreeOfThoughts != null)
            allAgents.AddRange(result.TreeOfThoughts.ThoughtTree.Select(t => t.Agent));
        
        if (result.ChainOfThought != null)
            allAgents.AddRange(result.ChainOfThought.ReasoningChain.Select(r => r.Agent));
        
        allAgents.AddRange(result.NestedChats.SelectMany(c => c.Participants));
        
        foreach (var agent in allAgents.Distinct())
        {
            var count = allAgents.Count(a => a == agent);
            contributions[agent] = Math.Min(1.0, count / 10.0); // Normalize to 0-1
        }
        
        return contributions;
    }

    private Dictionary<string, object> CalculateAdvancedMetrics(AdvancedCollaborativeResult result)
    {
        return new Dictionary<string, object>
        {
            ["totalDurationMinutes"] = result.TotalDuration.TotalMinutes,
            ["patternsUsed"] = CountPatternsUsed(result),
            ["thoughtsGenerated"] = result.TreeOfThoughts?.ThoughtTree.Count ?? 0,
            ["reasoningSteps"] = result.ChainOfThought?.ReasoningChain.Count ?? 0,
            ["nestedChats"] = result.NestedChats.Count,
            ["uniqueAgents"] = CalculateAgentContributions(result).Count,
            ["collaborationComplexity"] = CalculateCollaborationComplexity(result),
            ["convergenceScore"] = CalculateConvergenceScore(result),
            ["reasoningDepth"] = CalculateReasoningDepth(result)
        };
    }

    private int CountPatternsUsed(AdvancedCollaborativeResult result)
    {
        var count = 0;
        if (result.StandardCollaboration != null) count++;
        if (result.TreeOfThoughts != null) count++;
        if (result.ChainOfThought != null) count++;
        if (result.NestedChats.Any()) count++;
        return count;
    }

    private double CalculateCollaborationComplexity(AdvancedCollaborativeResult result)
    {
        var complexity = 0.0;
        complexity += CountPatternsUsed(result) * 0.2;
        complexity += CalculateAgentContributions(result).Count * 0.1;
        complexity += result.NestedChats.Count * 0.1;
        complexity += (result.TreeOfThoughts?.ThoughtTree.Count ?? 0) * 0.01;
        return Math.Min(1.0, complexity);
    }

    private double CalculateConvergenceScore(AdvancedCollaborativeResult result)
    {
        // Measure how well different patterns converged on similar conclusions
        var recommendations = ExtractFinalRecommendations(result);
        var uniqueRecommendations = recommendations.Distinct().Count();
        var totalRecommendations = recommendations.Count;
        
        return totalRecommendations > 0 ? 1.0 - (double)uniqueRecommendations / totalRecommendations : 0.5;
    }

    private int CalculateReasoningDepth(AdvancedCollaborativeResult result)
    {
        var depth = 0;
        depth += result.TreeOfThoughts?.OptimalPath?.MaxDepth ?? 0;
        depth += result.ChainOfThought?.ReasoningChain.Count ?? 0;
        depth += result.NestedChats.Sum(c => c.Rounds.Count);
        return depth;
    }

    private Dictionary<AgentType, double> CalculateAgentConfidenceScores(AgentConversation conversation)
    {
        var scores = new Dictionary<AgentType, double>();
        
        foreach (var agent in conversation.Participants)
        {
            var agentMessages = conversation.Messages.Where(m => m.FromAgent == agent).ToList();
            var evidenceCount = agentMessages.Count(m => m.Type == MessageType.Evidence);
            var responseCount = agentMessages.Count(m => m.Type == MessageType.Response);
            
            // Simple scoring based on participation quality
            var score = 0.6 + (evidenceCount * 0.1) + (responseCount * 0.05);
            scores[agent] = Math.Min(1.0, score);
        }
        
        return scores;
    }

    /// <summary>
    /// Conduct Tree of Thoughts collaborative reasoning for complex code analysis
    /// </summary>
    public async Task<TreeOfThoughtsResult> ConductTreeOfThoughtsAnalysisAsync(
        CodeReviewRequest request,
        RepositoryContext repositoryContext,
        List<AgentType> agents,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Tree of Thoughts analysis with {AgentCount} agents", agents.Count);

        var totResult = new TreeOfThoughtsResult
        {
            RootProblem = $"Analyze code quality, security, and architecture for: {request.FileName}",
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Step 1: Generate initial thought branches
            var initialThoughts = await GenerateInitialThoughts(request, repositoryContext, agents, cancellationToken);
            totResult.ThoughtTree.AddRange(initialThoughts);

            // Step 2: Expand promising thoughts through collaborative reasoning
            var expandedThoughts = await ExpandThoughtsCollaboratively(initialThoughts, repositoryContext, cancellationToken);
            totResult.ThoughtTree.AddRange(expandedThoughts);

            // Step 3: Evaluate thought paths using agent collaboration
            var evaluatedPaths = await EvaluateThoughtPaths(totResult.ThoughtTree, repositoryContext, cancellationToken);
            totResult.EvaluatedPaths = evaluatedPaths;

            // Step 4: Select best reasoning path through consensus
            totResult.OptimalPath = await SelectOptimalPath(evaluatedPaths, repositoryContext, cancellationToken);

            // Step 5: Generate final recommendations from optimal path
            totResult.FinalRecommendations = await GenerateTreeOfThoughtsRecommendations(totResult.OptimalPath, repositoryContext, cancellationToken);

            totResult.EndTime = DateTime.UtcNow;
            totResult.Success = true;

            _logger.LogInformation("Tree of Thoughts analysis completed with {ThoughtCount} thoughts and {PathCount} evaluated paths",
                totResult.ThoughtTree.Count, totResult.EvaluatedPaths.Count);

            return totResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to conduct Tree of Thoughts analysis");
            totResult.Success = false;
            totResult.ErrorMessage = ex.Message;
            return totResult;
        }
    }

    /// <summary>
    /// Conduct Chain of Thought reasoning for step-by-step collaborative analysis
    /// </summary>
    public async Task<ChainOfThoughtResult> ConductChainOfThoughtAnalysisAsync(
        CodeReviewRequest request,
        RepositoryContext repositoryContext,
        AgentType primaryAgent,
        List<AgentType> collaboratingAgents,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Chain of Thought analysis with {PrimaryAgent} and {CollaboratorCount} collaborators",
            primaryAgent, collaboratingAgents.Count);

        var cotResult = new ChainOfThoughtResult
        {
            PrimaryAgent = primaryAgent,
            CollaboratingAgents = collaboratingAgents,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Step 1: Primary agent starts reasoning chain
            var initialThought = await InitiateReasoningChain(request, repositoryContext, primaryAgent, cancellationToken);
            cotResult.ReasoningChain.Add(initialThought);

            // Step 2: Collaborative reasoning with nested chats
            var chainLength = Math.Min(8, collaboratingAgents.Count * 2); // Limit chain length
            for (int step = 1; step < chainLength; step++)
            {
                var nextAgent = collaboratingAgents[(step - 1) % collaboratingAgents.Count];
                var previousThought = cotResult.ReasoningChain.Last();

                // Create nested chat for this reasoning step
                var nestedChat = await ConductNestedChatForReasoning(
                    previousThought, nextAgent, repositoryContext, cotResult.ReasoningChain, cancellationToken);

                cotResult.NestedChats.Add(nestedChat);

                // Generate next thought based on nested chat
                var nextThought = await GenerateNextThoughtFromNestedChat(
                    nestedChat, nextAgent, repositoryContext, cancellationToken);

                cotResult.ReasoningChain.Add(nextThought);

                // Check if reasoning has reached a conclusion
                if (nextThought.IsConclusive || nextThought.ConfidenceScore > 0.9)
                {
                    _logger.LogInformation("Chain of Thought reached conclusive result at step {Step}", step);
                    break;
                }

                await Task.Delay(100, cancellationToken); // Prevent API rate limiting
            }

            // Step 3: Synthesize final reasoning
            cotResult.FinalReasoning = await SynthesizeChainOfThought(cotResult.ReasoningChain, repositoryContext, cancellationToken);
            cotResult.EndTime = DateTime.UtcNow;
            cotResult.Success = true;

            _logger.LogInformation("Chain of Thought completed with {StepCount} reasoning steps and {ChatCount} nested chats",
                cotResult.ReasoningChain.Count, cotResult.NestedChats.Count);

            return cotResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to conduct Chain of Thought analysis");
            cotResult.Success = false;
            cotResult.ErrorMessage = ex.Message;
            return cotResult;
        }
    }

    /// <summary>
    /// Conduct nested chat session for focused collaborative reasoning
    /// </summary>
    public async Task<NestedChatSession> ConductNestedChatAsync(
        string topic,
        List<AgentType> participants,
        RepositoryContext repositoryContext,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting nested chat on topic: {Topic} with {ParticipantCount} agents", 
            topic, participants.Count);

        var session = new NestedChatSession
        {
            Topic = topic,
            Participants = participants,
            StartTime = DateTime.UtcNow,
            Context = context
        };

        try
        {
            // Phase 1: Topic introduction
            await IntroduceNestedChatTopic(session, repositoryContext, cancellationToken);

            // Phase 2: Focused discussion rounds
            var maxRounds = 4; // Limit to prevent infinite loops
            for (int round = 1; round <= maxRounds; round++)
            {
                _logger.LogDebug("Nested chat round {Round} for topic: {Topic}", round, topic);

                var roundResult = await ConductNestedChatRound(session, round, repositoryContext, cancellationToken);
                session.Rounds.Add(roundResult);

                // Check if consensus reached
                if (roundResult.ConsensusReached || roundResult.ConfidenceScore > 0.85)
                {
                    _logger.LogInformation("Nested chat reached consensus in round {Round}", round);
                    break;
                }

                await Task.Delay(200, cancellationToken);
            }

            // Phase 3: Synthesize nested chat conclusion
            session.Conclusion = await SynthesizeNestedChatConclusion(session, repositoryContext, cancellationToken);
            session.EndTime = DateTime.UtcNow;
            session.Success = true;

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to conduct nested chat session");
            session.Success = false;
            session.ErrorMessage = ex.Message;
            return session;
        }
    }

    // Tree of Thoughts implementation methods

    private async Task<List<ThoughtNode>> GenerateInitialThoughts(
        CodeReviewRequest request,
        RepositoryContext repositoryContext,
        List<AgentType> agents,
        CancellationToken cancellationToken)
    {
        var thoughts = new List<ThoughtNode>();

        foreach (var agent in agents)
        {
            var thoughtPrompt = BuildInitialThoughtPrompt(request, repositoryContext, agent);
            var thoughtContent = await _aiService.GenerateReviewAsync(thoughtPrompt, cancellationToken);

            var thought = new ThoughtNode
            {
                Id = Guid.NewGuid().ToString(),
                Agent = agent,
                Content = thoughtContent,
                Depth = 0,
                ConfidenceScore = ExtractConfidenceFromThought(thoughtContent),
                ParentId = null,
                Timestamp = DateTime.UtcNow
            };

            thoughts.Add(thought);
        }

        return thoughts;
    }

    private string BuildInitialThoughtPrompt(CodeReviewRequest request, RepositoryContext context, AgentType agent)
    {
        return $@"
You are a {agent} agent participating in Tree of Thoughts analysis.

Code to analyze:
```
{request.Content}
```

Repository Context:
- Project: {context.ProjectName}
- Architecture: {context.Structure.ArchitecturePattern}
- Dependencies: {string.Join(", ", context.Dependencies.Keys.Take(5))}

Generate an initial thought about this code from your {agent} perspective.
Consider multiple possible analysis approaches or angles.
Think step-by-step and explore different reasoning paths.

Format your response as:
**Thought**: [Your reasoning approach]
**Analysis**: [Detailed analysis]
**Confidence**: [0.0-1.0]
**Alternative Angles**: [Other ways to approach this]
";
    }

    private async Task<List<ThoughtNode>> ExpandThoughtsCollaboratively(
        List<ThoughtNode> initialThoughts,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        var expandedThoughts = new List<ThoughtNode>();

        // Select most promising thoughts to expand
        var promisingThoughts = initialThoughts
            .Where(t => t.ConfidenceScore > 0.6)
            .OrderByDescending(t => t.ConfidenceScore)
            .Take(3)
            .ToList();

        foreach (var parentThought in promisingThoughts)
        {
            // Each thought can be expanded by other agents
            var otherAgents = initialThoughts
                .Where(t => t.Agent != parentThought.Agent)
                .Select(t => t.Agent)
                .Take(2)
                .ToList();

            foreach (var expandingAgent in otherAgents)
            {
                var expansionPrompt = BuildThoughtExpansionPrompt(parentThought, expandingAgent, repositoryContext);
                var expansionContent = await _aiService.GenerateReviewAsync(expansionPrompt, cancellationToken);

                var expandedThought = new ThoughtNode
                {
                    Id = Guid.NewGuid().ToString(),
                    Agent = expandingAgent,
                    Content = expansionContent,
                    Depth = parentThought.Depth + 1,
                    ConfidenceScore = ExtractConfidenceFromThought(expansionContent),
                    ParentId = parentThought.Id,
                    Timestamp = DateTime.UtcNow
                };

                expandedThoughts.Add(expandedThought);
            }
        }

        return expandedThoughts;
    }

    private string BuildThoughtExpansionPrompt(ThoughtNode parentThought, AgentType expandingAgent, RepositoryContext context)
    {
        return $@"
You are a {expandingAgent} agent expanding on another agent's thought in Tree of Thoughts analysis.

Parent thought from {parentThought.Agent}:
{parentThought.Content}

Repository Context:
- Project: {context.ProjectName}
- Architecture: {context.Structure.ArchitecturePattern}
- Historical Patterns: {context.HistoricalPatterns.Count} available

As a {expandingAgent}, expand this thought by:
1. Building on their analysis from your perspective
2. Identifying aspects they might have missed
3. Exploring alternative reasoning paths
4. Adding depth to their conclusions

Provide a thoughtful expansion that adds value while respecting their insights.

Format your response as:
**Expansion**: [How you're building on their thought]
**Additional Analysis**: [Your unique perspective]
**Confidence**: [0.0-1.0]
**Synthesis**: [Combined insights]
";
    }

    private async Task<List<ThoughtPath>> EvaluateThoughtPaths(
        List<ThoughtNode> allThoughts,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        var paths = new List<ThoughtPath>();

        // Build paths from root thoughts to leaves
        var rootThoughts = allThoughts.Where(t => t.ParentId == null).ToList();

        foreach (var root in rootThoughts)
        {
            var path = BuildThoughtPath(root, allThoughts);
            var evaluation = await EvaluateThoughtPath(path, repositoryContext, cancellationToken);
            
            path.Evaluation = evaluation;
            paths.Add(path);
        }

        return paths;
    }

    private ThoughtPath BuildThoughtPath(ThoughtNode root, List<ThoughtNode> allThoughts)
    {
        var path = new ThoughtPath
        {
            Id = Guid.NewGuid().ToString(),
            RootThought = root,
            Nodes = new List<ThoughtNode> { root }
        };

        // Recursively build path
        BuildPathRecursively(root, allThoughts, path.Nodes);

        return path;
    }

    private void BuildPathRecursively(ThoughtNode current, List<ThoughtNode> allThoughts, List<ThoughtNode> pathNodes)
    {
        var children = allThoughts.Where(t => t.ParentId == current.Id).ToList();
        
        foreach (var child in children)
        {
            pathNodes.Add(child);
            BuildPathRecursively(child, allThoughts, pathNodes);
        }
    }

    private async Task<PathEvaluation> EvaluateThoughtPath(
        ThoughtPath path,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        var evaluationPrompt = BuildPathEvaluationPrompt(path, repositoryContext);
        var evaluationContent = await _aiService.GenerateReviewAsync(evaluationPrompt, cancellationToken);

        return new PathEvaluation
        {
            OverallScore = ExtractScoreFromEvaluation(evaluationContent),
            Reasoning = evaluationContent,
            Strengths = ExtractStrengthsFromEvaluation(evaluationContent),
            Weaknesses = ExtractWeaknessesFromEvaluation(evaluationContent),
            NodesCount = path.Nodes.Count,
            DepthScore = path.Nodes.Max(n => n.Depth),
            CollaborationScore = CalculateCollaborationScore(path)
        };
    }

    private string BuildPathEvaluationPrompt(ThoughtPath path, RepositoryContext context)
    {
        var pathSummary = string.Join("\n\n", path.Nodes.Select(n => 
            $"[{n.Agent}] Depth {n.Depth}: {n.Content.Substring(0, Math.Min(200, n.Content.Length))}..."));

        return $@"
Evaluate this reasoning path from Tree of Thoughts analysis:

Path Summary:
{pathSummary}

Context:
- Project: {context.ProjectName}
- Architecture: {context.Structure.ArchitecturePattern}
- Collaboration: {path.Nodes.Select(n => n.Agent).Distinct().Count()} different agents

Evaluate this path on:
1. **Logical Coherence**: Does the reasoning flow logically?
2. **Depth of Analysis**: How thoroughly is the problem explored?
3. **Collaboration Quality**: How well do agents build on each other's thoughts?
4. **Practical Value**: How actionable are the insights?
5. **Confidence**: How reliable are the conclusions?

Provide an overall score (0.0-1.0) and detailed reasoning.

Format:
**Score**: [0.0-1.0]
**Strengths**: [What works well]
**Weaknesses**: [Areas for improvement]
**Reasoning**: [Detailed evaluation]
";
    }

    // Chain of Thought implementation methods

    private async Task<ReasoningStep> InitiateReasoningChain(
        CodeReviewRequest request,
        RepositoryContext repositoryContext,
        AgentType primaryAgent,
        CancellationToken cancellationToken)
    {
        var initiationPrompt = BuildChainInitiationPrompt(request, repositoryContext, primaryAgent);
        var reasoningContent = await _aiService.GenerateReviewAsync(initiationPrompt, cancellationToken);

        return new ReasoningStep
        {
            StepNumber = 1,
            Agent = primaryAgent,
            Reasoning = reasoningContent,
            ConfidenceScore = ExtractConfidenceFromThought(reasoningContent),
            IsConclusive = CheckIfConclusive(reasoningContent),
            Timestamp = DateTime.UtcNow
        };
    }

    private string BuildChainInitiationPrompt(CodeReviewRequest request, RepositoryContext context, AgentType agent)
    {
        return $@"
You are a {agent} agent starting a Chain of Thought analysis.

Code to analyze:
```
{request.Content}
```

Context:
- Project: {context.ProjectName}
- Architecture: {context.Structure.ArchitecturePattern}
- Historical Patterns: {context.HistoricalPatterns.Count} available

Start the reasoning chain by:
1. Identifying the key questions to answer
2. Breaking down the analysis into logical steps
3. Beginning with your first analytical step
4. Setting up the reasoning for the next agent

Think step-by-step and show your reasoning process clearly.

Format:
**Step 1 Reasoning**: [Your analysis]
**Key Questions**: [What needs to be answered]
**Next Step Setup**: [What the next agent should focus on]
**Confidence**: [0.0-1.0]
**Is Conclusive**: [true/false]
";
    }

    private async Task<NestedChatSession> ConductNestedChatForReasoning(
        ReasoningStep previousStep,
        AgentType nextAgent,
        RepositoryContext repositoryContext,
        List<ReasoningStep> chainHistory,
        CancellationToken cancellationToken)
    {
        var topic = $"Reasoning step {previousStep.StepNumber + 1} - {nextAgent} analysis";
        var participants = new List<AgentType> { previousStep.Agent, nextAgent };

        // Add context from chain history
        var context = new Dictionary<string, object>
        {
            ["previousStep"] = previousStep,
            ["chainHistory"] = chainHistory,
            ["stepNumber"] = previousStep.StepNumber + 1
        };

        return await ConductNestedChatAsync(topic, participants, repositoryContext, context, cancellationToken);
    }

    private async Task<ReasoningStep> GenerateNextThoughtFromNestedChat(
        NestedChatSession nestedChat,
        AgentType agent,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        var thoughtPrompt = BuildThoughtFromChatPrompt(nestedChat, agent, repositoryContext);
        var thoughtContent = await _aiService.GenerateReviewAsync(thoughtPrompt, cancellationToken);

        return new ReasoningStep
        {
            StepNumber = (int)nestedChat.Context["stepNumber"],
            Agent = agent,
            Reasoning = thoughtContent,
            ConfidenceScore = ExtractConfidenceFromThought(thoughtContent),
            IsConclusive = CheckIfConclusive(thoughtContent),
            NestedChatId = nestedChat.Id,
            Timestamp = DateTime.UtcNow
        };
    }

    private string BuildThoughtFromChatPrompt(NestedChatSession chat, AgentType agent, RepositoryContext context)
    {
        var chatSummary = chat.Conclusion?.Summary ?? "Chat in progress";
        
        return $@"
Based on the nested chat discussion, generate the next reasoning step.

Chat Topic: {chat.Topic}
Chat Conclusion: {chatSummary}
Your Role: {agent}

Context:
- Project: {context.ProjectName}
- Step Number: {chat.Context["stepNumber"]}

Generate the next step in the reasoning chain:
1. Build on the nested chat insights
2. Advance the analysis forward
3. Address any questions raised
4. Prepare for the next reasoning step

Format:
**Reasoning**: [Your step-by-step analysis]
**Insights from Chat**: [Key points from discussion]
**Confidence**: [0.0-1.0]
**Is Conclusive**: [true/false]
**Next Focus**: [What should be analyzed next]
";
    }

    // Nested Chat implementation methods

    private async Task IntroduceNestedChatTopic(
        NestedChatSession session,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        var introPrompt = BuildTopicIntroductionPrompt(session, repositoryContext);
        var introduction = await _aiService.GenerateReviewAsync(introPrompt, cancellationToken);

        session.Introduction = introduction;
        session.Messages.Add(new ChatMessage
        {
            Agent = AgentType.DomainExpert, // Facilitator
            Content = introduction,
            MessageType = ChatMessageType.TopicIntroduction,
            Timestamp = DateTime.UtcNow
        });
    }

    private string BuildTopicIntroductionPrompt(NestedChatSession session, RepositoryContext context)
    {
        return $@"
Introduce the nested chat topic for focused agent collaboration.

Topic: {session.Topic}
Participants: {string.Join(", ", session.Participants)}
Context: {JsonSerializer.Serialize(session.Context)}

Project Context:
- Name: {context.ProjectName}
- Architecture: {context.Structure.ArchitecturePattern}

Provide a clear introduction that:
1. Explains the specific focus of this nested chat
2. Sets expectations for the discussion
3. Identifies key questions to address
4. Establishes the scope and goals

Keep it concise but comprehensive.
";
    }

    private async Task<ChatRound> ConductNestedChatRound(
        NestedChatSession session,
        int roundNumber,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        var round = new ChatRound
        {
            RoundNumber = roundNumber,
            StartTime = DateTime.UtcNow
        };

        // Each participant contributes to the round
        foreach (var participant in session.Participants)
        {
            var contributionPrompt = BuildRoundContributionPrompt(session, participant, roundNumber, repositoryContext);
            var contribution = await _aiService.GenerateReviewAsync(contributionPrompt, cancellationToken);

            var message = new ChatMessage
            {
                Agent = participant,
                Content = contribution,
                MessageType = ChatMessageType.RoundContribution,
                Round = roundNumber,
                Timestamp = DateTime.UtcNow
            };

            session.Messages.Add(message);
            round.Messages.Add(message);

            await Task.Delay(100, cancellationToken);
        }

        // Evaluate round consensus
        round.ConsensusReached = await EvaluateRoundConsensus(round, cancellationToken);
        round.ConfidenceScore = CalculateRoundConfidence(round);
        round.EndTime = DateTime.UtcNow;

        return round;
    }

    private string BuildRoundContributionPrompt(
        NestedChatSession session,
        AgentType participant,
        int roundNumber,
        RepositoryContext context)
    {
        var recentMessages = session.Messages.TakeLast(5)
            .Select(m => $"{m.Agent}: {m.Content.Substring(0, Math.Min(150, m.Content.Length))}...")
            .ToList();

        return $@"
You are {participant} participating in round {roundNumber} of a nested chat.

Topic: {session.Topic}
Recent discussion:
{string.Join("\n", recentMessages)}

Context:
- Project: {context.ProjectName}
- Round: {roundNumber}

Contribute to this round by:
1. Responding to previous points raised
2. Adding your perspective as a {participant}
3. Building toward consensus or identifying disagreements
4. Being concise but valuable

Focus on advancing the discussion constructively.
";
    }

    // Helper methods for scoring and evaluation

    private double ExtractConfidenceFromThought(string content)
    {
        var match = System.Text.RegularExpressions.Regex.Match(content, @"confidence[:\s]*(\d*\.?\d+)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && double.TryParse(match.Groups[1].Value, out var confidence))
        {
            return Math.Max(0.0, Math.Min(1.0, confidence));
        }
        return 0.7; // Default confidence
    }

    private bool CheckIfConclusive(string content)
    {
        var conclusiveIndicators = new[] { "conclusive", "final", "definitive", "certain", "confirmed" };
        return conclusiveIndicators.Any(indicator => 
            content.Contains(indicator, StringComparison.OrdinalIgnoreCase));
    }

    private double ExtractScoreFromEvaluation(string evaluation)
    {
        var match = System.Text.RegularExpressions.Regex.Match(evaluation, @"score[:\s]*(\d*\.?\d+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && double.TryParse(match.Groups[1].Value, out var score))
        {
            return Math.Max(0.0, Math.Min(1.0, score));
        }
        return 0.6; // Default score
    }

    private List<string> ExtractStrengthsFromEvaluation(string evaluation)
    {
        var strengthsMatch = System.Text.RegularExpressions.Regex.Match(evaluation, 
            @"strengths[:\s]*([^*]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (strengthsMatch.Success)
        {
            return strengthsMatch.Groups[1].Value.Split('\n')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().TrimStart('-').Trim())
                .Take(3)
                .ToList();
        }
        return new List<string>();
    }

    private List<string> ExtractWeaknessesFromEvaluation(string evaluation)
    {
        var weaknessesMatch = System.Text.RegularExpressions.Regex.Match(evaluation,
            @"weaknesses[:\s]*([^*]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (weaknessesMatch.Success)
        {
            return weaknessesMatch.Groups[1].Value.Split('\n')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().TrimStart('-').Trim())
                .Take(3)
                .ToList();
        }
        return new List<string>();
    }

    private double CalculateCollaborationScore(ThoughtPath path)
    {
        var uniqueAgents = path.Nodes.Select(n => n.Agent).Distinct().Count();
        var totalNodes = path.Nodes.Count;
        var depthScore = path.Nodes.Max(n => n.Depth) / 5.0; // Normalize depth

        return Math.Min(1.0, (uniqueAgents / 5.0) + (totalNodes / 10.0) + depthScore);
    }

    private async Task<bool> EvaluateRoundConsensus(ChatRound round, CancellationToken cancellationToken)
    {
        // Simple consensus detection - could be enhanced with sentiment analysis
        var messages = round.Messages.Select(m => m.Content).ToList();
        var agreementWords = new[] { "agree", "yes", "correct", "exactly", "consensus" };
        var disagreementWords = new[] { "disagree", "no", "wrong", "however", "but" };

        var agreementCount = messages.Sum(m => agreementWords.Count(w => 
            m.Contains(w, StringComparison.OrdinalIgnoreCase)));
        var disagreementCount = messages.Sum(m => disagreementWords.Count(w =>
            m.Contains(w, StringComparison.OrdinalIgnoreCase)));

        return agreementCount > disagreementCount && agreementCount > messages.Count / 2;
    }

    private double CalculateRoundConfidence(ChatRound round)
    {
        var messageCount = round.Messages.Count;
        var baseConfidence = 0.5;
        var participationBonus = Math.Min(0.3, messageCount * 0.1);
        var consensusBonus = round.ConsensusReached ? 0.2 : 0.0;

        return Math.Min(1.0, baseConfidence + participationBonus + consensusBonus);
    }

    private async Task<ThoughtPath> SelectOptimalPath(
        List<ThoughtPath> evaluatedPaths,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        return evaluatedPaths
            .OrderByDescending(p => p.Evaluation.OverallScore)
            .ThenByDescending(p => p.Evaluation.CollaborationScore)
            .First();
    }

    private async Task<List<string>> GenerateTreeOfThoughtsRecommendations(
        ThoughtPath optimalPath,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        var recommendationPrompt = BuildTreeRecommendationPrompt(optimalPath, repositoryContext);
        var recommendations = await _aiService.GenerateReviewAsync(recommendationPrompt, cancellationToken);

        return recommendations.Split('\n')
            .Where(line => line.Trim().StartsWith("-"))
            .Select(line => line.Trim().TrimStart('-').Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(5)
            .ToList();
    }

    private string BuildTreeRecommendationPrompt(ThoughtPath path, RepositoryContext context)
    {
        var pathSummary = string.Join("\n", path.Nodes.Select(n =>
            $"{n.Agent}: {n.Content.Substring(0, Math.Min(200, n.Content.Length))}..."));

        return $@"
Generate final recommendations from the optimal Tree of Thoughts reasoning path.

Optimal Path Analysis:
{pathSummary}

Evaluation Score: {path.Evaluation.OverallScore:F2}
Collaboration Score: {path.Evaluation.CollaborationScore:F2}

Context:
- Project: {context.ProjectName}
- Architecture: {context.Structure.ArchitecturePattern}

Synthesize the collaborative reasoning into actionable recommendations:
1. Focus on the most important insights
2. Provide specific, implementable actions
3. Prioritize based on impact and confidence
4. Acknowledge the collaborative reasoning process

Format as a bulleted list of clear recommendations.
";
    }

    private async Task<string> SynthesizeChainOfThought(
        List<ReasoningStep> reasoningChain,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        var synthesisPrompt = BuildChainSynthesisPrompt(reasoningChain, repositoryContext);
        return await _aiService.GenerateReviewAsync(synthesisPrompt, cancellationToken);
    }

    private string BuildChainSynthesisPrompt(List<ReasoningStep> chain, RepositoryContext context)
    {
        var chainSummary = string.Join("\n\n", chain.Select(step =>
            $"Step {step.StepNumber} ({step.Agent}): {step.Reasoning}"));

        return $@"
Synthesize the Chain of Thought reasoning into final conclusions.

Reasoning Chain:
{chainSummary}

Context:
- Project: {context.ProjectName}
- Total Steps: {chain.Count}
- Final Confidence: {chain.Last().ConfidenceScore:F2}

Provide a comprehensive synthesis that:
1. Traces the logical progression
2. Highlights key insights from each step
3. Acknowledges collaborative elements
4. Draws final conclusions
5. Identifies areas of high confidence vs. uncertainty

Create a coherent narrative of the reasoning process and its outcomes.
";
    }

    private async Task<ChatConclusion> SynthesizeNestedChatConclusion(
        NestedChatSession session,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken)
    {
        var conclusionPrompt = BuildChatConclusionPrompt(session, repositoryContext);
        var conclusionContent = await _aiService.GenerateReviewAsync(conclusionPrompt, cancellationToken);

        return new ChatConclusion
        {
            Summary = conclusionContent,
            ParticipantCount = session.Participants.Count,
            MessageCount = session.Messages.Count,
            RoundCount = session.Rounds.Count,
            ConsensusReached = session.Rounds.LastOrDefault()?.ConsensusReached ?? false,
            FinalConfidence = session.Rounds.LastOrDefault()?.ConfidenceScore ?? 0.5,
            Timestamp = DateTime.UtcNow
        };
    }

    private string BuildChatConclusionPrompt(NestedChatSession session, RepositoryContext context)
    {
        var discussionSummary = string.Join("\n", session.Messages.TakeLast(8).Select(m =>
            $"{m.Agent}: {m.Content.Substring(0, Math.Min(150, m.Content.Length))}..."));

        return $@"
Synthesize the conclusion of this nested chat session.

Topic: {session.Topic}
Participants: {string.Join(", ", session.Participants)}
Rounds: {session.Rounds.Count}

Recent Discussion:
{discussionSummary}

Context:
- Project: {context.ProjectName}
- Message Count: {session.Messages.Count}

Provide a comprehensive conclusion that:
1. Summarizes key points of agreement
2. Identifies any remaining disagreements
3. Highlights the most valuable insights
4. Assesses the quality of collaboration
5. Draws actionable conclusions

Focus on the value generated through this focused discussion.
";
    }
}