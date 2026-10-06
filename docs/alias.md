# alias command group

Give the CLI your own name. An alias is the command name you type instead of `byo`, for example the name of your product, team or company.

## Table of Contents

- [How it works](#how-it-works)
- [First-run prompt](#first-run-prompt)
- [alias set](#alias-set)
  - [Syntax](#syntax)
  - [Behavior](#behavior)
- [alias delete](#alias-delete)
  - [Syntax](#syntax-1)
  - [Behavior](#behavior-1)
- [Supported shells](#supported-shells)
- [Settings keys](#settings-keys)

## How it works

The alias is stored in settings under `System:Alias` and written to the profile of every supported shell found on your machine as an alias for `byo`. Open a new terminal and run any command under your name:

```bash
acme commands list
acme run --target workflow
```

`byo` keeps working alongside your name. The [interactive shell](shell.md) shows your name in its prompt, `acme>` instead of `byo>`.

An alias must start with a letter and contain only letters, digits, `-` or `_`.

## First-run prompt

The first time you run `byo` in an interactive terminal with no alias stored, it asks:

```text
No alias is set for byo. Do you want to set one?
```

- Answer **yes** to enter an alias. It is saved and deployed exactly as with [`alias set`](#alias-set).
- Answer **no** to skip. `System:SkipAliasPrompt` is set to `true` and you are not asked again. You can still set an alias later with `byo alias set`.

The prompt never appears when input is redirected (scripts, CI, pipes).

## alias set

Set or change the alias.

### Syntax

```bash
byo alias set
```

### Behavior

- Shows the current alias, if any, then prompts for the new one and validates it
- Does nothing when the alias you enter is already the stored one
- Saves the alias to settings, then deploys it to every supported shell found on the machine and reports which ones were updated
- Open a new terminal for the alias to take effect
- Changing the alias adds the new one; the previous alias stays in your shell profiles until you remove it by hand
- If no supported shell is found, the alias is saved in settings only and you are asked to add it to your shell profile manually
- A shell whose profile cannot be updated is reported as a warning; the other shells are still updated

## alias delete

Remove the alias from settings.

### Syntax

```bash
byo alias delete
```

### Behavior

- Warns when no alias is set
- Confirms before removing
- Removes `System:Alias` from settings only. The interactive shell prompt goes back to `byo>`
- Shell profiles are not touched. Remove the alias line manually from:
  - PowerShell: `$PROFILE`
  - bash: `~/.bashrc` (or the login profile on macOS)
  - zsh: `~/.zshrc`
  - fish: `~/.config/fish/config.fish`
  - cmd (Windows): `%USERPROFILE%\.byo_aliases.doskey`

## Supported shells

Each shell found on the machine gets the alias written in its own way:

| Shell | Found when | What is written |
| --- | --- | --- |
| PowerShell 7+ (`pwsh`) | On `PATH` (all platforms) | `Set-Alias <alias> byo` appended to `$PROFILE` (created if missing) |
| Windows PowerShell 5.1 | On `PATH` (Windows only) | `Set-Alias <alias> byo` appended to its own `$PROFILE` |
| cmd | Windows only | `<alias>=byo $*` macro in `%USERPROFILE%\.byo_aliases.doskey`, loaded through the Command Processor `AutoRun` registry value. An existing `AutoRun` command is kept and the macro file is chained after it |
| bash | Git Bash on Windows, `bash` on `PATH` elsewhere | `alias <alias>="byo"` appended to `~/.bashrc`. On macOS the first existing of `~/.bash_profile`, `~/.bash_login` or `~/.profile` is used instead (`~/.bash_profile` when none exists) |
| zsh | On `PATH` (Linux and macOS) | `alias <alias>="byo"` appended to `~/.zshrc` (or `$ZDOTDIR/.zshrc`) |
| fish | On `PATH` (Linux and macOS) | `alias <alias> byo` appended to `~/.config/fish/config.fish` (or `$XDG_CONFIG_HOME/fish/config.fish`) |

On Windows the WSL `bash.exe` launcher is skipped on purpose: its home directory and `PATH` are separate from Windows. Add the alias inside WSL yourself if you need it there.

Each deployment runs the shell with a temporary script and gives up after 30 seconds.

## Settings keys

| Key | Meaning |
| --- | --- |
| `System:Alias` | The alias in use. Setting it with `byo settings set` changes the shell prompt but writes nothing to your shell profiles; use `byo alias set` for that |
| `System:SkipAliasPrompt` | `true` after you decline the first-run prompt. Delete it to be asked again |
