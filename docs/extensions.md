# extensions command group

Manage BYO CLI extensions distributed as NuGet packages.

## Table of Contents

- [extensions list](#extensions-list)
  - [Syntax](#syntax)
  - [Behavior](#behavior)
- [extensions install](#extensions-install)
  - [Syntax](#syntax-1)
  - [Options](#options)
  - [Examples](#examples)
  - [Behavior](#behavior-1)
- [extensions uninstall](#extensions-uninstall)
  - [Syntax](#syntax-2)
  - [Options](#options-1)
  - [Examples](#examples-1)
  - [Behavior](#behavior-2)

## extensions list

List available SoftwareWorker extension packages from NuGet.org.

### Syntax

```bash
byo extensions list
```

### Behavior

- Queries NuGet.org and lists packages matching `SoftwareWorker.BYO.Extensions.*`
- Filters results to owner `softwareworkercom`
- Displays package id, latest version, and description

## extensions install

Install a BYO extension package from NuGet.org.

### Syntax

```bash
byo extensions install --package <packageId> [--version <version>]
```

### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--package` | Yes | NuGet package id to install |
| `--version` | No | Package version to install. If omitted, installs latest stable version |

### Examples

```bash
byo extensions install --package SoftwareWorker.BYO.Extensions.GoogleCalendar
byo extensions install --package SoftwareWorker.BYO.Extensions.GoogleCalendar --version 1.0.0
```

### Behavior

- Downloads the package from NuGet.org
- Extracts assemblies and selects the best target framework automatically
- Validates that the package contains BYO command handlers
- Copies extension binaries into local BYO extension storage
- Shows detected handlers and suggests running `byo --help`

## extensions uninstall

Uninstall an installed extension package.

### Syntax

```bash
byo extensions uninstall --package <packageId> [--version <version>]
```

### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--package` | Yes | NuGet package id to uninstall |
| `--version` | No | Version to remove. If omitted, removes all installed versions |

### Examples

```bash
byo extensions uninstall --package SoftwareWorker.BYO.Extensions.GoogleCalendar
byo extensions uninstall --package SoftwareWorker.BYO.Extensions.GoogleCalendar --version 1.0.0
```

### Behavior

- Removes package and binary directories for the selected extension
- If `--version` is omitted, removes all installed versions for that package
- Cleans up empty package folders after version-specific uninstall