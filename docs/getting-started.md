# Getting Started Guide

This guide creates a simple but realistic API smoke-test flow using the public [data.gov.au](https://www.data.gov.au/) CKAN API with:

- 1 setting (`Demo:DataGovApiBaseUrl`)
- 1 secret (`Demo:DataGovApiKey`) collected by a workflow step
- 2 saved commands
- 1 workflow that runs the 2 commands in sequence

## Table of Contents

- [Prerequisites](#prerequisites)
- [1) Add one setting](#1-add-one-setting)
- [2) Create two saved commands](#2-create-two-saved-commands)
  - [Command 1: Search datasets (package_search)](#command-1-search-datasets-package_search)
  - [Command 2: List organizations (organization_list)](#command-2-list-organizations-organization_list)
- [3) Create one workflow using the two commands](#3-create-one-workflow-using-the-two-commands)
- [4) Validate what was created](#4-validate-what-was-created)
- [5) Run the workflow](#5-run-the-workflow)
- [6) Run interactively (optional)](#6-run-interactively-optional)

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

## 2) Create two saved commands

### Command 1: Search datasets (package_search)

```bash
byo commands set --name "Data.gov.au Package Search" --bookmark "Examples/GettingStarted" --shell PowerShell --executable "curl.exe  --ssl-no-revoke -H 'X-API-Key: {{Demo:DataGovApiKey}}' '{{Demo:DataGovApiBaseUrl}}/package_search?q=transport&rows=3'"
```

### Command 2: List organizations (organization_list)

```bash
byo commands set --name "Data.gov.au Organization List" --bookmark "Examples/GettingStarted" --shell PowerShell --executable "curl.exe --ssl-no-revoke  -H 'X-API-Key: {{Demo:DataGovApiKey}}' '{{Demo:DataGovApiBaseUrl}}/organization_list?limit=10'"
```

## 3) Create one workflow using the two commands

Run:

```bash
byo workflows set --name "Data.gov.au API Smoke Test" --bookmark "Examples/GettingStarted"
```

When prompted, add steps in this order:

1. **Message**
   - Enter message to display: `Starting data.gov.au API smoke test`
   - Color: `Cyan`
   - Wait for Enter: `No`
2. **Input As Secret**
   - Prompt: `Enter data.gov.au API key`
   - Secret key: `Demo:DataGovApiKey`
3. **Execute Command**
   - Command: `Data.gov.au Package Search`
   - Run asynchronously: `No`
4. **Execute Command**
   - Command: `Data.gov.au Organization List`
   - Run asynchronously: `No`
5. **Done - Finish adding steps**

## 4) Validate what was created

```bash
byo settings list
byo commands list
byo workflows list
```

The secret is intentionally not listed by a CLI command. It is stored locally by the `InputAsSecret` step and resolved when the commands run.

## 5) Run the workflow

```bash
byo run --target workflow --name "Data.gov.au API Smoke Test" --bookmark "Examples/GettingStarted"
```

The workflow runs immediately and executes both commands in sequence.

## 6) Run interactively (optional)

You can choose a saved command or workflow from the bookmark hierarchy by
omitting `--name`:

```bash
byo run --target command
byo run --target workflow
```