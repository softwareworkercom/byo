# secrets command group

Manage encrypted sensitive values such as API keys and passwords.

## Table of Contents

- [secrets set](#secrets-set)
  - [Syntax](#syntax)
  - [Options](#options)
  - [Examples](#examples)
  - [Behavior](#behavior)
- [secrets list](#secrets-list)
  - [Syntax](#syntax-1)
- [secrets delete](#secrets-delete)
  - [Syntax](#syntax-2)
- [secrets reencrypt](#secrets-reencrypt)
  - [Syntax](#syntax-3)

## secrets set

Create or update an encrypted secret.

### Syntax

```bash
byo secrets set --key <key> --value <value>
```

### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--key` | Yes | Secret key name |
| `--value` | Yes | Secret value |

### Examples

```bash
byo secrets set --key Demo:ApiKey --value sk_live_abc123xyz
byo secrets set --key DatabasePassword --value MySuperSecretPassword123
```

### Behavior

- Prompts before replacing an existing key
- Encrypts values at rest

## secrets list

List secrets.

### Syntax

```bash
byo secrets list
```

## secrets delete

Delete a secret.

### Syntax

```bash
byo secrets delete
```

## secrets reencrypt

Re-encrypt all stored secrets.

### Syntax

```bash
byo secrets reencrypt
```
