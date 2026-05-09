# Getting Started Guide

This guide creates a simple but realistic API smoke-test flow using a public test service ([httpbin.org](https://httpbin.org/)) with:

- 1 setting (`Demo:ApiBaseUrl`)
- 1 secret (`Demo:ApiToken`)
- 2 saved commands
- 1 workflow that runs the 2 commands in sequence

## Prerequisites

- .NET SDK 10+
- BYO CLI installed and available on your `PATH`

Quick check:

```bash
byo --help
```

## 1) Add one setting

```bash
byo settings set --key Demo:ApiBaseUrl --value https://httpbin.org
```

## 2) Add one secret

```bash
byo secrets set --key Demo:ApiToken --value public-demo-token
```

## 3) Create two saved commands

### Command 1: Bearer token check

```bash
byo commands set --name "Demo API Bearer Check" --bookmark "Examples/GettingStarted" --shell PowerShell --executable "curl.exe -s -H 'Authorization: Bearer {{Demo:ApiToken}}' '{{Demo:ApiBaseUrl}}/bearer'"
```

### Command 2: Echo users request

```bash
byo commands set --name "Demo API Users" --bookmark "Examples/GettingStarted" --shell PowerShell --executable "curl.exe -s -H 'Authorization: Bearer {{Demo:ApiToken}}' '{{Demo:ApiBaseUrl}}/anything/v1/users?limit=5'"
```

## 4) Create one workflow using the two commands

Run:

```bash
byo workflows set --name "Demo API Smoke Test" --bookmark "Examples/GettingStarted"
```

When prompted, add steps in this order:

1. **Message**
   - Enter message to display: `Starting demo API smoke test`
   - Color: `Cyan`
   - Wait for Enter: `No`
2. **Execute Command**
   - Command: `Demo API Bearer Check`
   - Run asynchronously: `No`
3. **Execute Command**
   - Command: `Demo API Users`
   - Run asynchronously: `No`
4. **Done - Finish adding steps**

## 5) Validate what was created

```bash
byo settings list
byo secrets list
byo commands list
byo workflows list
```

## 6) Run the workflow

```bash
byo run --target workflow --name "Demo API Smoke Test" --bookmark "Examples/GettingStarted"
```

The workflow runs immediately and executes both commands in sequence.

## 7) Run interactively (optional)

You can also use the interactive runner and choose a saved command/workflow from the bookmark hierarchy:

```bash
byo run --interactive
```

Or preselect target type:

```bash
byo run --target command
byo run --target workflow
```

---

## Token Replacement

Use `{{TokenName}}` syntax inside saved command executables. At runtime, BYO resolves token values from your configuration.

Resolution behavior:

- Settings tokens resolve from values saved with `byo settings set`.
- Secrets tokens resolve from values saved with `byo secrets set`.
- Tokens are replaced when the command executes.

Examples from this guide:

- `{{Demo:ApiBaseUrl}}`
- `{{Demo:ApiToken}}`

## Notes

- `{{Demo:ApiBaseUrl}}` and `{{Demo:ApiToken}}` are resolved from settings/secrets at runtime.
- `commands set` uses `--name` (not `--description`) and `--bookmark` (not `--path`).
- `workflows set` requires both `--name` and `--bookmark`.
- Use `run --target workflow` with `--name` and `--bookmark` to execute a specific workflow.
- `run` supports `--target` with values `command` or `workflow`, and `--interactive` to prompt for missing parameters.
- `settings`, `secrets`, `commands`, and `workflows` use `list` to view saved items.
- In PowerShell, keep single quotes around tokenized values (for example `'{{Demo:ApiToken}}'`) to avoid interpolation issues.
- If your environment does not have `curl`, replace the command executable with any HTTP command available in your shell.
