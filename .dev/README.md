# Local Development Environment

Docker Compose infrastructure for the Jarvis AI Personal Assistant.

## Services

- **Postgres** (port 54322) - Main database
- **Qdrant** (port 6333) - Vector database
- **n8n** (port 5678) - Workflow automation
- **MailHog** (ports 1025/8025) - Email testing
- **Adminer** (port 8080) - Database UI
- **Prometheus** (port 9090) - Metrics
- **Grafana** (port 3000) - Dashboards (admin/admin)
- **Langfuse** (port 3001) - LLM observability

## Quick Start

```bash
# Start all services
docker compose up -d

# Check status
docker compose ps

# View logs
docker compose logs -f [service-name]

# Stop all
docker compose down

# Reset everything (deletes data)
docker compose down -v
```

## Connection Strings

**Postgres:**
```
Host=localhost;Port=54322;Username=postgres;Password=postgres;Database=appdb
```

**Qdrant:**
```
http://localhost:6333
```

**MailHog SMTP:**
```json
{ "Host": "localhost", "Port": 1025, "EnableSsl": false }
```

**Langfuse:**
```
http://localhost:3001
```

## Web UIs

- Database: http://localhost:8080 (postgres/postgres/appdb)
- Email: http://localhost:8025
- Grafana: http://localhost:3000 (admin/admin)
- Langfuse: http://localhost:3001
- n8n: http://localhost:5678
- Qdrant: http://localhost:6333/dashboard

## Running .NET Services

```bash
dotnet run --project src/EventService
dotnet run --project src/MemoryService
# etc.
```

Services will connect to infrastructure via localhost on the ports above.
