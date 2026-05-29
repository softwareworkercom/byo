## byo CLI

Self-contained binaries for Windows, macOS (Intel + Apple Silicon) and Linux (x64 + arm64). **No .NET runtime required.**

### Install

**macOS / Linux**
```bash
curl -fsSL https://github.com/softwareworkercom/byo/releases/latest/download/install.sh | bash
```

**Windows (PowerShell)**
```powershell
iwr -useb https://github.com/softwareworkercom/byo/releases/latest/download/install.ps1 | iex
```

**Pin a specific version**
```bash
BYO_VERSION=1.0.0 \
  curl -fsSL https://github.com/softwareworkercom/byo/releases/download/v1.0.0/install.sh | bash
```

### Verify

```bash
byo --help
```

### Checksums

Every asset in this release has an entry in `SHA256SUMS.txt`. The installer scripts verify the checksum automatically before writing any file to disk. You can also verify manually:

```bash
shasum -a 256 -c SHA256SUMS.txt --ignore-missing
```

### Upgrade

Re-run the installer — it overwrites the binary in place.

### Rollback

```bash
BYO_VERSION=1.0.0 curl -fsSL https://github.com/softwareworkercom/byo/releases/download/v1.0.0/install.sh | bash
```

### What's changed

<!-- Auto-populated by GitHub when generate_release_notes is true -->
