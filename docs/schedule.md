# Schedule flag

Repeat an executable command at a fixed interval.

## Syntax

```bash
byo <command> --schedule <interval>
```

## Interval formats

The interval can use a full `hh:mm:ss` value or a short value with one of these units:

| Unit | Meaning | Example |
|------|---------|---------|
| `s` | Seconds | `30s` |
| `m` | Minutes | `5m` |
| `h` | Hours | `1h` |
| `d` | Days | `1d` |
| `w` | Weeks | `1w` |
| `mo` | 30-day periods | `1mo` |

Examples:

```bash
byo <command> --schedule 30s
byo <command> --schedule 01:30:00
```

The command runs once immediately, then repeats until the process is stopped.