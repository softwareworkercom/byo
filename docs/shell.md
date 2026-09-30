# Interactive shell

Start an interactive session with autocompletion, syntax highlighting and history.

## Syntax

Run `byo` without any arguments:

```bash
byo
```

The shell only starts in an interactive terminal. When input is redirected (for example in scripts or CI), `byo` without arguments behaves as before and prints usage information.

Type commands without the `byo` prefix (it is accepted if you type it anyway):

```text
byo> sqlserver query --filename "C:\Repositories\queries\Orders Search.sql" --connection Production
```

Each command runs in its own process, exactly as if you had typed it in your terminal. Pressing `Ctrl+C` stops the running command and returns to the prompt. Plugins installed or uninstalled from the shell are available to the next command.

The `>` in the prompt turns red when the last command failed.

## Features

- **Syntax highlighting**: commands, options, values and quoted text are colored as you type. Unknown commands or options are shown in red before you run them.
- **Completion menu**: suggestions for commands, subcommands, options, option values and file paths, with their descriptions. The menu opens as you type and the best match is always selected first.
- **Option values**: parameters declared with pipe-separated values (for example `command|workflow`) suggest those values.
- **Path completion**: options whose name contains `file`, `path`, `folder` or `dir`, and any value that looks like a path, complete file and folder names. Paths with spaces are quoted automatically.
- **History**: `Up`/`Down` browse previous commands. When you have typed something, only commands that start with that text are shown.
- **Inline suggestions**: the most recent matching command from history appears in grey after the cursor. Press `Right` or `End` to accept it, or `Ctrl+Right` to accept the next word.
- **Editing**: selection, undo/redo and word-by-word navigation and deletion.

## Key bindings

| Key | Action |
| --- | --- |
| `Tab` / `Ctrl+Space` | Open the completion menu, or complete directly when there is a single match |
| `Tab` / `Enter` (menu open) | Accept the selected completion |
| `Shift+Tab`, `Up`, `Down`, `PageUp`, `PageDown` (menu open) | Move through the completion menu |
| `Esc` | Close the menu, clear the selection, or clear the line |
| `Enter` | Run the command |
| `Up` / `Down` | Previous / next history entry matching the typed text |
| `Right` / `End` (end of line) | Accept the inline suggestion |
| `Ctrl+Right` (end of line) | Accept the next word of the inline suggestion |
| `Left` / `Right` | Move one character |
| `Ctrl+Left` / `Ctrl+Right` | Move one word |
| `Home` / `End` | Move to the start / end of the line |
| `Shift` + movement keys | Select text |
| `Ctrl+A` | Select the whole line |
| `Backspace` / `Delete` | Delete one character (or the selection) |
| `Ctrl+Backspace` / `Ctrl+W` | Delete the previous word |
| `Ctrl+Delete` | Delete the next word |
| `Ctrl+U` / `Ctrl+K` | Delete to the start / end of the line |
| `Ctrl+Z` / `Ctrl+Y` | Undo / redo |
| `Ctrl+L` | Clear the screen |
| `Ctrl+C` | Discard the current line |
| `Ctrl+D` (empty line) | Exit the shell |

## Built-in commands

| Command | Description |
| --- | --- |
| `help [command]` | Show help for BYO or for a command, for example `help sqlserver query` |
| `history` | Show the last 50 commands |
| `clear`, `cls` | Clear the screen |
| `exit`, `quit` | Exit the shell |

## History file

History is saved to `~/byo/shell_history.txt` (up to 1000 entries). Lines that contain words such as `password`, `secret`, `token` or `apikey` are kept for the current session only and never written to disk.

## Requirements

The shell needs an interactive terminal. In terminals without ANSI support it falls back to a plain prompt without completion or highlighting.
