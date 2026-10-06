import { defineCollection } from 'astro:content';
import { glob } from 'astro/loaders';

// The site renders the repository's own documentation. docs/*.md stays the single source;
// nothing is copied into the website folder.
const docs = defineCollection({
  loader: glob({ pattern: '*.md', base: '../docs' }),
});

export const collections = { docs };
