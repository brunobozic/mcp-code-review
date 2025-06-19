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
}