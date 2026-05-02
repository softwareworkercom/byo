# BYO CLI

[![.NET](https://img.shields.io/badge/.NET-10-blueviolet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Issues](https://img.shields.io/github/issues/softwareworkercom/byo)](https://github.com/softwareworkercom/byo/issues)
[![Stars](https://img.shields.io/github/stars/softwareworkercom/byo?style=social)](https://github.com/softwareworkercom/byo/stargazers)

**BYO CLI** is a modern, extensible developer productivity CLI built with .NET.
It helps teams organize commands, run repeatable workflows, and manage local secrets in a single command-line experience.

## Why BYO CLI?

- ⚡ **Fast local automation** for day-to-day developer and DevOps tasks
- 🧩 **Extensible command model** (trunk/branch/leaf command hierarchy)
- 🔐 **Encrypted secrets support** for local secure workflows
- 🗂️ **Saved commands & workflows** for consistency across environments
- 🎛️ **Interactive mode** and optional scheduling/background execution

## Quick Start

### Prerequisites

- .NET SDK 10+

### Install as a global tool

```bash
dotnet tool install --global byo
```

### Run

```bash
byo help
```

> If the package is not published yet, clone this repo and run from source:
>
> ```bash
> dotnet build
> dotnet run --project src/SoftwareWorker.BYO.CLI -- help
> ```

## Core Command Areas

- `byo help` — show all available commands
- `byo commands ...` — create/read/update/delete/run saved commands
- `byo workflows ...` — create/read/delete/run workflows
- `byo secrets ...` — read/update/delete/reencrypt encrypted secrets
- `byo settings ...` — read/update/delete configuration settings

## Contributing

We welcome contributions from developers, platform engineers, and DevOps practitioners.

1. Fork the repo
2. Create a branch (`feature/your-idea`)
3. Make your changes
4. Open a pull request

Please include a clear description of the problem, your solution, and examples when applicable.

## Community

- ⭐ Star this repo if BYO CLI is useful
- 🐞 Report bugs and request features in [Issues](https://github.com/softwareworkercom/byo/issues)
- 💬 Share ideas in Discussions

## License

This project is licensed under the [MIT License](LICENSE).
