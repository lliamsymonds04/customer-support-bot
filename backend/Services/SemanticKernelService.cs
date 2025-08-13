using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SupportBot.Skills;
using SupportBot.Services;
using SupportBot.Models;

public class GroqConfig
{
    public required string ApiKey { get; set; }
    public required string ModelName { get; set; }
    public required string Endpoint { get; set; }
}

public interface ISemanticKernelService
{
    Task<string> RunPromptAsync(string prompt);
    Task<string> ChatWithAgentAsync(string message, string sessionId);
    Task ClearSessionAsync(string sessionId);
}

public class SemanticKernelService : ISemanticKernelService
{
    private readonly Kernel _kernel;
    private readonly ChatCompletionAgent _agent;
    private readonly ISessionManager _sessionManager;

    public SemanticKernelService(GroqConfig config, LogFormSkill skill, ISessionManager sessionManager)
    {
        var builder = Kernel.CreateBuilder();

        builder.AddOpenAIChatCompletion(
            modelId: config.ModelName,
            apiKey: config.ApiKey,
            endpoint: new Uri(config.Endpoint)
        );

        _kernel = builder.Build();

        // Add the skills
        _kernel.Plugins.AddFromObject(skill);

        _agent = new ChatCompletionAgent()
        {
            Instructions = GetInstructions(),
            Kernel = _kernel,
            Name = "SupportAgent",
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
        };
        _sessionManager = sessionManager;
    }

    private string GetInstructions()
    {
        var instructions = """
            You are a helpful customer support assistant. Your role is to:
            
            1. Help customers with their questions and issues
            2. When customers have an issue, request or feedback, always use the LogForm function to create a ticket
            3. Classify issues into appropriate categories 
            4. Assess urgency levels 
            5. Be friendly, professional, and helpful
            6. If the customer asks a question which is not relevant to your role, don't respond and tell them your role.
            """;

        //join the relevant enums onto the instructions
        foreach (var category in Enum.GetValues(typeof(FormCategory)))
        {
            instructions += $"\n- Category values: {category}";
        }

        foreach (var urgency in Enum.GetValues(typeof(FormUrgency)))
        {
            instructions += $"\n- Urgency values: {urgency}";
        }

        return instructions;

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