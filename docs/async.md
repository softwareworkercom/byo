# Async flag

Start an executable command in a background process and return immediately.

## Syntax

```bash
byo <command> --async
```

The flag does not take a value. BYO starts a separate process with the command arguments, reports the process ID, and returns control to the terminal. The background process continues independently.

`--async` alone does not enable crash or restart recovery. When combined with [`--schedule`](schedule.md), the scheduled background process registers automatic recovery using Windows Task Scheduler, macOS launchd, or a Linux systemd user service. See the schedule documentation for prerequisites and how to stop it permanently.