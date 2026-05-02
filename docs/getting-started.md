# Getting Started Guide

This guide creates a simple but realistic API smoke-test flow using a public test service ([httpbin.org](https://httpbin.org/)) with:

- 1 setting (`Demo:ApiBaseUrl`)
- 1 secret (`Demo:ApiToken`)
- 2 saved commands
- 1 workflow that runs the 2 commands in sequence

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
byo commands create --description "Demo API Bearer Check" --executable "curl.exe -s -H 'Authorization: Bearer {{Demo:ApiToken}}' '{{Demo:ApiBaseUrl}}/bearer'" --path "Examples/GettingStarted"
```

### Command 2: Echo users request check

```bash
byo commands create --description "Demo API Users" --executable "curl.exe -s -H 'Authorization: Bearer {{Demo:ApiToken}}' '{{Demo:ApiBaseUrl}}/anything/v1/users?limit=5'" --path "Examples/GettingStarted"
```

## 4) Create one workflow using the two commands

Run:

```bash
byo workflows create --name "Demo API Smoke Test" --description "Runs basic API smoke checks" --path "Examples/GettingStarted"
```

When prompted, add steps in this order:

1. **Message**
   - Prompt: `Starting demo API smoke test`
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
byo settings read
byo secrets read
byo commands read
byo workflows read
```

## 6) Run the workflow

```bash
byo workflows run
```

Select `Demo API Smoke Test` and let the workflow execute both commands.

---

## Notes

- `{{Demo:ApiBaseUrl}}` and `{{Demo:ApiToken}}` are resolved from settings/secrets at runtime.
- If your environment does not have `curl`, replace the command executable with any HTTP command available in your shell.
