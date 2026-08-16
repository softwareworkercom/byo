# workflows command group

Manage multi-step automation workflows.

## Table of Contents

- [workflows set](#workflows-set)
  - [Syntax](#syntax)
  - [Options](#options)
  - [Behavior](#behavior)
  - [Workflow step types (detailed)](#workflow-step-types-detailed)
    - [1) Message](#1-message)
    - [2) YesNoQuestion](#2-yesnoquestion)
    - [3) InputAsSetting](#3-inputassetting)
    - [4) InputAsSecret](#4-inputassecret)
    - [5) ExecuteCommand](#5-executecommand)
  - [Example step sequence](#example-step-sequence)
  - [Step design tips](#step-design-tips)
- [workflows list](#workflows-list)
  - [Syntax](#syntax-1)
- [workflows delete](#workflows-delete)
  - [Syntax](#syntax-2)
  - [Behavior](#behavior-1)

## workflows set

Create or update a workflow with interactive step entry.

### Syntax

```bash
byo workflows set --name <name> --bookmark <path>
```

### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--name` | Yes | Workflow name |
| `--bookmark` | Yes | Bookmark hierarchy path |

### Behavior

Supports these step types:

1. Message
2. YesNoQuestion
3. InputAsSetting
4. InputAsSecret
5. ExecuteCommand

Steps execute in the order created.

### Workflow step types (detailed)

When you run `byo workflows set`, BYO repeatedly asks you to choose a step type and configure it.

---

#### 1) Message

Use this step to display status updates, guidance, or checkpoints.

**Interactive prompts:**

- `Enter message to display`
- `Select message color`
- `Wait for user to press Enter after displaying message?`

**Fields captured:**

- `Prompt` (the text to show)
- `Color` (for terminal output)
- `WaitForEnter` (`Yes`/`No`)

**Good use cases:**

- Start/end banners (for example, "Starting deployment")
- Human-readable progress markers
- Manual pause points before sensitive actions

---

#### 2) YesNoQuestion

Use this step for confirmation gates.

**Interactive prompts:**

- `Enter question to ask`
- `Interrupt workflow if answer is No?`

**Fields captured:**

- `Prompt` (question text)
- `InterruptOnNo` (`Yes`/`No`)

**Behavior notes:**

- If `InterruptOnNo` is enabled and the answer is `No`, the workflow stops.
- If disabled, the workflow continues regardless of answer.

**Good use cases:**

- "Proceed with production deployment?"
- "Did backup complete successfully?"

---

#### 3) InputAsSetting

Use this step to collect runtime input and store it as a setting.

**Interactive prompts:**

- `Enter prompt message`
- `Enter setting key name`

**Fields captured:**

- `Prompt` (what to ask)
- `StorageKey` (setting key to save value into)

**Behavior notes:**

- Value is stored as a regular (non-secret) setting.
- The saved key can be referenced later with token syntax (for example `{{MyKey}}`).

**Good use cases:**

- Branch name
- Environment name (`dev`, `staging`, `prod`)
- Non-sensitive URLs or IDs

---

#### 4) InputAsSecret

Use this step to collect sensitive input and store it as a secret.

**Interactive prompts:**

- `Enter prompt message`
- `Enter secret key name`

**Fields captured:**

- `Prompt` (what to ask)
- `StorageKey` (secret key to save value into)

**Behavior notes:**

- Value is stored via the secrets system (encrypted at rest).
- The saved key can be referenced later with token syntax.

**Good use cases:**

- Temporary API tokens
- One-time passwords
- Per-run credentials

---

#### 5) ExecuteCommand

Use this step to run a command — either an existing saved command from the
command library, or a one-off custom command entered inline that is not saved
with `byo commands set`.

**Interactive prompts:**

- `Select command` — the list always shows `Custom command (not saved)` at the top, followed by every saved command.
- If `Custom command (not saved)` is chosen: `Enter command executable`, `Enter working directory`, and optionally `Select shell type`
- `Run this command asynchronously in background?`

**Fields captured:**

- `CommandName` (selected saved command; empty when using a custom command)
- `CommandExecutable`, `CommandDirectory`, `CommandShell` (used only for custom, unsaved commands)
- `RunAsync` (`Yes`/`No`)

**Behavior notes:**

- Command execution supports token replacement the same way as `byo run --target command`.

**Good use cases:**

- Build, test, package, deploy sequences
- API smoke checks
- Cleanup and verification commands

---

### Example step sequence

Typical production workflow structure:

1. `Message` — announce start
2. `YesNoQuestion` — confirm continuation
3. `InputAsSetting` — collect environment/branch
4. `InputAsSecret` — collect temporary token
5. `ExecuteCommand` — run build
6. `ExecuteCommand` — run deploy
7. `Message` — announce completion

### Step design tips

- Keep each step focused on one action.
- Add `Message` steps before and after risky operations.
- Use `YesNoQuestion` before irreversible changes.
- Prefer `InputAsSecret` for anything credential-like.
- Reuse saved commands in `ExecuteCommand` steps to keep workflows maintainable.

## workflows list

List workflows.

### Syntax

```bash
byo workflows list
```

## workflows delete

Delete a workflow.

### Syntax

```bash
byo workflows delete
```

### Behavior

- Uses interactive selection
- Confirms before delete
