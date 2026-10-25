import fs from 'node:fs';
import http from 'node:http';

const html = await new Promise((resolve, reject) => {
  http
    .get('http://127.0.0.1:3000/', (res) => {
      const chunks = [];
      res.on('data', (c) => chunks.push(c));
      res.on('end', () => resolve(Buffer.concat(chunks).toString('utf8')));
    })
    .on('error', reject);
});

const scripts = [...html.matchAll(/\/_next\/static\/chunks\/[^"']+/g)].map(
  (m) => m[0],
);
console.log('script tags in HTML', scripts.length);
for (const s of scripts) console.log(s);
console.log(
  'html has 9161',
  scripts.some((s) => s.includes('9161')),
);

const r = JSON.parse(
  fs.readFileSync('apps/website/.lighthouseci-r1-4/home-mobile.json', 'utf8'),
);
const net = r.audits['network-requests'].details.items.filter(
  (i) => i.resourceType === 'Script',
);
console.log('\nLH scripts:');
for (const i of net) {
  console.log(i.transferSize, i.url.split('/').pop());
}
