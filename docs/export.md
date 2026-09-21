# CLI Storage

BYO CLI stores all your data in JSON format locally in your home directory.

## Table of Contents

- [Storage Location](#storage-location)
- [File Structure](#file-structure)
- [File Formats](#file-formats)
  - [Secrets](#secrets)
  - [Commands](#commands)
  - [Settings](#settings)
  - [Workflows](#workflows)

## Storage Location

BYO CLI stores all data in your local home directory under `byo`:

**Windows**

```powershell
$env:USERPROFILE\byo
# Typically: C:\Users\<YourUsername>\byo
```

**macOS / Linux**

```bash
~/byo
# Typically: /home/<YourUsername>/byo
```


## File Structure

The `byo` folder contains the following JSON files:

```
~/byo/
??? commands.json      # All saved shell commands
??? settings.json      # All saved settings (non-sensitive key-value pairs)
??? workflows.json     # All saved multi-step workflows
??? secrets.json       # Encrypted secrets (encrypted at rest)
```

## File Formats

### Secrets

**File:** `secrets.json`

**Format:**

Secrets are stored in encrypted JSON format. The file contains key-value pairs where values are encrypted using hybrid encryption:

```json
{
  "Demo:DataGovApiKey": "encrypted-base64-value",
  "Database:Password": "encrypted-base64-value"
}
```

**Encryption Details:**

For small values (? 446 bytes):
- **Algorithm:** RSA-OAEP with SHA-256
- **Key Size:** 4096-bit RSA key
- **Padding:** OAEP with SHA-256

For larger values (> 446 bytes):
- **Hybrid Encryption:** AES-GCM + RSA-OAEP
- **AES:** 256-bit key, GCM mode with 96-bit nonce
- **RSA:** 4096-bit key (encrypts the AES key)
- **Authentication:** 128-bit authentication tag

**Key Management:**

- The RSA private key is generated on first use and stored in the operating system credentials store for maximum security
- The key is bound to the machine using hardware identifiers
- Key derivation uses PBKDF2 with SHA-256 (600,000 iterations)
- Each encryption operation uses a random salt for additional security

**Security Notes:**

- Secrets are encrypted at rest using a machine-bound RSA key
- The `byo secrets list` command automatically decrypts and displays all secrets using the stored machine key
- Secrets are decrypted when referenced during workflow execution via token replacement
- The encryption key is machine-bound and cannot be transferred to another machine
- Access to secrets requires access to the local machine where the key is stored
- To inspect the stored key, open the operating system credentials store and look for `byo-key`:
  - **Windows:** Open **Credential Manager** from Start, then check **Windows Credentials** and **Generic Credentials**.
  - **macOS:** Open **Keychain Access** from Applications > Utilities, then search for `byo-key`.
  - **Linux:** Open your desktop keyring or secret service manager, then search for `byo-key`.

### Commands

**File:** `commands.json`

**Format:**

```json
[
  {
    "name": "Build Solution",
    "executable": "dotnet build",
    "directory": "C:\\Projects\\MySolution",
    "shell": "PowerShell",
    "bookmark": "DevOps/Build",
    "createdAt": "2025-01-15T10:30:00Z"
  },
  {
    "name": "API Health Check",
    "executable": "curl.exe -H 'Authorization: Bearer {{ApiToken}}' '{{ApiBaseUrl}}/health'",
    "directory": null,
    "shell": "PowerShell",
    "bookmark": "Monitoring/API",
    "createdAt": "2025-01-15T11:00:00Z"
  }
]
```

**Key Fields:**

- `name` (string): Display name of the saved command
- `executable` (string): The shell command to execute (may include `{{tokenName}}` placeholders)
- `directory` (string|null): Working directory for command execution
- `shell` (string): Shell type (`PowerShell`, `Cmd`, or `Wsl`)
- `bookmark` (string|null): Hierarchical folder path (e.g., `DevOps/Deploy`)
- `createdAt` (ISO 8601 datetime): When the command was created

### Settings

**File:** `settings.json`

**Format:**

```json
{
  "Database:Host": "localhost",
  "Database:Port": "5432",
  "Demo:DataGovApiBaseUrl": "https://www.data.gov.au/data/api/3/action",
  "Environment": "development",
  "ApiBaseUrl": "https://api.example.com"
}
```

**Notes:**

- Settings are stored as a flat key-value dictionary
- Keys use dot notation for logical grouping (e.g., `Database:Host`, `Database:Port`)
- Values are strings and unencrypted
- Use hierarchical naming conventions for organization

### Workflows

**File:** `workflows.json`

**Format:**

```json
[
  {
    "name": "Deploy to Production",
    "bookmark": "DevOps/Deployment",
    "steps": [
      {
        "stepType": "Message",
        "prompt": "Starting production deployment",
        "color": "Cyan",
        "waitForEnter": false
      },
      {
        "stepType": "YesNoQuestion",
        "prompt": "Proceed with production deployment?",
        "interruptOnNo": true
      },
      {
        "stepType": "InputAsSetting",
        "prompt": "Enter deployment version",
        "storageKey": "Deploy:Version"
      },
      {
        "stepType": "ExecuteCommand",
        "commandName": "Build Solution",
        "commandExecutable": null,
        "commandDirectory": null,
        "commandShell": null,
        "runAsync": false
      }
    ],
    "createdAt": "2025-01-15T14:30:00Z"
  }
]
```

**Step Types:**

- `Message`: Display text with optional color and pause
- `YesNoQuestion`: Ask for confirmation; optionally interrupt on "No"
- `InputAsSetting`: Prompt for input and save as a setting
- `InputAsSecret`: Prompt for input and save securely (encrypted)
- `ExecuteCommand`: Run a saved or inline command
