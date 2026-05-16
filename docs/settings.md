# settings command group

Manage non-sensitive configuration key-value pairs.

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
```

### Behavior

- Prompts before replacing an existing key
- Stores values in local configuration

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
byo settings delete
```

### Behavior

- Uses interactive selection
- Confirms before delete
