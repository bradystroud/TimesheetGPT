using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using TimesheetGPT.Core.Interfaces;

namespace TimesheetGPT.Core.Services;

public class SemKerAiService : IAiService
{
    private readonly string _apiKey;
    public SemKerAiService(IConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
        
        _apiKey = configuration["OpenAI:ApiKey"] ?? "";
    }

    public async Task<string> GetSummary(string text, string extraPrompts)
    {
        Console.WriteLine(text);
        var builder = new KernelBuilder();

        // builder.WithAzureChatCompletionService(
        //     "gpt-4-turbo", // Azure OpenAI Deployment Name
        //     "https://contoso.openai.azure.com/", // Azure OpenAI Endpoint
        //     "...your Azure OpenAI Key..."); // Azure OpenAI Key

        builder.WithOpenAIChatCompletionService(
            // "gpt-3.5-turbo", // Cheap mode
            "gpt-4", // 💸
            _apiKey);

        var kernel = builder.Build();
        
        var summarizeFunction = kernel.CreateSemanticFunction(Prompts.SummarizeEmailsAndCalendar, maxTokens: 400, temperature: 0, topP: 0.5);
        
        var context = kernel.CreateNewContext();

        context.Variables.TryAdd(PromptVariables.Input, text);
        context.Variables.TryAdd(PromptVariables.AdditionalNotes, text);
        context.Variables.TryAdd(PromptVariables.ExtraPrompts, extraPrompts);

        var summary = await summarizeFunction.InvokeAsync(context);

        Console.WriteLine(summary.ModelResults);

        return summary.Result;
    }
}
