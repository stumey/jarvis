using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Serialization;

namespace Shared.Llm.Providers;

public sealed class OpenAIProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _defaultModel;

    public OpenAIProvider(HttpClient httpClient, string defaultModel = "gpt-4o")
    {
        _httpClient = httpClient;
        _defaultModel = defaultModel;
    }

    public async Task<LlmResponse> SendMessageAsync(
        List<LlmMessage> messages,
        List<LlmTool>? tools = null,
        string? systemPrompt = null,
        LlmOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new LlmOptions();

        var openAiMessages = new List<object>();

        if (!string.IsNullOrEmpty(systemPrompt))
            openAiMessages.Add(new { role = "system", content = systemPrompt });

        openAiMessages.AddRange(messages.Select(m => new { role = m.Role, content = m.Content }));

        var openAiTools = tools?.Select(t => new
        {
            type = "function",
            function = new
            {
                name = t.Name,
                description = t.Description,
                parameters = t.Parameters
            }
        }).ToList();

        var request = new
        {
            model = options.Model ?? _defaultModel,
            messages = openAiMessages,
            max_tokens = options.MaxTokens,
            temperature = options.Temperature,
            tools = openAiTools
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "https://api.openai.com/v1/chat/completions",
                request,
                JsonOptions.Default,
                ct);

            response.EnsureSuccessStatusCode();

            var openAiResponse = await response.Content.ReadFromJsonAsync<OpenAIApiResponse>(JsonOptions.Default, ct)
                ?? throw new InvalidOperationException("No response from OpenAI");

            var choice = openAiResponse.Choices.FirstOrDefault()
                ?? throw new InvalidOperationException("No choices in OpenAI response");

            var contentBlocks = new List<LlmContentBlock>();

            if (!string.IsNullOrEmpty(choice.Message.Content))
                contentBlocks.Add(new LlmContentBlock("text", Text: choice.Message.Content));

            if (choice.Message.ToolCalls != null)
            {
                foreach (var toolCall in choice.Message.ToolCalls)
                {
                    var input = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        toolCall.Function.Arguments,
                        JsonOptions.Default) ?? new();

                    contentBlocks.Add(new LlmContentBlock(
                        "tool_use",
                        ToolUseId: toolCall.Id,
                        ToolName: toolCall.Function.Name,
                        ToolInput: input
                    ));
                }
            }

            var stopReason = choice.FinishReason switch
            {
                "stop" => "end_turn",
                "tool_calls" => "tool_use",
                _ => choice.FinishReason
            };

            return new LlmResponse(
                openAiResponse.Id,
                contentBlocks,
                stopReason,
                new LlmUsage(openAiResponse.Usage.PromptTokens, openAiResponse.Usage.CompletionTokens)
            );
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"OpenAI API request failed: {ex.Message}", ex);
        }
    }

    private sealed record OpenAIApiResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("choices")] List<OpenAIChoice> Choices,
        [property: JsonPropertyName("usage")] OpenAIUsage Usage
    );

    private sealed record OpenAIChoice(
        [property: JsonPropertyName("message")] OpenAIMessage Message,
        [property: JsonPropertyName("finish_reason")] string FinishReason
    );

    private sealed record OpenAIMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string? Content = null,
        [property: JsonPropertyName("tool_calls")] List<OpenAIToolCall>? ToolCalls = null
    );

    private sealed record OpenAIToolCall(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("function")] OpenAIFunction Function
    );

    private sealed record OpenAIFunction(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("arguments")] string Arguments
    );

    private sealed record OpenAIUsage(
        [property: JsonPropertyName("prompt_tokens")] int PromptTokens,
        [property: JsonPropertyName("completion_tokens")] int CompletionTokens
    );
}
