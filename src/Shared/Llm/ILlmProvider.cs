namespace Shared.Llm;

public interface ILlmProvider
{
    Task<LlmResponse> SendMessageAsync(
        List<LlmMessage> messages,
        List<LlmTool>? tools = null,
        string? systemPrompt = null,
        LlmOptions? options = null,
        CancellationToken ct = default);
}

public sealed record LlmMessage(
    string Role,
    object Content
);

public sealed record LlmTool(
    string Name,
    string Description,
    object Parameters
);

public sealed record LlmResponse(
    string Id,
    List<LlmContentBlock> Content,
    string? StopReason,
    LlmUsage Usage
);

public sealed record LlmContentBlock(
    string Type,
    string? Text = null,
    string? ToolUseId = null,
    string? ToolName = null,
    Dictionary<string, object>? ToolInput = null
);

public sealed record LlmUsage(
    int InputTokens,
    int OutputTokens
);

public sealed record LlmOptions(
    int MaxTokens = 4096,
    double Temperature = 1.0,
    string? Model = null
);
