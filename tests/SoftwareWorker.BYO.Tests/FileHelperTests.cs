using SoftwareWorker.BYO.SDK.Helpers;
using SoftwareWorker.BYO.SDK.Model.Enums;
using SoftwareWorker.BYO.Tests.Support;
using System.Data;
using System.IO.Compression;
using System.Xml;

namespace SoftwareWorker.BYO.Tests;

public sealed class FileHelperTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("byo-files-").FullName;

    [Fact]
    public void SaveFile_ReadFile_AndReplaceFile_ShouldRoundTripContent()
    {
        using var console = new ConsoleScope();
        var source = Path.Combine(_root, "nested", "deeper", "source.txt");
        var destination = Path.Combine(_root, "destination.txt");

        FileHelper.SaveFile(source, "content");
        File.WriteAllText(destination, "old");
        FileHelper.ReplaceFile(source, destination);

        Assert.Equal("content", FileHelper.ReadFile(source));
        Assert.Equal("content", File.ReadAllText(destination));
        Assert.Contains("replaced successfully", console.SystemOutput);
    }

    [Fact]
    public void FindAllFiles_ShouldSearchSubdirectories()
    {
        FileHelper.SaveFile(Path.Combine(_root, "a.sql"), "1");
        FileHelper.SaveFile(Path.Combine(_root, "sub", "b.sql"), "2");
        FileHelper.SaveFile(Path.Combine(_root, "sub", "c.txt"), "3");

        var files = FileHelper.FindAllFiles(_root, "*.sql");

        Assert.Equal(["a.sql", "b.sql"], files.Select(file => file.Name).OrderBy(name => name));
    }

    [Fact]
    public void CopyAllContent_ShouldCopyFilesAndSubdirectories()
    {
        var source = Path.Combine(_root, "source");
        var target = Path.Combine(_root, "target");
        FileHelper.SaveFile(Path.Combine(source, "root.txt"), "root");
        FileHelper.SaveFile(Path.Combine(source, "child", "leaf.txt"), "leaf");

        FileHelper.CopyAllContent(source, target);

        Assert.Equal("root", File.ReadAllText(Path.Combine(target, "root.txt")));
        Assert.Equal("leaf", File.ReadAllText(Path.Combine(target, "child", "leaf.txt")));
    }

    [Fact]
    public void ExtractZipFile_ShouldRecreateTheTargetFolder_AndExtractFilesAndDirectories()
    {
        using var console = new ConsoleScope();
        var zipPath = Path.Combine(_root, "archive.zip");
        var target = Path.Combine(_root, "extracted");
        FileHelper.SaveFile(Path.Combine(target, "stale.txt"), "stale");
        // Nested files are extracted only after their directory entry, which is how archivers lay zips out.
        CreateZip(zipPath, ("a.txt", "A"), ("sub/", null), ("sub/b.txt", "B"), ("empty/", null));

        FileHelper.ExtractZipFile(target, zipPath);

        Assert.False(File.Exists(Path.Combine(target, "stale.txt")));
        Assert.Equal("A", File.ReadAllText(Path.Combine(target, "a.txt")));
        Assert.Equal("B", File.ReadAllText(Path.Combine(target, "sub", "b.txt")));
        Assert.True(Directory.Exists(Path.Combine(target, "empty")));
        Assert.Contains($"Zip file extracted to {target}", console.Output);
    }

    [Fact]
    public void ExtractZipFile_ShouldRejectEntriesOutsideTheTargetFolder()
    {
        var zipPath = Path.Combine(_root, "traversal.zip");
        CreateZip(zipPath, ("../evil.txt", "evil"));

        var exception = Assert.Throws<InvalidOperationException>(() => FileHelper.ExtractZipFile(Path.Combine(_root, "out"), zipPath));

        Assert.Equal("Zip file contains invalid file paths.", exception.Message);
    }

    [Fact]
    public void ExtractZipFile_ShouldRejectArchivesWithTooManyFiles()
    {
        var zipPath = Path.Combine(_root, "many.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            for (var index = 0; index <= 1000; index++)
            {
                archive.CreateEntry($"file-{index}.txt");
            }
        }

        var exception = Assert.Throws<InvalidOperationException>(() => FileHelper.ExtractZipFile(Path.Combine(_root, "out"), zipPath));

        Assert.Equal("Zip file contains too many files.", exception.Message);
    }

    [Fact]
    public void ExtractZipFile_ShouldRejectArchivesThatExpandBeyondTheLimit()
    {
        var zipPath = Path.Combine(_root, "large.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        using (var stream = archive.CreateEntry("zeros.bin").Open())
        {
            var chunk = new byte[1024 * 1024];
            for (var index = 0; index < 101; index++)
            {
                stream.Write(chunk);
            }
        }

        var exception = Assert.Throws<InvalidOperationException>(() => FileHelper.ExtractZipFile(Path.Combine(_root, "out"), zipPath));

        Assert.Equal("Zip file is too large.", exception.Message);
    }

    [Fact]
    public void ChangeValue_ShouldUpdateTheMatchingAddElement()
    {
        using var console = new ConsoleScope();
        var document = new XmlDocument();
        document.LoadXml("""<configuration><appSettings><add key="Mode" value="old" /><add key="Other" value="x" /></appSettings></configuration>""");

        FileHelper.ChangeValue(document, "Mode", "new");

        Assert.Equal("new", document.SelectSingleNode("//add[@key='Mode']")!.Attributes!["value"]!.Value);
        Assert.Equal("x", document.SelectSingleNode("//add[@key='Other']")!.Attributes!["value"]!.Value);
    }

    [Fact]
    public void ImportFromJsonFile_ShouldThrow_WhenTheFileIsMissing()
    {
        Assert.Throws<FileNotFoundException>(() => FileHelper.ImportFromJsonFile(_root, "missing"));
    }

    [Fact]
    public void ImportFromJsonFile_ShouldRebuildTypedColumns_AndConvertValues()
    {
        using var console = new ConsoleScope();
        var enumType = typeof(ShellTypeEnum).AssemblyQualifiedName;
        File.WriteAllText(Path.Combine(_root, "typed.json"), $$"""
            {
              "databaseName": "Sales",
              "tableName": "Orders",
              "schemaName": "dbo",
              "columns": [
                { "columnName": "Id", "dataType": "System.Int32" },
                { "columnName": "Big", "dataType": "System.Int64" },
                { "columnName": "Small", "dataType": "System.Int16" },
                { "columnName": "Tiny", "dataType": "System.Byte" },
                { "columnName": "Price", "dataType": "System.Decimal" },
                { "columnName": "Ratio", "dataType": "System.Double" },
                { "columnName": "Single", "dataType": "System.Single" },
                { "columnName": "Flag", "dataType": "System.Boolean" },
                { "columnName": "Key", "dataType": "System.Guid" },
                { "columnName": "When", "dataType": "System.DateTime" },
                { "columnName": "Offset", "dataType": "System.DateTimeOffset" },
                { "columnName": "Span", "dataType": "System.TimeSpan" },
                { "columnName": "Shell", "dataType": "{{enumType}}" },
                { "columnName": "Letter", "dataType": "System.Char" },
                { "columnName": "Text", "dataType": "System.String" },
                { "columnName": "Unknown", "dataType": "No.Such.Type" }
              ],
              "data": [
                {
                  "Id": 1, "Big": 9000000000, "Small": 7, "Tiny": 8, "Price": 19.99, "Ratio": 0.5, "Single": 1.5, "Flag": true,
                  "Key": "7f3c0d1e-0a4b-4c8d-9e2f-1a2b3c4d5e6f", "When": "2024-01-02T03:04:05", "Offset": "2024-01-02T03:04:05+02:00",
                  "Span": "01:02:03", "Shell": "cmd", "Letter": "x", "Text": { "nested": true }, "Unknown": "plain"
                },
                {
                  "Id": "12", "Big": "13", "Small": "14", "Tiny": "15", "Price": "1.25", "Ratio": "2.5", "Single": "3.5", "Flag": "false",
                  "Key": "{{Guid.Empty}}", "When": "2024-05-06T07:08:09", "Offset": "2024-05-06T07:08:09Z", "Span": "00:00:01",
                  "Shell": "Wsl", "Letter": "y", "Text": [1, 2], "Unknown": null
                },
                {
                  "Id": "abc", "Big": "", "Small": {}, "Tiny": [1], "Price": " ", "Ratio": null, "Single": "x", "Flag": "maybe",
                  "Key": "not-a-guid", "When": "nope", "Offset": "nope", "Span": "nope", "Shell": "nope", "Letter": "", "Text": {}, "Unknown": ""
                }
              ]
            }
            """);

        var table = FileHelper.ImportFromJsonFile(_root, "typed");

        Assert.Equal("Sales", table.Namespace);
        Assert.Equal("Orders", table.TableName);
        Assert.Equal("dbo", table.ExtendedProperties["SchemaName"]);
        Assert.Equal(typeof(int), table.Columns["Id"]!.DataType);
        Assert.Equal(typeof(ShellTypeEnum), table.Columns["Shell"]!.DataType);
        Assert.Equal(typeof(string), table.Columns["Unknown"]!.DataType);
        Assert.Equal(3, table.Rows.Count);

        var first = table.Rows[0];
        Assert.Equal(1, first["Id"]);
        Assert.Equal(9_000_000_000L, first["Big"]);
        Assert.Equal((short)7, first["Small"]);
        Assert.Equal((byte)8, first["Tiny"]);
        Assert.Equal(19.99m, first["Price"]);
        Assert.Equal(0.5, first["Ratio"]);
        Assert.Equal(1.5f, first["Single"]);
        Assert.Equal(true, first["Flag"]);
        Assert.Equal(Guid.Parse("7f3c0d1e-0a4b-4c8d-9e2f-1a2b3c4d5e6f"), first["Key"]);
        Assert.Equal(new DateTime(2024, 1, 2, 3, 4, 5), first["When"]);
        Assert.Equal(new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(2)), first["Offset"]);
        Assert.Equal(new TimeSpan(1, 2, 3), first["Span"]);
        Assert.Equal((int)ShellTypeEnum.Cmd, Convert.ToInt32(first["Shell"]));
        Assert.Equal('x', first["Letter"]);
        Assert.Equal("""{ "nested": true }""", first["Text"]);
        Assert.Equal("plain", first["Unknown"]);

        var second = table.Rows[1];
        Assert.Equal(12, second["Id"]);
        Assert.Equal(13L, second["Big"]);
        Assert.Equal((short)14, second["Small"]);
        Assert.Equal((byte)15, second["Tiny"]);
        Assert.Equal(1.25m, second["Price"]);
        Assert.Equal(2.5, second["Ratio"]);
        Assert.Equal(3.5f, second["Single"]);
        Assert.Equal(false, second["Flag"]);
        Assert.Equal(Guid.Empty, second["Key"]);
        Assert.Equal((int)ShellTypeEnum.Wsl, Convert.ToInt32(second["Shell"]));
        Assert.Equal("[1, 2]", second["Text"]);
        Assert.Equal(DBNull.Value, second["Unknown"]);

        var third = table.Rows[2];
        Assert.All(["Id", "Big", "Small", "Tiny", "Price", "Ratio", "Single", "Flag", "Key", "When", "Offset", "Span", "Shell", "Letter"],
            column => Assert.Equal(DBNull.Value, third[column]));
        Assert.Equal("{}", third["Text"]);
        Assert.Equal(string.Empty, third["Unknown"]);
        Assert.Contains("Imported from:", console.Output);
    }

    [Fact]
    public void ImportFromJsonFile_ShouldCreateStringColumnsDynamically_WhenThereIsNoSchema()
    {
        using var console = new ConsoleScope();
        File.WriteAllText(Path.Combine(_root, "loose.json"), """
            {
              "tableName": "Loose",
              "data": [
                { "a": 1, "b": "text", "c": null },
                { "a": "2", "d": { "x": 1 } }
              ]
            }
            """);

        var table = FileHelper.ImportFromJsonFile(_root, "loose");

        Assert.Equal(["a", "b", "c", "d"], table.Columns.Cast<DataColumn>().Select(column => column.ColumnName));
        Assert.All(table.Columns.Cast<DataColumn>(), column => Assert.Equal(typeof(string), column.DataType));
        Assert.Equal("1", table.Rows[0]["a"]);
        Assert.Equal("text", table.Rows[0]["b"]);
        Assert.Equal(DBNull.Value, table.Rows[0]["c"]);
        Assert.Equal("""{ "x": 1 }""", table.Rows[1]["d"]);
    }

    [Fact]
    public void ImportFromJsonFile_ShouldAcceptTheSchemaKeyForColumns()
    {
        using var console = new ConsoleScope();
        File.WriteAllText(Path.Combine(_root, "schema.json"), """
            {
              "schema": [ { "columnName": "Id", "dataType": "System.Int32" } ],
              "data": [ { "Id": 5 } ]
            }
            """);

        var table = FileHelper.ImportFromJsonFile(_root, "schema");

        Assert.Equal(typeof(int), table.Columns["Id"]!.DataType);
        Assert.Equal(5, table.Rows[0]["Id"]);
    }

    [Fact]
    public async Task SelectFile_ShouldReturnEmpty_WhenNoFilesMatch()
    {
        using var console = new ConsoleScope();

        var selected = await FileHelper.SelectFile(_root, "sql");

        Assert.Equal(string.Empty, selected);
        Assert.Contains("No sql files found", console.Output);
    }

    [Fact]
    public async Task SelectFile_ShouldReturnTheChosenFile()
    {
        using var console = new ConsoleScope(interactive: true);
        File.WriteAllText(Path.Combine(_root, "query.sql"), "select 1");
        console.PushKeys(ConsoleKey.Enter);

        var selected = await FileHelper.SelectFile(_root, "sql");

        Assert.Equal(Path.Combine(_root, "query.sql"), selected);
    }

    [Fact]
    public async Task DownloadFileAsync_ShouldWriteTheResponseBody_AndSendTheAuthorizationHeader()
    {
        using var console = new ConsoleScope();
        using var server = new LocalHttpServer(_ => new LocalHttpResponse(200, "downloaded-bytes", "application/octet-stream"));
        var target = Path.Combine(_root, "download.bin");
        File.WriteAllText(target, "stale");

        await FileHelper.DownloadFileAsync($"{server.BaseUrl}/file", "Bearer token", target);

        Assert.Equal("downloaded-bytes", await File.ReadAllTextAsync(target, TestContext.Current.CancellationToken));
        Assert.Equal("Bearer token", server.LastRequest.Authorization);
    }

    private static void CreateZip(string zipPath, params (string Name, string? Content)[] entries)
    {
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            var entry = archive.CreateEntry(name);
            if (content == null)
            {
                continue;
            }

            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }
}
