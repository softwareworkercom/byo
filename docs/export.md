# Export results

Export supported command results as a JSON file.

## Syntax

```bash
byo <command> --export
```

Add `--export` to a command that supports result exporting. The flag does not take a value.

## SDK consumer requirements

The command handler must add the `DataTable` instances it wants to export to
the inherited `ExportSource` collection during `ExecuteAsync`:

```csharp
ExportSource.Add(resultsTable);
ExportSource.Add(summaryTable);
```

When `--export` is used, BYO exports the tables in `ExportSource` after the
handler completes. If the collection is empty, command execution fails with an
error asking the consumer to add at least one `DataTable`.

## Output

- Results are always written as formatted JSON.
- Exported files use the `.json` extension.
- The output filename includes a UTC timestamp to avoid overwriting earlier exports.
- Files are written to the current directory where the command is run.

CSV, Excel, and other export formats are not supported.
