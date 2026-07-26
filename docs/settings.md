# settings command group

Manage non-sensitive configuration key-value pairs, including JSON arrays.

## Table of Contents

- [settings set](#settings-set)
  - [Syntax](#syntax)
  - [Options](#options)
  - [Examples](#examples)
  - [Behavior](#behavior)
- [settings list](#settings-list)
  - [Syntax](#syntax-1)
- [settings delete](#settings-delete)
  - [Syntax](#syntax-2)
  - [Behavior](#behavior-1)

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
- Stores values in local configuration
- If `--value` is a valid JSON array, it is saved as a real JSON array in `settings.json`
- In PowerShell, prefer single quotes around JSON values to avoid escaping double quotes

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
