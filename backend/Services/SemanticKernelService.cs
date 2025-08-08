using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using SupportBot.Skills;
using SupportBot.Services;

public class GroqConfig
{
    public required string ApiKey { get; set; }
    public required string ModelName { get; set; }
    public required string Endpoint { get; set; }
}

public interface ISemanticKernelService
{
    Task<string> RunPromptAsync(string prompt);
}

public class SemanticKernelService : ISemanticKernelService
{
    private readonly Kernel _kernel;
    private readonly ChatCompletionAgent _agent;
    private readonly ISessionManager _sessionManager;

    public SemanticKernelService(GroqConfig config, IServiceProvider serviceProvider, ISessionManager sessionManager)
    {
        var builder = Kernel.CreateBuilder();

        builder.AddOpenAIChatCompletion(
            modelId: config.ModelName,
            apiKey: config.ApiKey,
            endpoint: new Uri(config.Endpoint)
        );

        _kernel = builder.Build();

        // Add the skills
        _kernel.Plugins.AddFromObject(serviceProvider.GetRequiredService<LogFormSkill>());
        _sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

        _agent = new ChatCompletionAgent()
        {
            Instructions = """
                You are a helpful customer support assistant. Your role is to:
                
                1. Help customers with their questions and issues
                2. When customers want to submit a support request, use the LogForm function to create a ticket
                3. Classify issues into appropriate categories (Technical, Billing, General, Account)
                4. Assess urgency levels (Low, Medium, High, Critical)
                5. Be friendly, professional, and helpful
                """,
            Kernel = _kernel,
            Name = "SupportAgent"
        };
    }

    public async Task<string> RunPromptAsync(string prompt)
    {
        var result = await _kernel.InvokePromptAsync(prompt);

        if (result is null)
        {
            throw new Exception("Chat completion returned null result.");
        }

        return result.ToString()!;
    }

    public async Task<string> ChatWithAgentAsync(string message, string sessionId)
    {
        var session = await _sessionManager.GetOrCreateSessionAsync(sessionId);
        session.ChatHistory.AddUserMessage(message);

        var allResponses = new List<string>();

        await foreach (var item in _agent.InvokeAsync(session.ChatHistory))
        {
            if (item.Message is ChatMessageContent chatContent)
            {
                if (item.Message.Role == AuthorRole.Assistant)
                {
                    allResponses.Add(chatContent.Content ?? string.Empty);
                    session.ChatHistory.AddAssistantMessage(chatContent.Content ?? string.Empty);
                }
                else if (item.Message.Role == AuthorRole.User)
                {
                    session.ChatHistory.AddUserMessage(chatContent.Content ?? string.Empty);
                }
                else if (item.Message.Role == AuthorRole.Tool)
                {
                    // Handle tool responses (e.g., LogForm)
                    if (chatContent.Content != null)
                    {
                        allResponses.Add($"Tool response: {chatContent.Content}");
                        session.ChatHistory.AddAssistantMessage(chatContent.Content);
                    }
                }
            }
            
        }

        if (allResponses.Count == 0)
        {
            throw new Exception("Chat agent returned no assistant response.");
        }

        await _sessionManager.UpdateSessionAsync(session);

        return string.Join("\n", allResponses);
    }

    public async Task ClearSessionAsync(string sessionId)
    {
        await _sessionManager.RemoveSessionAsync(sessionId);
    }
}