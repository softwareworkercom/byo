# Export JSON with `ExportService`

`ExportService` lets SDK consumers write one or more `DataTable` instances to a JSON file with an optional timestamp.

## When to use it

Use this helper when your SDK integration already produces `DataTable` results and you want to persist them as a JSON export for later inspection, sharing, or downstream processing.

## Behavior

- Writes a file named like `export_20250915083045.json` by default (UTC timestamp)
- Pass `includeTimestamp: false` to write `export.json` instead; an existing file with that name is overwritten
- Saves the file in the current working directory
- Serializes output as indented camelCase JSON
- Includes table metadata, column metadata, and row data
- Throws `InvalidOperationException` when no tables are provided

## Namespace

```csharp
using SoftwareWorker.BYO.SDK.Services;
```

## Basic example

```csharp
using SoftwareWorker.BYO.SDK.Services;
using System.Data;

var table = new DataTable("customers")
{
	Namespace = "sales-db"
};

table.Columns.Add("id", typeof(int));
table.Columns.Add("name", typeof(string));
table.Columns.Add("email", typeof(string));
table.Rows.Add(1, "Ada Lovelace", "ada@example.com");
table.Rows.Add(2, "Grace Hopper", "grace@example.com");

table.ExtendedProperties["SchemaName"] = "dbo";

var exportPath = await ExportService.ExportFile(new[] { table }, "customers");

Console.WriteLine($"Export written to: {exportPath}");
```

## JSON shape

To omit the timestamp:

```csharp
var exportPath = await ExportService.ExportFile(new[] { table }, "customers", includeTimestamp: false);
```

This writes `customers.json` in the current working directory.

The exported file contains a root `tables` array. Each entry includes:

- `databaseName`: from `DataTable.Namespace`
- `tableName`: from `DataTable.TableName`
- `schemaName`: from `DataTable.ExtendedProperties["SchemaName"]`
- `columns`: column names and CLR types
- `data`: row values as JSON objects

Example output:

```json
{
  "tables": [
	{
	  "databaseName": "sales-db",
	  "tableName": "customers",
	  "schemaName": "dbo",
	  "columns": [
		{
		  "columnName": "id",
		  "dataType": "System.Int32"
		},
		{
		  "columnName": "name",
		  "dataType": "System.String"
		},
		{
		  "columnName": "email",
		  "dataType": "System.String"
		}
	  ],
	  "data": [
		{
		  "id": 1,
		  "name": "Ada Lovelace",
		  "email": "ada@example.com"
		},
		{
		  "id": 2,
		  "name": "Grace Hopper",
		  "email": "grace@example.com"
		}
	  ]
	}
  ]
}
```

## Multiple tables

You can export multiple tables in a single file:

```csharp
var exportPath = await ExportService.ExportFile(new[] { customersTable, ordersTable }, "export");
```

Each `DataTable` becomes one item in the exported `tables` array.
