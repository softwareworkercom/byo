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

The command runs once immediately, then repeats at the requested interval (from 1 millisecond to approximately 49 days).

## Crash and restart recovery (Windows)

On Windows, `--schedule` automatically registers a per-user Windows Task Scheduler task. If the process crashes, the task restarts it within about a minute. After a computer restart, it resumes when the same user signs in; it does not run before sign-in. The recovered command runs immediately and starts a fresh interval; missed runs are not replayed.

The task uses your account without storing a password or requesting administrator privileges. Windows Task Scheduler must be available and permit task creation; registration failures are reported rather than silently running without recovery. The original executable path, command arguments and working directory are retained, so keep those paths available and supply all values needed to run without prompts. Use BYO secrets/token replacement rather than passing sensitive values directly: arguments are stored in the task definition. Custom shell environment variables are not retained across restarts.

Identical arguments, executable path and working directory share one recovery task. A lock prevents the foreground process and recovery task from executing the same schedule concurrently. Different arguments or working directories create separate schedules.

Press **Ctrl+C** to stop the schedule and delete its recovery task. Closing the terminal or killing the process leaves recovery enabled. To stop a background schedule, find the `BYO-Schedule-...` task name printed at startup, **disable the task**, end its running instance (and stop the original process if still running), then delete the task in Windows Task Scheduler. Deleting a task alone does not stop an already-running process. Combine `--schedule` with [`--async`](async.md) to start in the background and return to the terminal.

On Linux and macOS, schedules remain process-local: stopping the process or restarting the computer ends the schedule. Automatic recovery is not currently implemented on these platforms.