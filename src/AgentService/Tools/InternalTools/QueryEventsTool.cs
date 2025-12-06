using System.Text.Json;

namespace AgentService.Tools.InternalTools;

public sealed class QueryEventsTool : ITool
{
    private readonly HttpClient _httpClient;

    public string Name => "query_events";
    public string Description => "Search and retrieve events from memory based on filters like source, type, or text search";
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            query = new
            {
                type = "string",
                description = "Full-text search query to find relevant events"
            },
            source = new
            {
                type = "string",
                description = "Filter by event source (email, calendar, bank, etc.)"
            },
            type = new
            {
                type = "string",
                description = "Filter by event type (email_received, payment_due, etc.)"
            },
            limit = new
            {
                type = "number",
                description = "Maximum number of results to return (default 20)"
            }
        }
    };

    public QueryEventsTool(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("MemoryService");
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> input, CancellationToken ct = default)
    {
        var query = input.GetValueOrDefault("query")?.ToString();
        var source = input.GetValueOrDefault("source")?.ToString();
        var type = input.GetValueOrDefault("type")?.ToString();
        var limit = input.GetValueOrDefault("limit")?.ToString() ?? "20";

        string url;
        if (!string.IsNullOrEmpty(query))
            url = $"/memory/search?q={Uri.EscapeDataString(query)}&limit={limit}";
        else if (!string.IsNullOrEmpty(source))
            url = $"/memory/by-source/{Uri.EscapeDataString(source)}?limit={limit}";
        else if (!string.IsNullOrEmpty(type))
            url = $"/memory/by-type/{Uri.EscapeDataString(type)}?limit={limit}";
        else
            url = $"/memory/recent?limit={limit}";

        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        return json;
    }
}
