.PHONY: infra-up infra-down infra-logs infra-reset infra-status build clean test run-event run-memory run-reminder run-pattern run-task run-context run-rule run-agent restore

# Infrastructure commands
infra-up:
	@echo "Starting infrastructure..."
	cd .dev && docker compose up -d

infra-down:
	@echo "Stopping infrastructure..."
	cd .dev && docker compose down

infra-logs:
	cd .dev && docker compose logs -f

infra-reset:
	@echo "Resetting infrastructure (this will delete all data)..."
	cd .dev && docker compose down -v

infra-status:
	cd .dev && docker compose ps

# Build commands
restore:
	dotnet restore

build: restore
	dotnet build --no-restore

clean:
	dotnet clean
	find . -type d -name "bin" -o -name "obj" | xargs rm -rf

test:
	dotnet test --no-build --verbosity normal

# Run individual services
run-event:
	dotnet run --project src/EventService

run-memory:
	dotnet run --project src/MemoryService

run-reminder:
	dotnet run --project src/ReminderService

run-pattern:
	dotnet run --project src/PatternService

run-task:
	dotnet run --project src/TaskService

run-context:
	dotnet run --project src/ContextService

run-rule:
	dotnet run --project src/RuleEngineService

run-agent:
	dotnet run --project src/AgentService
