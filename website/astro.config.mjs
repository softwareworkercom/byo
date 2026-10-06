// @ts-check
import { defineConfig } from 'astro/config';
import { unified } from '@astrojs/markdown-remark';
import { fileURLToPath } from 'node:url';
import rehypeDocsLinks from './src/lib/rehype-docs-links.mjs';

// The documentation is read in place from ../docs, so the dev server must be allowed
// to serve files from the repository root rather than only from website/.
const repoRoot = fileURLToPath(new URL('..', import.meta.url));

export default defineConfig({
  site: 'https://www.byocli.com',
  trailingSlash: 'always',
  markdown: {
    // Code blocks are one quiet colour on a grey field, like the rest of the docs; no token colours.
    syntaxHighlight: false,
    // The unified (remark/rehype) processor, so the docs' relative `name.md` links can be
    // rewritten to site routes. Astro 7's default processor has no plugin hook for that.
    processor: unified({ rehypePlugins: [rehypeDocsLinks] }),
  },
  vite: {
    server: { fs: { allow: [repoRoot] } },
  },
});
