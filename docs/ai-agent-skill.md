# AI agent skill

This skill helps an AI agent turn a user's request into the right BYO CLI setup with as little friction as possible.

It is aimed at adoption: taking a natural-language task such as "save this command", "automate this runbook", or "make this API callable by agents" and mapping it to the simplest BYO building block that fits.

## Where the skill lives

The reusable skill file is:

- `skills/byo-cli-adoption/SKILL.md`

Use it as grounding for any agent that should help users adopt BYO CLI.

## What the skill teaches the agent to do

- Prefer a saved command when one shell command is enough
- Prefer a workflow when the task has multiple steps or needs secret collection
- Prefer a plugin when the user needs a real `byo <group> <action>` capability
- Turn repeated values into settings and sensitive values into secrets
- End with concrete verification commands

## Fast path by scenario

### Save one command

Use this when the user already has a command that works and wants to stop retyping it.

Typical output from the agent should include:

```bash
byo commands set --name "Build solution" --bookmark "Development" --shell PowerShell --executable "dotnet build"
byo commands list
byo run --target command --name "Build solution" --bookmark "Development"
```

See also: [commands](commands.md), [run](run.md), and [Token replacement](token-replacement.md).

### Turn a runbook into a workflow

Use this when the task has several steps, or when the flow should prompt for a secret and then execute saved commands.

Typical output from the agent should include:

```bash
byo workflows set --name "Release smoke test" --bookmark "Operations"
byo workflows list
byo run --target workflow --name "Release smoke test" --bookmark "Operations"
```

The agent should also list the workflow steps to add in the order they should run.

See also: [workflows](workflows.md), [run](run.md), [settings](settings.md), and [secrets](secrets.md).

### Build a plugin for humans and agents

Use this when shelling out is not the right abstraction and the user wants a discoverable BYO command group.

Typical output from the agent should include:

```bash
dotnet new classlib -n BYO.Plugin.Example
dotnet add package BYO.SDK --version <match the installed BYO CLI>
byo settings set --key System:Plugins:Sources --value '["<local feed folder>"]'
byo plugins install --package BYO.Plugin.Example
```

See also: [Building a plugin](building-a-plugin.md) and [plugins](plugins.md).

## Recommended agent workflow

1. Identify the user outcome.
2. Decide whether the answer is a saved command, workflow, or plugin.
3. Move reused values into settings and secrets.
4. Produce ready-to-run `byo` commands.
5. Finish with a verification command.

## Why this helps adoption

BYO is most approachable when users do not have to learn every command group up front. This skill lets an agent start from the user's goal, pick the smallest fitting BYO abstraction, and produce a runnable path immediately.
