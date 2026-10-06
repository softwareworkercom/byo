/**
 * The markdown in docs/ links between files as `alias.md` or `plugins.md#package-sources`,
 * which is right on GitHub. On the site each file is a page under /docs/<name>/, so this
 * plugin rewrites those relative links and keeps their fragments.
 */
const RELATIVE_MD_LINK = /^(?![a-z]+:|\/|#)([\w.-]+)\.md(#.*)?$/i;

export default function rehypeDocsLinks() {
  return (tree) => visit(tree);
}

function visit(node) {
  if (node.type === 'element' && node.tagName === 'a' && typeof node.properties?.href === 'string') {
    const match = node.properties.href.match(RELATIVE_MD_LINK);
    if (match) {
      node.properties.href = `/docs/${match[1]}/${match[2] ?? ''}`;
    }
  }

  for (const child of node.children ?? []) {
    visit(child);
  }
}
