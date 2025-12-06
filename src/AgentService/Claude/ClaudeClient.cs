using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentService.Tools;
using Shared.Serialization;

namespace AgentService.Claude;

public sealed class ClaudeClient
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public ClaudeClient(HttpClient httpClient, string model = "claude-3-5-sonnet-20241022")
    {
        _httpClient = httpClient;
        _model = model;
    }

    public async Task<ClaudeResponse> SendMessageAsync(
        List<ClaudeMessage> messages,
        List<ToolDefinition>? tools = null,
        string? system = null,
        int maxTokens = 4096,
        CancellationToken ct = default)
    {
        var request = new
        {
            model = _model,
            max_tokens = maxTokens,
            system,
            messages,
            tools
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "https://api.anthropic.com/v1/messages",
                request,
                JsonOptions.Default,
                ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ClaudeResponse>(JsonOptions.Default, ct);
            return result ?? throw new InvalidOperationException("No response from Claude");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to call Claude API: {ex.Message}", ex);
        }
    }
}

public sealed record ClaudeMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] object Content
);

public sealed record ClaudeResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] List<ContentBlock> Content,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("stop_reason")] string? StopReason,
    [property: JsonPropertyName("usage")] Usage Usage
);

public sealed record ContentBlock(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string? Text = null,
    [property: JsonPropertyName("id")] string? Id = null,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("input")] Dictionary<string, object>? Input = null
);

public sealed record Usage(
    [property: JsonPropertyName("input_tokens")] int InputTokens,
    [property: JsonPropertyName("output_tokens")] int OutputTokens
);
