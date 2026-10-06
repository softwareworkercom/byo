# 🚨 Testers Needed: Linux and macOS

We are currently looking for testers on **Linux** and **macOS**. If you can help validate the CLI behavior on these platforms, please open an issue or share feedback.

# BYO CLI

### Build CLIs for the AI Era

**Turn your software into tools that humans, automation, and AI agents can use.**

[![.NET](https://img.shields.io/badge/.NET-10-blueviolet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Issues](https://img.shields.io/github/issues/softwareworkercom/byo)](https://github.com/softwareworkercom/byo/issues)
[![Stars](https://img.shields.io/github/stars/softwareworkercom/byo?style=social)](https://github.com/softwareworkercom/byo/stargazers)

⭐ **Give us a star to support the project**

[Get Started](#get-started-in-seconds) · [Building a plugin](#building-a-plugin) · [Documentation](#documentation) · [Getting Started Guide](docs/getting-started.md)

---

## The way we interact with software is changing

- **APIs** made software accessible to applications.
- **CLIs** made software accessible to developers.
- **AI agents** are making software capabilities accessible through natural language.

But agents still need reliable, deterministic tools to actually get things done. That's where the CLI comes in.

## The CLI is becoming an interface for AI

A good CLI is already designed for what AI agents need:

- **Discoverable.** Agents can understand commands and capabilities.
- **Composable.** Commands can be combined into workflows.
- **Automatable.** CLIs work naturally with scripts, CI/CD and automation.
- **Deterministic.** The same command produces predictable results.
- **Readable by machines.** Structured JSON output makes results easy for software and agents to consume.
- **Local and secure.** CLIs can run alongside the developer, their code and their existing authentication environment.
- **Cheaper in tokens.** Agents discover a CLI on demand through `--help` and can filter its output before it reaches the model. A typical MCP setup loads every tool definition into the context up front and passes every result through it.

Anthropic's engineering team describes those two costs, and the same remedy of on-demand discovery and filtering results before they reach the model, in [Code execution with MCP](https://www.anthropic.com/engineering/code-execution-with-mcp).

The CLI isn't going away. It's evolving.

## Build the capability, not the plumbing

Building a production-ready CLI from scratch means repeatedly solving the same problems:

- Authentication
- Configuration
- Secrets
- Command parsing
- Validation
- Logging
- Error handling
- Output formatting
- Updates
- Platform support

The BYO SDK takes care of the infrastructure so you can focus on what actually matters: **your software's capabilities.**

## One CLI. Multiple interfaces.

Build your CLI once and expose it to:

| | Audience | What it gets |
| --- | --- | --- |
| 👨‍💻 | **Developers** | A powerful interface to your platform. |
| ⚙️ | **Automation** | Your capabilities in scripts, CI/CD and workflows. |
| 🤖 | **AI Agents** | Deterministic, executable tools that agents can discover and use. |

```text
            Your Software
                 ▼
              CLI SDK
                 ▼
   Humans   Automation   AI Agents
```

Instead of building separate experiences for every consumer, create one executable interface that serves them all.

## Build once. Let humans and AI use it.

The next generation of software won't just be consumed through websites and APIs. It will be executed by people, automated by systems, and orchestrated by AI agents.

Build the tools they'll need. **Build your CLI with BYO.**

*From API to CLI. From CLI to AI.*

---

## Get Started in Seconds

Run a single command to install. Works across all major platforms.

**macOS / Linux**

```bash
curl -fsSL https://github.com/softwareworkercom/byo/releases/latest/download/install.sh | bash
```

**Windows (PowerShell)**

```powershell
iwr -useb https://github.com/softwareworkercom/byo/releases/latest/download/install.ps1 | iex
```

> The installers detect your OS/architecture, download the matching binary, verify its SHA256 checksum, and add it to your PATH.

Then run:

```bash
byo --help
```

## What's in the box

The `byo` CLI is built with the BYO SDK and is the reference for what you get. It helps you save repeatable commands, organize local settings, and turn messy terminal habits into reliable automation.

- **Never lose a command again.** Save the exact commands that worked, and run them instantly whenever you need them.
- **Less context switching.** No more digging through notes, Slack messages, or shell history to remember flags and scripts.
- **Secrets stay local.** Sensitive values are collected by workflows and stored in the local, encrypted secrets store. No account, no sync service, no hosted backend.
- **Automate the boring stuff.** Turn repetitive setup, deployment, and debugging steps into reusable workflows in seconds.
- **Extend it.** Add new command groups through NuGet plugins built against `BYO.SDK`. See [Building a plugin](#building-a-plugin).

## Building a plugin

A plugin is a NuGet package that adds your own command groups to `byo`. Write command handlers against `BYO.SDK`, pack, install, and your commands appear in `byo --help`, the interactive shell, scripts and CI alongside the built-in ones.

- **[Building a plugin](docs/building-a-plugin.md)**: Quick start, the command model, parameters, SDK services, packaging and troubleshooting

## Documentation

BYO CLI is built around seven command groups:

- **[run](docs/run.md)**: Execute saved commands or workflows
- **[commands](docs/commands.md)**: Manage saved shell commands
- **[settings](docs/settings.md)**: Manage configuration key-value pairs
- **[secrets](docs/secrets.md)**: Manage encrypted sensitive values
- **[workflows](docs/workflows.md)**: Manage multi-step automation workflows
- **[plugins](docs/plugins.md)**: Discover, install, and uninstall plugins from NuGet.org, your own NuGet feeds or local folders
- **[alias](docs/alias.md)**: Give the CLI your own name

Prefer to stay in BYO? Run **[byo](docs/shell.md)** without arguments to open an interactive shell with autocompletion, syntax highlighting and history.

**[Token replacement](docs/token-replacement.md)** resolves `{{Key}}` against settings, secrets, built-in values, and command-line overrides when a command runs.

### Built-in flags

These flags are available on executable commands where applicable:

- **[schedule](docs/schedule.md)**: Repeat a command on a schedule
- **[async](docs/async.md)**: Run a command in the background

### Data Storage

- **[storage](docs/export.md)**: Learn where BYO CLI stores your data and understand the file formats

Secrets can be set directly with `byo secrets set`, or collected during a run by a workflow `InputAsSecret` step. Either way they are encrypted at rest and referenced with token replacement.

All commands follow a consistent pattern: `byo <group> <action> [options]`

For a complete walkthrough with a practical example check out the [Getting Started Guide](docs/getting-started.md).

## Community

- 🐞 Report bugs and request features in [Issues](https://github.com/softwareworkercom/byo/issues)
- 💬 Share ideas in [Discussions](https://github.com/softwareworkercom/byo/discussions)

## License

This project is licensed under the [MIT License](LICENSE).
