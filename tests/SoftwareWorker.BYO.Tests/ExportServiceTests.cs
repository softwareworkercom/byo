using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.Tests.Support;
using System.Data;
using System.Text.Json;

namespace SoftwareWorker.BYO.Tests;

public sealed class ExportServiceTests
{
    [Fact]
    public async Task ExportFile_ShouldThrow_WhenThereAreNoTables()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => ExportService.ExportFile([], "export"));
    }

    [Fact]
    public async Task ExportFile_ShouldWriteTablesColumnsAndRowsAsJson_IntoTheCurrentDirectory()
    {
        using var console = new ConsoleScope();
        var originalDirectory = Directory.GetCurrentDirectory();
        var directory = Directory.CreateTempSubdirectory("byo-export-").FullName;
        Directory.SetCurrentDirectory(directory);
        // On macOS the temp directory lives behind a symlink (/var -> /private/var), and the export
        // path is built from the resolved current directory, so compare against that resolved form.
        var resolvedDirectory = Directory.GetCurrentDirectory();

        try
        {
            var orders = new DataTable("Orders") { Namespace = "Sales" };
            orders.ExtendedProperties["SchemaName"] = "dbo";
            orders.Columns.Add("Id", typeof(int));
            orders.Columns.Add("Note", typeof(string));
            orders.Rows.Add(1, "first");
            orders.Rows.Add(2, DBNull.Value);

            var unnamed = new DataTable();
            unnamed.Columns.Add("Value");
            unnamed.Rows.Add("x");

            var path = await ExportService.ExportFile([orders, unnamed], " ");

            Assert.NotNull(path);
            Assert.StartsWith(Path.Combine(resolvedDirectory, "export_"), path);
            Assert.EndsWith(".json", path);
            Assert.Contains("Exported to:", console.Output);

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
            var tables = document.RootElement.GetProperty("tables");
            Assert.Equal(2, tables.GetArrayLength());

            var first = tables[0];
            Assert.Equal("Sales", first.GetProperty("databaseName").GetString());
            Assert.Equal("Orders", first.GetProperty("tableName").GetString());
            Assert.Equal("dbo", first.GetProperty("schemaName").GetString());
            Assert.Equal("System.Int32", first.GetProperty("columns")[0].GetProperty("dataType").GetString());
            Assert.Equal("Note", first.GetProperty("columns")[1].GetProperty("columnName").GetString());
            Assert.Equal(1, first.GetProperty("data")[0].GetProperty("Id").GetInt32());
            Assert.Equal(JsonValueKind.Null, first.GetProperty("data")[1].GetProperty("Note").ValueKind);

            var second = tables[1];
            Assert.Equal(JsonValueKind.Null, second.GetProperty("databaseName").ValueKind);
            Assert.Equal("Table2", second.GetProperty("tableName").GetString());
            Assert.Equal(JsonValueKind.Null, second.GetProperty("schemaName").ValueKind);
            Assert.Equal("x", second.GetProperty("data")[0].GetProperty("Value").GetString());
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(directory, recursive: true);
        }
    }
}
