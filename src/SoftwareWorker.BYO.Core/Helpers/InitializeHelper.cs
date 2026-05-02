using Microsoft.Data.Sqlite;
using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Service;

namespace SoftwareWorker.BYO.CLI.Core.Helpers
{
    public static class InitializeHelper
    {
        private const string CreateLlmContextTableSql = """
            CREATE TABLE IF NOT EXISTS LlmContext (
                key TEXT PRIMARY KEY,
                context_json TEXT NOT NULL,
                updated_utc TEXT NOT NULL
            );
            """;

        public static void Initialize()
        {
            Directory.CreateDirectory(SystemConstants.STORAGE_DIRECTORY);

            using var connection = new SqliteConnection($"Data Source={SystemConstants.STORAGE_LOCAL_DB_FILE};Mode=ReadWriteCreate;Cache=Shared");
            connection.Open();

            using var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = CreateLlmContextTableSql;
            createTableCommand.ExecuteNonQuery();

            UserInterfaceService.ShowInformation($"Initialized local context database: {SystemConstants.STORAGE_LOCAL_DB_FILE}");
        }
    }
}
