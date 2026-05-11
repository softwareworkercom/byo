# Commands Reference

This document provides a comprehensive reference for all BYO CLI commands.

## Table of Contents

- [Overview](#overview)
- [Global Options](#global-options)
- [Command Documents](#command-documents)

## Overview

BYO CLI is built around five main command groups:

- **run**: Execute saved commands or workflows
- **commands**: Manage saved shell commands
- **settings**: Manage configuration key-value pairs
- **secrets**: Manage encrypted sensitive data
- **workflows**: Manage multi-step automation workflows

All commands follow a consistent pattern: `byo <group> <action> [options]`

## Global Options

```bash
--help    # Display help for any command
```

## Command Documents

- [run command](command-run.md)
- [commands command group](command-commands.md)
- [settings command group](command-settings.md)
- [secrets command group](command-secrets.md)
- [workflows command group](command-workflows.md)

## Commands

### run

Execute saved commands or workflows interactively or directly.

#### Syntax

```bash
byo run --target <command|workflow> [--name <name>] [--bookmark <path>] [--<TokenKey>=<value>]
```

#### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--target` | Yes | Type of target to run: `command` or `workflow` |
| `--name` | No | Name of the command/workflow to run. If omitted, prompts interactively |
| `--bookmark` | No | Bookmark hierarchy path to locate the command/workflow. If omitted, searches all bookmarks |
| `--<TokenKey>=<value>` | No | Explicit token override (e.g., `--Demo:ApiToken=xyz`) |

#### Interactive Mode

If `--name` is omitted, BYO presents an interactive folder navigation interface to select a command/workflow from your bookmarks.

#### Examples

**Interactive selection:**
```bash
# Choose from all commands
byo run --target command

# Choose from all workflows
byo run --target workflow
```

**Direct execution:**
```bash
# Run a specific command by name and bookmark
byo run --target command --name "Deploy API" --bookmark "DevOps/Production"

# Run a workflow with token override
byo run --target workflow --name "API Smoke Test" --bookmark "Examples/GettingStarted" --Demo:ApiToken=test-token
```

#### Behavior

- Resolves tokens using the [Token Replacement](token-replacement.md) system
- For commands: executes the shell command in the specified shell (PowerShell, Cmd, or WSL)
- For workflows: executes all workflow steps in sequence
- Returns the exit code of the executed command/workflow

---

### commands

Manage saved shell commands that can be executed repeatedly.

#### commands set

Create or update a saved command.

##### Syntax

```bash
byo commands set --name <name> --executable <command> [--bookmark <path>] [--shell <PowerShell|Cmd|Wsl>] [--directory <path>]
```

##### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--name` | Yes | Display name for the command |
| `--executable` | Yes | The shell command to execute. Use `{{tokenName}}` for token replacement |
| `--bookmark` | No | Bookmark hierarchy path (e.g., `DevOps/Deploy`). Defaults to root `/` |
| `--shell` | No | Shell type: `PowerShell`, `Cmd`, or `Wsl`. Defaults to `PowerShell` |
| `--directory` | No | Working directory for command execution |

##### Examples

```bash
# Create a simple command
byo commands set --name "List Files" --executable "ls -la" --shell PowerShell

# Create a command with tokens
byo commands set --name "API Health Check" --bookmark "Monitoring/API" --shell PowerShell --executable "curl.exe -H 'Authorization: Bearer {{ApiToken}}' '{{ApiBaseUrl}}/health'"

# Create a command with a working directory
byo commands set --name "Build Solution" --executable "dotnet build" --directory "C:\Projects\MySolution" --shell PowerShell
```

##### Behavior

- If a command with the same name and bookmark exists, prompts for confirmation to update
- Updates all properties except name and bookmark (these identify the command)
- Validates that name and executable are provided

#### commands list

Display all saved commands.

##### Syntax

```bash
byo commands list
```

##### Examples

```bash
byo commands list
```

##### Behavior

- Groups commands by bookmark hierarchy
- Displays commands in a formatted table with columns: Name, Bookmark, Executable, Directory, Created
- Shows "(root)" for commands without a bookmark
- Sorts by bookmark path, then by command name

#### commands delete

Delete a saved command.

##### Syntax

```bash
byo commands delete
```

##### Examples

```bash
byo commands delete
```

##### Behavior

- Presents an interactive folder navigation interface to select a command
- Prompts for confirmation before deletion
- Removes the command permanently from storage

---

### settings

Manage non-sensitive configuration key-value pairs that can be referenced as tokens in commands and workflows.

#### settings set

Create or update a setting.

##### Syntax

```bash
byo settings set --key <key> --value <value>
```

##### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--key` | Yes | Setting key name (e.g., `Demo:ApiBaseUrl`) |
| `--value` | Yes | Setting value |

##### Examples

```bash
# Set an API base URL
byo settings set --key Demo:ApiBaseUrl --value https://api.example.com

# Set a project name
byo settings set --key ProjectName --value MyApplication

# Set a multi-value setting (pipe-separated)
byo settings set --key Environment --value "dev|staging|prod"
```

##### Behavior

- If the key exists, prompts for confirmation to replace the value
- Stores the setting unencrypted in local configuration
- Setting keys are case-insensitive when resolved as tokens

#### settings list

Display all settings.

##### Syntax

```bash
byo settings list
```

##### Examples

```bash
byo settings list
```

##### Behavior

- Displays all settings in a formatted table
- Shows keys and their corresponding values
- Settings are stored in plain text (use `secrets` for sensitive data)

#### settings delete

Delete a setting.

##### Syntax

```bash
byo settings delete
```

##### Examples

```bash
byo settings delete
```

##### Behavior

- Presents a list of all settings to choose from
- Prompts for confirmation before deletion
- Removes the setting permanently

---

### secrets

Manage encrypted sensitive data (like API keys, passwords, tokens) that can be referenced as tokens in commands and workflows.

#### secrets set

Create or update an encrypted secret.

##### Syntax

```bash
byo secrets set --key <key> --value <value>
```

##### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--key` | Yes | Secret key name (e.g., `Demo:ApiKey`) |
| `--value` | Yes | Secret value (will be encrypted) |

##### Examples

```bash
# Store an API key
byo secrets set --key Demo:ApiKey --value sk_live_abc123xyz

# Store a password
byo secrets set --key DatabasePassword --value MySuperSecretPassword123

# Store a token with pipe-separated values
byo secrets set --key ApiToken --value "token1|token2|token3"
```

##### Behavior

- If the key exists, prompts for confirmation to replace the value
- Encrypts the value using Windows Data Protection API (DPAPI)
- Secret keys are case-insensitive when resolved as tokens
- Encrypted secrets are stored locally and can only be decrypted by the same user on the same machine

#### secrets list

Display all secret keys (values are masked).

##### Syntax

```bash
byo secrets list
```

##### Examples

```bash
byo secrets list
```

##### Behavior

- Displays all secret keys
- Shows decrypted values (use with caution in shared environments)
- Values are encrypted at rest but displayed in clear when listed

#### secrets delete

Delete a secret.

##### Syntax

```bash
byo secrets delete
```

##### Examples

```bash
byo secrets delete
```

##### Behavior

- Presents a list of all secret keys to choose from
- Prompts for confirmation before deletion
- Removes the encrypted secret permanently

#### secrets reencrypt

Re-encrypt all secrets (useful after moving to a new machine or user account).

##### Syntax

```bash
byo secrets reencrypt
```

##### Examples

```bash
byo secrets reencrypt
```

##### Behavior

- Decrypts all secrets with the old encryption key
- Re-encrypts them with the current user's encryption key
- Useful when restoring secrets from backup or migrating to a new machine

---

### workflows

Manage multi-step automation workflows that combine commands, prompts, and logic.

#### workflows set

Create a new workflow with interactive step definition.

##### Syntax

```bash
byo workflows set --name <name> --bookmark <path>
```

##### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--name` | Yes | Workflow name |
| `--bookmark` | Yes | Bookmark hierarchy path (e.g., `DevOps/Deploy`) |

##### Examples

```bash
byo workflows set --name "API Smoke Test" --bookmark "Examples/GettingStarted"
```

##### Behavior

After running the command, BYO enters an interactive mode where you define each step:

**Step Types:**

1. **Message**
   - Display a message to the user
   - Options:
     - Message text
     - Color (Cyan, Green, Yellow, Red, White)
     - Wait for Enter key press

2. **YesNoQuestion**
   - Ask a yes/no question
   - Options:
     - Question text
     - Interrupt workflow if answer is No

3. **InputAsSetting**
   - Prompt user for input and save as a setting
   - Options:
     - Prompt message
     - Setting key name

4. **InputAsSecret**
   - Prompt user for input and save as an encrypted secret
   - Options:
     - Prompt message
     - Secret key name

5. **ExecuteCommand**
   - Run a saved command
   - Options:
     - Command to execute (select from saved commands)
     - Run asynchronously (background execution)

**Example Interactive Session:**

```bash
byo workflows set --name "Deploy to Production" --bookmark "DevOps/Production"

# Step 1: Message
Select step type: Message
Enter message to display: Starting production deployment
Select message color: Cyan
Wait for user to press Enter after displaying message? No

# Step 2: YesNoQuestion
Select step type: YesNoQuestion
Enter question to ask: Are you sure you want to deploy to production?
Interrupt workflow if answer is No? Yes

# Step 3: ExecuteCommand
Select step type: ExecuteCommand
Select command: Build Solution
Run this command asynchronously in background? No

# Step 4: ExecuteCommand
Select step type: ExecuteCommand
Select command: Deploy API
Run this command asynchronously in background? No

# Step 5: Message
Select step type: Message
Enter message to display: Deployment completed successfully!
Select message color: Green
Wait for user to press Enter after displaying message? Yes

Select step type: Done - Finish adding steps
```

##### Notes

- Steps execute in the order they are defined
- Workflows can reference settings, secrets, and saved commands
- Token replacement works in all command steps
- If a workflow with the same name and bookmark exists, prompts for confirmation to replace

#### workflows list

Display all workflows.

##### Syntax

```bash
byo workflows list
```

##### Examples

```bash
byo workflows list
```

##### Behavior

- Groups workflows by bookmark hierarchy
- Displays workflows in a formatted table
- Shows workflow name, bookmark path, number of steps, and creation date

#### workflows delete

Delete a workflow.

##### Syntax

```bash
byo workflows delete
```

##### Examples

```bash
byo workflows delete
```

##### Behavior

- Presents an interactive folder navigation interface to select a workflow
- Prompts for confirmation before deletion
- Removes the workflow and all its steps permanently

---

## Common Patterns

### Organizing with Bookmarks

Bookmarks provide a hierarchical folder structure for organizing commands and workflows:

```bash
# Single level
--bookmark "DevOps"

# Multi-level hierarchy
--bookmark "DevOps/Production"
--bookmark "DevOps/Staging"
--bookmark "Monitoring/API/Health"
```

Benefits:
- Logical grouping of related commands/workflows
- Easier navigation in interactive mode
- Better organization as your library grows

### Token-Driven Commands

Leverage tokens to make commands reusable across environments:

```bash
# Define settings for different environments
byo settings set --key Dev:ApiUrl --value https://dev-api.example.com
byo settings set --key Prod:ApiUrl --value https://api.example.com

# Create a command that uses tokens
byo commands set --name "Health Check" --executable "curl {{Env:ApiUrl}}/health"

# Run with different token values
byo run --target command --name "Health Check" --Env:ApiUrl={{Dev:ApiUrl}}
byo run --target command --name "Health Check" --Env:ApiUrl={{Prod:ApiUrl}}
```

### Multi-Value Settings/Secrets

Use pipe-separated values to create selectable options:

```bash
# Create a multi-value setting
byo settings set --key Environment --value "dev|staging|prod"

# When used in a command, prompts user to choose one value
byo commands set --name "Connect" --executable "ssh user@{{Environment}}.example.com"
```

### Workflow Composition

Build complex automation by combining step types:

1. **Pre-flight checks**: Use YesNoQuestion steps
2. **Environment setup**: Use InputAsSetting/InputAsSecret steps
3. **Execution**: Use ExecuteCommand steps
4. **Status updates**: Use Message steps throughout

## See Also

- [Getting Started Guide](getting-started.md) - Complete walkthrough with examples
- [Token Replacement](token-replacement.md) - Detailed token system documentation
