using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Anthropic.SDK.Constants;

namespace Mcp.CodeReview.Services;

public class ClaudeService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<ClaudeService> _logger;

    public ClaudeService(ILogger<ClaudeService> logger)
    {
        _logger = logger;
        var apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");
        
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("CLAUDE_API_KEY environment variable is required");
        
        _client = new AnthropicClient(new APIAuthentication(apiKey));
    }

    public async Task<string> GenerateReview(string prompt)
    {
        return await GenerateReviewWithParameters(prompt, temperature: 0.3, maxTokens: 2000);
    }

    public async Task<string> GenerateReviewWithParameters(string prompt, double temperature = 0.3, int maxTokens = 2000, string model = null)
    {
        try
        {
            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = prompt
                }
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                Model = model ?? "claude-3-sonnet-20240229",
                MaxTokens = maxTokens,
                Temperature = (decimal)temperature
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            
            return response.Content.FirstOrDefault()?.Text ?? "No response generated";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Claude review with parameters. Temperature: {Temperature}, MaxTokens: {MaxTokens}", temperature, maxTokens);
            throw new InvalidOperationException($"Claude API error: {ex.Message}");
        }
    }

    public async Task<bool> TestConnection()
    {
        try
        {
            var testPrompt = "Hello, this is a connection test. Please respond with 'OK'.";
            var response = await GenerateReview(testPrompt);
            var isConnected = !string.IsNullOrEmpty(response);
            
            if (isConnected)
                _logger.LogInformation("Claude connection test successful");
            else
                _logger.LogWarning("Claude connection test failed: Empty response");
                
            return isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Claude connection test failed: {Message}", ex.Message);
            return false;
        }
    }
}