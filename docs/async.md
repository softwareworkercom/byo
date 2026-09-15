# Async flag

Start an executable command in a background process and return immediately.

## Syntax

```bash
byo <command> --async
```

The flag does not take a value. BYO starts a separate process with the command arguments, reports the process ID, and returns control to the terminal. The background process continues independently.