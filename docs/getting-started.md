# Getting Started Guide

This guide creates a simple but realistic API smoke-test flow using the public [data.gov.au](https://www.data.gov.au/) CKAN API with:

- 1 setting (`Demo:DataGovApiBaseUrl`)
- 1 secret (`Demo:DataGovApiKey`)
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
byo settings set --key Demo:DataGovApiBaseUrl --value https://www.data.gov.au/data/api/3/action
```

## 2) Add one secret

```bash
byo secrets set --key Demo:DataGovApiKey --value public-demo-key
```

## 3) Create two saved commands

### Command 1: Search datasets (package_search)

```bash
byo commands set --name "Data.gov.au Package Search" --bookmark "Examples/GettingStarted" --shell PowerShell --executable "curl.exe  --ssl-no-revoke -H 'X-API-Key: {{Demo:DataGovApiKey}}' '{{Demo:DataGovApiBaseUrl}}/package_search?q=transport&rows=3'"
```

### Command 2: List organizations (organization_list)

```bash
byo commands set --name "Data.gov.au Organization List" --bookmark "Examples/GettingStarted" --shell PowerShell --executable "curl.exe --ssl-no-revoke  -H 'X-API-Key: {{Demo:DataGovApiKey}}' '{{Demo:DataGovApiBaseUrl}}/organization_list?limit=10'"
```

## 4) Create one workflow using the two commands

Run:

```bash
byo workflows set --name "Data.gov.au API Smoke Test" --bookmark "Examples/GettingStarted"
```

When prompted, add steps in this order:

1. **Message**
   - Enter message to display: `Starting data.gov.au API smoke test`
   - Color: `Cyan`
   - Wait for Enter: `No`
2. **Execute Command**
   - Command: `Data.gov.au Package Search`
   - Run asynchronously: `No`
3. **Execute Command**
   - Command: `Data.gov.au Organization List`
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
byo run --target workflow --name "Data.gov.au API Smoke Test" --bookmark "Examples/GettingStarted"
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