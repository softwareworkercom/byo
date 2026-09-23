# Token Replacement

BYO resolves `{{...}}` tokens at runtime when it executes saved commands, workflow command steps, and string settings that contain tokens.

## Table of Contents

- [Token format](#token-format)
- [Resolution order](#resolution-order)
- [Built-in tokens](#built-in-tokens)
- [Command-line overrides](#command-line-overrides)
- [Settings and secrets tokens](#settings-and-secrets-tokens)
- [Programmatic token overrides](#programmatic-token-overrides)
- [Object and JSON payload tokens](#object-and-json-payload-tokens)
- [Unresolved tokens](#unresolved-tokens)
- [Examples](#examples)

## Token format

- Use double curly braces: `{{TokenName}}`
- Token names can include letters, numbers, `_`, `:`, and `.`
- Token matching is case-insensitive

Examples:

- `{{Demo:ApiBaseUrl}}`
- `{{Demo:ApiToken}}`
- `{{context:region}}`
- `{{Project.Repository.Name}}`
- `{{UtcDateTimeNow}}`
- `{{Guid}}`

## Resolution order

Tokens are resolved in this order:

1. Token overrides
   - Command-line overrides parsed from the current process
   - Explicit `tokenOverrides` passed to `TokenService.ResolveTokens(...)`
2. Built-in system tokens
3. Saved settings and secrets
4. Object or JSON payload values
5. Unresolved tokens are replaced with an empty string

If the same token is supplied by multiple override sources, the explicit `tokenOverrides` dictionary wins over command-line values.

## Built-in tokens

These tokens are resolved without needing a saved setting or secret:

| Token | Description | Format |
|-------|-------------|--------|
| `{{DateNow}}` | Current local date | `yyyy-MM-dd` |
| `{{DateTimeNow}}` | Current local date and time | `yyyy-MM-dd HH:mm` |
| `{{UtcDateNow}}` | Current UTC date | `yyyy-MM-dd` |
| `{{UtcDateTimeNow}}` | Current UTC date and time | `yyyy-MM-dd HH:mm` |
| `{{Guid}}` | New random GUID | Standard GUID string |

### `{{DateTimeWindow}}`

`{{DateTimeWindow}}` is a special override token. When supplied through command-line or programmatic overrides, BYO converts a relative time expression into an absolute local datetime.

Supported expressions:

- `1m`, `30m` — minutes ago
- `1h`, `8h` — hours ago
- `1d`, `7d` — days ago
- `1w`, `4w` — weeks ago
- `1mo`, `3mo` — months ago

Behavior details:

- Output format: `yyyy-MM-dd HH:mm`
- Matching is case-insensitive
- Extra spaces are ignored, so `7 d` and `7D` are normalized
- Invalid values are not converted and continue through normal resolution

## Command-line overrides

Any CLI option that starts with `--` and has a value is available as a token override.

Supported forms:

- `--Tenant prod`
- `--Tenant=prod`
- `--context:region westus`
- `--context:region=westus`

Examples:

```bash
byo run --target command --name "Demo API Bearer Check" --Tenant prod --context:region westus
byo run --target command --name "Recent Errors" --DateTimeWindow 7d
```

Those values can satisfy matching tokens such as `{{Tenant}}`, `{{context:region}}`, and `{{DateTimeWindow}}`.

## Settings and secrets tokens

Settings and secrets are commonly referenced with namespaced keys such as `{{Demo:ApiToken}}`.

BYO looks up saved keys by prefix. Exact token names are the simplest option, but if multiple saved keys start with the same token text, BYO asks you to choose which saved key to use.

If a resolved setting or secret value contains pipe-separated values such as `value1|value2|value3`, BYO asks you to choose one of those values at runtime.

## Programmatic token overrides

You can also supply overrides directly when calling `TokenService.ResolveTokens(...)`. BYO merges command-line overrides with the explicit dictionary you pass in.

Example:

```csharp
var text = "Tenant={{Tenant}}, Region={{Deployment.Region}}, From={{DateTimeWindow}}";
var payload = new
{
    Deployment = new
    {
        Region = "westus"
    }
};

var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["Tenant"] = "prod",
    ["DateTimeWindow"] = "7d"
};

var resolved = TokenService.ResolveTokens(
    text,
    obj: payload,
    tokenOverrides: overrides);
```

## Object and JSON payload tokens

BYO can resolve token values from an object payload or a `JsonElement` payload.

Use dot notation for nested values:

- `{{Project.Repository.Name}}`
- `{{project.repository.name}}`
- `{{Deployment.Region}}`

Behavior details:

- Regular object payloads use case-insensitive public property lookup
- Regular object payloads do not resolve single-segment tokens such as `{{Name}}`
- `JsonElement` payloads support case-insensitive property lookup for both single-segment and dot-notation tokens

## Unresolved tokens

If BYO cannot resolve a token, it replaces that token with an empty string and shows a warning.

BYO does not keep unresolved token text in place, and it does not prompt for arbitrary missing token values.

## Examples

Save a command that uses stored settings/secrets and built-in tokens:

```bash
byo commands set --name "Demo API Bearer Check" --bookmark "Examples/GettingStarted" --shell PowerShell --executable "curl.exe -s -H 'Authorization: Bearer {{Demo:ApiToken}}' '{{Demo:ApiBaseUrl}}/bearer?correlationId={{Guid}}'"
```

Run a saved command with a time window override:

```bash
byo run --target command --name "Recent Errors" --DateTimeWindow 7d
```

