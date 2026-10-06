# byocli.com

The marketing and documentation site for BYO CLI, built with [Astro](https://astro.build) and deployed to GitHub Pages at `www.byocli.com`.

## Working on it

```bash
cd website
npm install
npm run dev        # http://localhost:4321
npm run build      # static output in dist/
npm run preview    # serve dist/ locally
```

Node 22.12 or later is required.

## Where things come from

- **Documentation pages** are rendered from the repository's own `../docs/*.md` files by the content collection in `src/content.config.ts`. Edit the markdown in `docs/`, not a copy. Links between docs written as `name.md` are rewritten to `<base>/docs/name/` at build time by `src/lib/rehype-docs-links.mjs`.
- **Reading order and rail labels** for the docs live in `src/lib/docs-nav.ts`. Add a new doc there to put it on the rail; pages not listed still build and are reachable by URL.
- **The landing page** is `src/pages/index.astro`. Its example commands are the ones from the documentation.
- **Brand marks** in `public/` are copies of `docs/Images/byo-icon.png` and `docs/Images/byo-logo.png`.

## Deployment

`.github/workflows/pages.yml` builds this folder and deploys `dist/` to GitHub Pages on every push to `main` that touches `website/` or `docs/`. The workflow asks GitHub where the site is served (`actions/configure-pages`) and passes the origin and base path to the build as `ASTRO_SITE` and `ASTRO_BASE`, so the same build works at the project URL `softwareworkercom.github.io/byo/` and, once the custom domain is set in the repository's Pages settings and a DNS CNAME points `www.byocli.com` at `softwareworkercom.github.io`, at the root of `www.byocli.com`. Every internal link and asset path goes through `withBase()` in `src/lib/paths.ts`; Astro prefixes its own bundles. A local build without those variables targets the custom domain at the root. `public/CNAME` is ignored by Actions deployments and kept as a record of the intended domain.
