# BYO CLI

⭐ **Give us a star to support the project**

[![.NET](https://img.shields.io/badge/.NET-10-blueviolet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Issues](https://img.shields.io/github/issues/softwareworkercom/byo)](https://github.com/softwareworkercom/byo/issues)
[![Stars](https://img.shields.io/github/stars/softwareworkercom/byo?style=social)](https://github.com/softwareworkercom/byo/stargazers)

**BYO CLI** helps developers save repeatable workflows, organize local secrets and settings, and turn messy terminal habits into reliable automation.

## Built for Developers Who Live in the Terminal

### ⚡ Never Lose a Command Again

Save the exact commands that worked, and run them instantly whenever you need them.

### 🧠 Less Context Switching

No more digging through notes, Slack messages, or shell history to remember flags and scripts.

### 🔐 Secrets Stay Local

Manage environment variables and credentials safely without scattering them across files.

### 🚀 Automate the Boring Stuff

Turn repetitive setup, deployment, and debugging steps into reusable workflows in seconds.

## Benefits

1. Spend less time remembering commands
2. Reduce onboarding friction
3. Standardize workflows without heavy tooling
4. Keep your terminal fast, clean, and predictable

## Quick Start

### Prerequisites

- .NET SDK 10 (https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

### Install as a global tool

```bash
dotnet tool install --global byo --version 0.0.2-gff85c0aba4
```

### Run

```bash
byo --help
```

## Documentation

- [Getting Started](docs/getting-started.md) - Complete walkthrough with practical examples
- [Commands Reference](docs/commands-reference.md) - Command docs index
- [run command](docs/command-run.md)
- [commands command group](docs/command-commands.md)
- [settings command group](docs/command-settings.md)
- [secrets command group](docs/command-secrets.md)
- [workflows command group](docs/command-workflows.md)
- [Token System](docs/token-replacement.md) - Token replacement and resolution details

## Commands

BYO provides five main command groups for managing your development workflows:

- **`byo run`** - Execute saved commands or workflows
- **`byo commands`** - Manage saved shell commands (set, list, delete)
- **`byo settings`** - Manage configuration key-value pairs (set, list, delete)
- **`byo secrets`** - Manage encrypted sensitive data (set, list, delete, reencrypt)
- **`byo workflows`** - Manage multi-step automation workflows (set, list, delete)

For detailed usage and examples of each command, see the command docs listed in [Commands Reference](docs/commands-reference.md).

### Quick Command Reference

```bash
byo --help                    # Show general help
byo run --help                # Show help for run command
byo commands --help           # Show help for commands management
byo settings --help           # Show help for settings management
byo secrets --help            # Show help for secrets management
byo workflows --help          # Show help for workflows management
```

## Community

- 🐞 Report bugs and request features in [Issues](https://github.com/softwareworkercom/byo/issues)
- 💬 Share ideas in [Discussions](https://github.com/softwareworkercom/byo/discussions)

## License

This project is licensed under the [MIT License](LICENSE).
