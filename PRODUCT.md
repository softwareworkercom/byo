# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Stack

Astro, deployed as static output to GitHub Pages from this repository. Confirmed by the user. GitHub Pages means static only: no server-side rendering, no API routes, no server-held secrets. Existing `docs/*.md` is the documentation source and should be reused rather than re-authored.

The product being marketed is a terminal CLI (.NET 10, `byo`); this repository's design surface is the marketing + documentation site that sits in front of it, not the CLI's own terminal UX.

## Users

**Primary: the solo developer, on their own machine.** Someone who works in a terminal all day and keeps re-deriving the same commands — digging through shell history, notes, or Slack to recover the flags and scripts that worked last time. The job is recall and repetition: save the exact command that worked, run it again without thinking.

**Secondary, real but not the entry point: the team lead standardizing a team.** Someone authoring workflows other people run, to cut onboarding friction and make setup, deployment, and debugging steps reproducible across a team. Confirmed as a genuine audience, explicitly ranked behind the solo developer.

**Third audience, adjacent: plugin and SDK authors.** Developers extending BYO by publishing NuGet packages against `BYO.SDK`. Not a primary design target, but the plugin system is a real product capability the site must represent.

## Product Purpose

BYO CLI turns ad-hoc terminal habits into saved, runnable, shareable automation. It lets a developer save shell commands by name, group them into multi-step workflows, keep configuration as settings, and collect sensitive values into a locally encrypted secrets store — then run any of it by name, on a schedule, or in the background.

Success is a developer who stops looking things up: the command they need is already saved, and the workflow they used to run by hand is one invocation.

The site's job is to make that shift legible to someone who has never heard of BYO, and to get them from landing to installed to first saved command without leaving the page or the terminal.

## Positioning

The mechanism a neighbouring product could not truthfully copy: **everything stays local and inspectable.** Commands, settings, workflows, and secrets live as plain JSON in `~/byo` (`%USERPROFILE%\byo` on Windows), with secrets encrypted at rest. There is no account, no sync service, no hosted backend. This is a deliberate position, not a missing feature — it is why secrets can be collected by a workflow step at all.

The second differentiator: **workflows compose saved commands, and the whole thing extends through NuGet plugins.** A workflow step can display a message, prompt for a secret, or execute a saved command; token replacement (`{{Key}}`) resolves settings and secrets at run time. Plugins published as `BYO.Plugin.*` packages add new command groups to the CLI itself.

The CLI is free, MIT-licensed, and open source. There is no paid tier, no pricing page, and nothing to sell — the site persuades toward installation and a GitHub star, not a purchase.

## Operating Context

- **Install** is a single piped command: `curl ... install.sh | bash` on macOS/Linux, `iwr ... install.ps1 | iex` on Windows. The installers detect OS/architecture, download the matching binary, verify a SHA256 checksum, and add it to PATH. Installation is also possible as a .NET tool (`ToolCommandName: byo`).
- **Everything follows one grammar:** `byo <group> <action> [options]`. The six groups are `run`, `commands`, `settings`, `secrets`, `workflows`, and `plugins`.
- **Running `byo` with no arguments opens an interactive shell** with autocompletion, syntax highlighting, inline history suggestions, and a prompt whose `>` turns red when the last command failed. It only starts in a real TTY; redirected input falls back to usage output.
- **Data lives in `~/byo`** as `commands.json`, `settings.json`, `workflows.json`, `secrets.json`, and `shell_history.txt` (lines that look sensitive are not written to history).
- The audience is in a terminal when they arrive and will be in a terminal thirty seconds later. The site is read in a browser but every outcome it drives happens in a shell.

## Capabilities and Constraints

**Confirmed capabilities**

- Save, list, and delete named shell commands, organized by a `--bookmark` hierarchy, with a target shell (e.g. PowerShell) per command.
- Multi-step workflows built from step types including Message, Input As Secret, and Execute Command; steps run in sequence and can run asynchronously.
- Settings as non-sensitive key/value pairs; secrets encrypted at rest. Secrets can be set directly via the `secrets` group (`set`, `list`, `delete`, `reencrypt`) or collected during a run by a workflow `InputAsSecret` step. Verified against `byo --help` on 2026-10-03; earlier drafts of this brief wrongly claimed there was no `secrets` group.
- Token replacement resolves `{{Key}}` against settings and secrets when a command runs.
- `--schedule` to repeat a command on a schedule and `--async` to run it in the background, available on executable commands.
- Interactive selection: omitting `--name` on `byo run` lets the user pick from the bookmark hierarchy.
- Plugin discovery, install, and uninstall against NuGet.org, filtered to `BYO.Plugin.*` packages owned by `softwareworkercom`, with a `--source` option for a local `.nupkg` folder.
- `BYO.SDK` for plugin authors, including `ExportService` for writing `DataTable` results to a timestamped JSON export. `BYO.Integrations` ships API connectors (Jira, Confluence, GitHub, Azure DevOps, Google Calendar, Microsoft Graph, Stripe, Telegram, Toggl, Auth0, Bitwarden, HashiCorp Vault, Raindrop, AgentMail, Turso) for SDK consumers.

**Constraints**

- The site must be static. GitHub Pages hosting rules out anything requiring a server at request time.
- Requires .NET 10 to build; the shipped binaries are self-contained per platform.
- Version is **0.38.0** — pre-1.0. The site must not imply a stable 1.0 release or long production track record.
- **Linux and macOS are explicitly under-tested.** The README currently opens with a call for testers on those platforms. Any cross-platform claim on the site must stay honest about this.
- Terminology to use exactly as the product does: *commands*, *workflows*, *settings*, *secrets*, *bookmarks*, *plugins*, *steps*, *token replacement*, *interactive shell*.

**Open / undecided**

- Whether the site replaces, mirrors, or redirects whatever currently answers at `www.byocli.com` (referenced as `PackageProjectUrl`). The user confirmed there is no live site to preserve.
- Whether `docs/*.md` is consumed in place by the Astro build or copied into a site content directory.

## Brand Commitments

- **Name:** BYO CLI. The binary and the command are `byo` (lowercase).
- **Company:** Software Worker (`softwareworkercom` on GitHub and NuGet). Author: Leandro Monaco.
- **Domain:** `www.byocli.com`. No live site exists yet, so nothing visual carries over.
- **Existing assets:** [docs/Images/byo-logo.png](docs/Images/byo-logo.png) and [docs/Images/byo-icon.png](docs/Images/byo-icon.png). These are the only committed brand marks; the icon already ships as the NuGet package icon across all three projects.
- **Voice, as written today:** direct, second-person, benefit-first, and short. "Never lose a command again." "Secrets stay local." Developer-to-developer, not marketing-to-buyer. No visual direction has been established or constrained — that is deliberately left to later design work.

## Evidence on Hand

**Real and citable**

- Live GitHub repository activity at `github.com/softwareworkercom/byo` — stars, issues, and discussions. The README already surfaces these as shields. Counts must be read live or linked, never hardcoded as a claim.
- The repository itself: MIT license, CI/publish/release workflows, 15 shipped SDK integrations, plugin submodules for Jira, Confluence, Google Calendar, MS Teams, SoftwareWorker, and a tester plugin.
- The documentation in [docs/](docs/), including a complete [getting-started walkthrough](docs/getting-started.md) against the public data.gov.au CKAN API — a real, runnable example, not an invented one.

**Absent — must not be fabricated**

- No testimonials, quotes, named users, or customer logos.
- No usage metrics, download counts, install counts, or time-saved figures.
- No case studies, press coverage, awards, or endorsements.
- No pricing, licensing tiers, enterprise plans, SLAs, or support commitments.
- No team, headcount, or funding story.
- No security certification or audit claim. "Encrypted at rest, stored locally" is accurate; anything stronger is not.

Published-plugin availability on NuGet.org was not confirmed by the user. Verify the packages are live before presenting them as installable proof.

## Product Principles

1. **The terminal is the destination, not the site.** Every page earns its keep by shortening the path to an installed binary and a first saved command. A visitor who reads and does not install was not served.
2. **Local-first is the headline, not the footnote.** No account, no sync, no backend is the product's position. State it early and plainly rather than burying it under feature lists.
3. **Show the real command.** This product's proof is its own syntax. Concrete, copyable, runnable commands outrank abstract benefit copy — and the examples must be ones that actually work.
4. **Honest about v0.** Pre-1.0, under-tested on Linux and macOS, actively asking for testers. Confidence in the idea, candour about the maturity. Overstating it would be the fastest way to lose this audience.
5. **Marketing and docs are one continuous surface.** The visitor crosses from "why" to "how" without a seam, a second design language, or a re-orientation.

## Accessibility & Inclusion

No product-specific accessibility requirement or standard was established by the user. Standard web accessibility practice applies to the site; record a binding standard here if one is later chosen.
