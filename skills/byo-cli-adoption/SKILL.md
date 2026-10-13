# BYO CLI Adoption Skill

Use this skill when a user wants help adopting BYO CLI, turning repeated terminal work into saved commands or workflows, or deciding whether a capability belongs in a BYO plugin.

## Goal

Help the user get from a natural-language task to a working BYO setup quickly, with the fewest moving parts that still fit the job.

## What BYO is for

- Save repeatable shell commands with `byo commands set`
- Store reusable configuration with `byo settings set`
- Store sensitive values with `byo secrets set` or workflow secret input steps
- Orchestrate multi-step automation with `byo workflows set`
- Run saved commands or workflows with `byo run`
- Extend the CLI with NuGet plugins built against `BYO.SDK`

## Agent behavior

1. Start from the user's outcome, not from BYO features.
2. Prefer the simplest BYO building block that solves the problem:
   - one repeatable shell command -> saved command
   - multiple repeatable steps -> workflow
   - new executable capability with its own command surface -> plugin
3. Ask only the questions needed to produce correct commands:
   - what task should become reusable
   - whether the task is one command or many steps
   - which shell should run the saved command (`PowerShell`, `Cmd`, or `Wsl`)
   - which values should become settings or secrets
   - whether the user wants an interactive workflow or a plugin
4. Produce concrete `byo` commands the user can run immediately.
5. End with verification steps such as `byo commands list`, `byo workflows list`, `byo plugins list`, or a `byo run` command.

## Decision guide

### Use a saved command when

- The user already has a shell command that works
- The main problem is remembering flags, paths, or arguments
- Token replacement with `{{Key}}` is enough to parameterize it

Typical pattern:

```bash
byo commands set --name "<name>" --bookmark "<bookmark>" --shell PowerShell --executable "<command with {{Tokens}}>"
```

### Use a workflow when

- The task has multiple steps
- The user needs messages, confirmation, or secret collection
- Several saved commands must run in sequence

Typical flow:

1. Save any repeatable shell commands first.
2. Create the workflow with `byo workflows set`.
3. Add steps in the order the task should run.
4. Run it with `byo run --target workflow`.

### Use a plugin when

- The user wants a new BYO command group
- The capability should be discoverable in `byo --help`
- Shelling out is not the right abstraction
- The logic belongs in .NET code and should use SDK services

Typical flow:

1. Create a class library whose package id starts with `BYO.Plugin.`
2. Add `BYO.SDK` matching the installed CLI version.
3. Implement a handler with `[TrunkCommand]` and optional `[BranchCommand]` or `[LeafCommand]`.
4. Pack the plugin.
5. Configure a local package source if needed.
6. Install it with `byo plugins install`.
7. Validate with `byo --help` and a real command invocation.

## Recommended adoption playbook

### Track 1: save one command

Use when the user says things like:

- "I keep re-running the same command"
- "I always forget the right curl or dotnet flags"
- "Turn this shell snippet into something reusable"

Agent output should usually include:

1. One or more `byo settings set` commands for stable non-secret values
2. Optional `byo secrets set` commands for sensitive values
3. A `byo commands set` command
4. `byo commands list`
5. `byo run --target command --name "..."`

### Track 2: turn a manual procedure into a workflow

Use when the user says things like:

- "Automate this runbook"
- "I have to do these five steps every release"
- "Prompt for a secret and then run the checks"

Agent output should usually include:

1. Saved commands for the shell-execution parts
2. A `byo workflows set` command
3. The exact workflow steps to add
4. A `byo workflows list` check
5. A `byo run --target workflow --name "..."` command

### Track 3: build a plugin for agents and humans

Use when the user says things like:

- "I want `byo <group> <action>` for this"
- "Expose this API as a real BYO command"
- "Make this discoverable for AI agents in `byo --help`"

Agent output should usually include:

1. `dotnet new classlib`
2. `dotnet add package BYO.SDK --version <match the CLI>`
3. A handler outline
4. `dotnet pack`
5. `byo settings set --key System:Plugins:Sources --value '...'`
6. `byo plugins install --package BYO.Plugin.<Name>`
7. A final `byo <group> <action>` example

## Output rules for the agent

- Prefer ready-to-run commands over abstract advice.
- Keep examples consistent with BYO docs and command names.
- When secrets are involved, keep them out of saved command text and place them in BYO secrets or workflow secret-input steps.
- When values are reused across commands, move them into settings and reference them as `{{Key}}`.
- If the user already has working shell commands, do not jump straight to a plugin.
- If a plugin is the right answer, mention that handlers appear in `byo --help`, the interactive shell, scripts, and CI.

## Validation checklist

Before finishing, make sure the proposed solution includes:

- at least one concrete command the user can copy
- the right BYO abstraction for the task
- a verification command
- a short explanation of why this approach is the simplest fit

## Source material to consult

- `README.md`
- `docs/getting-started.md`
- `docs/commands.md`
- `docs/workflows.md`
- `docs/settings.md`
- `docs/secrets.md`
- `docs/run.md`
- `docs/plugins.md`
- `docs/building-a-plugin.md`
