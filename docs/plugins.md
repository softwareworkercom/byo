# plugins command group

Manage BYO CLI plugins distributed as NuGet packages. Plugins can come from NuGet.org, from any other NuGet feed (Azure Artifacts, GitHub Packages, Artifactory, Nexus, BaGet and others) or from a folder of `.nupkg` files.

## Table of Contents

- [Package sources](#package-sources)
  - [Configuring sources](#configuring-sources)
  - [Private feeds](#private-feeds)
  - [Search order](#search-order)
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

## Package sources

A package source is one of:

- a **NuGet V3 feed**, identified by its service index URL, for example `https://api.nuget.org/v3/index.json` or `https://pkgs.dev.azure.com/<org>/_packaging/<feed>/nuget/v3/index.json`
- a **folder** containing `.nupkg` files, for example a folder you pack into with `dotnet pack -o <folder>`

NuGet.org is always available. Other sources are configured once in settings and used by every `plugins` command. Feeds that only speak the NuGet V2 (OData) protocol are not supported.

### Configuring sources

Store the sources in the `System:Plugins:Sources` setting as a JSON array. An entry is a location, or `name=location` when you want to attach credentials to it. Use absolute paths for folders:

```bash
byo settings set --key System:Plugins:Sources --value '["corp=https://pkgs.dev.azure.com/acme/_packaging/tools/nuget/v3/index.json", "C:\\feeds\\plugins"]'
```

A folder source is the way to install a plugin you are building before it is published: pack into the folder and the package is installable right away.

To stop using NuGet.org, for example on a network where it is blocked:

```bash
byo settings set --key System:Plugins:UseNuGetOrg --value false
```

### Private feeds

Credentials for a named source live in secrets, encrypted at rest, under `System:Plugins:Sources:<name>:Username` and `System:Plugins:Sources:<name>:Password`. They are sent as HTTP basic authentication, which is what Azure Artifacts and GitHub Packages expect for a personal access token:

```bash
byo secrets set --key System:Plugins:Sources:corp:Username --value build-user
byo secrets set --key System:Plugins:Sources:corp:Password --value <personal access token>
```

Azure Artifacts accepts any user name together with the token; GitHub Packages needs your GitHub user name.

### Search order

Commands look through the sources in this order and use the first one that has the package:

1. The configured sources, in the order they are listed in `System:Plugins:Sources`.
2. NuGet.org, unless `System:Plugins:UseNuGetOrg` is `false`.

A source that cannot be reached, or that rejects the credentials, is reported as a warning and skipped, so one feed being down does not block installs from the others. The dependencies of a plugin are fetched the same way, starting with the source the plugin came from.

## plugins list

List the plugins available in the configured package sources.

### Syntax

```bash
byo plugins list
```

### Behavior

- Searches every source for packages whose id starts with `BYO.Plugin.`
- On NuGet.org, keeps only packages owned by `softwareworkercom`; other sources are listed in full
- Displays package id, latest version, the source it was found in, and description

## plugins install

Install a BYO plugin package from the first configured source that has it.

### Syntax

```bash
byo plugins install --package <packageId> [--version <version>]
```

### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--package` | Yes | NuGet package id to install |
| `--version` | No | Package version to install. If omitted, installs the latest stable version, or the latest prerelease when there is no stable one |

### Examples

```bash
byo plugins install --package BYO.Plugin.GoogleCalendar
byo plugins install --package BYO.Plugin.GoogleCalendar --version 1.0.0
```

The same command installs from a private feed or a local folder once that source is [configured](#configuring-sources).

### Behavior

- Walks the sources in the [search order](#search-order) and downloads from the first one that has a matching version
- Extracts assemblies and selects the best target framework automatically
- Validates that the package contains BYO command handlers
- Copies plugin binaries into local BYO plugin storage
- Downloads the plugin's NuGet dependencies from the same sources and places them next to it
- Shows the source the plugin came from and the detected handlers, and suggests running `byo --help`

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
