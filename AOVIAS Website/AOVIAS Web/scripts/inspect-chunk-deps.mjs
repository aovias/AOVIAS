import fs from 'node:fs';

const candidates = [
  '9272-42f5dd319749e5ba.js',
  '9661-4ecc67711f65351a.js',
  '2808-59978e48f049bcfb.js',
  '9166-58f4a9ee1d306a88.js',
  '1631-d9ed563ea06c732d.js',
  '4159-9a8249b562521020.js',
  '1972-9096906acd16a7f6.js',
  '7418-47fa5120191f6128.js',
  '101-0be9c0e66bc61aa0.js',
  '4799-aab1af6c30caba46.js',
  '8903-1948c2d1af494753.js',
  'app/layout-4fa6b57d10884598.js',
  'app/error-c30549d9c757affb.js',
  'app/(technology)/layout-5411af36ae48204b.js',
  'app/global-error-c222c55c9e5bde6a.js',
];

for (const name of candidates) {
  const p = `apps/website/.next/static/chunks/${name}`;
  if (!fs.existsSync(p)) {
    console.log('missing', name);
    continue;
  }
  const s = fs.readFileSync(p, 'utf8');
  const hits = [];
  if (s.includes('9161')) hits.push('9161');
  if (s.includes('framer')) hits.push('framer');
  if (s.includes('AnimatePresence')) hits.push('AP');
  if (s.includes('mega-menu') || s.includes('MegaMenu')) hits.push('mega');
  if (s.includes('MotionReveal') || s.includes('sectionReveal')) hits.push('reveal');
  console.log(name, 'size', s.length, hits.join(',') || 'clean');
}
