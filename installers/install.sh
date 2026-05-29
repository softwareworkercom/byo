#!/usr/bin/env bash
#
# byo CLI installer (macOS / Linux)
#
# Usage:
#   curl -fsSL https://github.com/softwareworkercom/byo/releases/latest/download/install.sh | bash
#
# Environment variables:
#   BYO_VERSION       Specific version to install (e.g. "1.2.3"). Defaults to latest.
#   BYO_INSTALL_DIR   Install directory. Defaults to "$HOME/.local/bin".
#   BYO_RID           Override the detected runtime identifier (e.g. linux-arm64).
#
set -euo pipefail

REPO="softwareworkercom/byo"
BIN_NAME="byo"
INSTALL_DIR="${BYO_INSTALL_DIR:-${HOME}/.local/bin}"
REQUESTED_VERSION="${BYO_VERSION:-latest}"

log()  { printf '==> %s\n'   "$*"; }
warn() { printf 'warn: %s\n' "$*" >&2; }
err()  { printf 'error: %s\n' "$*" >&2; exit 1; }

require() {
  command -v "$1" >/dev/null 2>&1 || err "'$1' is required but not installed"
}

sha256_check() {
  local file="$1" expected="$2"
  local actual
  if command -v sha256sum >/dev/null 2>&1; then
    actual=$(sha256sum "$file" | awk '{print $1}')
  elif command -v shasum >/dev/null 2>&1; then
    actual=$(shasum -a 256 "$file" | awk '{print $1}')
  else
    err "Neither sha256sum nor shasum is available"
  fi
  if [[ "${actual}" != "${expected}" ]]; then
    err "Checksum mismatch for ${file}: expected ${expected}, got ${actual}"
  fi
}

detect_rid() {
  if [[ -n "${BYO_RID:-}" ]]; then
    echo "${BYO_RID}"
    return
  fi
  local os arch
  case "$(uname -s)" in
    Linux)  os="linux" ;;
    Darwin) os="osx"   ;;
    *) err "Unsupported OS: $(uname -s). Use the Windows installer (install.ps1) instead." ;;
  esac
  case "$(uname -m)" in
    x86_64|amd64)   arch="x64"   ;;
    arm64|aarch64)  arch="arm64" ;;
    *) err "Unsupported architecture: $(uname -m)" ;;
  esac
  echo "${os}-${arch}"
}

resolve_version() {
  if [[ "${REQUESTED_VERSION}" != "latest" ]]; then
    echo "${REQUESTED_VERSION#v}"
    return
  fi
  # Follow the 'latest' redirect to discover the tag name without needing the GitHub API.
  local effective_url
  effective_url=$(curl -fsSLI -o /dev/null -w '%{url_effective}' \
    "https://github.com/${REPO}/releases/latest")
  echo "${effective_url##*/tag/v}"
}

add_to_path_hint() {
  case ":${PATH}:" in
    *":${INSTALL_DIR}:"*) return ;;
  esac
  warn "${INSTALL_DIR} is not on your PATH."
  echo "    Add this to your shell profile (e.g. ~/.bashrc, ~/.zshrc):"
  echo "        export PATH=\"${INSTALL_DIR}:\$PATH\""
}

main() {
  require curl
  require tar
  require uname

  local rid version asset url base_url tmp
  rid=$(detect_rid)
  version=$(resolve_version)
  asset="byo-${version}-${rid}.tar.gz"
  base_url="https://github.com/${REPO}/releases/download/v${version}"
  url="${base_url}/${asset}"

  log "Platform : ${rid}"
  log "Version  : ${version}"
  log "Source   : ${url}"
  log "Target   : ${INSTALL_DIR}"

  tmp=$(mktemp -d)
  trap 'rm -rf "${tmp}"' EXIT

  log "Downloading archive"
  curl -fsSL --retry 3 --retry-delay 2 -o "${tmp}/${asset}"        "${url}"
  log "Downloading checksums"
  curl -fsSL --retry 3 --retry-delay 2 -o "${tmp}/SHA256SUMS.txt"  "${base_url}/SHA256SUMS.txt"

  log "Verifying checksum"
  local expected
  expected=$(grep -E "[[:space:]]\\*?${asset}\$" "${tmp}/SHA256SUMS.txt" | awk '{print $1}' | head -n1)
  [[ -n "${expected}" ]] || err "Could not find checksum for ${asset} in SHA256SUMS.txt"
  sha256_check "${tmp}/${asset}" "${expected}"

  log "Extracting"
  tar -xzf "${tmp}/${asset}" -C "${tmp}"

  log "Installing to ${INSTALL_DIR}"
  mkdir -p "${INSTALL_DIR}"
  install -m 0755 "${tmp}/${BIN_NAME}" "${INSTALL_DIR}/${BIN_NAME}"

  add_to_path_hint

  log "Verifying installation"
  if ! "${INSTALL_DIR}/${BIN_NAME}" --help >/dev/null 2>&1; then
    "${INSTALL_DIR}/${BIN_NAME}" --version >/dev/null 2>&1 || \
      err "Installation verification failed: ${INSTALL_DIR}/${BIN_NAME} did not run successfully"
  fi

  log "Done. byo ${version} installed at ${INSTALL_DIR}/${BIN_NAME}"
}

main "$@"
