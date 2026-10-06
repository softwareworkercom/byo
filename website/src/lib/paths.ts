/**
 * Prefixes a root-relative path with the site's base path. GitHub Pages serves the site at the
 * domain root with a custom domain, but under /byo/ on the project URL; Astro prefixes its own
 * bundles, and every link and asset path the pages write goes through here.
 */
export function withBase(path: string): string {
  const base = import.meta.env.BASE_URL.replace(/\/$/, '');
  return `${base}${path}`;
}
