-- Initialize Langfuse schema
-- This script runs on first DB initialization

-- Create langfuse schema (Langfuse will auto-migrate its tables)
CREATE SCHEMA IF NOT EXISTS langfuse;

-- Grant permissions
GRANT ALL ON SCHEMA langfuse TO postgres;

-- Note: Langfuse will auto-create its tables on first startup
-- No need to manually create tables here
