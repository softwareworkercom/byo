# commands command group

Manage saved shell commands.

## Table of Contents

- [commands set](#commands-set)
  - [Syntax](#syntax)
  - [Options](#options)
  - [Shell types](#shell-types)
  - [Examples](#examples)
  - [Behavior](#behavior)
- [commands list](#commands-list)
  - [Syntax](#syntax-1)
  - [Behavior](#behavior-1)
- [commands delete](#commands-delete)
  - [Syntax](#syntax-2)
  - [Behavior](#behavior-2)

## commands set

Create or update a saved command.

### Syntax

```bash
byo commands set --name <name> --executable <command> [--bookmark <path>] [--shell <PowerShell|Cmd|Wsl>] [--directory <path>]
```

### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--name` | Yes | Display name for the command |
| `--executable` | Yes | Shell command to execute. Supports `{{tokenName}}` |
| `--bookmark` | No | Bookmark hierarchy path (for example `DevOps/Deploy`) |
| `--shell` | No | Shell type: `PowerShell`, `Cmd`, or `Wsl` |
| `--directory` | No | Working directory for execution |

### Shell types

The `--shell` option controls which shell runtime executes your saved command.

#### `PowerShell`

- Best for modern Windows scripting and cross-platform PowerShell usage.
- Good choice when your command uses PowerShell syntax, cmdlets, or objects.
- Typical examples: `Get-ChildItem`, `Invoke-RestMethod`, `dotnet build`.

```bash
byo commands set --name "Build" --shell PowerShell --executable "dotnet build"
```

#### `Cmd`

- Uses classic Windows Command Prompt behavior.
- Useful for legacy batch commands and tools that assume `cmd.exe` syntax.
- Typical examples: `dir`, `set`, `.bat`-style command chains.

```bash
byo commands set --name "Legacy Build" --shell Cmd --executable "build.bat"
```

#### `Wsl`

- Runs the command through Windows Subsystem for Linux.
- Useful for Linux-native tools or scripts (`bash`, `grep`, `awk`, `sed`, Linux package managers).
- Prefer Linux-style command syntax and paths inside WSL context.

```bash
byo commands set --name "List Linux Files" --shell Wsl --executable "ls -la ~/project"
```

#### Choosing the right shell

- Use `PowerShell` for most .NET and Windows-native developer workflows.
- Use `Cmd` only when you need Command Prompt compatibility.
- Use `Wsl` when your command depends on Linux tooling or scripts.

### Examples

```bash
byo commands set --name "List Files" --executable "ls -la" --shell PowerShell
byo commands set --name "API Health Check" --bookmark "Monitoring/API" --shell PowerShell --executable "curl.exe -H 'Authorization: Bearer {{ApiToken}}' '{{ApiBaseUrl}}/health'"
byo commands set --name "Build Solution" --executable "dotnet build" --directory "C:\Projects\MySolution" --shell PowerShell
```

### Behavior

- If name + bookmark already exists, prompts to update
- Validates required fields

## commands list

List saved commands.

### Syntax

```bash
byo commands list
```

### Behavior

- Groups by bookmark
- Displays name, bookmark, executable, directory, and created date

## commands delete

Delete a saved command.

### Syntax

```bash
byo commands delete
```

### Behavior

- Uses interactive selection
- Confirms before delete
