# Token Replacement

BYO replaces `{{...}}` tokens at runtime when commands/workflows are executed.

## Table of Contents

- [Token format](#token-format)
- [Resolution order](#resolution-order)
- [Settings and secrets tokens](#settings-and-secrets-tokens)
- [Dot notation tokens](#dot-notation-tokens)
- [Practical example](#practical-example)
- [Interactive vs non-interactive behavior](#interactive-vs-non-interactive-behavior)

## Token format

- Use double curly braces: `{{TokenName}}`
- Token names can include letters, numbers, `_`, `:`, and `.`

Examples:

- `{{Demo:ApiBaseUrl}}`
- `{{Demo:ApiToken}}`
- `{{Project.Repository.Name}}`
- `{{Guid}}`

Token matching is case-insensitive.

## Resolution order

Tokens are resolved in this order:

1. Built-in system tokens
   - `{{Date}}`
   - `{{DateTimeRangeFromNow}}`
   - `{{Guid}}`
2. Saved settings and secrets
3. Object/JSON payload values (dot notation)
4. Interactive prompt (when running interactively)

If a token cannot be resolved, BYO keeps it unchanged.

## Settings and secrets tokens

Settings/secrets are commonly referenced with namespaced keys such as `{{Demo:ApiToken}}`.

If a setting/secret contains pipe-separated values (`value1|value2|value3`), BYO prompts you to pick one value at runtime.

## Dot notation tokens

Use dot notation for nested object/JSON values:

- `{{Project.Repository.Name}}`
- `{{project.repository.name}}` (case-insensitive)

For object traversal, single-segment tokens (for example `{{Name}}`) are not resolved from object payloads.

## Practical example

```bash
byo commands set --name "Demo API Bearer Check" --bookmark "Examples/GettingStarted" --shell PowerShell --executable "curl.exe -s -H 'Authorization: Bearer {{Demo:ApiToken}}' '{{Demo:ApiBaseUrl}}/bearer?correlationId={{Guid}}'"
```

## Interactive vs non-interactive behavior

- Interactive: unresolved tokens prompt for a value.
- Non-interactive: unresolved tokens remain unchanged.

