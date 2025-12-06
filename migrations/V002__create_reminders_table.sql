-- V002: Create reminders table
-- Stores reminders with state machine tracking

CREATE TABLE IF NOT EXISTS reminders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(500) NOT NULL,
    state VARCHAR(20) NOT NULL DEFAULT 'scheduled',
    due_at_utc TIMESTAMPTZ,
    attempts INT NOT NULL DEFAULT 0,
    last_attempt_at TIMESTAMPTZ,
    metadata JSONB,
    related_event_id UUID REFERENCES events(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT valid_state CHECK (state IN ('scheduled', 'sending', 'sent', 'acknowledged', 'ignored', 'failed'))
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_reminders_state ON reminders(state);
CREATE INDEX IF NOT EXISTS idx_reminders_due_at ON reminders(due_at_utc) WHERE state = 'scheduled';
