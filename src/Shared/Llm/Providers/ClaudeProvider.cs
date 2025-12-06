using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Shared.Serialization;

namespace Shared.Llm.Providers;

public sealed class ClaudeProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _defaultModel;

    public ClaudeProvider(HttpClient httpClient, string defaultModel = "claude-3-5-sonnet-20241022")
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

        var claudeTools = tools?.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            input_schema = t.Parameters
        }).ToList();

        var request = new
        {
            model = options.Model ?? _defaultModel,
            max_tokens = options.MaxTokens,
            temperature = options.Temperature,
            system = systemPrompt,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
            tools = claudeTools
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "https://api.anthropic.com/v1/messages",
                request,
                JsonOptions.Default,
                ct);

            response.EnsureSuccessStatusCode();

            var claudeResponse = await response.Content.ReadFromJsonAsync<ClaudeApiResponse>(JsonOptions.Default, ct)
                ?? throw new InvalidOperationException("No response from Claude");

            return new LlmResponse(
                claudeResponse.Id,
                claudeResponse.Content.Select(c => new LlmContentBlock(
                    c.Type,
                    c.Text,
                    c.Id,
                    c.Name,
                    c.Input
                )).ToList(),
                claudeResponse.StopReason,
                new LlmUsage(claudeResponse.Usage.InputTokens, claudeResponse.Usage.OutputTokens)
            );
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Claude API request failed: {ex.Message}", ex);
        }
    }

    private sealed record ClaudeApiResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("content")] List<ClaudeContentBlock> Content,
        [property: JsonPropertyName("stop_reason")] string? StopReason,
        [property: JsonPropertyName("usage")] ClaudeUsage Usage
    );

    private sealed record ClaudeContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text = null,
        [property: JsonPropertyName("id")] string? Id = null,
        [property: JsonPropertyName("name")] string? Name = null,
        [property: JsonPropertyName("input")] Dictionary<string, object>? Input = null
    );

    private sealed record ClaudeUsage(
        [property: JsonPropertyName("input_tokens")] int InputTokens,
        [property: JsonPropertyName("output_tokens")] int OutputTokens
    );
}
