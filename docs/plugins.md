# plugins command group

Manage BYO CLI plugins distributed as NuGet packages.

## Table of Contents

- [plugins list](#plugins-list)
  - [Syntax](#syntax)
  - [Behavior](#behavior)
- [plugins install](#plugins-install)
  - [Syntax](#syntax-1)
  - [Options](#options)
  - [Examples](#examples)
  - [Behavior](#behavior-1)
- [plugins uninstall](#plugins-uninstall)
  - [Syntax](#syntax-2)
  - [Options](#options-1)
  - [Examples](#examples-1)
  - [Behavior](#behavior-2)

## plugins list

List available SoftwareWorker plugin packages from NuGet.org.

### Syntax

```bash
byo plugins list
```

### Behavior

- Queries NuGet.org and lists packages matching `BYO.Plugin.*`
- Filters results to owner `softwareworkercom`
- Displays package id, latest version, and description

## plugins install

Install a BYO plugin package from NuGet.org.

### Syntax

```bash
byo plugins install --package <packageId> [--version <version>]
```

### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--package` | Yes | NuGet package id to install |
| `--version` | No | Package version to install. If omitted, installs latest stable version |

### Examples

```bash
byo plugins install --package BYO.Plugin.GoogleCalendar
byo plugins install --package BYO.Plugin.GoogleCalendar --version 1.0.0
```

### Behavior

- Downloads the package from NuGet.org
- Extracts assemblies and selects the best target framework automatically
- Validates that the package contains BYO command handlers
- Copies plugin binaries into local BYO plugin storage
- Shows detected handlers and suggests running `byo --help`

## plugins uninstall

Uninstall an installed plugin package.

### Syntax

```bash
byo plugins uninstall --package <packageId> [--version <version>]
```

### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--package` | Yes | NuGet package id to uninstall |
| `--version` | No | Version to remove. If omitted, removes all installed versions |

### Examples

```bash
byo plugins uninstall --package BYO.Plugin.GoogleCalendar
byo plugins uninstall --package BYO.Plugin.GoogleCalendar --version 1.0.0
```

### Behavior

- Removes package and binary directories for the selected plugin
- If `--version` is omitted, removes all installed versions for that package
- Cleans up empty package folders after version-specific uninstall