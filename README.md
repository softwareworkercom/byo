# BYO CLI

⭐ **Give us a star to support the project**

[![.NET](https://img.shields.io/badge/.NET-10-blueviolet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Issues](https://img.shields.io/github/issues/softwareworkercom/byo)](https://github.com/softwareworkercom/byo/issues)
[![Stars](https://img.shields.io/github/stars/softwareworkercom/byo?style=social)](https://github.com/softwareworkercom/byo/stargazers)

Automate the commands you run every day, without breaking your flow.

**BYO CLI** helps developers save repeatable workflows, organize local secrets and settings, and turn messy terminal habits into reliable automation.

Stop retyping. Start shipping.

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
dotnet tool install --global byo --version 0.0.1-g19f405295c
```

### Run

```bash
byo --help
```

> If the package is not published yet, clone this repo and run from source:
>
> ```bash
> dotnet build
> dotnet run --project src/SoftwareWorker.BYO.CLI -- --help
> ```

## Documentation

- [Getting Started](docs/getting-started.md)
- [Token System](docs/token-replacement)

## Run tests

From the repository root:

```bash
dotnet test tests/SoftwareWorker.BYO.Tests/SoftwareWorker.BYO.Tests.csproj
```

## Commands

- `byo --help`
- `byo run --help`
- `byo commands --help`
- `byo settings --help`
- `byo secrets --help`
- `byo workflows --help`

## Community

- 🐞 Report bugs and request features in [Issues](https://github.com/softwareworkercom/byo/issues)
- 💬 Share ideas in [Discussions](https://github.com/softwareworkercom/byo/discussions)

## License

This project is licensed under the [MIT License](LICENSE).
