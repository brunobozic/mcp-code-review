-- GitLab PostgreSQL Database Initialization
-- This script ensures proper database setup for GitLab

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS btree_gist;
CREATE EXTENSION IF NOT EXISTS plpgsql;

-- Set proper collation and encoding
ALTER DATABASE gitlabhq_production SET default_text_search_config = 'pg_catalog.english';

-- Optimize database settings for GitLab
ALTER SYSTEM SET shared_preload_libraries = 'pg_stat_statements';
ALTER SYSTEM SET track_activity_query_size = 1048576;
ALTER SYSTEM SET track_io_timing = on;
ALTER SYSTEM SET track_functions = 'pl';
ALTER SYSTEM SET wal_buffers = '16MB';
ALTER SYSTEM SET checkpoint_completion_target = 0.9;
ALTER SYSTEM SET random_page_cost = 1.1;
ALTER SYSTEM SET effective_io_concurrency = 200;

-- Reload configuration
SELECT pg_reload_conf();

-- Grant necessary permissions to gitlab user
GRANT ALL PRIVILEGES ON DATABASE gitlabhq_production TO gitlab;
GRANT ALL PRIVILEGES ON SCHEMA public TO gitlab;
ALTER USER gitlab CREATEDB;