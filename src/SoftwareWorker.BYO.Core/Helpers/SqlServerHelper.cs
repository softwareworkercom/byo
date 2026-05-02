using Microsoft.Data.SqlClient;
using SoftwareWorker.BYO.CLI.Core.Service;
using System.Data;
using System.Text.RegularExpressions;

namespace SoftwareWorker.BYO.CLI.Core.Helpers
{
    public static class SqlServerHelper
    {
        public static async Task<IReadOnlyList<DataTable>> ExecuteSqlFileFromPathAsTables(string? connectionString, string? queryPath, int timeoutInSeconds, IReadOnlyDictionary<string, string>? tokenOverrides = null, string? scriptLocation = null)
        {
            scriptLocation = await ResolveScriptLocationAsync(queryPath, scriptLocation);

            if (string.IsNullOrEmpty(scriptLocation))
            {
                UserInterfaceService.ShowWarning("No script selected. Operation cancelled.");
                return [];
            }

            UserInterfaceService.ShowMarkup($"[green]Executing SQL script:[/] [cyan]{Path.GetFileName(scriptLocation)}[/]");
            UserInterfaceService.ShowInformation($"Location: {scriptLocation}");

            return await ExecuteSqlFileAsTables(connectionString, scriptLocation, timeoutInSeconds, tokenOverrides);
        }

        private static async Task<string?> ResolveScriptLocationAsync(string? queryPath, string? scriptLocation)
        {
            if (string.IsNullOrWhiteSpace(scriptLocation))
            {
                if (string.IsNullOrWhiteSpace(queryPath))
                {
                    return null;
                }

                return await FileHelper.SelectFile(queryPath, "sql");
            }

            if (File.Exists(scriptLocation))
            {
                return scriptLocation;
            }

            if (string.IsNullOrWhiteSpace(queryPath))
            {
                return scriptLocation;
            }

            var scriptName = scriptLocation.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
                ? scriptLocation
                : $"{scriptLocation}.sql";

            var resolvedScriptPath = Path.Combine(queryPath, scriptName);

            return resolvedScriptPath;
        }

        public static async Task<DataTable?> ExecuteSqlFileFromPath(string? connectionString, string? queryPath, int timeoutInSeconds, IReadOnlyDictionary<string, string>? tokenOverrides = null)
        {
            var results = await ExecuteSqlFileFromPathAsTables(connectionString, queryPath, timeoutInSeconds, tokenOverrides);
            return results.FirstOrDefault();
        }

        public static async Task<DataTable?> ExecuteSqlFile(string? connectionString, string scriptLocation, int timeoutInSeconds, IReadOnlyDictionary<string, string>? tokenOverrides = null)
        {
            var results = await ExecuteSqlFileAsTables(connectionString, scriptLocation, timeoutInSeconds, tokenOverrides);
            return results.FirstOrDefault();
        }

        public static async Task<DataTable?> ExecuteSqlQuery(string? connectionString, string? sql, int timeoutInSeconds, IReadOnlyDictionary<string, string>? tokenOverrides = null)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                UserInterfaceService.ShowWarning("No SQL query provided.");
                return null;
            }

            sql = TokenService.ResolveTokens(sql, tokenOverrides: tokenOverrides);
            sql = FormatPipeDelimitedValues(sql);

            try
            {
                var tables = await ExecuteSqlAsTables(connectionString, sql, timeoutInSeconds);
                return tables.FirstOrDefault();
            }
            catch (Exception ex)
            {
                UserInterfaceService.ShowError($"SQL Error: {ex.Message}");
                UserInterfaceService.ShowError($"SQL Query: {sql}");
                return null;
            }
        }

        private static async Task<IReadOnlyList<DataTable>> ExecuteSqlFileAsTables(string? connectionString, string scriptLocation, int timeoutInSeconds, IReadOnlyDictionary<string, string>? tokenOverrides = null)
        {
            if (!File.Exists(scriptLocation))
            {
                UserInterfaceService.ShowError($"SQL script file not found: {scriptLocation}");
                return [];
            }

            var sql = File.ReadAllText(scriptLocation);
            sql = TokenService.ResolveTokens(sql, tokenOverrides: tokenOverrides);
            sql = FormatPipeDelimitedValues(sql);

            try
            {
                return await ExecuteSqlAsTables(connectionString, sql, timeoutInSeconds);
            }
            catch (Exception ex)
            {
                UserInterfaceService.ShowError($"SQL Error: {ex.Message}");
                UserInterfaceService.ShowError($"SQL Query: {sql}");
                return [];
            }
        }

        private static async Task<IReadOnlyList<DataTable>> ExecuteSqlAsTables(string? connectionString, string sql, int timeoutInSeconds)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = timeoutInSeconds
            };

            using var reader = await command.ExecuteReaderAsync();

            var tables = new List<DataTable>();
            var tableIndex = 1;

            do
            {
                if (reader.FieldCount <= 0)
                {
                    continue;
                }

                var dataTable = new DataTable($"ResultSet{tableIndex}");
                var columnNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var baseColumnName = reader.GetName(i);
                    if (string.IsNullOrWhiteSpace(baseColumnName))
                    {
                        baseColumnName = $"Column{i + 1}";
                    }

                    if (!columnNameCounts.TryAdd(baseColumnName, 0))
                    {
                        columnNameCounts[baseColumnName]++;
                        baseColumnName = $"{baseColumnName}_{columnNameCounts[baseColumnName]}";
                    }

                    dataTable.Columns.Add(baseColumnName, reader.GetFieldType(i));
                }

                while (await reader.ReadAsync())
                {
                    var values = new object[reader.FieldCount];
                    reader.GetValues(values);
                    dataTable.Rows.Add(values);
                }

                tables.Add(dataTable);
                tableIndex++;
            }
            while (await reader.NextResultAsync());

            if (tables.Count == 0)
            {
                UserInterfaceService.ShowWarning("No tabular result sets returned from the query.");
            }

            return tables;
        }

        private static string FormatPipeDelimitedValues(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql) || !sql.Contains('|'))
            {
                return sql;
            }

            return Regex.Replace(
                sql,
                @"'(?<values>[^'\r\n;()]+\|[^'\r\n;()]+)'|(?<!')(?<values>[^'\r\n;()]+\|[^'\r\n;()]+)(?!')",
                match => string.Join(", ", match.Groups["values"].Value
                    .Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => $"'{value.Trim()}'")));
        }
    }
}
