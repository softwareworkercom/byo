using SoftwareWorker.BYO.CLI.Core.Service;
using System.Data;
using System.Text.Json;

namespace SoftwareWorker.BYO.SDK.Service
{
    public static class ExportService
    {
        public static Task<string?> ExportFile(IReadOnlyList<DataTable> dataTables)
        {
            var path = Directory.GetCurrentDirectory();
            var filename = GetTimestampedFileName("export");
            var fullPath = Path.Combine(path, $"{filename}.json");

            if (dataTables.Count == 0)
            {
                throw new InvalidOperationException("No DataTables were added to ExportSource. Add at least one DataTable before using --export.");
            }

            ExportAsJsonFile(dataTables, path, filename);

            return Task.FromResult<string?>(fullPath);
        }
        private static void ExportAsJsonFile(IReadOnlyList<DataTable> dataTables, string rootPath, string fileName)
        {
            var filePath = Path.Combine(rootPath, $"{fileName}.json");

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var tableRows = new List<List<Dictionary<string, object?>>>(dataTables.Count);
            var totalRows = dataTables.Sum(table => table.Rows.Count);
            UserInterfaceService.ExecuteWithProgress(ctx =>
                {
                    var task = ctx.AddTask("Exporting JSON rows", maxValue: totalRows);
                    foreach (var dataTable in dataTables)
                    {
                        var rows = new List<Dictionary<string, object?>>(dataTable.Rows.Count);
                        foreach (DataRow row in dataTable.Rows)
                        {
                            var rowData = new Dictionary<string, object?>();
                            foreach (DataColumn column in dataTable.Columns)
                            {
                                var value = row[column];
                                rowData[column.ColumnName] = value == DBNull.Value ? null : value;
                            }

                            rows.Add(rowData);
                            task.Increment(1);
                        }

                        tableRows.Add(rows);
                    }
                });

            var exportData = new
            {
                Tables = dataTables.Select((table, index) => new
                {
                    DatabaseName = string.IsNullOrWhiteSpace(table.Namespace) ? null : table.Namespace,
                    TableName = string.IsNullOrWhiteSpace(table.TableName) ? GetTableDisplayName(table, index + 1) : table.TableName,
                    SchemaName = table.ExtendedProperties.ContainsKey("SchemaName")
                        ? table.ExtendedProperties["SchemaName"]?.ToString()
                        : null,
                    Columns = table.Columns.Cast<DataColumn>().Select(col => new
                    {
                        ColumnName = col.ColumnName,
                        DataType = col.DataType.FullName
                    }).ToArray(),
                    Data = tableRows[index].ToArray()
                }).ToArray()
            };

            var json = JsonSerializer.Serialize(exportData, jsonOptions);
            File.WriteAllText(filePath, json);

            UserInterfaceService.ShowMarkup($"[green]Exported to:[/] [bold]{filePath}[/]");
        }
        private static string GetTableDisplayName(DataTable dataTable, int index)
        {
            return string.IsNullOrWhiteSpace(dataTable.TableName) ? $"Table{index}" : dataTable.TableName;
        }

        private static string GetTimestampedFileName(string fileName)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"{fileName}_{timestamp}";
        }
    }
}