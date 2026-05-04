# BYO CLI

⭐ **Give us a star to support the project**

[![.NET](https://img.shields.io/badge/.NET-10-blueviolet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Issues](https://img.shields.io/github/issues/softwareworkercom/byo)](https://github.com/softwareworkercom/byo/issues)
[![Stars](https://img.shields.io/github/stars/softwareworkercom/byo?style=social)](https://github.com/softwareworkercom/byo/stargazers)

**BYO CLI** is a developer productivity tool. It helps developers organize commands, run repeatable workflows, and manage local secrets.

<img width="1113" height="626" alt="WindowsTerminal_aI8IWCqW4q" src="https://github.com/user-attachments/assets/0cb79010-16e7-4d17-a103-70d8dc1332fe" />

## Why BYO CLI?

⚡ Stay in the Zone
Run saved commands right away without leaving your terminal.

🧠
Less Friction, More Focus
Save the details once so you do not have to remember flags or search old notes.

🔐
Tailored to Your Stack
Keep your commands, settings, and secrets organized around the way you work.

🚀
Your Workflow, Your Rules
Build custom automations that match how you actually work. BYO adapts to you, not the other way around.

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

- [`docs/getting-started.md`](docs/getting-started.md)
- [`docs/token-replacement.md`](docs/token-replacement.md)

## Commands

- `byo --help`
- `byo commands --help`
- `byo settings --help`
- `byo secrets --help`
- `byo workflows --help`

## Contributing

We welcome contributions from developers, platform engineers, and DevOps practitioners.

1. Fork the repo
2. Create a branch (`feature/your-idea`)
3. Make your changes
4. Open a pull request

Please include a clear description of the problem, your solution, and examples when applicable.

## Community

- 🐞 Report bugs and request features in [Issues](https://github.com/softwareworkercom/byo/issues)
- 💬 Share ideas in [Discussions](https://github.com/softwareworkercom/byo/discussions)

## License

This project is licensed under the [MIT License](LICENSE).
