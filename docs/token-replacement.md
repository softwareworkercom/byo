# Token Replacement

BYO replaces `{{...}}` tokens at runtime when commands/workflows are executed.

## Table of Contents

- [Token format](#token-format)
- [Resolution order](#resolution-order)
- [Settings and secrets tokens](#settings-and-secrets-tokens)
- [Dynamic parameters](#dynamic-parameters)
- [Programmatic token overrides](#programmatic-token-overrides)
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

1. Token overrides
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

## Dynamic Parameters

Dynamic parameters are captured automatically from the command line and made available through `DynamicParameters`.

A dynamic parameter such as `--Tenant "prod"` can satisfy `{{Tenant}}`, and `--context:region westus` can satisfy `{{context:region}}`. Prefer the space-separated form for dynamic parameters. Token names are matched case-insensitively, and braces such as `{{Tenant}}` are normalized automatically.

## Programmatic Token Overrides

You can also supply token overrides programmatically when calling `TokenService.ResolveTokens(...)`. BYO merges automatic dynamic parameters with any explicit override dictionary.

Example:

```csharp
public override async Task ExecuteAsync()
{
    var text = "Tenant={{Tenant}}, Region={{context:region}}";

    var programmaticOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Tenant"] = "prod"
    };

    var resolved = TokenService.ResolveTokens(
        text,
        payload: null,
        tokenOverrides: programmaticOverrides);

    Console.WriteLine(resolved);
}
```

If the same token is provided by multiple sources, token overrides win over built-in tokens, settings, secrets, object values, and interactive prompts.

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

