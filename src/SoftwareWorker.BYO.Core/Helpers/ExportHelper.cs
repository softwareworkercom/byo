using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Model.Enums;
using SpreadCheetah;
using SpreadCheetah.Worksheets;
using System.Data;
using System.Text.Json;

namespace SoftwareWorker.BYO.CLI.Core.Helpers
{
    public static class ExportHelper
    {
        public static async Task<string?> ExportFile(ExportEnum format, DataTable dataTable, string path, string filename)
        {
            return await ExportFile(format, [dataTable], path, filename);
        }

        public static async Task<string?> ExportFile(ExportEnum format, IReadOnlyList<DataTable> dataTables, string path, string filename)
        {
            filename = GetTimestampedFileName(filename);
            var fullPath = Path.Combine(path, $"{filename}.{format.ToString().ToLower()}");

            if (dataTables.Count == 0)
            {
                UserInterfaceService.ShowWarning("No tables available to export.");
                return null;
            }

            switch (format)
            {
                case ExportEnum.Json:
                    ExportAsJsonFile(dataTables, path, filename);
                    break;
                case ExportEnum.Csv:
                    ExportAsCsvFile(dataTables, path, filename);
                    break;
                case ExportEnum.Excel:
                    await ExportAsExcelFile(dataTables, path, filename);
                    break;
            }

            return fullPath;
        }

        private static void ExportAsCsvFile(IReadOnlyList<DataTable> dataTables, string rootPath, string fileName)
        {
            var filePath = Path.Combine(rootPath, $"{fileName}.csv");
            using (var writer = new StreamWriter(filePath))
            {
                var totalRows = dataTables.Sum(table => table.Rows.Count);
                var includeTableHeaders = dataTables.Count > 1;

                UserInterfaceService.ExecuteWithProgress(ctx =>
                    {
                        var task = ctx.AddTask("Exporting CSV rows", maxValue: totalRows);
                        for (var tableIndex = 0; tableIndex < dataTables.Count; tableIndex++)
                        {
                            var dataTable = dataTables[tableIndex];

                            if (includeTableHeaders)
                            {
                                if (tableIndex > 0)
                                {
                                    writer.WriteLine();
                                }

                                writer.WriteLine($"\"Table\",\"{EscapeCsvValue(GetTableDisplayName(dataTable, tableIndex + 1))}\"");
                            }

                            var columnNames = dataTable.Columns.Cast<DataColumn>().Select(col => col.ColumnName);
                            writer.WriteLine(string.Join(",", columnNames));

                            foreach (DataRow row in dataTable.Rows)
                            {
                                var fields = row.ItemArray.Select(field => EscapeCsvValue(field?.ToString() ?? string.Empty));
                                writer.WriteLine(string.Join(",", fields.Select(f => $"\"{f}\"")));
                                task.Increment(1);
                            }
                        }
                    });
            }

            UserInterfaceService.ShowMarkup($"[green]Exported to:[/] [bold]{filePath}[/]");
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
        private static async Task ExportAsExcelFile(IReadOnlyList<DataTable> dataTables, string rootPath, string fileName)
        {
            var filePath = Path.Combine(rootPath, $"{fileName}.xlsx");

            using var fileStream = new FileStream(filePath, FileMode.Create);
            using var spreadsheet = await Spreadsheet.CreateNewAsync(fileStream);

            var totalRows = dataTables.Sum(table => table.Rows.Count);
            var worksheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await UserInterfaceService.ExecuteWithProgressAsync(async ctx =>
            {
                var task = ctx.AddTask("Exporting Excel rows", maxValue: totalRows);

                for (var tableIndex = 0; tableIndex < dataTables.Count; tableIndex++)
                {
                    var dataTable = dataTables[tableIndex];
                    var worksheetName = GetWorksheetName(dataTable, tableIndex + 1, dataTables.Count == 1 ? fileName : null, worksheetNames);

                    var endColumn = GetColumnLetter(dataTable.Columns.Count);
                    var endRow = dataTable.Rows.Count + 1;
                    var autoFilterRange = $"A1:{endColumn}{endRow}";

                    var worksheetOptions = new WorksheetOptions
                    {
                        FrozenRows = 1,
                        AutoFilter = new AutoFilterOptions(autoFilterRange)
                    };

                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        var columnIndex = i + 1;
                        var maxWidth = CalculateColumnWidth(dataTable, i);
                        worksheetOptions.Column(columnIndex).Width = maxWidth;
                    }

                    await spreadsheet.StartWorksheetAsync(worksheetName, worksheetOptions);

                    var headerNames = dataTable.Columns.Cast<DataColumn>().Select(col => col.ColumnName).ToArray();
                    await spreadsheet.AddHeaderRowAsync(headerNames);

                    foreach (DataRow row in dataTable.Rows)
                    {
                        var cells = row.ItemArray.Select(field => new Cell(field?.ToString() ?? string.Empty)).ToArray();
                        await spreadsheet.AddRowAsync(cells);
                        task.Increment(1);
                    }
                }
            });

            await spreadsheet.FinishAsync();

            UserInterfaceService.ShowMarkup($"[green]Exported to:[/] [bold]{filePath}[/]");
        }

        private static string BuildMultiTableContextData(IReadOnlyList<DataTable> dataTables)
        {
            var payload = new
            {
                Tables = dataTables.Select((dataTable, index) => new
                {
                    TableName = GetTableDisplayName(dataTable, index + 1),
                    Data = ConvertDataTableToRows(dataTable)
                }).ToArray()
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(payload, jsonOptions);
        }

        private static List<Dictionary<string, object?>> ConvertDataTableToRows(DataTable dataTable)
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
            }

            return rows;
        }

        private static string EscapeCsvValue(string value)
        {
            return value.Replace("\"", "\"\"");
        }

        private static string GetTableDisplayName(DataTable dataTable, int index)
        {
            return string.IsNullOrWhiteSpace(dataTable.TableName) ? $"Table{index}" : dataTable.TableName;
        }

        private static string GetWorksheetName(DataTable dataTable, int index, string? defaultName, HashSet<string> existingNames)
        {
            var rawName = string.IsNullOrWhiteSpace(defaultName)
                ? GetTableDisplayName(dataTable, index)
                : defaultName;

            var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };
            var cleanedName = new string(rawName.Select(character => invalidChars.Contains(character) ? '_' : character).ToArray()).Trim();
            if (cleanedName.Length == 0)
            {
                cleanedName = $"Table{index}";
            }

            if (cleanedName.Length > 31)
            {
                cleanedName = cleanedName[..31];
            }

            var worksheetName = cleanedName;
            var suffix = 1;
            while (!existingNames.Add(worksheetName))
            {
                var suffixText = $"_{suffix}";
                var baseLength = Math.Min(cleanedName.Length, 31 - suffixText.Length);
                worksheetName = $"{cleanedName[..baseLength]}{suffixText}";
                suffix++;
            }

            return worksheetName;
        }

        private static double CalculateColumnWidth(DataTable dataTable, int columnIndex)
        {
            var column = dataTable.Columns[columnIndex];
            double maxWidth = column.ColumnName.Length; // Start with header length

            // Check all data rows for this column
            foreach (DataRow row in dataTable.Rows)
            {
                var cellValue = row[columnIndex]?.ToString() ?? string.Empty;
                if (cellValue.Length > maxWidth)
                {
                    maxWidth = cellValue.Length;
                }
            }

            // Apply some padding and convert to Excel width units
            // Excel width units are approximately 1/256th of the width of the zero character
            // in the default font. A rough approximation is character count * 1.2 + 2 for padding
            var excelWidth = Math.Min(maxWidth * 1.2 + 2, 100); // Cap at 100 to prevent extremely wide columns
            return Math.Max(excelWidth, 8); // Minimum width of 8
        }
        private static string GetColumnLetter(int columnNumber)
        {
            string columnLetter = "";
            while (columnNumber > 0)
            {
                int remainder = (columnNumber - 1) % 26;
                columnLetter = (char)('A' + remainder) + columnLetter;
                columnNumber = (columnNumber - 1) / 26;
            }
            return columnLetter;
        }

        private static string GetTimestampedFileName(string fileName)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"{fileName}_{timestamp}";
        }
    }
}
