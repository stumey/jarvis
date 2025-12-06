using Microsoft.Extensions.Configuration;
using Shared.Llm.Providers;

namespace Shared.Llm;

public sealed class LlmProviderFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public LlmProviderFactory(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public ILlmProvider CreateProvider()
    {
        var provider = _configuration["Llm:Provider"]
            ?? throw new InvalidOperationException("Llm:Provider configuration is required");

        return provider.ToLowerInvariant() switch
        {
            "claude" => CreateClaudeProvider(),
            "openai" => CreateOpenAIProvider(),
            "bedrock" => throw new NotImplementedException("Bedrock provider not yet implemented"),
            _ => throw new InvalidOperationException($"Unknown LLM provider: {provider}")
        };
    }

    private ILlmProvider CreateClaudeProvider()
    {
        var apiKey = _configuration["Llm:Claude:ApiKey"]
            ?? throw new InvalidOperationException("Claude API key is required");

        var httpClient = _httpClientFactory.CreateClient("Claude");
        httpClient.BaseAddress = new Uri("https://api.anthropic.com");
        httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var model = _configuration["Llm:Claude:Model"] ?? "claude-3-5-sonnet-20241022";
        return new ClaudeProvider(httpClient, model);
    }

    private ILlmProvider CreateOpenAIProvider()
    {
        var apiKey = _configuration["Llm:OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI API key is required");

        var httpClient = _httpClientFactory.CreateClient("OpenAI");
        httpClient.BaseAddress = new Uri("https://api.openai.com");
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var model = _configuration["Llm:OpenAI:Model"] ?? "gpt-4o";
        return new OpenAIProvider(httpClient, model);
    }
}
