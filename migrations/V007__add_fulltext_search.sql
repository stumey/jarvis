-- V007: Add full-text search support for events
-- Creates a tsvector column and GIN index for fast text search

ALTER TABLE events ADD COLUMN IF NOT EXISTS search_vector tsvector;

CREATE OR REPLACE FUNCTION events_search_vector_update() RETURNS trigger AS $$
BEGIN
    NEW.search_vector := to_tsvector('english',
        coalesce(NEW.source, '') || ' ' ||
        coalesce(NEW.event_type, '') || ' ' ||
        coalesce(NEW.payload::text, '')
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS events_search_vector_trigger ON events;
CREATE TRIGGER events_search_vector_trigger
    BEFORE INSERT OR UPDATE ON events
    FOR EACH ROW EXECUTE FUNCTION events_search_vector_update();

CREATE INDEX IF NOT EXISTS idx_events_search ON events USING GIN(search_vector);

-- Update existing rows
UPDATE events SET search_vector = to_tsvector('english',
    coalesce(source, '') || ' ' ||
    coalesce(event_type, '') || ' ' ||
    coalesce(payload::text, '')
);
