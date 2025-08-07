using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

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

    public SemanticKernelService(GroqConfig config)
    {
        var builder = Kernel.CreateBuilder();

        builder.AddOpenAIChatCompletion(
            modelId: config.ModelName,
            apiKey: config.ApiKey,
            endpoint: new Uri(config.Endpoint)
        );

        _kernel = builder.Build();
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