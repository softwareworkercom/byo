# Token Replacement

BYO replaces `{{...}}` tokens at runtime when commands/workflows are executed.

## Token format

- Use double curly braces: `{{TokenName}}`
- Token names can include letters, numbers, `_`, `:`, and `.`

Examples:

- `{{Demo:ApiBaseUrl}}`
- `{{Demo:ApiToken}}`
- `{{Project.Repository.Name}}`
- `{{Guid}}`

## Resolution order

Tokens are resolved in this order:

1. Explicit token overrides (highest priority)
2. Built-in system tokens
   - `{{Date}}`
   - `{{DateTimeRangeFromNow}}`
   - `{{Guid}}`
3. Saved settings and secrets
4. Object/JSON payload values (dot notation)
5. Interactive prompt (when running interactively)

If a token cannot be resolved, BYO keeps it unchanged.

## Settings and secrets tokens

Settings/secrets are commonly referenced with namespaced keys such as `{{Demo:ApiToken}}`.

If a setting/secret contains pipe-separated values (`value1|value2|value3`), BYO prompts you to pick one value at runtime.

## Dot notation tokens

Use dot notation for nested object/JSON values:

- `{{Project.Repository.Name}}`
- `{{project.repository.name}}` (case-insensitive)

## Practical example

```bash
byo commands create --name "Demo API Bearer Check" --bookmark "Examples/GettingStarted" --shell PowerShell --executable "curl.exe -s -H 'Authorization: Bearer {{Demo:ApiToken}}' '{{Demo:ApiBaseUrl}}/bearer?correlationId={{Guid}}'"
```
