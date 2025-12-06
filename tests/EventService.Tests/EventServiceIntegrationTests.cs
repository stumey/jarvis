using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Shared.Data;

namespace EventService.Tests;

[Collection("Postgres")]
public class EventServiceIntegrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private EventServiceFactory _factory = null!;
    private HttpClient _client = null!;

    public EventServiceIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _factory = new EventServiceFactory(_fixture.ConnectionString);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostEvent_ValidEvent_ReturnsCreated()
    {
        var eventJson = """
        {
            "event_id": "11111111-1111-1111-1111-111111111111",
            "source": "email",
            "event_type": "email_received",
            "timestamp": "2025-11-16T17:30:00Z",
            "schema_version": "1",
            "payload": {
                "from": "test@example.com",
                "subject": "Test email"
            }
        }
        """;

        var content = new StringContent(eventJson, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/events", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("id").GetGuid().Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public async Task PostEvent_InvalidSchema_ReturnsBadRequest()
    {
        var eventJson = """
        {
            "source": "email",
            "event_type": "email_received"
        }
        """;

        var content = new StringContent(eventJson, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/events", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetEvent_ExistingEvent_ReturnsEvent()
    {
        var eventId = Guid.NewGuid();
        var eventJson = $$"""
        {
            "event_id": "{{eventId}}",
            "source": "calendar",
            "event_type": "calendar_event_created",
            "timestamp": "2025-11-16T10:00:00Z",
            "schema_version": "1",
            "payload": {
                "title": "Team meeting"
            }
        }
        """;

        var content = new StringContent(eventJson, Encoding.UTF8, "application/json");
        await _client.PostAsync("/events", content);

        var response = await _client.GetAsync($"/events/{eventId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<EventRecord>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(eventId);
        result.Source.Should().Be("calendar");
        result.EventType.Should().Be("calendar_event_created");
    }

    [Fact]
    public async Task GetEvent_NonExistingEvent_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/events/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEvents_FilterBySource_ReturnsFilteredEvents()
    {
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();

        await PostEventAsync(eventId1, "bank", "payment_due");
        await PostEventAsync(eventId2, "email", "email_received");

        var response = await _client.GetAsync("/events?source=bank");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var events = await response.Content.ReadFromJsonAsync<List<EventRecord>>();
        events.Should().Contain(e => e.Id == eventId1);
        events.Should().NotContain(e => e.Id == eventId2);
    }

    [Fact]
    public async Task MarkEventProcessed_UpdatesEvent()
    {
        var eventId = Guid.NewGuid();
        await PostEventAsync(eventId, "email", "email_received");

        var markResponse = await _client.PostAsync($"/events/{eventId}/processed", null);
        markResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/events/{eventId}");
        var result = await getResponse.Content.ReadFromJsonAsync<EventRecord>();
        result!.Processed.Should().BeTrue();
    }

    [Fact]
    public async Task GetStats_ReturnsCorrectCounts()
    {
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();

        await PostEventAsync(eventId1, "email", "email_received");
        await PostEventAsync(eventId2, "calendar", "calendar_event_created");
        await _client.PostAsync($"/events/{eventId1}/processed", null);

        var response = await _client.GetAsync("/events/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stats = await response.Content.ReadFromJsonAsync<JsonElement>();
        stats.GetProperty("total").GetInt64().Should().BeGreaterThanOrEqualTo(2);
        stats.GetProperty("unprocessed").GetInt64().Should().BeGreaterThanOrEqualTo(1);
    }

    private async Task PostEventAsync(Guid eventId, string source, string eventType)
    {
        var eventJson = $$"""
        {
            "event_id": "{{eventId}}",
            "source": "{{source}}",
            "event_type": "{{eventType}}",
            "timestamp": "2025-11-16T12:00:00Z",
            "schema_version": "1",
            "payload": {}
        }
        """;

        var content = new StringContent(eventJson, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/events", content);
        response.EnsureSuccessStatusCode();
    }
}
