/**
 * Reading order and short labels for the documentation rail. Page titles come from each
 * file's own first heading; these labels are what hangs on the rail.
 */
export interface DocLink {
  slug: string;
  label: string;
  /** For pages that are not rendered from docs/*.md, the line shown under the label on the index. */
  summary?: string;
}

export interface DocGroup {
  label: string;
  items: DocLink[];
}

export const docGroups: DocGroup[] = [
  {
    label: 'Start here',
    items: [
      {
        slug: 'start',
        label: 'Start here',
        summary: 'Two paths, one sitting each: automate a workflow, or build a plugin an agent can run',
      },
      { slug: 'getting-started', label: 'Getting started' },
    ],
  },
  {
    label: 'Command groups',
    items: [
      { slug: 'run', label: 'run' },
      { slug: 'commands', label: 'commands' },
      { slug: 'settings', label: 'settings' },
      { slug: 'secrets', label: 'secrets' },
      { slug: 'workflows', label: 'workflows' },
      { slug: 'plugins', label: 'plugins' },
      { slug: 'alias', label: 'alias' },
    ],
  },
  {
    label: 'Shell and flags',
    items: [
      { slug: 'shell', label: 'Interactive shell' },
      { slug: 'schedule', label: '--schedule' },
      { slug: 'async', label: '--async' },
    ],
  },
  {
    label: 'Data',
    items: [
      { slug: 'token-replacement', label: 'Token replacement' },
      { slug: 'export', label: 'Storage' },
    ],
  },
  {
    label: 'SDK',
    items: [
      { slug: 'building-a-plugin', label: 'Building a plugin' },
      { slug: 'sdk-export-service', label: 'ExportService' },
    ],
  },
  { label: 'Maintainers', items: [{ slug: 'release-and-distribution', label: 'Release and distribution' }] },
];

export const docOrder = docGroups.flatMap((group) => group.items.map((item) => item.slug));

/** The first level-one heading of a markdown body, with inline code marks removed. */
export function titleFromBody(body: string | undefined, fallback: string): string {
  const match = body?.match(/^#\s+(.+?)\s*$/m);
  return match ? match[1].replace(/`/g, '') : fallback;
}

/**
 * The first paragraph of a markdown body as plain text, for the page description: headings,
 * code fences, lists, tables and quotes are skipped, inline marks removed, and the result cut at
 * a sentence end within the length social previews show.
 */
export function summaryFromBody(body: string | undefined, fallback: string, max = 160): string {
  if (!body) return fallback;
  const lines = body.split(/\r?\n/);
  let inFence = false;
  const paragraph: string[] = [];
  for (const raw of lines) {
    const line = raw.trim();
    if (line.startsWith('```')) {
      inFence = !inFence;
      continue;
    }
    if (inFence) continue;
    if (paragraph.length === 0) {
      if (!line || /^(#|[-*+]\s|\d+\.\s|\||>|<)/.test(line)) continue;
      paragraph.push(line);
      continue;
    }
    if (!line) break;
    paragraph.push(line);
  }
  if (paragraph.length === 0) return fallback;
  const text = paragraph
    .join(' ')
    .replace(/!\[[^\]]*\]\([^)]*\)/g, '')
    .replace(/\[([^\]]+)\]\([^)]*\)/g, '$1')
    .replace(/[`*_]/g, '')
    .replace(/\s+/g, ' ')
    .trim()
    // A paragraph that introduces a list ends in a colon, often after a preposition; end the sentence instead.
    .replace(/(\s+(with|for|to|of|in|by|on|at|from|as|and|or|like|including|such as))?:$/, '.');
  if (text.length <= max) return text;
  let cut = text.slice(0, max);
  const sentence = cut.lastIndexOf('. ');
  if (sentence > max / 2) return cut.slice(0, sentence + 1);
  const open = cut.lastIndexOf('(');
  if (open > 0 && cut.indexOf(')', open) < 0) cut = cut.slice(0, open);
  return `${cut.trimEnd().replace(/[,;:]$/, '').slice(0, Math.max(0, cut.trimEnd().lastIndexOf(' ')))}…`;
}

export function labelFor(slug: string): string {
  for (const group of docGroups) {
    const item = group.items.find((candidate) => candidate.slug === slug);
    if (item) {
      return item.label;
    }
  }
  return slug;
}
