Phase A — Foundation & shared schemas (smallest chunks)
A.1 Create canonical Event schema + versioning
Goal: All connectors emit an event conforming to a single schema with schema_version.
Acceptance: n8n Event Normalizer accepts any incoming raw source and outputs event matching schema v1.
Schema (JSON, v1):
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
Work items:
Create JSON Schema file (event_schema_v1.json) and validation function.
Update Event Normalizer n8n workflow to validate & add schema_version:1.
Unit test: send sample Gmail, Calendar, Bill payloads and assert valid output.
A.2 Add shopping_lists + shopping_items SQL schema (Supabase)
Goal: Add shopping/task domain to DB.
SQL (ready-to-run):
CREATE TABLE shopping_lists (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL,
  name text NOT NULL,
  created_at timestamptz DEFAULT now(),
  updated_at timestamptz DEFAULT now()
);

CREATE TABLE shopping_items (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  list_id uuid REFERENCES shopping_lists(id) ON DELETE CASCADE,
  item_name text NOT NULL,
  status text DEFAULT 'needed', -- needed|bought|postponed
  added_by_event_id uuid NULL,
  detected_from text, -- manual|email|calendar|chat|pattern
  quantity text NULL,
  notes text NULL,
  created_at timestamptz DEFAULT now(),
  updated_at timestamptz DEFAULT now()
);
Work items:
Apply SQL migration to Supabase.
Add REST API endpoints (or RPC) for create/get/update lists & items.
Add sample curl tests.
A.3 Add schema_version migration policy doc
Goal: Document how to bump schema versions, migration steps, and compatibility checks.
Work items: provide MIGRATIONS.md with recommended process and n8n test hooks.
Phase B — Core service refactor & Context Fusion
Goal: refactor AI processing into a small set of service responsibilities so Claude/engineers can implement clean functions and unit tests rather than tangled n8n logic.
B.1 Define Service API surface
Services & core functions (HTTP / RPC):
EventService
POST /events (validates event schema, writes to events table, writes to vector DB, returns event_id)
MemoryService
POST /memory/index (embedding + upsert vector)
POST /memory/query (query by text returning top-k)
PatternService
POST /patterns/analyze (analyze recent events -> patterns JSON)
GET /patterns/{user_id}
ReminderService
POST /reminders (create reminder)
GET /reminders?status=pending
PATCH /reminders/{id} (state transitions)
ListService
POST/GET/PUT shopping lists & items
Work items:
Create OpenAPI (yaml) that defines these endpoints and sample requests/responses.
Implement thin HTTP wrapper/stub in Node/TypeScript (or your language) that calls the DB/vector APIs — these can be mocked for now. Claude can generate code from the OpenAPI.
Acceptance: OpenAPI file exists and a simple curl against stubs returns expected JSON.
B.2 Implement Context Fusion module
Goal: given a user request, return a single fused JSON context for AI prompts.
Fused context shape:
{
  "recent_events": [ ... ],
  "relevant_memories": [ ... ],
  "patterns": [ ... ],
  "user_prefs": { ... },
  "active_lists": [ ... ],
  "reminder_queue_snapshot": [ ... ],
  "agent_state": { "last_interaction": "...", "do_not_disturb": { ... } }
}
Work items:
Implement /context/fuse?user_id=...&query=... endpoint.
Unit tests: assert returned object contains top-k vector lookups + recent events.
Acceptance: Any AI prompt uses /context/fuse to get context.
Phase C — Rule engine, reminder state machine, and temporal module
C.1 Rule-based engine (small JSON rules)
Goal: deterministic automation for common cases.
Rule JSON example:
[
  { "id": "bill_due_7d", "if": {"event_type":"bill_due","days_before":7}, "then": {"action":"create_reminder","reminder_type":"payment"} },
  { "id": "party_shopping_suggest", "if":{"pattern":"hosting_friends","confidence_gt":0.7}, "then": {"action":"suggest_list_items","items":["beer","chips","ice"]} }
]
Work items:
Create a small rule engine library (JS/TS) with evaluate(rule, context) and unit tests.
Create an admin UI (CRUD) for rule JSON or store in Supabase automation_rules table.
Wire evaluate calls inside EventProcessor when new events arrive.
Acceptance: Able to create a rule that triggers a reminder when a bill is due in <7 days.
C.2 Reminder State Machine
Goal: deterministic state transitions and retry behavior.
States & transitions:
scheduled -> sending -> sent -> acknowledged | ignored
ignored -> follow_up -> sent
sending -> failed -> scheduled (retry policy)
Work items:
Implement state machine library or use small library (xstate optional).
Integrate into ReminderService — each reminder stores state, attempts, last_attempt_at.
Add reminder_logs table.
Acceptance: A reminder that fails due to channel outage retries twice then sets failed.
C.3 Temporal fuzzy-date module
Goal: parse natural/fuzzy time expressions into concrete datetimes or ranges.
Work items:
Implement a converter (use chrono-node or similar) with extension rules:
"sometime next week" → date range Mon-Fri next week
"before the concert" → concert start - 3 hours
"when I get home" → support geofence hook (returns placeholder trigger)
Add tests for common phrases.
Acceptance: returned object includes { "type":"range", "start":"...", "end":"..." } or trigger token WHEN_AT_HOME.
Phase D — Conversational agent tools & prompts (Claude-focused)
D.1 Tool surface for agent
Add these tools as functions that Claude can call (function-calling style):
query_events(filters) → returns events
create_reminder(details) → replies with reminder id
get_schedule(date_range) → returns calendar items
analyze_patterns() → returns patterns
update_preferences(payload) → updates prefs
NEW add_to_list(item, list_name) → adds shopping item
NEW create_task(title, due_date?, priority?)
NEW get_lists() → list shopping lists
NEW update_list_item(item_id, status)
Work items:
Implement function stubs that call the Service API.
Provide OpenAPI + example payloads to Claude so it can generate agent-level code.
Acceptance: Agent can call add_to_list("milk","grocery") and database has the item.
D.2 Agent system prompt and function-calling prompt templates
Provide Claude the following system prompt skeleton (feed as single string):
System prompt (short):
You are a personal assistant for a single user. You have access to tools:
query_events, create_reminder, get_schedule, analyze_patterns, update_preferences, add_to_list, create_task, get_lists, update_list_item.

Always: be proactive but not intrusive. Use context from the fused context service when present. Respect user privacy and DND. When user asks to add items, call add_to_list(item, list). When creating reminders, use create_reminder(details). If ambiguous dates are specified, use the temporal module to interpret.

Return only JSON matching the function-call or a short clarifying question when necessary.
Work items:
Store system prompt as agent_prompt_v1 in repo/config.
Create sample prompts & few-shot examples for common tasks (add milk, set bill reminder, postpone reminder).
Acceptance: Claude can generate function-calling JSON for add_to_list for the phrase “we’re out of milk.”
Phase E — n8n workflows & connector templates
Make each of these small, importable n8n workflows.
E.1 Data Connector Template (n8n)
Nodes:
Trigger (Gmail/Calendar/HTTP)
Extract & transform node
AI classification node (call to PatternService or direct model)
Normalize & validate (event_schema_v1.json)
POST /events (EventService)
Webhook to EventProcessor
Work items:
Provide an example Gmail connector n8n JSON with those nodes and sample credentials fields.
Provide test fixture to send sample email data.
Acceptance: Import into n8n and process sample email to /events.
E.2 Event Processor (n8n or service)
Nodes/steps:
Receive event webhook
If rules match → run action (Rule Engine)
Forward to PatternService (async)
If event_type=bill_due → call ReminderService + Bill optimizer
Work items: Implement the above and include logging nodes to workflow_logs table.
Phase F — Vector DB & embedding pipeline
F.1 Standardize embedding dimension & provider abstraction
Work items:
Create MemoryService adapter interface with implementations for OpenAI embeddings, Anthropic, or local.
Add vector_metadata that stores source_event_id, user_id, created_at, namespace in vector DB.
Acceptance: MemoryService stores embedding and can return top-k.
Phase G — Monitoring, logging, and admin
G.1 Monitoring dashboard
Widgets:
events processed / hour
pending reminders count
failed workflows last 24h
vector DB size
cost estimates (API calls)
Work items:
Create Grafana or Supabase dashboard SQL views + a simple React/Tailwind admin UI.
Add alerting hooks to Slack/Telegram when failure thresholds trip.
Acceptance: Dashboard shows live metrics for connectors and reminders.
Phase H — Privacy, retention, and RLS
H.1 Data retention & forget API
Work items:
Add data_retention_policies table (per user or global).
Implement POST /user/{id}/forget?domain=email which:
deletes raw email content older than X
preserves anonymized embeddings (unless requested)
logs compliance action
Acceptance: API executes retention policy and returns audit log.
H.2 RLS (Row Level Security)
Work items:
Add Supabase RLS policies to ensure user-scoped access (example SQL policy).
Unit tests with service tokens.
Phase I — Prompt engineering & Pattern detection templates (Claude-ready)
I.1 Pattern detector prompt (Claude)
Prompt skeleton:
You are given 30 days of events in JSON (attach fused context). Identify recurring patterns: calendar_pattern, email_pattern, spending_pattern, habit. For each pattern produce:
{ "type","description","confidence", "frequency", "next_occurrence" }
Return strict JSON only.
Work items:
Provide 5 annotated examples (input events → pattern JSON) for few-shot prompting.
Store prompt as patterns_v1.
I.2 Reminder generator prompt
Prompt skeleton:
Given fused context and an upcoming event, decide whether a reminder should be created. If yes, return JSON {create:true, reminder:{title,description,channel,send_at,priority,related_event_id}} else {create:false, reason:"..."}.
Work items: Provide examples and acceptance tests.
Phase J — Deliverables for Claude Code (what to generate automatically)
Ask Claude to produce (one item per request to keep chunks small):
event_schema_v1.json + validator code + testfixtures.
SQL migration file for shopping lists & items.
OpenAPI yaml for service APIs (EventService, MemoryService, PatternService, ReminderService, ListService).
Node/TypeScript service stubs from OpenAPI (server + client).
n8n Gmail connector JSON workflow (importable).
n8n Event Processor workflow JSON.
Rule Engine library (JS) + small admin UI scaffolding.
Context Fusion endpoint implementation (Node/TS) with unit tests.
Temporal fuzzy-date module (Node/TS) with test cases.
Reminder state machine implementation (xstate or small lib) + DB updates and example transitions.
Agent system prompt + 10 few-shot examples for add_to_list, create_reminder, create_task.
Pattern detector prompt + 5 annotated examples.
Monitoring SQL views + dashboard layout JSON (Grafana / or React).
Data retention forget API and RLS policies SQL.
README and deployment checklist (how to wire n8n, Supabase, Vector DB, OpenAI).
Example small work item you can paste to Claude RIGHT NOW
(ask Claude to implement this single item first; it's minimal and unlocks the rest)
Title: Create event_schema_v1.json and validator function
Description:
Create a JSON Schema (Draft-07 or newer) that enforces the canonical event schema (see A.1). Export event_schema_v1.json and a small Node.js validator module (validateEvent.js) that exports validateEvent(event) and returns {valid: boolean, errors: []}. Include unit tests with three fixtures: gmail-like event, calendar event, malformed event (missing event_type).
Acceptance Criteria:
JSON Schema file exists
validator passes fixtures
unit test suite exits 0
Prompt to Claude: (paste as-is)
Generate event_schema_v1.json (JSON Schema) and Node.js validator module validateEvent.js plus jest tests for three fixtures: gmail_event.json, calendar_event.json, malformed_event.json. Use Draft-07. Output files and test commands.
Testing & QA guidance
Every new connector must include: fixture -> normalization -> assert event_schema_v1.json valid -> /events accepted.
For AI prompts, include unit tests where Claude returns JSON strictly and we parse it.
For reminder state transitions, add integration tests simulating channel failures.