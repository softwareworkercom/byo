# Release & Distribution Guide

This document describes how the **byo** CLI is built, versioned, packaged, and
released to end users. It is the source of truth for maintainers and
contributors.

> **TL;DR** — Push a `v*.*.*` tag to `main`. GitHub Actions builds self-contained
> binaries for all five supported platforms, generates SHA256 checksums, and
> publishes a GitHub Release with auto-generated notes. Users install with one
> command:
>
> ```bash
> # macOS / Linux
> curl -fsSL https://github.com/softwareworkercom/byo/releases/latest/download/install.sh | bash
> # Windows (PowerShell)
> iwr -useb https://github.com/softwareworkercom/byo/releases/latest/download/install.ps1 | iex
> ```

---

## 1. Distribution Channels

| Channel | Status | Where | Purpose |
|---|---|---|---|
| `dotnet tool install -g byo` | **Active** | nuget.org | For developers who already have .NET 10 SDK. Published by `publish.yml`. |
| Self-contained binaries (GitHub Releases) | **New, this guide** | github.com/softwareworkercom/byo/releases | For users without the .NET SDK/runtime. Published by `release.yml`. |
| winget / Homebrew | Future | — | Considered once download volume justifies the maintenance overhead. |

Both channels coexist. The NuGet tool path is unchanged.

---

## 2. Supported Platforms (RIDs)

| RID | Runner | Archive | Binary |
|---|---|---|---|
| `win-x64` | `windows-latest` | `.zip` | `byo.exe` |
| `linux-x64` | `ubuntu-latest` | `.tar.gz` | `byo` |
| `linux-arm64` | `ubuntu-latest` (cross) | `.tar.gz` | `byo` |
| `osx-x64` | `macos-13` (Intel) | `.tar.gz` | `byo` |
| `osx-arm64` | `macos-latest` (Apple Silicon) | `.tar.gz` | `byo` |

---

## 3. Architecture Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| ADR-01 | **Self-contained publish** | Users do not need any .NET SDK or runtime installed. |
| ADR-02 | **Single-file publish** with `IncludeNativeLibrariesForSelfExtract=true` and `EnableCompressionInSingleFile=true` | One file to copy onto `PATH`. Compression keeps download size manageable. |
| ADR-03 | **ReadyToRun (R2R)** enabled | Faster cold start; the small size cost is worth it for a frequently invoked CLI. |
| ADR-04 | **Trimming = `partial`** | Smaller binaries while keeping reflection/DI/serializer code paths safe. `full` trimming would require auditing every reflective code path (Spectre.Console, System.CommandLine, Refit). |
| ADR-05 | **Native AOT = No (for now)** | Several dependencies (Refit, parts of Spectre.Console, JSON reflection) are not AOT-safe. Revisit after dependency surface is audited. |
| ADR-06 | **GitHub Releases as primary channel** | Free, integrity-checked over HTTPS, no third-party registry overhead. Package managers can be layered on later. |
| ADR-07 | **Tag-driven release** (`v*.*.*`) | A single source of truth for versions and a clean audit trail. |
| ADR-08 | **SHA256 checksums** in every release | Integrity verification in the installers without requiring a code-signing certificate. |
| ADR-09 | **Reproducible build flags** (`Deterministic`, `ContinuousIntegrationBuild`, `EmbedUntrackedSources`) | Output verifiable across CI runs. |

---

## 4. Versioning (SemVer 2.0.0)

- Tag format: **`vMAJOR.MINOR.PATCH`** (e.g. `v1.4.2`). Pre-releases: `v1.4.2-rc.1`.
- **MAJOR** — breaking CLI surface (removed/renamed commands, changed exit codes, breaking config format).
- **MINOR** — new commands, new flags, backward-compatible behavior.
- **PATCH** — bug fixes, performance, docs.
- Pre-1.0: anything may change; communicate breakage in release notes.
- The release workflow extracts the version from the tag (`${GITHUB_REF_NAME#v}`)
  and overrides MSBuild `Version` and `InformationalVersion` on the publish
  command line. The `Version` baked into the binary always matches the tag.
- Nerdbank.GitVersioning (`version.json`) continues to drive NuGet tool
  versioning. The two pipelines are independent.

---

## 5. Folder Structure

```
byo/
├── src/
│   └── SoftwareWorker.BYO.CLI/          # CLI project (PackAsTool=true for NuGet)
├── tests/
│   └── SoftwareWorker.BYO.Tests/
├── installers/
│   ├── install.sh                       # macOS / Linux installer
│   └── install.ps1                      # Windows installer
├── .github/
│   ├── workflows/
│   │   ├── ci.yml                       # Build + test matrix on PRs / main
│   │   ├── publish.yml                  # NuGet tool publish (existing)
│   │   └── release.yml                  # Tag-driven self-contained release
│   ├── release.yml                      # Auto-release-notes config
│   └── RELEASE_TEMPLATE.md              # Body template for each release
└── docs/
    └── release-and-distribution.md      # (this file)
```

---

## 6. Local Publish Testing

Test a single RID locally without touching the workflow:

```bash
dotnet publish src/SoftwareWorker.BYO.CLI/SoftwareWorker.BYO.CLI.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -p:PublishReadyToRun=true \
  -p:PublishTrimmed=true \
  -p:TrimMode=partial \
  -p:AssemblyName=byo \
  -p:Version=0.0.0-local \
  -p:PackAsTool=false \
  --output out/linux-x64

out/linux-x64/byo --help
```

Test all five RIDs (cross-OS R2R will be disabled with a warning; that is fine
for a local smoke test — the real artifacts are built on matching runners in
CI):

```bash
for rid in win-x64 linux-x64 linux-arm64 osx-x64 osx-arm64; do
  dotnet publish src/SoftwareWorker.BYO.CLI/SoftwareWorker.BYO.CLI.csproj \
    -c Release -r "$rid" --self-contained true \
    -p:PublishSingleFile=true -p:PublishTrimmed=true -p:TrimMode=partial \
    -p:AssemblyName=byo -p:PackAsTool=false -p:Version=0.0.0-local \
    -o "out/$rid"
done
```

---

## 7. Releasing

### Standard release

```bash
git switch main && git pull
# 1. Update CHANGELOG.md (if present) and commit any final docs.
# 2. Tag and push.
git tag v1.0.0
git push origin v1.0.0
```

The push triggers `release.yml`, which:

1. Publishes self-contained binaries for all five RIDs in parallel.
2. Runs a `--help` smoke test on every same-arch runner.
3. Packages each binary into a `.zip` (Windows) or `.tar.gz` (Unix).
4. Generates per-RID SHA256 sums and uploads each artifact.
5. Aggregates all sums into a single `SHA256SUMS.txt`.
6. Copies `installers/install.sh` and `installers/install.ps1` into the release.
7. Creates the GitHub Release with auto-generated notes plus the template body.

### Manual / dry-run release

The `release.yml` workflow accepts a `workflow_dispatch` input for manual runs:

- Go to **Actions → Release → Run workflow**.
- Provide a version (e.g. `0.0.0-rc1`) — artifacts will be built and uploaded as
  workflow artifacts, but **no GitHub Release will be created** (the `release`
  job is gated on `refs/tags/v*`).
- Use this to validate end-to-end packaging before cutting a real tag.

### Dry-run the installers locally

```bash
# Bash installer against an ephemeral install dir
BYO_INSTALL_DIR="$(mktemp -d)" \
BYO_VERSION="1.0.0" \
bash -x installers/install.sh

# PowerShell installer against an ephemeral install dir
$env:BYO_INSTALL_DIR = New-Item -ItemType Directory -Path (Join-Path $env:TEMP "byo-dryrun-$([guid]::NewGuid())")
$env:BYO_VERSION     = '1.0.0'
.\installers\install.ps1
```

---

## 8. Installation (end-user)

### macOS / Linux

```bash
curl -fsSL https://github.com/softwareworkercom/byo/releases/latest/download/install.sh | bash
```

Environment overrides:

| Variable | Default | Purpose |
|---|---|---|
| `BYO_VERSION` | `latest` | Pin a version (`1.2.3` or `v1.2.3`). |
| `BYO_INSTALL_DIR` | `$HOME/.local/bin` | Override install location. |
| `BYO_RID` | auto-detected | Force a specific RID (e.g. `linux-arm64`). |

The installer:

1. Detects OS + architecture from `uname`.
2. Resolves the version (follows the `latest` redirect or uses the pinned tag).
3. Downloads the archive and `SHA256SUMS.txt`.
4. Verifies SHA256 **before** writing to disk.
5. Installs `byo` into `$HOME/.local/bin` with mode `0755`.
6. Prints a `PATH` hint if `$HOME/.local/bin` is not already on `PATH`.
7. Runs `byo --help` (or `--version`) as a verification step.

### Windows (PowerShell)

```powershell
iwr -useb https://github.com/softwareworkercom/byo/releases/latest/download/install.ps1 | iex
```

Environment overrides (set before piping):

| Variable | Default | Purpose |
|---|---|---|
| `BYO_VERSION` | `latest` | Pin a version. |
| `BYO_INSTALL_DIR` | `%LOCALAPPDATA%\Programs\byo` | Override install location. |

The installer downloads, verifies SHA256, expands the archive, updates the
**user** PATH (no admin rights needed), and runs `byo --help` to verify.

### winget (future)

Once releases stabilize, publish a manifest to `microsoft/winget-pkgs` under
`SoftwareWorker.Byo`. Until then, `install.ps1` is the recommended path.

### Upgrade

Re-run the installer — it overwrites the binary in place.

### Uninstall

**macOS / Linux**
```bash
rm -f "$HOME/.local/bin/byo"
# Optionally remove the PATH export line you added to your shell rc.
```

**Windows**
```powershell
Remove-Item "$env:LOCALAPPDATA\Programs\byo" -Recurse -Force
# Remove from user PATH via:
#   System Properties → Environment Variables → User PATH → Edit
```

---

## 9. Security

| Practice | Status |
|---|---|
| HTTPS-only downloads | ✅ All assets served via `github.com` over TLS. |
| SHA256 checksums | ✅ `SHA256SUMS.txt` in every release; verified by both installers before any file is written to disk. |
| Least-privilege workflows | ✅ `ci.yml` is `contents: read`; only the `release` job in `release.yml` is `contents: write`. |
| Reproducible builds | ✅ `Deterministic=true`, `ContinuousIntegrationBuild=true`, `EmbedUntrackedSources=true`. |
| Code signing (Authenticode / cosign) | 🔜 Recommended next step. Use [Sigstore cosign](https://docs.sigstore.dev/) keyless OIDC signing: `cosign sign-blob --yes <file>`; attach `.sig` and certificate to the release. Authenticode signing requires a code-signing certificate (cost). |
| macOS notarization | 🔜 Requires an Apple Developer account. Until then, document `xattr -d com.apple.quarantine $(which byo)` for users who hit Gatekeeper. |
| SBOM | 🔜 Add `dotnet CycloneDX` or GitHub's SBOM export and attach to the release. |
| Pin Actions by SHA | 🔜 Replace `@v4`/`@v2` references with full commit SHAs for supply-chain hygiene. |

**Why no signing today?** Signing requires either a paid Authenticode cert
(Windows) or an Apple Developer account (macOS). Checksums + HTTPS gives a
strong baseline; signing is a planned upgrade once user count justifies the
operational overhead.

---

## 10. CI Recommendations

- **Build matrix**: every PR builds and tests on Linux, Windows, and macOS (`ci.yml`).
- **Release matrix**: five RIDs build in parallel; failures in one RID do not
  block the others (`fail-fast: false`).
- **Smoke tests**: each same-arch runner invokes `byo --help` after publish.
- **Artifact validation**: the aggregating `release` job recomputes the merged
  `SHA256SUMS.txt` from all per-RID files. Any mismatch fails the release.
- **Branch protection**: require `ci.yml` to be green on `main`. Only tag from `main`.

---

## 11. Rollback & Hotfix

### Rollback (user-side)

```bash
BYO_VERSION=1.0.0 curl -fsSL https://github.com/softwareworkercom/byo/releases/download/v1.0.0/install.sh | bash
```

### Rollback (maintainer-side)

1. Mark the bad release as **pre-release** so the `latest` pointer falls back to
   the previous stable version, **or** delete it outright:
   ```bash
   gh release delete v1.4.0 --yes
   git push origin :refs/tags/v1.4.0
   ```
2. Communicate the rollback in the next release's notes.

### Hotfix flow

1. Branch from the bad tag:
   ```bash
   git checkout -b hotfix/1.4.1 v1.4.0
   ```
2. Commit the fix and a regression test.
3. Tag and push:
   ```bash
   git tag v1.4.1
   git push origin v1.4.1
   ```
4. Forward-merge `hotfix/1.4.1` back into `main` so the fix is not lost.

---

## 12. Tradeoffs

### Self-contained vs framework-dependent

| | Self-contained ✅ chosen | Framework-dependent |
|---|---|---|
| User needs runtime | No | Yes (.NET 10) |
| Binary size | 30–80 MB | 1–5 MB |
| Runtime patching | Re-release required | OS picks it up |
| CLI fit | Great — zero prereqs | Good only when you control the environment |

### Single-file vs multi-file

- **Single-file** (chosen): one file on `PATH`, trivial installer logic.
  Compression cuts ~30–50% size at a small first-run cost.
- **Multi-file**: smaller updates, but installation/uninstallation gets messy.

### Trimming

- `partial` (chosen): trims framework assemblies; safe with reflection-heavy
  libraries like Spectre.Console, System.CommandLine, Refit, and reflection-based
  System.Text.Json.
- `full`: smaller binaries, but every reflective code path must be audited
  (`[DynamicDependency]`, `TrimmerRootAssembly`, source-generated JSON contexts,
  etc.). Not worth the risk for a single-maintainer project.

### Native AOT for CLIs

| | Pros | Cons |
|---|---|---|
| AOT | ~10–30 ms startup, no JIT, lower memory, no extraction | No `Reflection.Emit`, limited dynamic code, some dependencies (Refit/Spectre/JSON reflection) not yet AOT-safe, per-RID cross-compilation limited |

**Verdict**: revisit AOT once the dependency surface is audited and startup
becomes a measured bottleneck.

---

## 13. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `byo: command not found` after install | `$HOME/.local/bin` not on PATH | Add `export PATH="$HOME/.local/bin:$PATH"` to your shell rc. |
| macOS: *"cannot be opened because the developer cannot be verified"* | Gatekeeper quarantine (no notarization yet) | `xattr -d com.apple.quarantine $(which byo)` or right-click → Open once. |
| Linux: `cannot execute binary file: Exec format error` | Wrong RID downloaded | Re-run with `BYO_RID=linux-arm64 ...` (or `linux-x64`). |
| Windows: PATH change not visible | New env vars apply to new shells | Open a fresh PowerShell window. |
| Checksum mismatch | Partial download or proxy rewriting | Re-run the installer; if persistent, download manually and compare with `SHA256SUMS.txt`. |
| Slow first launch | Single-file extraction | Set `DOTNET_BUNDLE_EXTRACT_BASE_DIR` to a fast local disk. |
| Trim warning broke a feature | Reflection over a trimmed assembly | Keep `TrimMode=partial`. If it persists, add `[DynamicDependency]` or a `TrimmerRootAssembly`. |
| Release workflow failed on `osx-x64` | macOS-13 runner deprecation | Update the matrix `os:` for `osx-x64`, or drop the RID in favor of Apple Silicon + Rosetta. |

---

## 14. Best-Practice Checklist for Maintainers

- [ ] `main` is protected; `ci.yml` is a required check.
- [ ] Tags are signed (`git tag -s`) where possible.
- [ ] Every release has a `SHA256SUMS.txt`.
- [ ] Installers verify checksums **before** writing any file.
- [ ] `byo --version` prints version + commit + build date.
- [ ] Release notes link to install one-liners.
- [ ] CHANGELOG (if used) follows *Keep a Changelog*.
- [ ] Smoke test passes on every same-arch runner.
- [ ] Rollback procedure exercised at least once on a pre-release.
