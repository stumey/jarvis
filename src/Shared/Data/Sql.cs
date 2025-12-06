namespace Shared.Data;

public static class Sql
{
    public static class Events
    {
        public const string Insert = """
            INSERT INTO events (id, source, event_type, timestamp_utc, schema_version, payload, metadata, processed)
            VALUES (@Id, @Source, @EventType, @TimestampUtc, @SchemaVersion, @Payload::jsonb, @Metadata::jsonb, FALSE)
            RETURNING id
            """;

        public const string SelectById = """
            SELECT id, source, event_type AS EventType, timestamp_utc AS TimestampUtc, schema_version AS SchemaVersion,
                   payload, metadata, processed, created_at AS CreatedAt
            FROM events WHERE id = @Id
            """;

        public const string SelectBySource = """
            SELECT id, source, event_type AS EventType, timestamp_utc AS TimestampUtc, schema_version AS SchemaVersion,
                   payload, metadata, processed, created_at AS CreatedAt
            FROM events WHERE source = @Source
            ORDER BY timestamp_utc DESC LIMIT @Limit OFFSET @Offset
            """;

        public const string SelectByType = """
            SELECT id, source, event_type AS EventType, timestamp_utc AS TimestampUtc, schema_version AS SchemaVersion,
                   payload, metadata, processed, created_at AS CreatedAt
            FROM events WHERE event_type = @EventType
            ORDER BY timestamp_utc DESC LIMIT @Limit OFFSET @Offset
            """;

        public const string SelectUnprocessed = """
            SELECT id, source, event_type AS EventType, timestamp_utc AS TimestampUtc, schema_version AS SchemaVersion,
                   payload, metadata, processed, created_at AS CreatedAt
            FROM events WHERE processed = FALSE
            ORDER BY timestamp_utc ASC LIMIT @Limit
            """;

        public const string SelectRecent = """
            SELECT id, source, event_type AS EventType, timestamp_utc AS TimestampUtc, schema_version AS SchemaVersion,
                   payload, metadata, processed, created_at AS CreatedAt
            FROM events ORDER BY timestamp_utc DESC LIMIT @Limit OFFSET @Offset
            """;

        public const string MarkProcessed = "UPDATE events SET processed = TRUE WHERE id = @Id";

        public const string MarkProcessedBatch = "UPDATE events SET processed = TRUE WHERE id = ANY(@Ids)";

        public const string Count = "SELECT COUNT(*) FROM events";

        public const string CountUnprocessed = "SELECT COUNT(*) FROM events WHERE processed = FALSE";

        public const string Search = """
            SELECT id, source, event_type AS EventType, timestamp_utc AS TimestampUtc, schema_version AS SchemaVersion,
                   payload, metadata, processed, created_at AS CreatedAt
            FROM events
            WHERE search_vector @@ plainto_tsquery('english', @Query)
            ORDER BY ts_rank(search_vector, plainto_tsquery('english', @Query)) DESC, timestamp_utc DESC
            LIMIT @Limit
            """;
    }

    public static class Reminders
    {
        public const string Insert = """
            INSERT INTO reminders (id, user_id, title, message, due_at, state, channel)
            VALUES (@Id, @UserId, @Title, @Message, @DueAt, @State, @Channel)
            RETURNING id
            """;

        public const string SelectById = """
            SELECT id, user_id AS UserId, title, message, due_at AS DueAt, state, channel,
                   retry_count AS RetryCount, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM reminders WHERE id = @Id
            """;

        public const string SelectByUserId = """
            SELECT id, user_id AS UserId, title, message, due_at AS DueAt, state, channel,
                   retry_count AS RetryCount, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM reminders WHERE user_id = @UserId
            ORDER BY due_at ASC LIMIT @Limit OFFSET @Offset
            """;

        public const string SelectPending = """
            SELECT id, user_id AS UserId, title, message, due_at AS DueAt, state, channel,
                   retry_count AS RetryCount, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM reminders WHERE state = 'scheduled' AND due_at <= @Now
            ORDER BY due_at ASC LIMIT @Limit
            """;

        public const string UpdateState = """
            UPDATE reminders SET state = @State, retry_count = @RetryCount, updated_at = NOW()
            WHERE id = @Id
            """;

        public const string Delete = "DELETE FROM reminders WHERE id = @Id";
    }

    public static class ShoppingLists
    {
        public const string Insert = """
            INSERT INTO shopping_lists (id, user_id, name)
            VALUES (@Id, @UserId, @Name)
            RETURNING id
            """;

        public const string SelectById = """
            SELECT id, user_id AS UserId, name, created_at AS CreatedAt
            FROM shopping_lists WHERE id = @Id
            """;

        public const string SelectByUserId = """
            SELECT id, user_id AS UserId, name, created_at AS CreatedAt
            FROM shopping_lists WHERE user_id = @UserId
            ORDER BY created_at DESC
            """;

        public const string Delete = "DELETE FROM shopping_lists WHERE id = @Id";
    }

    public static class ShoppingItems
    {
        public const string Insert = """
            INSERT INTO shopping_items (id, list_id, name, quantity, status)
            VALUES (@Id, @ListId, @Name, @Quantity, @Status)
            RETURNING id
            """;

        public const string SelectByListId = """
            SELECT id, list_id AS ListId, name, quantity, status, created_at AS CreatedAt
            FROM shopping_items WHERE list_id = @ListId
            ORDER BY created_at ASC
            """;

        public const string UpdateStatus = """
            UPDATE shopping_items SET status = @Status WHERE id = @Id
            """;

        public const string Delete = "DELETE FROM shopping_items WHERE id = @Id";
    }

    public static class Patterns
    {
        public const string Insert = """
            INSERT INTO patterns (id, user_id, type, description, confidence, frequency, next_occurrence, metadata)
            VALUES (@Id, @UserId, @Type, @Description, @Confidence, @Frequency, @NextOccurrence, @Metadata::jsonb)
            RETURNING id
            """;

        public const string SelectByUserId = """
            SELECT id, user_id AS UserId, type, description, confidence, frequency,
                   next_occurrence AS NextOccurrence, metadata, detected_at AS DetectedAt
            FROM patterns WHERE user_id = @UserId
            ORDER BY confidence DESC LIMIT @Limit
            """;

        public const string SelectByType = """
            SELECT id, user_id AS UserId, type, description, confidence, frequency,
                   next_occurrence AS NextOccurrence, metadata, detected_at AS DetectedAt
            FROM patterns WHERE user_id = @UserId AND type = @Type
            ORDER BY confidence DESC
            """;

        public const string Delete = "DELETE FROM patterns WHERE id = @Id";
    }

    public static class Rules
    {
        public const string Insert = """
            INSERT INTO automation_rules (id, name, condition, action, is_active)
            VALUES (@Id, @Name, @Condition::jsonb, @Action::jsonb, @IsActive)
            RETURNING id
            """;

        public const string SelectActive = """
            SELECT id, name, condition, action, is_active AS IsActive,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM automation_rules WHERE is_active = TRUE
            """;

        public const string SelectById = """
            SELECT id, name, condition, action, is_active AS IsActive,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM automation_rules WHERE id = @Id
            """;

        public const string UpdateActive = """
            UPDATE automation_rules SET is_active = @IsActive, updated_at = NOW()
            WHERE id = @Id
            """;

        public const string Delete = "DELETE FROM automation_rules WHERE id = @Id";
    }

    public static class UserPreferences
    {
        public const string Upsert = """
            INSERT INTO user_preferences (user_id, preferences, updated_at)
            VALUES (@UserId, @Preferences::jsonb, NOW())
            ON CONFLICT (user_id) DO UPDATE SET preferences = @Preferences::jsonb, updated_at = NOW()
            """;

        public const string SelectByUserId = """
            SELECT user_id AS UserId, preferences, updated_at AS UpdatedAt
            FROM user_preferences WHERE user_id = @UserId
            """;

        public const string Delete = "DELETE FROM user_preferences WHERE user_id = @UserId";
    }
}
