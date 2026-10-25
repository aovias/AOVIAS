import fs from 'node:fs';
import path from 'node:path';

const root = 'apps/website/src/components/system-states';
const files = fs
  .readdirSync(root)
  .filter((f) => f.endsWith('.tsx'))
  .map((f) => path.join(root, f));

for (const file of files) {
  let source = fs.readFileSync(file, 'utf8');
  if (!source.includes('MarketingReveal')) continue;

  source = source.replace(
    /import \{ MarketingReveal \} from '@\/components\/marketing\/reveal';\n/,
    '',
  );
  source = source.replace(
    /if \(!reveal\) return body;\n\s*return <MarketingReveal enabled>\{body\}<\/MarketingReveal>;/,
    'void reveal;\n  return body;',
  );
  source = source.replace(
    /return <MarketingReveal enabled>\{body\}<\/MarketingReveal>;/,
    'return body;',
  );
  source = source.replace(
    /\{reveal \? <MarketingReveal enabled>\{(\w+)\}<\/MarketingReveal> : \1\}/g,
    '{$1}',
  );

  fs.writeFileSync(file, source);
  console.log('stripped reveal from', file);
}
