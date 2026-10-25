import http from 'node:http';
import fs from 'node:fs';

const html = await new Promise((resolve, reject) => {
  http
    .get('http://127.0.0.1:3000/', (res) => {
      let data = '';
      res.on('data', (c) => {
        data += c;
      });
      res.on('end', () => resolve(data));
    })
    .on('error', reject);
});

const srcs = [...html.matchAll(/src="(\/_next\/static\/chunks\/[^"]+)"/g)].map(
  (m) => m[1],
);
console.log('src count', srcs.length);
for (const s of srcs) console.log(s);

const hrefs = [...html.matchAll(/href="(\/_next\/static\/chunks\/[^"]+)"/g)].map(
  (m) => m[1],
);
console.log('\nhref count', hrefs.length);
for (const s of hrefs.filter(
  (p) =>
    p.includes('9161') || p.includes('technology') || p.includes('layout'),
)) {
  console.log('href', s);
}

// Who requires chunk 9161?
const dir = 'apps/website/.next/static/chunks';
function walk(d, acc = []) {
  for (const f of fs.readdirSync(d, { withFileTypes: true })) {
    const p = `${d}/${f.name}`;
    if (f.isDirectory()) walk(p, acc);
    else if (f.name.endsWith('.js')) acc.push(p);
  }
  return acc;
}

console.log('\nfiles referencing 9161 module id:');
let n = 0;
for (const p of walk(dir)) {
  if (p.includes('9161-')) continue;
  const s = fs.readFileSync(p, 'utf8');
  if (s.includes('9161')) {
    console.log(p.replace(/.*chunks\//, ''));
    n += 1;
    if (n > 25) break;
  }
}
