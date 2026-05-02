using Microsoft.Data.Sqlite;
using SoftwareWorker.BYO.CLI.Core.Constants;
using System.Text.Json;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public static class ContextService
    {
        private static readonly Lock DbLock = new();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static void SetContextData(string cacheKey, string jsonFile)
        {
            if (string.IsNullOrWhiteSpace(jsonFile))
            {
                return;
            }

            UpsertContext(cacheKey, jsonFile);
            UserInterfaceService.ShowInformation($"Saved context: {cacheKey}");
        }

        public static string? GetContextData(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                return null;
            }

            lock (DbLock)
            {
                using var connection = CreateConnection();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT context_json FROM LlmContext WHERE key = $key LIMIT 1;";
                command.Parameters.AddWithValue("$key", cacheKey);

                return command.ExecuteScalar() as string;
            }
        }

        public static (string Key, string ContextJson)? GetMostRecentContextData()
        {
            lock (DbLock)
            {
                using var connection = CreateConnection();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT key, context_json FROM LlmContext ORDER BY updated_utc DESC LIMIT 1;";

                using var reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    return null;
                }

                var key = reader.GetString(0);
                var contextJson = reader.GetString(1);
                return (key, contextJson);
            }
        }

        private static void UpsertContext(string cacheKey, string contextJson)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                return;
            }

            lock (DbLock)
            {
                using var connection = CreateConnection();
                using var command = connection.CreateCommand();
                command.CommandText = """
                    INSERT INTO LlmContext(key, context_json, updated_utc)
                    VALUES ($key, $json, $updated)
                    ON CONFLICT(key)
                    DO UPDATE SET context_json = excluded.context_json, updated_utc = excluded.updated_utc;
                    """;
                command.Parameters.AddWithValue("$key", cacheKey);
                command.Parameters.AddWithValue("$json", contextJson);
                command.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O"));
                command.ExecuteNonQuery();
            }
        }

        private static SqliteConnection CreateConnection()
        {
            if (!File.Exists(SystemConstants.STORAGE_LOCAL_DB_FILE))
            {
                throw new InvalidOperationException("Local context database is not initialized. Run 'init' first.");
            }

            var connection = new SqliteConnection($"Data Source={SystemConstants.STORAGE_LOCAL_DB_FILE};Mode=ReadWrite;Cache=Shared");
            connection.Open();

            using var validateTableCommand = connection.CreateCommand();
            validateTableCommand.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'LlmContext' LIMIT 1;";
            if (validateTableCommand.ExecuteScalar() is null)
            {
                throw new InvalidOperationException("Local context table is not initialized. Run 'init' first.");
            }

            return connection;
        }

        private sealed class ContextDataPayload
        {
            public string? Namespace { get; set; }
            public string? TableName { get; set; }
            public ContextColumn[] Columns { get; set; } = [];
            public Dictionary<string, object?>[] Data { get; set; } = [];
        }

        private sealed class ContextColumn
        {
            public string ColumnName { get; set; } = string.Empty;
            public string DataType { get; set; } = string.Empty;
        }
    }
}
