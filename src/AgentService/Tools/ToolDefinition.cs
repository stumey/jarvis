using System.Text.Json.Serialization;

namespace AgentService.Tools;

public sealed record ToolDefinition(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("input_schema")] object InputSchema
);

public sealed record ToolUse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("input")] Dictionary<string, object> Input
);

public sealed record ToolResult(
    [property: JsonPropertyName("tool_use_id")] string ToolUseId,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("is_error")] bool? IsError = null
);
