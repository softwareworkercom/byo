# run command

Execute saved commands or workflows interactively or directly.

## Table of Contents

- [Syntax](#syntax)
- [Options](#options)
- [Examples](#examples)
- [Behavior](#behavior)

## Syntax

```bash
byo run --target <command|workflow> [--name <name>] [--bookmark <path>] [--<TokenKey>=<value>]
```

## Options

| Option | Required | Description |
|--------|----------|-------------|
| `--target` | Yes | Type of target to run: `command` or `workflow` |
| `--name` | No | Name of the command/workflow to run. If omitted, prompts interactively |
| `--bookmark` | No | Bookmark hierarchy path to locate the command/workflow |
| `--<TokenKey>=<value>` | No | Explicit token override (for example `--Demo:ApiToken=xyz`) |

## Examples

```bash
# Choose from all commands
byo run --target command

# Choose from all workflows
byo run --target workflow

# Run a specific command
byo run --target command --name "Deploy API" --bookmark "DevOps/Production"

# Run a workflow with token override
byo run --target workflow --name "API Smoke Test" --bookmark "Examples/GettingStarted" --Demo:ApiToken=test-token
```

## Behavior

- Resolves tokens using the [Token Replacement](token-replacement.md) system
- Runs command targets with the configured shell
- Executes workflow targets step-by-step
- Returns the execution exit code
