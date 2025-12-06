using AgentService.Tools;
using AgentService.Tools.InternalTools;
using Shared.Llm;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

builder.Services.AddHttpClient("MemoryService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:MemoryService"] ?? "http://localhost:5001");
});

builder.Services.AddHttpClient("TaskService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:TaskService"] ?? "http://localhost:5003");
});

builder.Services.AddSingleton<LlmProviderFactory>();
builder.Services.AddSingleton<ILlmProvider>(sp => sp.GetRequiredService<LlmProviderFactory>().CreateProvider());

builder.Services.AddSingleton(sp =>
{
    var executor = new ToolExecutor();
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var config = sp.GetRequiredService<IConfiguration>();

    executor.RegisterTool(new QueryEventsTool(factory));
    executor.RegisterTool(new AddShoppingItemTool(factory, config));

    return executor;
});

var app = builder.Build();

app.MapPost("/chat", async (
    ChatRequest request,
    ILlmProvider llm,
    ToolExecutor toolExecutor,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        var messages = new List<LlmMessage>
        {
            new("user", request.Message)
        };

        var tools = toolExecutor.GetToolDefinitions()
            .Select(t => new LlmTool(t.Name, t.Description, t.InputSchema))
            .ToList();

        var systemPrompt = """
            # Role
            You are a personal AI assistant with access to the user's event history, shopping lists, and reminders.

            # Core Responsibilities
            - Answer questions about past events (emails, calendar appointments, bills, transactions)
            - Manage shopping lists by adding or retrieving items
            - Help the user stay organized and remember important information

            # Response Guidelines
            - Be concise and direct. Avoid unnecessary elaboration.
            - Provide specific dates, times, and details when available.
            - If information is missing or unclear, state what you found and what's unknown.

            # Tool Usage Instructions

            ## query_events Tool
            Use this tool when the user asks about:
            - Past events, emails, or messages
            - Calendar appointments or scheduled items
            - Bills, payments, or financial transactions
            - Any historical information stored in their event log

            Examples requiring query_events:
            - "What bills are due this week?"
            - "Show me emails from last Monday"
            - "Did I have any meetings yesterday?"

            Query parameters:
            - Use `query` for full-text search when the user provides keywords
            - Use `source` to filter by data source (email, calendar, bank)
            - Use `type` to filter by event type (email_received, payment_due, etc.)
            - Combine filters when appropriate for precise results

            ## add_shopping_item Tool
            Use this tool when the user wants to:
            - Add items to their shopping list
            - Remember to buy something
            - Note items they're running low on

            Examples requiring add_shopping_item:
            - "Add milk to my shopping list"
            - "Remind me to buy batteries"
            - "I need to get 2 lbs of chicken"

            Extract:
            - `item`: The product name
            - `quantity`: Optional amount or measurement
            - `notes`: Optional details (brand, size, preferences)

            # Workflow
            1. Analyze the user's request to determine which tool(s) are needed
            2. Call the appropriate tool(s) with relevant parameters
            3. Synthesize the tool results into a clear, actionable response
            4. If multiple pieces of information are needed, make all necessary tool calls

            # Constraints
            - Only use available tools. Do not invent capabilities you don't have.
            - When tool results are empty, acknowledge this clearly.
            - Do not make assumptions about data you haven't queried.
            """;

        while (true)
        {
            var response = await llm.SendMessageAsync(messages, tools, systemPrompt, ct: ct);

            if (response.StopReason == "end_turn")
            {
                var textContent = response.Content.FirstOrDefault(c => c.Type == "text");
                return Results.Ok(new { response = textContent?.Text ?? "No response" });
            }

            if (response.StopReason == "tool_use")
            {
                var assistantContent = new List<object>();
                var toolResults = new List<object>();

                foreach (var block in response.Content)
                {
                    assistantContent.Add(new
                    {
                        type = block.Type,
                        text = block.Text,
                        id = block.ToolUseId,
                        name = block.ToolName,
                        input = block.ToolInput
                    });

                    if (block.Type == "tool_use" && block.ToolName != null && block.ToolInput != null)
                    {
                        logger.LogInformation("Executing tool: {ToolName}", block.ToolName);

                        try
                        {
                            var result = await toolExecutor.ExecuteToolAsync(block.ToolName, block.ToolInput, ct);
                            toolResults.Add(new
                            {
                                type = "tool_result",
                                tool_use_id = block.ToolUseId,
                                content = result
                            });
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Tool execution failed");
                            toolResults.Add(new
                            {
                                type = "tool_result",
                                tool_use_id = block.ToolUseId,
                                content = $"Error: {ex.Message}",
                                is_error = true
                            });
                        }
                    }
                }

                messages.Add(new LlmMessage("assistant", assistantContent));
                messages.Add(new LlmMessage("user", toolResults));
            }
            else
            {
                return Results.Problem($"Unexpected stop reason: {response.StopReason}");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Chat request failed");
        return Results.Problem("Failed to process chat request", statusCode: 500);
    }
});

app.MapGet("/tools", (ToolExecutor executor) =>
{
    var tools = executor.GetToolDefinitions();
    return Results.Ok(tools);
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program { }

public sealed record ChatRequest(string Message);
