# Building a plugin with the SDK

A plugin adds your own command groups to the `byo` CLI. You write command handlers against `BYO.SDK`, publish them as a NuGet package, and `byo` discovers them at startup — your commands then appear in `byo --help` alongside the built-in ones, and work in the interactive shell, in scripts, and in CI like any other command.

> **Scope:** the SDK gives you the command model, parameter binding, and the services below. The command-line host is the `byo` CLI itself, so today you extend `byo` rather than producing a separate standalone executable.

## Table of Contents

- [How commands are discovered](#how-commands-are-discovered)
- [The command model](#the-command-model)
- [A minimal handler](#a-minimal-handler)
- [Parameters](#parameters)
  - [Interactive and non-interactive behavior](#interactive-and-non-interactive-behavior)
  - [Enumerated values](#enumerated-values)
  - [Type conversion](#type-conversion)
- [Dynamic parameters](#dynamic-parameters)
- [Services available to handlers](#services-available-to-handlers)
- [Packaging and distribution](#packaging-and-distribution)
- [Troubleshooting](#troubleshooting)

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

## A minimal handler

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
            var forecast = await GetForecastAsync(City!, Units ?? "metric");

            UserInterfaceService.ShowGreen($"{City}: {forecast}");
        }
    }
}
```

This produces:

```bash
byo weather forecast --city Sydney --units metric
```

`ExecuteAsync` is the only member you must implement. Parameters are already bound to your properties by the time it runs.

## Parameters

`[Parameter]` is applied to the **class**, not to the property, and may be repeated:

```csharp
[Parameter(name, description, isRequired, defaultValue, isPromptable = true)]
```

| Argument | Meaning |
| --- | --- |
| `name` | Exposed on the command line as `--{name}` |
| `description` | Shown in `--help` and when prompting |
| `isRequired` | Whether the command fails without it in non-interactive mode |
| `defaultValue` | A default, or pipe-separated choices — see [Enumerated values](#enumerated-values) |
| `isPromptable` | Whether the user may be prompted for it in an interactive session. Defaults to `true` |

Each parameter is matched to a **public writable property whose name matches `name`**, case-insensitively — `--city` binds to `City`. A parameter with no matching property is still accepted on the command line and still prompted for; it simply is not bound, so read it from `DynamicParameters` or add the property.

### Interactive and non-interactive behavior

The same handler behaves differently depending on whether it is attached to an interactive console:

- **Interactive** — every declared parameter marked `isPromptable` that was not supplied on the command line is prompted for. Required parameters re-prompt until a value is given; optional ones may be left empty.
- **Non-interactive** (piped, redirected, CI) — nothing is prompted. Missing required parameters produce `Missing required parameter(s): --city.` and the command does not run.

This is what lets one handler serve a human at a prompt and an automated caller, without branching in your code. Mark a parameter `isPromptable: false` when prompting for it would make no sense — the built-in `run` handler does this for `--name` and `--bookmark`, which it resolves through its own selection UI instead.

### Enumerated values

A `defaultValue` containing `|` is treated as a list of choices rather than a default:

```csharp
[Parameter("units", "Unit system", false, "metric|imperial")]
```

Interactively this renders a selection prompt instead of a free-text prompt. The interactive shell also offers those values as completions for `--units`.

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
  <PackageReference Include="BYO.SDK" Version="0.38.0" />
</ItemGroup>
```

Test it locally before publishing by installing from a folder of `.nupkg` files:

```bash
dotnet pack -c Release
byo plugins install --package BYO.Plugin.Weather --source ./bin/Release
byo weather forecast --city Sydney
```

Then publish to NuGet.org and install by package id:

```bash
byo plugins install --package BYO.Plugin.Weather
```

Installed plugins live under `~/byo/plugins` (`packages` for the downloaded package, `bin` for the extracted assemblies). `byo plugins list` shows `BYO.Plugin.*` packages owned by `softwareworkercom` on NuGet.org; your own package is installable by id or from a local `--source` whether or not it appears in that list.

See the [plugins command group](plugins.md) for the full install and uninstall reference.

## Troubleshooting

**`Could not load type 'SoftwareWorker.BYO.SDK.BaseCommandHandler'`, or your commands are missing from `--help`.**

The plugin was compiled against a different `BYO.SDK` version than the CLI is running, and the types no longer line up. `byo` reports this as a warning at startup and skips the plugin rather than failing. Rebuild the plugin against the SDK version matching your CLI (`byo --version`) and reinstall it.

**Your handler is ignored but the assembly loads.**

Check that the class is concrete (not abstract, not an interface), derives from `BaseCommandHandler`, and carries `[TrunkCommand]`. All three are required for discovery. Declaring it `public` is not strictly required, but it is the safer default for a type instantiated from another assembly.

**A parameter is always empty.**

The property name must match the `[Parameter]` name and be publicly writable. If the value is supplied but the type cannot be converted, the property is silently left at its default — see [Type conversion](#type-conversion).
