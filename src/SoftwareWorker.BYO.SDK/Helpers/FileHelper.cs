using SoftwareWorker.BYO.CLI.Core.Service;
using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Xml;

namespace SoftwareWorker.BYO.CLI.Core.Helpers
{
    public static class FileHelper
    {
        public static void ReplaceFile(string sourceFilePath, string destinationFilePath)
        {
            // Copy the file and overwrite if it already exists
            File.Copy(sourceFilePath, destinationFilePath, true);
            Console.WriteLine($"{destinationFilePath} replaced successfully.");
        }

        public static async Task<string> SelectFile(string path, string fileExtension)
        {
            var files = Directory.GetFiles(path, $"*.{fileExtension}", SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
            {
                UserInterfaceService.ShowError($"No {fileExtension} files found in the specified path.");
                return string.Empty;
            }

            var filenames = files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();


            var selectedFile = UserInterfaceService.SelectSingleItem(
                                                        $"[green]Select a {fileExtension} file to execute:[/]",
                                                        filenames
                                                    );


            return Path.Combine(path, $"{selectedFile}.{fileExtension}");
        }

        public static void ChangeValue(XmlDocument xdoc, string key, string newValue)
        {
            UserInterfaceService.ShowError($"{key}={newValue}");
            XmlNode xn = xdoc.SelectSingleNode($"//add[@key=\"{key}\"]")!;
            XmlElement el = (XmlElement)xn;
            el.SetAttribute("value", newValue);
        }

        public static string ReadFile(string fileLocation)
        {
            return File.ReadAllText(fileLocation);
        }

        public static void SaveFile(string fileLocation, string content)
        {
            var directory = Path.GetDirectoryName(fileLocation);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(fileLocation, content);
        }

        public static List<FileInfo> FindAllFiles(string path, string name)
        {
            var response = new List<FileInfo>();
            var files = Directory.GetFiles(path, name, SearchOption.AllDirectories);
            files.ToList().ForEach(f => response.Add(new FileInfo(f)));
            return response;
        }

        public static void ExtractZipFile(string zipFolder, string zipFile)
        {
            if (Directory.Exists(zipFolder))
            {
                Directory.Delete(zipFolder, true);
            }
            Directory.CreateDirectory(zipFolder);

            using (ZipArchive archive = ZipFile.OpenRead(zipFile))
            {
                //Limit to prevent zip bomb attacks
                //https://sonarsource.github.io/rspec/#/rspec/S5042
                var MaxFileCount = 1000;
                var MaxExtractedSize = 100 * 1024 * 1024; // 100 MB;

                long totalSize = 0;
                int fileCount = 0;

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (fileCount >= MaxFileCount)
                    {
                        throw new InvalidOperationException("Zip file contains too many files.");
                    }

                    string destinationPath = Path.GetFullPath(Path.Combine(zipFolder, entry.FullName));

                    if (!destinationPath.StartsWith(zipFolder, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Zip file contains invalid file paths.");
                    }

                    totalSize += entry.Length;
                    if (totalSize > MaxExtractedSize)
                    {
                        throw new InvalidOperationException("Zip file is too large.");
                    }

                    if (entry.Name == "")
                    {
                        // Assuming it's a directory
                        Directory.CreateDirectory(destinationPath);
                    }
                    else
                    {
                        entry.ExtractToFile(destinationPath, true);
                    }

                    fileCount++;
                }
            }

            UserInterfaceService.ShowGreen($"Zip file extracted to {zipFolder}");
        }


        public static void CopyAllContent(string sourceDirectory, string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);

            foreach (string file in Directory.GetFiles(sourceDirectory))
            {
                string targetFile = Path.Combine(targetDirectory, Path.GetFileName(file));
                File.Copy(file, targetFile, true);
            }

            foreach (string directory in Directory.GetDirectories(sourceDirectory))
            {
                string targetDirectoryPath = Path.Combine(targetDirectory, Path.GetFileName(directory));
                CopyAllContent(directory, targetDirectoryPath);
            }
        }


        public static async Task DownloadFileAsync(string url, string authorization, string filePath)
        {
            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
            {
                client.DefaultRequestHeaders.Add("Authorization", authorization);

                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault();
                    await StartDownloadWithProgressBarAsync(
                        () => response.Content.ReadAsStreamAsync(),
                        totalBytes,
                        filePath,
                        "[green]Downloading file...[/]"
                    );
                }
            }
        }

        private static async Task StartDownloadWithProgressBarAsync(
                   Func<Task<Stream>> getContentStreamAsync,
                   long totalBytes,
                   string filePath,
                   string taskDescription)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (Stream contentStream = await getContentStreamAsync())
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                await UserInterfaceService.ExecuteWithProgressAsync(async ctx =>
                {
                    var task = ctx.AddTask(taskDescription, maxValue: totalBytes);

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;
                        task.Increment(bytesRead);

                        // Update the progress bar
                        task.Value = totalRead;
                    }
                });
            }
        }

        public static DataTable ImportFromJsonFile(string rootPath, string fileName)
        {
            var filePath = Path.Combine(rootPath, $"{fileName}.json");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var json = File.ReadAllText(filePath);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var exportData = JsonSerializer.Deserialize<JsonElement>(json, jsonOptions);

            var dataTable = new DataTable();

            if (exportData.TryGetProperty("databaseName", out var databaseNameElement))
            {
                dataTable.Namespace = databaseNameElement.GetString() ?? string.Empty;
            }

            if (exportData.TryGetProperty("tableName", out var tableNameElement))
            {
                dataTable.TableName = tableNameElement.GetString() ?? string.Empty;
            }

            if (exportData.TryGetProperty("schemaName", out var schemaNameElement))
            {
                var schemaName = schemaNameElement.GetString();
                if (!string.IsNullOrWhiteSpace(schemaName))
                {
                    dataTable.ExtendedProperties["SchemaName"] = schemaName;
                }
            }

            // Recreate columns with original types if schema is available
            if (exportData.TryGetProperty("columns", out var columnsElement) ||
                exportData.TryGetProperty("schema", out columnsElement))
            {
                foreach (var columnElement in columnsElement.EnumerateArray())
                {
                    var columnName = columnElement.GetProperty("columnName").GetString();
                    var dataTypeName = columnElement.GetProperty("dataType").GetString();
                    var dataType = Type.GetType(dataTypeName) ?? typeof(string);

                    dataTable.Columns.Add(columnName, dataType);
                }
            }

            // Add data rows
            if (exportData.TryGetProperty("data", out var dataElement))
            {
                foreach (var rowElement in dataElement.EnumerateArray())
                {
                    var dataRow = dataTable.NewRow();

                    foreach (var property in rowElement.EnumerateObject())
                    {
                        // If no schema was found, add columns dynamically
                        if (!dataTable.Columns.Contains(property.Name))
                        {
                            dataTable.Columns.Add(property.Name, typeof(string));
                        }

                        var targetType = dataTable.Columns[property.Name]!.DataType;
                        dataRow[property.Name] = ConvertJsonValue(property.Value, targetType);
                    }

                    dataTable.Rows.Add(dataRow);
                }
            }

            UserInterfaceService.ShowMarkup($"[green]Imported from:[/] [bold]{filePath}[/]");
            return dataTable;
        }

        private static object ConvertJsonValue(JsonElement value, Type targetType)
        {
            if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return DBNull.Value;
            }

            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (value.ValueKind == JsonValueKind.Object)
            {
                if (!value.EnumerateObject().Any())
                {
                    return underlyingType == typeof(string) ? "{}" : DBNull.Value;
                }

                return underlyingType == typeof(string)
                    ? value.GetRawText()
                    : DBNull.Value;
            }

            if (value.ValueKind == JsonValueKind.Array)
            {
                return underlyingType == typeof(string)
                    ? value.GetRawText()
                    : DBNull.Value;
            }

            var rawText = value.ToString();

            if (string.IsNullOrWhiteSpace(rawText))
            {
                return underlyingType == typeof(string) ? string.Empty : DBNull.Value;
            }

            if (rawText == "{}" && underlyingType != typeof(string))
            {
                return DBNull.Value;
            }

            try
            {
                if (underlyingType == typeof(string)) return rawText;
                if (underlyingType == typeof(int)) return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue) ? intValue : int.Parse(rawText, CultureInfo.InvariantCulture);
                if (underlyingType == typeof(long)) return value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var longValue) ? longValue : long.Parse(rawText, CultureInfo.InvariantCulture);
                if (underlyingType == typeof(short)) return value.ValueKind == JsonValueKind.Number && value.TryGetInt16(out var shortValue) ? shortValue : short.Parse(rawText, CultureInfo.InvariantCulture);
                if (underlyingType == typeof(byte)) return value.ValueKind == JsonValueKind.Number && value.TryGetByte(out var byteValue) ? byteValue : byte.Parse(rawText, CultureInfo.InvariantCulture);
                if (underlyingType == typeof(decimal)) return value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var decimalValue) ? decimalValue : decimal.Parse(rawText, CultureInfo.InvariantCulture);
                if (underlyingType == typeof(double)) return value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var doubleValue) ? doubleValue : double.Parse(rawText, CultureInfo.InvariantCulture);
                if (underlyingType == typeof(float)) return value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out var floatValue) ? floatValue : float.Parse(rawText, CultureInfo.InvariantCulture);
                if (underlyingType == typeof(bool)) return value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False ? value.GetBoolean() : bool.Parse(rawText);
                if (underlyingType == typeof(Guid)) return value.ValueKind == JsonValueKind.String ? value.GetGuid() : Guid.Parse(rawText);
                if (underlyingType == typeof(DateTime)) return value.ValueKind == JsonValueKind.String ? value.GetDateTime() : DateTime.Parse(rawText, CultureInfo.InvariantCulture);
                if (underlyingType == typeof(DateTimeOffset)) return value.ValueKind == JsonValueKind.String ? value.GetDateTimeOffset() : DateTimeOffset.Parse(rawText, CultureInfo.InvariantCulture);
                if (underlyingType == typeof(TimeSpan)) return TimeSpan.Parse(rawText, CultureInfo.InvariantCulture);
                if (underlyingType.IsEnum) return Enum.Parse(underlyingType, rawText, ignoreCase: true);

                return Convert.ChangeType(rawText, underlyingType, CultureInfo.InvariantCulture) ?? DBNull.Value;
            }
            catch
            {
                return underlyingType == typeof(string) ? rawText : DBNull.Value;
            }
        }

    }
}
