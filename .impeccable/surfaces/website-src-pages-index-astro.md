---
version: 1
slug: "website-src-pages-index-astro"
primary_target: "website/src/pages/index.astro"
related_targets: ["website/src/pages/docs/[...slug].astro", "website/src/pages/docs/start.astro"]
---

# byocli.com landing page (website/src/pages/index.astro)

## Scope

Persuade surface: the landing page of the static Astro site under `website/`, deployed to GitHub Pages at the root of www.byocli.com. The docs section (`website/src/pages/docs/[...slug].astro`, Read mode) renders `docs/*.md` in place and inherits this world without a seam.

## Audience and job

A developer who builds software and wants it usable from a terminal, by scripts and by AI agents, and who at their own terminal keeps re-deriving the same commands. They must understand in one viewport that BYO is an SDK for building CLIs that humans, automation and AI agents can use, with the `byo` CLI as the reference for what you get; within the page, that commands are saved by name, everything stays in `~/byo` with no account or backend, and a workflow is one invocation. The action is the install one-liner, copyable, for macOS/Linux and Windows, and every section's pointer into the documentation. The page is marketing only, by the user's instruction of 2026-10-06: no commands, code, transcripts or file lists; the detail lives in the docs. Proof is the README's own argument and the live GitHub repository; nothing else is citable. Constraints: pre-1.0, Linux and macOS under-tested, no metrics or testimonials, motion must never delay reading or copying.

## Direction contract

THESIS: byocli.com is an apple.com product page for a command-line tool: one idea per screen, each a large sentence-case headline with a short subhead, carried by the README's argument and pointing to the documentation for every detail. It refuses the dev-tool defaults: no dark hero with a neon accent, no code or commands on the marketing page, no row of three icon cards, no metaphor world.

OWN-WORLD: White ground with alternating #f5f5f7 bands and one black get-started band. Type is Inter Variable: display 600 at 56 to 80px with -0.03em tracking, subheads 400 at 21 to 28px in #6e6e73, body 17px/1.47 in #1d1d1f; every command in Geist Mono. One accent, the brand navy #203b58, carries buttons and links; the brand green appears only in the logo and as the prompt inside terminal sessions. Pill buttons, 28px tile radii, a frosted sticky nav bar, one soft offset shadow under the hero's window-shaped frame. Bento tiles of varied size carry one benefit each and a link into the docs. No illustration, no gradients in type or fills, no eyebrows, no icons where words do the job.

STORY: The visitor reads one sentence about building CLIs for the AI era, sees their software become a CLI that developers, automation and AI agents all use, reads why a CLI is what agents need and what the SDK takes care of, sees what the reference CLI gives them, and copies the install line or follows a link into the docs. The copy follows README.md section by section.

FIRST VIEWPORT: Frosted nav bar, 48px: logo mark and BYO CLI left; Docs, Build a plugin, GitHub and a small navy Install pill right. Centred headline "Build CLIs for the AI era." at 80px over a 28px subhead "Turn your software into tools that humans, automation, and AI agents can use.", then a navy pill Install and a text link to the Start page, where two first-run paths begin. Below, the flow figure: a hairline pill "Your software", a hairline down to a 640px dark window-shaped frame titled "Your CLI" that is a demo of the interactive shell (its real banner, a byo> prompt you can type into, three suggested commands as chips), filling the rest of the viewport; the hairline tree down to the three pills, Developers, Automation and AI agents, and the caption that says it is a demo follow on scroll.

Signature interaction: the demo shell answers what you type the way the CLI does: the help text, the list tables, the confirmations and the errors are the CLI's own strings, the prompt's > turns red after a failure, Tab completes, Up and Down walk the history and a grey inline suggestion is accepted with Right or End. Output lines appear with a short stagger, capped at half a second; the figure itself appears top to bottom on first paint, about 1.3s in total; `prefers-reduced-motion` shows everything at once. Buttons and tiles respond with Apple's restraint: a slight darkening on hover, a 3px halo on focus. No scroll-driven effects.

Cross-surface reach: the docs take the Apple Developer documentation register: a 240px left rail of plain text links with the current page in navy, white content column at 72ch, light grey code blocks with dark text, hairline tables, the same type and accent.

Honest risk: Apple's register is familiar and this page will sit near many developer-tool sites built from the same vocabulary; the craft bar (Apple, Raycast, Warp, Linear, Vercel) is what distinguishes it, so spacing, type and the product shot must be exact.

FORM: The category standard, executed at Apple grade. Pinned by the user on 2026-10-06 after rejecting the Shadow Board (seed 7893a1c4); references Apple, Raycast, Warp, Linear and Vercel. No roll: a user-pinned direction beats the roll. Code-led, no image generation in this harness.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance.

## Resolved

- Docs are read in place from `../docs` by the content layer with the `unified` processor from `@astrojs/markdown-remark` and `rehype-docs-links.mjs`; syntax highlighting is off so code blocks stay one colour.
- Deployment: `.github/workflows/pages.yml` builds `website/` and deploys `dist/` with the `www.byocli.com` CNAME at the root path. Untested until GitHub Pages is enabled on the repository with the "GitHub Actions" source.
- Copy (2026-10-06): the landing page follows README.md in order: the hero, "The CLI is becoming an interface for AI.", "Build the capability, not the plumbing.", "One CLI. Multiple interfaces.", "What's in the box." (the README's five bullets as tiles, each linking to its docs page), "Build once. Let humans and AI use it." and "Get started in seconds.", with the tester call for Linux and macOS in the install notes. Section subheads stay at three lines or fewer on desktop.
- Argument (2026-10-06): "Cheaper in tokens, too." added under the CLI qualities at the user's request, after checking Anthropic's engineering post "Code execution with MCP" (4 November 2025) and independent CLI-versus-MCP comparisons. Stated by mechanism (on-demand discovery, output filtered before it reaches the model, versus tool definitions loaded up front and every result passing through the context), with no figures, since benchmark numbers vary by setup.
- Onboarding (2026-10-06): a Start page at /docs/start/ (Read mode) carries two first-run tracks chosen with the user: "Automate a workflow" (about 15 minutes, the getting started guide's commands, ending with the two-call workflow running by name) and "Build a plugin for agents" (about 20 minutes, the plugin guide's quick start, ending with an AI agent with shell access discovering and running the new command). Each step shows one real command and a checkpoint quoting the CLI's own message. The hero link and the install band point at the paths in words; the docs index leads with the page. The plugin guide's handler gained a stand-in data method so the quick start compiles as pasted.
- Delight (2026-10-06): at the user's request the hero frame became a typeable demo of the interactive shell, the one place on the marketing page where commands appear. The demo prints only what the CLI prints (captured from the CLI itself) and runs only echo commands faithfully; anything else it cannot do honestly is a grey "(demo)" line. A caption under the figure says it is a demo and that nothing runs on the visitor's machine.
- Scope (2026-10-06, later the same day): the user ruled the landing page marketing only. The terminal sessions, the handler code, the command chips, the grammar chip and the `~/byo` file list were removed; the `Window` component was deleted. The install one-liner stays as the call to action. Technical detail is in the docs, which every section links to.
