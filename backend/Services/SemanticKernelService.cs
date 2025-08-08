using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
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

    public SemanticKernelService(GroqConfig config, IServiceProvider serviceProvider, ISessionManager sessionManager )
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

}