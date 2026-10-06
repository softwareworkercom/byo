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

- **Documentation pages** are rendered from the repository's own `../docs/*.md` files by the content collection in `src/content.config.ts`. Edit the markdown in `docs/`, not a copy. Links between docs written as `name.md` are rewritten to `/docs/name/` at build time by `src/lib/rehype-docs-links.mjs`.
- **Reading order and rail labels** for the docs live in `src/lib/docs-nav.ts`. Add a new doc there to put it on the rail; pages not listed still build and are reachable by URL.
- **The landing page** is `src/pages/index.astro`. Its example commands are the ones from the documentation.
- **Brand marks** in `public/` are copies of `docs/Images/byo-icon.png` and `docs/Images/byo-logo.png`.

## Deployment

`.github/workflows/pages.yml` builds this folder and deploys `dist/` to GitHub Pages on every push to `main` that touches `website/` or `docs/`. The site is built for the root path, and `public/CNAME` points the Pages site at `www.byocli.com`. Enable GitHub Pages with the "GitHub Actions" source on the repository for the first deployment.
