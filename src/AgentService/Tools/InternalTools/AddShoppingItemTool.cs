using System.Text.Json;

namespace AgentService.Tools.InternalTools;

public sealed class AddShoppingItemTool : ITool
{
    private readonly HttpClient _httpClient;
    private readonly Guid _defaultListId;

    public string Name => "add_shopping_item";
    public string Description => "Add an item to the shopping list";
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            item = new
            {
                type = "string",
                description = "The item to add to the shopping list"
            },
            quantity = new
            {
                type = "string",
                description = "Optional quantity (e.g., '2', '1 lb', '500g')"
            },
            notes = new
            {
                type = "string",
                description = "Optional notes about the item"
            }
        },
        required = new[] { "item" }
    };

    public AddShoppingItemTool(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient("TaskService");
        _defaultListId = Guid.Parse(config["DefaultShoppingListId"] ?? Guid.NewGuid().ToString());
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object> input, CancellationToken ct = default)
    {
        var item = input["item"].ToString()!;
        var quantity = input.GetValueOrDefault("quantity")?.ToString();
        var notes = input.GetValueOrDefault("notes")?.ToString();

        var queryParams = $"?name={Uri.EscapeDataString(item)}";
        if (!string.IsNullOrEmpty(quantity))
            queryParams += $"&quantity={Uri.EscapeDataString(quantity)}";
        if (!string.IsNullOrEmpty(notes))
            queryParams += $"&notes={Uri.EscapeDataString(notes)}";

        var response = await _httpClient.PostAsync($"/lists/{_defaultListId}/items{queryParams}", null, ct);
        response.EnsureSuccessStatusCode();

        return $"Added '{item}' to shopping list";
    }
}
