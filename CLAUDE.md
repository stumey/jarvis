# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an AI Personal Assistant system architecture specification repository. The repository contains comprehensive technical specifications for building a multi-component system that includes:

- .NET 8 microservices architecture
- Event-driven processing with canonical event schemas
- Vector database (Qdrant) for semantic memory
- n8n workflow automation for data connectors
- Claude API integration for AI reasoning and tool calling
- Supabase/Postgres for data persistence
- Shopping lists, reminders, pattern detection, and context fusion capabilities

## Core Architecture

### Microservices (All .NET 8 Minimal APIs)

The system is designed around these microservices:

1. **EventService** - Validates and stores normalized events, pushes to vector DB
2. **MemoryService** - Handles embeddings and semantic search via Claude API
3. **ReminderService** - Manages reminders with state machine
4. **PatternService** - Daily/weekly pattern detection using Claude prompts
5. **ListService** - Shopping lists and tasks management
6. **ContextService** - Fuses events, patterns, lists, preferences into AI-ready context
7. **RuleEngineService** - Deterministic rule evaluation
8. **AgentService** - Chat interface with tool-calling assistant

### Canonical Event Schema

All connectors must emit events conforming to `event_schema_v1.json`:

```json
{
  "schema_version": 1,
  "event_id": "<uuid>",
  "source": "gmail|calendar|bills|spotify|custom",
  "event_type": "email_received|appointment_scheduled|bill_due|song_played|...",
  "timestamp": "2025-11-16T12:00:00Z",
  "data": { /* source-specific JSON */ },
  "metadata": {
    "priority": "high|medium|low",
    "category": "work|personal|finance|shopping",
    "sentiment": "positive|neutral|negative",
    "action_required": true
  },
  "embedding": [ /* float[] optional */ ],
  "processed": false
}
```

### Database Schema

Key tables include:
- `shopping_lists` and `shopping_items` (with CASCADE delete)
- `events` table for all normalized events
- `reminders` with state tracking
- `patterns` for detected behavior patterns
- `automation_rules` for rule engine
- `langfuse` schema for LLM observability

## Local Development Environment

### Starting the Stack

```bash
cd local-dev
docker compose up -d
```

This starts all infrastructure services:
- **Supabase/Postgres** - Port 54322 (local DB)
- **Qdrant** - Port 6333 (vector database)
- **n8n** - Port 5678 (workflow engine)
- **MailHog** - SMTP 1025, UI 8025 (email testing)
- **Adminer** - Port 8080 (DB management)
- **Prometheus** - Port 9090 (metrics)
- **Grafana** - Port 3000 (dashboards)
- **Langfuse** - Port 3001 (LLM observability)

### Running .NET Services

Each microservice runs separately:

```bash
dotnet run --project src/EventService
dotnet run --project src/MemoryService
# ... etc
```

### Connection Strings

**Postgres:**
```
Host=localhost;Port=54322;Username=postgres;Password=postgres;Database=appdb
```

**Qdrant:**
```
http://localhost:6333
```

**MailHog SMTP:**
```
Host=localhost;Port=1025;EnableSsl=false
```

**Langfuse:**
```
http://localhost:3001
```

**n8n webhooks:**
```
http://localhost:5678/webhook/<path>
```

## Key Implementation Patterns

### Claude API Integration

Use HttpClient for Claude API calls:

```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Add("x-api-key", config.ClaudeApiKey);

var response = await client.PostAsJsonAsync(
    "https://api.anthropic.com/v1/messages",
    new {
        model = "claude-3.5-sonnet",
        max_tokens = 2000,
        system = systemPrompt,
        messages = new[] {
            new { role = "user", content = eventBatchJson }
        }
    }
);
```

### Background Services

Pattern detection and reminder dispatching use BackgroundService:

```csharp
public class PatternDetectionWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DetectPatterns();
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
```

### Reminder State Machine

States: `scheduled → sending → sent → acknowledged | ignored`

Transitions handle retries and failure scenarios with state tracking in DB.

### Context Fusion

The Context Fusion module (`/context/fuse` endpoint) returns:

```json
{
  "recent_events": [...],
  "relevant_memories": [...],
  "patterns": [...],
  "user_prefs": {...},
  "active_lists": [...],
  "reminder_queue_snapshot": [...],
  "agent_state": {...}
}
```

## Agent Tool Functions

The AgentService exposes these tool functions for Claude:

- `query_events(filters)` - Returns filtered events
- `create_reminder(details)` - Creates reminder, returns ID
- `get_schedule(date_range)` - Returns calendar items
- `analyze_patterns()` - Returns detected patterns
- `update_preferences(payload)` - Updates user preferences
- `add_to_list(item, list_name)` - Adds shopping item
- `create_task(title, due_date?, priority?)` - Creates task
- `get_lists()` - Lists all shopping lists
- `update_list_item(item_id, status)` - Updates item status

## Implementation Phases

The specification documents outline implementation in phases A-J:

- **Phase A** - Foundation & shared schemas
- **Phase B** - Core service refactor & Context Fusion
- **Phase C** - Rule engine, reminder state machine, temporal module
- **Phase D** - Conversational agent tools & prompts
- **Phase E** - n8n workflows & connector templates
- **Phase F** - Vector DB & embedding pipeline
- **Phase G** - Monitoring, logging, and admin
- **Phase H** - Privacy, retention, and RLS
- **Phase I** - Prompt engineering & Pattern detection templates
- **Phase J** - Deliverables checklist

## Testing Requirements

### Event Connector Testing

Every new connector must include:
1. Fixture data → normalization → validation against `event_schema_v1.json`
2. POST to `/events` endpoint must accept the event
3. Unit tests with at least 3 fixtures (valid, valid alternate, malformed)

### AI Prompt Testing

For AI prompts, include unit tests that:
- Verify Claude returns strict JSON
- Parse and validate the JSON structure
- Test few-shot examples

### State Machine Testing

Integration tests simulating:
- Channel failures
- Retry logic
- State transitions

## Langfuse Integration

Every .NET microservice must instrument LLM calls:

**Configuration (`appsettings.Development.json`):**

```json
{
  "Langfuse": {
    "BaseUrl": "http://localhost:3001",
    "PublicKey": "dev-public",
    "SecretKey": "dev-secret",
    "Enabled": true
  }
}
```

**Trace Structure:**
- Trace - Logical operation (e.g., "remind user to pay bill")
- Generations - Each Claude completion
- Span - Each tool call
- Events - Errors and metrics

## n8n Workflow Structure

Data connector workflows follow this pattern:

1. Trigger (Gmail/Calendar/HTTP)
2. Extract & transform node
3. AI classification node
4. Normalize & validate (event_schema_v1.json)
5. POST /events (EventService)
6. Webhook to EventProcessor

## Schema Versioning

When bumping schema versions:
1. Update `schema_version` field
2. Create migration document in `MIGRATIONS.md`
3. Ensure backward compatibility checks in n8n
4. Test with validation function

## Data Retention & Privacy

- RLS (Row Level Security) policies ensure user-scoped access
- Retention policies stored in `data_retention_policies` table
- POST `/user/{id}/forget?domain=email` implements right-to-be-forgotten
- Audit logs track compliance actions
