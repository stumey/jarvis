# Jarvis - AI Personal Assistant

A modular AI personal assistant system built with .NET 9, featuring event ingestion, semantic search, shopping lists, and an agentic chat interface with Claude/OpenAI.

## Architecture

**Microservices (.NET 9 Minimal APIs)**
- **EventService** (`:5000`) - Ingests and validates events from connectors
- **MemoryService** (`:5001`) - Full-text search over event history
- **TaskService** (`:5003`) - Shopping lists and task management
- **AgentService** (`:5004`) - Agentic chat with tool-calling (Claude/OpenAI)
- **ReminderService** (`:5005`) - State-based reminder dispatch _(not yet implemented)_
- **ContextService** (`:5006`) - Context fusion for agent _(not yet implemented)_
- **PatternService** (`:5007`) - Behavioral pattern detection _(not yet implemented)_

**Infrastructure**
- PostgreSQL (`:54322`) - Event storage with full-text search
- Qdrant (`:6333`) - Vector database _(reserved for future semantic search)_
- n8n (`:5678`) - Workflow automation for data connectors
- Langfuse (`:3001`) - LLM observability _(reserved)_

**Shared Components**
- Canonical event schema validation (JSON Schema)
- Provider-agnostic LLM interface (Claude, OpenAI, Bedrock-ready)
- Dapper-based repository pattern with SQL constants
- Domain models and contracts

## Getting Started

### Prerequisites
- .NET 9 SDK
- Docker & Docker Compose
- Make (optional, for convenience commands)

### 1. Start Infrastructure

```bash
cd .dev
docker compose up -d
```

This starts Postgres, Qdrant, n8n, MailHog, Prometheus, Grafana, and Langfuse.

### 2. Run Database Migrations

```bash
make migrate
# OR manually:
for f in migrations/*.sql; do
  psql "postgresql://postgres:postgres@localhost:54322/appdb" -f "$f"
done
```

### 3. Configure Services

Create `appsettings.Development.json` files for each service (already in `.gitignore`):

**EventService/MemoryService/TaskService:**
```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=54322;Username=postgres;Password=postgres;Database=appdb"
  }
}
```

**AgentService:**
```json
{
  "Llm": {
    "Provider": "Claude",
    "Claude": {
      "ApiKey": "sk-ant-...",
      "Model": "claude-3-5-sonnet-20241022"
    },
    "OpenAI": {
      "ApiKey": "sk-...",
      "Model": "gpt-4o"
    }
  },
  "Services": {
    "MemoryService": "http://localhost:5001",
    "TaskService": "http://localhost:5003"
  },
  "DefaultShoppingListId": "00000000-0000-0000-0000-000000000001"
}
```

### 4. Run Services

```bash
# Terminal 1
dotnet run --project src/EventService

# Terminal 2
dotnet run --project src/MemoryService

# Terminal 3
dotnet run --project src/TaskService

# Terminal 4
dotnet run --project src/AgentService
```

Or use the Makefile:
```bash
make run-all  # Runs all services in background
```

### 5. Test the Agent

```bash
curl -X POST http://localhost:5004/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Add milk to my shopping list"}'
```

## API Endpoints

### EventService (`:5000`)
```
POST   /events              - Ingest event (validates against schema)
GET    /events/{id}         - Get event by ID
GET    /events?source=...   - Filter by source/type/unprocessed
POST   /events/{id}/processed - Mark event as processed
GET    /events/stats        - Event counts
```

### MemoryService (`:5001`)
```
GET    /memory/search?q=...      - Full-text search
GET    /memory/recent            - Recent events
GET    /memory/by-source/{source} - Filter by source
GET    /memory/by-type/{type}    - Filter by event type
```

### TaskService (`:5003`)
```
POST   /lists?userId=...&name=...     - Create shopping list
GET    /lists/user/{userId}           - Get user's lists
GET    /lists/{id}                    - Get list by ID
DELETE /lists/{id}                    - Delete list

POST   /lists/{listId}/items?name=... - Add item
GET    /lists/{listId}/items          - Get items (optional ?status=needed)
PATCH  /items/{id}/status?status=...  - Update item status
DELETE /items/{id}                    - Delete item
```

### AgentService (`:5004`)
```
POST   /chat          - Chat with assistant (body: {"message": "..."})
GET    /tools         - List available tools
GET    /health        - Health check
```

## Event Schema

Events must conform to `event_schema_v1.json`:

```json
{
  "event_id": "uuid",
  "source": "email|calendar|bank|manual|voice|task|shopping_list|system",
  "event_type": "email_received|payment_due|calendar_event_created|...",
  "timestamp": "2025-12-06T10:00:00Z",
  "schema_version": "1",
  "payload": { /* source-specific data */ },
  "metadata": { /* optional */ }
}
```

## LLM Provider Configuration

Switch between providers by changing `Llm:Provider`:

**Claude (default)**
```json
{
  "Llm": {
    "Provider": "Claude",
    "Claude": {
      "ApiKey": "sk-ant-...",
      "Model": "claude-3-5-sonnet-20241022"
    }
  }
}
```

**OpenAI**
```json
{
  "Llm": {
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "sk-...",
      "Model": "gpt-4o"
    }
  }
}
```

The agent automatically adapts to the configured provider.

## Testing

```bash
# Run all tests
dotnet test

# Run integration tests only
dotnet test tests/EventService.Tests
```

Integration tests use Testcontainers to spin up a PostgreSQL instance automatically.

## Development Workflow

1. **Add a new event source**: Create n8n workflow → normalize to schema → POST to EventService
2. **Add a new tool**: Implement `ITool` in `AgentService/Tools/` → register in `ToolExecutor`
3. **Query events**: Use MemoryService full-text search or filter by source/type
4. **Chat with agent**: Agent uses tools to query memory, manage lists, etc.

## Project Structure

```
jarvis/
├── .dev/                   # Docker Compose infrastructure
├── migrations/             # SQL migrations (Flyway-style naming)
├── src/
│   ├── Shared/            # Common domain, contracts, data layer, LLM abstraction
│   ├── EventService/      # Event ingestion
│   ├── MemoryService/     # Event search
│   ├── TaskService/       # Shopping lists
│   ├── AgentService/      # Agentic chat
│   └── .../               # Other services (not yet implemented)
└── tests/
    └── EventService.Tests/ # Integration tests with Testcontainers
```

## Deployment

For Raspberry Pi deployment:
1. Use `appsettings.Production.json` with production connection strings
2. Run migrations on production DB
3. Run services as systemd units or Docker containers
4. Configure n8n workflows to point to production EventService

## Contributing

See [GitHub Issues](https://github.com/stumey/jarvis/issues) for planned features and improvements.

## License

Private project - not licensed for distribution.
