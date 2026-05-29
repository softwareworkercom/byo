## BYO CLI

Self-contained binaries are attached below for Windows, macOS (Apple Silicon), and Linux (x64 + arm64). No .NET SDK required.

### Install (one-liner)

**macOS / Linux**

```bash
curl -fsSL https://github.com/softwareworkercom/byo/releases/latest/download/install.sh | bash
```

**Windows (PowerShell)**

```powershell
iwr -useb https://github.com/softwareworkercom/byo/releases/latest/download/install.ps1 | iex
```

> The installers detect your OS/architecture, download the matching binary, verify its SHA256 checksum, and add it to your PATH.

### Install as a .NET global tool

```bash
dotnet tool install --global byo
```

### Manual install

1. Download the archive for your platform from the **Assets** below:
   - `byo-<version>-win-x64.zip`
   - `byo-<version>-linux-x64.tar.gz`
   - `byo-<version>-linux-arm64.tar.gz`
   - `byo-<version>-osx-arm64.tar.gz`
2. Extract it and place the `byo` (or `byo.exe`) binary on your `PATH`.
3. (Optional) Verify the download against `SHA256SUMS.txt`.

### Verify

```bash
byo --help
byo --version
```

### Supported platforms

| OS | Architectures |
| --- | --- |
| Windows | x64 |
| macOS | arm64 (Apple Silicon) |
| Linux | x64, arm64 |

See [docs/release-and-distribution.md](https://github.com/softwareworkercom/byo/blob/main/docs/release-and-distribution.md) for upgrade, rollback, and version pinning details.
