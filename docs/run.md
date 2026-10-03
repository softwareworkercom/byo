# run command

Execute saved commands or workflows interactively or directly.

## Table of Contents

- [Syntax](#syntax)
- [Options](#options)
- [Examples](#examples)
- [Behavior](#behavior)

## Syntax

```bash
byo run --target <command|workflow> [--name <name>] [--bookmark <path>] [--schedule <interval>] [--async]
```

## Options

| Option | Required | Description |
|--------|----------|-------------|
| `--target` | Yes | Type of target to run: `command` or `workflow`. In an interactive session you are prompted when it is omitted |
| `--name` | No | Name of the command/workflow to run. If omitted, prompts interactively |
| `--bookmark` | No | Bookmark hierarchy path to locate the command/workflow |
| `--schedule` | No | Repeat on a schedule. Accepts `hh:mm:ss` or short forms like `30s`, `5m`, `1h`, `1d`, `1w`, `1mo`. See [schedule](schedule.md) |
| `--async` | No | Run in the background and return immediately. See [async](async.md) |

Any other `--key value` pair you pass is available to the command as a `{{key}}` token. See [Token Replacement](token-replacement.md).

## Examples

```bash
# Choose from all commands
byo run --target command

# Choose from all workflows
byo run --target workflow

# Run a specific command
byo run --target command --name "Deploy API" --bookmark "DevOps/Production"

# Run a workflow
byo run --target workflow --name "API Smoke Test" --bookmark "Examples/GettingStarted"
```

## Behavior

- Resolves tokens using the [Token Replacement](token-replacement.md) system
- Runs command targets with the configured shell
- Executes workflow targets step-by-step
- Returns the execution exit code
