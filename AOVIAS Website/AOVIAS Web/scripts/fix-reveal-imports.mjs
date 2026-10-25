import fs from 'node:fs';

const files = [
  'apps/website/src/features/company/leadership/section.tsx',
  'apps/website/src/features/company/mission/section.tsx',
  'apps/website/src/features/company/story/section.tsx',
  'apps/website/src/features/company/vision/section.tsx',
  'apps/website/src/features/pricing/overview/section.tsx',
  'apps/website/src/features/products/catalog/section.tsx',
  'apps/website/src/features/technology/technology-landing-page.tsx',
];

for (const file of files) {
  let source = fs.readFileSync(file, 'utf8');
  if (!source.includes('MarketingReveal')) continue;

  source = source.replace(
    /import \{([^}]+)\} from '@\/components\/marketing';/g,
    (full, inner) => {
      const parts = inner
        .split(',')
        .map((part) => part.trim())
        .filter(Boolean);
      const keep = parts.filter(
        (part) =>
          !part.startsWith('MarketingReveal') &&
          part !== 'type MarketingRevealProps',
      );
      const hasReveal = parts.some((part) => part.startsWith('MarketingReveal'));
      const lines = [];
      if (hasReveal) {
        lines.push(
          "import { MarketingReveal } from '@/components/marketing/reveal';",
        );
      }
      if (keep.length > 0) {
        lines.push(
          `import { ${keep.join(', ')} } from '@/components/marketing';`,
        );
      }
      return lines.join('\n') || full;
    },
  );

  fs.writeFileSync(file, source);
  console.log('updated', file);
}
