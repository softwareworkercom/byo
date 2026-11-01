# settings command group

Manage non-sensitive configuration key-value pairs, including JSON arrays.

## Table of Contents

- [settings set](#settings-set)
  - [Syntax](#syntax)
  - [Options](#options)
  - [Examples](#examples)
  - [Behavior](#behavior)
- [settings import](#settings-import)
  - [Syntax](#syntax-1)
  - [Behavior](#behavior-1)
- [settings list](#settings-list)
  - [Syntax](#syntax-2)
- [settings delete](#settings-delete)
  - [Syntax](#syntax-3)
  - [Behavior](#behavior-2)

## settings set

Create or update a setting.

### Syntax

```bash
byo settings set --key <key> --value <value>
```

### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--key` | Yes | Setting key name |
| `--value` | Yes | Setting value |

### Examples

```bash
byo settings set --key Demo:ApiBaseUrl --value https://api.example.com
byo settings set --key ProjectName --value MyApplication
byo settings set --key Environment --value "dev|staging|prod"
byo settings set --key Jira:Priorities --value ["High","Medium","Low"]
byo settings set --key Teams:Channels --value ["general","engineering","alerts"]
```

### Behavior

- Prompts before replacing an existing key
- In an interactive session, omitting `--key` lets you pick an existing setting and omitting `--value` prompts for the new value
- Stores values in local configuration
- If `--value` is a valid JSON array, it is saved as a real JSON array in `settings.json`
- In PowerShell, prefer single quotes around JSON values to avoid escaping double quotes

## settings import

Import one value from a JSON file into a setting. The destination setting key and source JSON path are specified separately. Paths support dot-separated object properties and numeric array indexes, with an optional `$` root prefix.

### Syntax

```bash
byo settings import --key <setting-key> --path <json-path> --file <path>
```

### Behavior

- Resolves the specified JSON path directly in the file; missing or null values are reported without changing settings
- Prompts before replacing an existing key and enforces the same settings/secrets key exclusivity as `settings set`
- String values are stored as strings; non-string JSON values are stored as their JSON text

Example:

```bash
byo settings import --key ImportedIds --path 'tables[0].data[0].Ids' --file export.json
```

## settings list

List settings.

### Syntax

```bash
byo settings list
```

## settings delete

Delete a setting.

### Syntax

```bash
byo settings delete --key <key>
```

### Behavior

- Deletes the exact key provided by `--key`
