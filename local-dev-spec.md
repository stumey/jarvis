FULL SPEC DOCUMENT — Local Development Environment (Option D + Langfuse)
Version: 1.1
Purpose: Provide a complete local development infrastructure stack supporting all .NET microservices, Claude integration, vector search, workflow automation, monitoring, metrics, email testing, and LLM/agent observability with Langfuse.
1. Overview
This specification defines a Docker Compose–based local development environment required for developing the AI Personal Assistant system using:
.NET 8 microservices (run outside Docker)
Supabase/Postgres (local)
Qdrant vector database
n8n workflow engine
MailHog for email testing
Adminer or pgAdmin for DB management
Prometheus + Grafana for system metrics
Langfuse for LLM/agent observability
The environment must allow developers to run:
docker compose up -d
and immediately have:
Database
Vector store
Workflow automation engine
Email test server
Monitoring dashboards
LLM trace/observability system
—all running locally.
All .NET services will connect into this environment via localhost.
2. Architecture Components
The local environment will run infrastructure only, not the .NET services themselves.
2.1 Supabase (local Postgres)
Runs locally and includes:
Postgres 15+
Listening on: 54322 (mapped to container 5432)
The system will rely on this DB for:
events
reminders
patterns
shopping lists
rule engine
optional user profile data
langfuse schema
Supabase “extras” (Auth, Edge Functions, Realtime, Studio) are not required.
2.2 Qdrant Vector Database
Vector storage for:
embeddings
semantic search
memory
long-term context retrieval
Port: 6333
Persistent volume required.
2.3 n8n (workflow engine)
Used for:
email/calendar/bank connectors
event normalization
webhook ingestion
background integrations
scheduled workflows
Port: 5678
2.4 MailHog (local email testing)
Used by .NET ReminderService or NotificationService.
SMTP: 1025
Web UI: 8025
2.5 Adminer (or pgAdmin)
Database inspection tool.
Port: 8080
2.6 Monitoring Stack (Prometheus & Grafana)
Used for:
.NET metrics (/metrics endpoint)
system-level container metrics
reminder run rates
queue depth
LLM latency (if exported)
Prometheus:
Port: 9090
Grafana:
Port: 3000
3. Langfuse Integration (LLM + Agent Observability)
Langfuse must be included as a full observability component.
3.1 Purpose
Track:
Claude API calls
LLM token usage
Tool calls
Agent step-by-step reasoning
Embedding requests
Context fusion steps
Errors, retries, and latency
Multi-step chain traces
This is essential for debugging the agent’s behavior and improving reliability.
3.2 Langfuse Service Details
Image
langfuse/langfuse:latest
Port
3001:3001
Database
Reuse the same Postgres DB as Supabase using a separate schema:
langfuse
URL:
postgresql://postgres:postgres@supabase-db:5432/appdb?schema=langfuse
Required Env Vars
Claude Code must generate:
LANGFUSE_DATABASE_URL=postgresql://postgres:postgres@supabase-db:5432/appdb?schema=langfuse
LANGFUSE_NEXTAUTH_SECRET=<random-32-byte>
LANGFUSE_ENCRYPTION_KEY=<random-32-byte>
LANGFUSE_TELEMETRY_ENABLED=false
Schema Init
Claude must create:
CREATE SCHEMA IF NOT EXISTS langfuse;
3.3 .NET Integration Requirements
Every .NET microservice must include:
A LangfuseClient wrapper
Configurable via appsettings.Development.json
Ability to send:
traces
spans
generations
events
Example config:
"Langfuse": {
  "BaseUrl": "http://localhost:3001",
  "PublicKey": "dev-public",
  "SecretKey": "dev-secret",
  "Enabled": true
}
3.4 Required Instrumentation
Every LLM call via Claude must produce:
Trace — logical operation (e.g., remind user to pay bill)
Generations — each Claude completion
Span — each tool call
Errors — captured as Langfuse events
Examples:
Trace: /agent/message
Span: CreateReminderTool
Span: QdrantEmbedding
Generation: claude-3.5-sonnet completion
4. Folder Layout
Claude Code must generate the following structure:
local-dev/
    docker-compose.yml
    .env
    README.md

    prometheus/
        prometheus.yml

    grafana/
        provisioning/
            datasources/
            dashboards/

    qdrant/
        (persistent storage)

    supabase/
        (persistent storage)

    n8n/
        (persistent storage)

    mailhog/
        (persistent storage)

    langfuse/
        init.sql   # optional schema init
5. Environment Variables
.env file must include:
# Postgres / Supabase
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=appdb
POSTGRES_PORT=54322

# Qdrant
QDRANT_PORT=6333

# n8n
N8N_PORT=5678

# MailHog
MAILHOG_SMTP_PORT=1025
MAILHOG_UI_PORT=8025

# Adminer
ADMINER_PORT=8080

# Prometheus
PROMETHEUS_PORT=9090

# Grafana
GRAFANA_PORT=3000

# Langfuse
LANGFUSE_PORT=3001
LANGFUSE_DATABASE_URL=postgresql://postgres:postgres@supabase-db:5432/appdb?schema=langfuse
LANGFUSE_NEXTAUTH_SECRET=<GENERATE>
LANGFUSE_ENCRYPTION_KEY=<GENERATE>
LANGFUSE_TELEMETRY_ENABLED=false

# Internal network
NETWORK_NAME=ai_personal_assistant_net
6. Docker Compose Requirements
At minimum, compose must include:
6.1 Network
All services must join the same network:
ai_personal_assistant_net
6.2 Volumes
Declare:
supabase_data
qdrant_data
n8n_data
mailhog_data
grafana_data
6.3 Services
supabase-db
Image: supabase/postgres
Ports: 54322:5432
Env from .env
Volume: supabase_data:/var/lib/postgresql/data
Healthcheck using pg_isready
qdrant
Image: qdrant/qdrant
Ports: 6333:6333
Volume: qdrant_data
Healthcheck: GET /readyz
n8n
Image: n8nio/n8n
Ports: 5678:5678
Volume: n8n_data
Timezone: America/New_York
Restart: unless-stopped
mailhog
Image: mailhog/mailhog
Ports: 1025:1025, 8025:8025
Volume: mailhog_data
adminer
Image: adminer
Port: 8080:8080
Depends on: supabase-db
prometheus
Mount file: /prometheus/prometheus.yml
Port: 9090:9090
grafana
Port: 3000:3000
Mount provisioning files
Volume: grafana_data
langfuse
Image: langfuse/langfuse
Port: 3001:3001
Env vars from .env
Depends on: supabase-db
7. Connection Strings for .NET Services
Postgres
Host=localhost;Port=54322;Username=postgres;Password=postgres;Database=appdb
Qdrant
http://localhost:6333
MailHog SMTP
Host=localhost
Port=1025
EnableSsl=false
Langfuse
http://localhost:3001
n8n webhooks
http://localhost:5678/webhook/<path>
8. Developer Experience Requirements
Commands
Start stack:
docker compose up -d
Stop stack:
docker compose down
Reset volumes:
docker compose down -v
Documentation
Claude must generate local-dev/README.md explaining:
Purpose of each service
How to access each UI
How .NET services connect
How to test Langfuse integration
Troubleshooting
9. Validation Requirements
Claude must validate:
YAML passes docker compose config
No port conflicts
No duplicate service names
All environment variables are mapped
Health checks are valid
Correct Docker networking
Langfuse schema is initialized or auto-initializing
10. Expected Deliverables from Claude
Claude Code must generate:
1. local-dev/docker-compose.yml
Production-grade, ready to run.
2. local-dev/.env
With all required variables populated.
3. local-dev/prometheus/prometheus.yml
To scrape:
http://host.docker.internal:5000/metrics
http://host.docker.internal:5001/metrics
etc.
4. Grafana provisioning files
Data source config
Starter dashboard folder
5. Optional Langfuse init SQL
local-dev/langfuse/init.sql
6. local-dev/README.md
Detailed local usage documentation.