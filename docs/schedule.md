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

## Crash and restart recovery

`--schedule` automatically registers a per-user recovery job with the operating system:

| Platform | Recovery manager | After a crash | After a computer restart |
|----------|------------------|---------------|--------------------------|
| Windows | Task Scheduler | Within about a minute | When the same user signs in |
| macOS | launchd LaunchAgent | Automatically, with a 10-second restart throttle | When the same user signs in |
| Linux | systemd user service | Automatically, with a 10-second restart delay | At sign-in, or at boot if user lingering is enabled |

The recovered command runs immediately and starts a fresh interval; missed runs are not replayed. Linux requires an available systemd user manager; non-systemd distributions and sessions without a user bus are not supported. macOS registration requires a graphical login session. Registration errors are reported rather than silently running without recovery.

Recovery uses your account without storing a password or requesting administrator privileges. BYO does not enable Linux user lingering automatically. To allow a Linux schedule to resume before sign-in, you can explicitly run `loginctl enable-linger "$USER"` (authorization may be required). Whether commands can access your keyring, graphical applications, or network before sign-in depends on your environment.

The original executable path, command arguments and working directory are retained, so keep those paths available and supply all values needed to run without prompts. Use BYO secrets/token replacement rather than passing sensitive values directly: arguments are stored in the recovery definition. Custom shell environment variables are not retained across restarts; Linux preserves the effective `XDG_CONFIG_HOME` so the recovered worker can remove its own service correctly.

Identical arguments, executable path and working directory share one recovery job. A lock prevents the foreground process and recovery worker from executing the same schedule concurrently. On Unix, the native manager starts a standby worker that waits while the foreground process owns the schedule, then takes over if it exits. Different arguments or working directories create separate schedules.

### Stopping permanently

Press **Ctrl+C** to stop a foreground schedule and remove its recovery definition. On Unix, BYO also handles `SIGINT` without a terminal; `SIGTERM` leaves recovery enabled for shutdowns and process termination. If a command is executing, cleanup waits for that execution to finish. Closing the terminal or killing the process leaves recovery enabled. Combine `--schedule` with [`--async`](async.md) to start in the background and return to the terminal.

For background schedules, use the identifier printed at startup:

- **Windows:** Find the `BYO-Schedule-...` task in Task Scheduler, disable it, end its running instance, then delete it.
- **Linux:** The unit is the printed identifier in lowercase plus `.service`. Run `systemctl --user disable --now <unit>`, remove its definition from `${XDG_CONFIG_HOME:-$HOME/.config}/systemd/user/`, and run `systemctl --user daemon-reload`. Output from recovered workers is available through `journalctl --user -u <unit>`.
- **macOS:** The label is `com.softwareworker.byo.` followed by the printed identifier in lowercase. Remove `~/Library/LaunchAgents/<label>.plist`, then run `launchctl bootout "gui/$(id -u)/<label>"`. Recovered worker output is written to `~/Library/Logs/BYO/<label>.out.log` and `<label>.err.log`.

Also stop the original process if it is still running. Removing a recovery definition alone does not stop an already-running process.