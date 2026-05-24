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

### Install without the .NET SDK (self-contained)

**macOS / Linux**

```bash
curl -fsSL https://github.com/softwareworkercom/byo/releases/latest/download/install.sh | bash
```

**Windows (PowerShell)**

```powershell
iwr -useb https://github.com/softwareworkercom/byo/releases/latest/download/install.ps1 | iex
```

See [docs/release-and-distribution.md](docs/release-and-distribution.md) for upgrade, rollback, pinning a version, supported platforms (Windows, macOS Intel + Apple Silicon, Linux x64 + arm64), and the full release process.

### Run

```bash
byo --help
```

## Documentation


BYO CLI is built around six main command groups:

- **[run](docs/run.md)**: Execute saved commands or workflows
- **[commands](docs/commands.md)**: Manage saved shell commands
- **[settings](docs/settings.md)**: Manage configuration key-value pairs
- **[secrets](docs/secrets.md)**: Manage encrypted sensitive data
- **[workflows](docs/workflows.md)**: Manage multi-step automation workflows
- **[extensions](docs/extensions.md)**: Discover, install, and uninstall extensions

All commands follow a consistent pattern: `byo <group> <action> [options]`

For a complete walkthrough with a practical example check out the [Getting Started Guide](docs/getting-started.md).

## Community

- 🐞 Report bugs and request features in [Issues](https://github.com/softwareworkercom/byo/issues)
- 💬 Share ideas in [Discussions](https://github.com/softwareworkercom/byo/discussions)

## License

This project is licensed under the [MIT License](LICENSE).
