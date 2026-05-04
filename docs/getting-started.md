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
byo settings update --key Demo:ApiBaseUrl --value https://httpbin.org
```

## 2) Add one secret

```bash
byo secrets update --key Demo:ApiToken --value public-demo-token
```

## 3) Create two saved commands

### Command 1: Bearer token check

```bash
byo commands create --name "Demo API Bearer Check" --bookmark "Examples/GettingStarted" --shell PowerShell --executable "curl.exe -s -H 'Authorization: Bearer {{Demo:ApiToken}}' '{{Demo:ApiBaseUrl}}/bearer'"
```

### Command 2: Echo users request

```bash
byo commands create --name "Demo API Users" --bookmark "Examples/GettingStarted" --shell PowerShell --executable "curl.exe -s -H 'Authorization: Bearer {{Demo:ApiToken}}' '{{Demo:ApiBaseUrl}}/anything/v1/users?limit=5'"
```

## 4) Create one workflow using the two commands

Run:

```bash
byo workflows create --name "Demo API Smoke Test" --bookmark "Examples/GettingStarted"
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
byo workflows run --name "Demo API Smoke Test" --bookmark "Examples/GettingStarted"
```

The workflow runs immediately and executes both commands in sequence.

---

## Token Replacement

See [`docs/token-replacement.md`](token-replacement.md) for token syntax, resolution order, and examples.

## Notes

- `{{Demo:ApiBaseUrl}}` and `{{Demo:ApiToken}}` are resolved from settings/secrets at runtime.
- `commands create` uses `--name` (not `--description`) and `--bookmark` (not `--path`).
- `workflows create` requires both `--name` and `--bookmark`.
- `workflows run` expects both `--name` and `--bookmark` to identify which workflow to execute.
- `settings`, `secrets`, `commands`, and `workflows` use `list` to view saved items.
- In PowerShell, keep single quotes around tokenized values (for example `'{{Demo:ApiToken}}'`) to avoid interpolation issues.
- If your environment does not have `curl`, replace the command executable with any HTTP command available in your shell.
