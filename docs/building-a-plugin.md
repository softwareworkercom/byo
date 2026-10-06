# Building a plugin

A plugin is a NuGet package that adds your own command groups to `byo`. You write command handlers against `BYO.SDK`; command parsing, parameter binding, settings, secrets, token replacement and output are already done. `byo` discovers the handlers at startup, so your commands appear in `byo --help` alongside the built-in ones and work in the interactive shell, in scripts and in CI like any other command.

> **Scope:** the SDK gives you the command model, parameter binding, and the services below. The command-line host is the `byo` CLI itself, so today you extend `byo` rather than producing a separate standalone executable.

## Table of Contents

- [Quick start](#quick-start)
  - [1) Create the project](#1-create-the-project)
  - [2) Add a handler](#2-add-a-handler)
  - [3) Pack and install](#3-pack-and-install)
  - [4) Run it](#4-run-it)
- [How commands are discovered](#how-commands-are-discovered)
- [The command model](#the-command-model)
- [Parameters](#parameters)
  - [Interactive and non-interactive behavior](#interactive-and-non-interactive-behavior)
  - [Enumerated values](#enumerated-values)
  - [Type conversion](#type-conversion)
- [Dynamic parameters](#dynamic-parameters)
- [Services available to handlers](#services-available-to-handlers)
- [Packaging and distribution](#packaging-and-distribution)
- [Troubleshooting](#troubleshooting)

## Quick start

The steps below take a new plugin from an empty folder to a working `byo weather forecast` command.

### 1) Create the project

Create a class library whose name starts with `BYO.Plugin.` and reference the `BYO.SDK` version that matches your CLI (`byo --version`):

```bash
dotnet new classlib -n BYO.Plugin.Weather
cd BYO.Plugin.Weather
dotnet add package BYO.SDK --version 0.41.1
```

The `BYO.Plugin.` prefix is required: it is how `byo` tells plugin assemblies apart from their dependencies. The project file in [Packaging and distribution](#packaging-and-distribution) shows the resulting settings.

### 2) Add a handler

Replace the generated `Class1.cs` with a handler. The attributes declare the command and its parameters, and the SDK binds `--city` and `--units` to the matching properties before `ExecuteAsync` runs:

```csharp
using SoftwareWorker.BYO.SDK;
using SoftwareWorker.BYO.SDK.Abstractions.Attributes;
using SoftwareWorker.BYO.SDK.Services;

namespace BYO.Plugin.Weather.Handlers
{
    [TrunkCommand("weather", "Weather operations")]
    [BranchCommand("forecast", "Show the forecast for a city")]
    [Parameter("city", "City to look up", true, null)]
    [Parameter("units", "Unit system", false, "metric|imperial")]
    public class WeatherForecastHandler : BaseCommandHandler
    {
        public string? City { get; set; }
        public string? Units { get; set; }

        public override async Task ExecuteAsync()
        {
            // Non-interactive runs stop before this when --city is missing,
            // so only a terminal user is asked.
            if (string.IsNullOrWhiteSpace(City))
            {
                City = UserInterfaceService.Ask<string>("City to look up:");
            }

            var forecast = await GetForecastAsync(City, Units ?? "metric");

            UserInterfaceService.ShowGreen($"{City}: {forecast}");
        }

        // Stands in for your own data source, so the command works
        // before you wire anything up.
        private static Task<string> GetForecastAsync(string city, string units)
        {
            var forecast = units == "imperial" ? "72°F, clear" : "22°C, clear";
            return Task.FromResult(forecast);
        }
    }
}
```

`ExecuteAsync` is the only member you must implement. Nothing else is needed to register the command.

### 3) Pack and install

Register a local feed folder as a package source once, pack into it, and install from it:

```bash
byo settings set --key System:Plugins:Sources --value '["<local feed folder>"]'
dotnet pack -c Release -o <local feed folder>
byo plugins install --package BYO.Plugin.Weather
```

Use an absolute path for the folder. Plugins are installed from the first [package source](plugins.md#package-sources) that has them, so the folder is searched before NuGet.org and you can test before publishing. Once the package is on NuGet.org, or on a feed you have configured, the install command alone is enough.

### 4) Run it

```bash
byo weather forecast --city Sydney --units metric
```

With the stand-in method above it prints `Sydney: 22°C, clear`. The command appears in `byo --help` and in the interactive shell with completion, and it accepts `--schedule` and `--async` like every other command. At a terminal the handler asks for a missing city; in scripts and CI a missing required parameter fails fast instead of hanging on a prompt. See [Interactive and non-interactive behavior](#interactive-and-non-interactive-behavior).

## How commands are discovered

At startup `byo` scans its own assembly and every assembly under `~/byo/plugins/bin`, and collects every type that:

- is a concrete class (not abstract, not an interface), **and**
- derives from `SoftwareWorker.BYO.SDK.BaseCommandHandler`, **and**
- is decorated with `[TrunkCommand]`.

Each plugin is loaded into its own `AssemblyLoadContext` so its dependencies stay isolated from the CLI's. Only the plugin's own entry assembly is scanned for handlers, which is why the assembly name must start with `BYO.Plugin.` — see [Packaging and distribution](#packaging-and-distribution).

No registration call, manifest, or startup hook is required. The attributes are the registration.

## The command model

Commands are a three-level tree, which is what produces the `byo <group> <action> [options]` grammar. How many levels you get depends on which attributes you apply:

| Attributes on the handler | Resulting command |
| --- | --- |
| `[TrunkCommand]` | `byo <trunk>` — the group itself is executable |
| `[TrunkCommand]` + `[BranchCommand]` | `byo <trunk> <branch>` |
| `[TrunkCommand]` + `[BranchCommand]` + `[LeafCommand]` | `byo <trunk> <branch> <leaf>` |

Each attribute takes a name and a description. The description is what appears in `--help` and in the interactive shell's completion menu, so write it as a short sentence.

Handlers that share a trunk name are merged into one group. To add three actions to a `weather` group, you write three handler classes that all carry `[TrunkCommand("weather", ...)]` with different `[BranchCommand]` names — exactly how the built-in `settings set`, `settings list`, and `settings delete` are composed.

## Parameters

`[Parameter]` is applied to the **class**, not to the property, and may be repeated:

```csharp
[Parameter(name, description, isRequired, defaultValue)]
```

| Argument | Meaning |
| --- | --- |
| `name` | Exposed on the command line as `--{name}` |
| `description` | Shown in `--help` and in the interactive shell's completion menu |
| `isRequired` | Whether the command fails without it in non-interactive mode |
| `defaultValue` | Pipe-separated choices offered as completions — see [Enumerated values](#enumerated-values). Any other value is not applied, so pass `null` and fall back in `ExecuteAsync` |

Each parameter is matched to a **public writable property whose name matches `name`**, case-insensitively — `--city` binds to `City`. A parameter with no matching property is still accepted on the command line, but nothing binds its value, so add the property.

### Interactive and non-interactive behavior

The SDK never prompts for parameters. What it checks depends on whether the command is attached to an interactive console:

- **Non-interactive** (piped, redirected, CI) — missing required parameters produce `Missing required parameter(s): --city.` and the command does not run.
- **Interactive** — nothing is checked. The handler runs with whatever was supplied, and a missing value leaves its property at its default (`null` for the `string?` properties above).

So a handler that serves both a person at a terminal and an automated caller resolves missing values itself, as the [quick start handler](#2-add-a-handler) does for `--city`: ask with `UserInterfaceService.Ask<T>`, offer a list with `UserInterfaceService.SelectSingleItem`, or show an error. The built-in commands work the same way — `run` asks what to run when `--target` is missing, and lets you browse the bookmark hierarchy when `--name` is missing.

> **Upgrading a plugin:** earlier SDK versions had a fifth `isPromptable` argument on `[Parameter]` and could prompt for missing parameters. Both are gone. Remove the argument, ask for any value you still want to collect in `ExecuteAsync`, and rebuild the plugin against the new SDK: a build made against the older SDK fails to load, even if it never passed the argument.

### Enumerated values

A `defaultValue` containing `|` is treated as a list of choices rather than a default:

```csharp
[Parameter("units", "Unit system", false, "metric|imperial")]
```

The interactive shell offers those values as completions for `--units`. They are not enforced, so check the value in `ExecuteAsync` when anything else would be a problem.

### Type conversion

Values arrive as strings and are converted to the property type. `string`, `bool`, `int`, `long`, `double`, `decimal`, enums, and their nullable forms are supported.

**Conversion failures are silent.** A value that cannot be converted leaves the property at its default — `0`, `false`, `null`, or the first enum member — rather than raising an error. Validate inside `ExecuteAsync` when a wrong value would be worse than a missing one:

```csharp
if (string.IsNullOrEmpty(City))
{
    UserInterfaceService.ShowError("--city is required.");
    return;
}
```

## Dynamic parameters

Add `[AllowDynamicParameters]` when a command should accept options you cannot declare ahead of time — passing arbitrary flags through to another tool, for instance. Undeclared `--key value` pairs are then collected into the inherited `DynamicParameters` dictionary (case-insensitive) instead of being rejected:

```csharp
[TrunkCommand("proxy", "Forward arguments to an upstream tool")]
[AllowDynamicParameters]
public class ProxyHandler : BaseCommandHandler
{
    public override async Task ExecuteAsync()
    {
        foreach (var (key, value) in DynamicParameters)
        {
            Console.WriteLine($"{key} = {value}");
        }
    }
}
```

Note that any `--key value` pair on the command line is also available to [token replacement](token-replacement.md) as `{{key}}`, independently of this attribute.

## Services available to handlers

These live in `SoftwareWorker.BYO.SDK.Services` and are the same services the built-in commands use:

| Service | Use it for |
| --- | --- |
| `UserInterfaceService` | Output and prompts — `ShowGreen`, `ShowWarning`, `ShowError`, `Confirm`, `Ask<T>`, `SelectSingleItem`, and `IsInteractive` to branch on session type |
| `SettingsService` | Reading and writing non-sensitive key/value settings |
| `SecretsService` | Reading and writing encrypted secrets |
| `TokenService` | Resolving `{{Token}}` values from settings, secrets, built-ins, and object payloads — see [Token Replacement](token-replacement.md) |
| `CommandService` / `WorkflowService` | Reading and running the user's saved commands and workflows |
| `RestService` | Declarative HTTP clients defined as interfaces with attributes |
| `ResilienceService` | Retry, timeout, and circuit-breaker policies around calls you make |
| `ExportService` | Writing `DataTable` results to a timestamped JSON file — see [ExportService](sdk-export-service.md) |

Prefer `UserInterfaceService` over `Console.WriteLine` for anything a user reads: it honors the CLI's styling and behaves correctly when output is redirected.

## Packaging and distribution

A plugin is an ordinary NuGet package with two hard requirements:

1. **The assembly and package id must start with `BYO.Plugin.`** — for example `BYO.Plugin.Weather`. Only assemblies with this prefix are scanned for handlers; everything else next to them is treated as a transitive dependency.
2. **`BYO.SDK` must be referenced as a package**, and its version must match the CLI the plugin will run against. See [Troubleshooting](#troubleshooting).

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <AssemblyName>BYO.Plugin.Weather</AssemblyName>
  <PackageId>BYO.Plugin.Weather</PackageId>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="BYO.SDK" Version="0.41.1" />
</ItemGroup>
```

Test it locally before publishing by packing into a folder that is configured as a package source, as in the [quick start](#3-pack-and-install):

```bash
dotnet pack -c Release -o <local feed folder>
byo plugins install --package BYO.Plugin.Weather
byo weather forecast --city Sydney
```

Then publish to NuGet.org, or to your own NuGet feed (Azure Artifacts, GitHub Packages or any other NuGet V3 server), and install by package id:

```bash
byo plugins install --package BYO.Plugin.Weather
```

A feed other than NuGet.org is configured once as a [package source](plugins.md#package-sources), with its credentials kept in secrets. Installs then search that feed and NuGet.org in order, and dependencies are fetched from the same sources.

Installed plugins live under `~/byo/plugins` (`packages` for the downloaded package, `bin` for the extracted assemblies). `byo plugins list` shows `BYO.Plugin.*` packages owned by `softwareworkercom` on NuGet.org and every `BYO.Plugin.*` package in your configured sources. A package you publish to NuGet.org under another owner does not appear in that list, but it installs by id all the same.

See the [plugins command group](plugins.md) for the full install and uninstall reference.

## Troubleshooting

**`Could not load type 'SoftwareWorker.BYO.SDK.BaseCommandHandler'`, or your commands are missing from `--help`.**

The plugin was compiled against a different `BYO.SDK` version than the CLI is running, and the types no longer line up. `byo` reports this as a warning at startup and skips the plugin rather than failing. Rebuild the plugin against the SDK version matching your CLI (`byo --version`) and reinstall it.

**Your handler is ignored but the assembly loads.**

Check that the class is concrete (not abstract, not an interface), derives from `BaseCommandHandler`, and carries `[TrunkCommand]`. All three are required for discovery. Declaring it `public` is not strictly required, but it is the safer default for a type instantiated from another assembly.

**A parameter is always empty.**

The property name must match the `[Parameter]` name and be publicly writable. If the value is supplied but the type cannot be converted, the property is silently left at its default — see [Type conversion](#type-conversion).

**A plugin no longer asks for a missing parameter, or fails to build on `isPromptable`.**

The SDK no longer prompts for parameters, and `[Parameter]` no longer takes an `isPromptable` argument. Remove the argument and ask for the value in `ExecuteAsync` — see [Interactive and non-interactive behavior](#interactive-and-non-interactive-behavior).

**Every command fails with `Method not found: 'Void SoftwareWorker.BYO.SDK.Abstractions.Attributes.ParameterAttribute..ctor(...)'`.**

An installed plugin was built against an older SDK whose `[Parameter]` constructor took the `isPromptable` argument. Rebuild it against the SDK version matching your CLI and reinstall it. Until then no `byo` command can start, `byo plugins uninstall` included, so delete the plugin's folder under `~/byo/plugins/bin` to get the CLI working again.
