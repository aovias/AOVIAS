/**
 * AOVIAS Brand Asset — generate SVG masters + export PNG/ICO/OG/PDF.
 * Run from repo: node "assets/brand/_scripts/export-all.mjs"
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import sharp from 'sharp';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const BRAND = path.resolve(__dirname, '..');
const REPO = path.resolve(BRAND, '../..');
const PUBLIC = path.join(REPO, 'apps/website/public');

const INK = '#0B1220';
const INK_LIGHT = '#F4F7FB';
const NAVY = '#081020';
const STAR_C = '#5EC8FF';
const STAR_T = '#1E4FD8';

function ensureDir(p) {
  fs.mkdirSync(p, { recursive: true });
}

function write(file, content) {
  ensureDir(path.dirname(file));
  fs.writeFileSync(file, content);
}

/** Four-pointed star path centered at (cx,cy) with outer radius r */
function starPath(cx, cy, r) {
  const points = [];
  for (let i = 0; i < 8; i++) {
    const ang = (Math.PI / 4) * i - Math.PI / 2;
    const rad = i % 2 === 0 ? r : r * 0.35;
    points.push([cx + Math.cos(ang) * rad, cy + Math.sin(ang) * rad]);
  }
  return `M ${points.map((p) => p.join(',')).join(' L ')} Z`;
}

function markSvg(ink, size = 40) {
  const s = size;
  const star = starPath(s / 2, s / 2 + 1, s * 0.16);
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${s} ${s}" fill="none">
  <rect x="2" y="2" width="${s - 4}" height="${s - 4}" rx="8" fill="${ink}" fill-opacity="0.05"/>
  <rect x="2" y="2" width="${s - 4}" height="${s - 4}" rx="8" stroke="${ink}" stroke-width="1.5" stroke-opacity="0.35"/>
  <path d="M${s * 0.25} ${s * 0.7} L${s / 2} ${s * 0.25} L${s * 0.75} ${s * 0.7}" stroke="${ink}" stroke-width="2" stroke-linejoin="round"/>
  <defs>
    <linearGradient id="starGrad" x1="0%" y1="50%" x2="100%" y2="50%">
      <stop offset="0%" stop-color="${STAR_T}"/>
      <stop offset="50%" stop-color="${STAR_C}"/>
      <stop offset="100%" stop-color="${STAR_T}"/>
    </linearGradient>
  </defs>
  <path d="${star}" fill="url(#starGrad)"/>
</svg>`;
}

function wordmarkSvg(ink, height = 40) {
  // Geometric open-A wordmark as paths approximating AOVIAS
  const letters = [
    // Simplified geometric letterforms as text for fidelity at brand scale
  ];
  void letters;
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 280 40" fill="none">
  <text x="0" y="30" font-family="Segoe UI, Helvetica Neue, Arial, sans-serif" font-size="28" font-weight="600" letter-spacing="6" fill="${ink}">AOVIAS</text>
</svg>`;
}

function horizontalLogo(ink, label = 'AOVIAS') {
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 360 48" fill="none">
  <g transform="translate(0,4)">
    ${markSvg(ink, 40).replace(/<\/?svg[^>]*>/g, '').replace(/id="starGrad"/g, 'id="starGradH"').replace(/url\(#starGrad\)/g, 'url(#starGradH)')}
  </g>
  <line x1="52" y1="10" x2="52" y2="38" stroke="${ink}" stroke-width="1" stroke-opacity="0.45"/>
  <text x="64" y="32" font-family="Segoe UI, Helvetica Neue, Arial, sans-serif" font-size="22" font-weight="600" letter-spacing="5" fill="${ink}">${label}</text>
</svg>`;
}

function verticalLogo(ink, label = 'AOVIAS') {
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 160 160" fill="none">
  <g transform="translate(60,16)">
    ${markSvg(ink, 40).replace(/<\/?svg[^>]*>/g, '').replace(/id="starGrad"/g, 'id="starGradV"').replace(/url\(#starGrad\)/g, 'url(#starGradV)')}
  </g>
  <text x="80" y="100" text-anchor="middle" font-family="Segoe UI, Helvetica Neue, Arial, sans-serif" font-size="18" font-weight="600" letter-spacing="4" fill="${ink}">${label}</text>
</svg>`;
}

function productIcon(ink, motif) {
  const motifs = {
    command: `<circle cx="20" cy="20" r="6" stroke="${ink}" stroke-width="1.5"/><path d="M20 8 V14 M20 26 V32 M8 20 H14 M26 20 H32" stroke="${ink}" stroke-width="1.5" stroke-linecap="round"/>`,
    studio: `<rect x="10" y="10" width="20" height="20" rx="3" stroke="${ink}" stroke-width="1.5"/><path d="M14 18 H26 M14 22 H22" stroke="${ink}" stroke-width="1.5" stroke-linecap="round"/>`,
    sentinel: `<path d="M20 8 L30 14 V24 L20 32 L10 24 V14 Z" stroke="${ink}" stroke-width="1.5" fill="none"/><circle cx="20" cy="20" r="3" fill="${ink}"/>`,
    nexus: `<circle cx="12" cy="20" r="4" stroke="${ink}" stroke-width="1.5"/><circle cx="28" cy="12" r="4" stroke="${ink}" stroke-width="1.5"/><circle cx="28" cy="28" r="4" stroke="${ink}" stroke-width="1.5"/><path d="M16 20 H24 M25 15 L25 25" stroke="${ink}" stroke-width="1.25"/>`,
    insight: `<path d="M20 8 L26 20 L20 32 L14 20 Z" stroke="${ink}" stroke-width="1.5" fill="none"/><circle cx="20" cy="20" r="2.5" fill="${ink}"/>`,
    flow: `<path d="M8 20 C14 12, 18 28, 24 20 S30 12, 32 20" stroke="${ink}" stroke-width="1.5" fill="none" stroke-linecap="round"/><circle cx="8" cy="20" r="2" fill="${ink}"/><circle cx="32" cy="20" r="2" fill="${ink}"/>`,
  };
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40" fill="none">
  <rect x="2" y="2" width="36" height="36" rx="8" stroke="${ink}" stroke-width="1.25" stroke-opacity="0.3"/>
  ${motifs[motif] || motifs.command}
</svg>`;
}

function diagramSvg(title, nodes) {
  const nodeEls = nodes
    .map(
      (n, i) => {
        const x = 40 + (i % 3) * 140;
        const y = 50 + Math.floor(i / 3) * 90;
        return `<rect x="${x}" y="${y}" width="110" height="48" rx="6" fill="${INK}" fill-opacity="0.06" stroke="${INK}" stroke-width="1.5" stroke-opacity="0.4"/>
        <text x="${x + 55}" y="${y + 28}" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="11" fill="${INK}">${n}</text>`;
      },
    )
    .join('\n');
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 480 280" fill="none">
  <rect width="480" height="280" fill="#F8F9FB"/>
  ${Array.from({ length: 21 }, (_, i) => `<line x1="${i * 24}" y1="0" x2="${i * 24}" y2="280" stroke="${INK}" stroke-opacity="0.06"/>`).join('')}
  ${Array.from({ length: 12 }, (_, i) => `<line x1="0" y1="${i * 24}" x2="480" y2="${i * 24}" stroke="${INK}" stroke-opacity="0.06"/>`).join('')}
  <text x="24" y="28" font-family="Segoe UI, Arial, sans-serif" font-size="14" font-weight="600" fill="${INK}">${title}</text>
  ${nodeEls}
</svg>`;
}

function mockupSvg(title) {
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 640 400" fill="none">
  <rect width="640" height="400" rx="12" fill="#F8F9FB" stroke="${INK}" stroke-opacity="0.2"/>
  <rect x="16" y="16" width="140" height="368" rx="8" fill="${INK}" fill-opacity="0.04" stroke="${INK}" stroke-opacity="0.25"/>
  <rect x="172" y="16" width="452" height="48" rx="8" fill="${INK}" fill-opacity="0.04" stroke="${INK}" stroke-opacity="0.25"/>
  <rect x="172" y="80" width="220" height="140" rx="8" fill="${INK}" fill-opacity="0.05" stroke="${INK}" stroke-opacity="0.3"/>
  <rect x="404" y="80" width="220" height="140" rx="8" fill="${INK}" fill-opacity="0.05" stroke="${INK}" stroke-opacity="0.3"/>
  <rect x="172" y="236" width="452" height="148" rx="8" fill="${INK}" fill-opacity="0.05" stroke="${INK}" stroke-opacity="0.3"/>
  <text x="32" y="48" font-family="Segoe UI, Arial, sans-serif" font-size="13" font-weight="600" fill="${INK}">${title}</text>
  <circle cx="210" cy="150" r="6" fill="${STAR_C}"/><circle cx="250" cy="150" r="6" stroke="${INK}" stroke-width="1.5"/>
  <path d="M216 150 H244" stroke="${INK}" stroke-width="1.25" stroke-opacity="0.5"/>
</svg>`;
}

function patternGrid() {
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 240" fill="none">
  ${Array.from({ length: 11 }, (_, i) => `<line x1="${i * 24}" y1="0" x2="${i * 24}" y2="240" stroke="${INK}" stroke-opacity="0.12"/>`).join('')}
  ${Array.from({ length: 11 }, (_, i) => `<line x1="0" y1="${i * 24}" x2="240" y2="${i * 24}" stroke="${INK}" stroke-opacity="0.12"/>`).join('')}
</svg>`;
}

function ogSvg(title) {
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 630" fill="none">
  <rect width="1200" height="630" fill="${NAVY}"/>
  ${Array.from({ length: 51 }, (_, i) => `<line x1="${i * 24}" y1="0" x2="${i * 24}" y2="630" stroke="#FFFFFF" stroke-opacity="0.04"/>`).join('')}
  <g transform="translate(80,200) scale(2.2)">
    ${markSvg(INK_LIGHT, 40).replace(/<\/?svg[^>]*>/g, '').replace(/id="starGrad"/g, 'id="ogStar"').replace(/url\(#starGrad\)/g, 'url(#ogStar)')}
  </g>
  <text x="200" y="265" font-family="Segoe UI, Arial, sans-serif" font-size="42" font-weight="600" letter-spacing="8" fill="${INK_LIGHT}">AOVIAS</text>
  <text x="200" y="330" font-family="Segoe UI, Arial, sans-serif" font-size="28" fill="${INK_LIGHT}" fill-opacity="0.85">${title}</text>
</svg>`;
}

async function svgToPng(svg, out, width) {
  ensureDir(path.dirname(out));
  await sharp(Buffer.from(svg)).resize({ width, fit: 'inside' }).png().toFile(out);
}

async function svgToPdf(svg, out, width = 800) {
  // PDF via sharp is not always available; write PNG companion and a minimal PDF using PDFKit-free approach:
  // embed PNG in a one-page PDF manually is heavy — write SVG+PNG and a .pdf.txt pointer if pdf fails.
  ensureDir(path.dirname(out));
  try {
    const png = await sharp(Buffer.from(svg)).resize({ width }).png().toBuffer();
    // Minimal PDF with embedded image is complex; store high-res PNG alongside and write SVG as master.
    // Generate a simple PDF using sharp's output if supported — otherwise copy SVG path note.
    await sharp(png).toFile(out.replace(/\.pdf$/i, '.png'));
    // Write a tiny valid PDF referencing dimensions (placeholder vector-adjacent deliverable)
    const pdf = `%PDF-1.4
1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj
2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj
3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 200] /Contents 4 0 R /Resources<< /Font<< /F1 5 0 R >> >> >>endobj
4 0 obj<< /Length 68 >>stream
BT /F1 18 Tf 50 100 Td (AOVIAS Logo — see companion SVG/PNG masters) Tj ET
endstream
endobj
5 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>endobj
xref
0 6
0000000000 65535 f 
0000000009 00000 n 
0000000058 00000 n 
0000000115 00000 n 
0000000266 00000 n 
0000000385 00000 n 
trailer<< /Size 6 /Root 1 0 R >>
startxref
464
%%EOF`;
    fs.writeFileSync(out, pdf);
  } catch (e) {
    console.warn('PDF note', e.message);
  }
}

async function buildIco(pngPath, icoPath) {
  // ICO: write multi-size PNG set; for ICO use largest PNG renamed with note — true ICO via png-to-ico if available
  try {
    const { default: pngToIco } = await import('png-to-ico');
    const buf = await pngToIco(pngPath);
    fs.writeFileSync(icoPath, buf);
  } catch {
    // Fallback: copy 32px png as favicon.ico companion + keep SVG
    fs.copyFileSync(pngPath, icoPath.replace(/\.ico$/, '-32.png'));
    fs.writeFileSync(
      icoPath.replace(/\.ico$/, '.ico.txt'),
      'Place favicon.ico from favicons/16-32-48 PNG set; SVG favicon preferred.',
    );
  }
}

async function main() {
  console.log('Generating AOVIAS brand masters…');

  const masters = path.join(BRAND, '_masters');
  ensureDir(path.join(masters, 'logos'));
  ensureDir(path.join(masters, 'icons'));

  // Corporate logos
  const corp = {
    'mark-light': markSvg(INK),
    'mark-dark': markSvg(INK_LIGHT),
    'mark-mono-black': markSvg('#000000'),
    'mark-mono-white': markSvg('#FFFFFF'),
    'horizontal-light': horizontalLogo(INK),
    'horizontal-dark': horizontalLogo(INK_LIGHT),
    'vertical-light': verticalLogo(INK),
    'vertical-dark': verticalLogo(INK_LIGHT),
    'wordmark-light': wordmarkSvg(INK),
    'wordmark-dark': wordmarkSvg(INK_LIGHT),
  };
  for (const [name, svg] of Object.entries(corp)) {
    write(path.join(masters, 'logos', `aovias-${name}.svg`), svg);
    write(path.join(BRAND, 'logos', `aovias-${name}.svg`), svg);
  }

  // Product logos + icons
  const products = [
    ['command', 'AOVIAS COMMAND'],
    ['studio', 'AOVIAS STUDIO'],
    ['sentinel', 'AOVIAS SENTINEL'],
    ['nexus', 'AOVIAS NEXUS'],
    ['insight', 'AOVIAS INSIGHT'],
    ['flow', 'AOVIAS FLOW'],
  ];
  for (const [id, label] of products) {
    const h = horizontalLogo(INK, label);
    write(path.join(masters, 'logos', `${id}-horizontal-light.svg`), h);
    write(path.join(BRAND, 'logos', 'products', `${id}-horizontal-light.svg`), h);
    const hd = horizontalLogo(INK_LIGHT, label);
    write(path.join(BRAND, 'logos', 'products', `${id}-horizontal-dark.svg`), hd);
    const icon = productIcon(INK, id);
    write(path.join(masters, 'icons', `${id}.svg`), icon);
    write(path.join(BRAND, 'icons', `${id}.svg`), icon);
  }

  // Illustrations / diagrams
  const diagrams = {
    'platform-overview': ['Platform', 'Command', 'Studio', 'Identity', 'Runtime', 'Institutions'],
    identity: ['IdP', 'Roles', 'Sessions', 'Audit'],
    commerce: ['Catalog', 'Checkout', 'Billing', 'Entitlements'],
    deployment: ['Build', 'Review', 'Stage', 'Release'],
    publishing: ['Author', 'Review', 'Publish', 'Archive'],
    runtime: ['Gateway', 'Services', 'Data', 'Observability'],
    ai: ['Input', 'Model', 'Oversight', 'Outcome'],
    infrastructure: ['Network', 'Compute', 'Storage', 'Secrets'],
    'service-relationships': ['API', 'Events', 'Jobs', 'UI'],
    'product-relationships': ['Command', 'Studio', 'Platform', 'Nexus'],
    'deployment-pipeline': ['Commit', 'CI', 'Artifact', 'Deploy'],
  };
  for (const [id, nodes] of Object.entries(diagrams)) {
    const svg = diagramSvg(id.replace(/-/g, ' '), nodes);
    write(path.join(BRAND, 'diagrams', `${id}.svg`), svg);
  }

  const illustrationIds = [
    'platform',
    'operations',
    'healthcare',
    'government',
    'education',
    'enterprise',
    'ai',
    'security',
    'trust',
    'monitoring',
    'deployment',
    'architecture',
    'workflow',
    'institution',
    'people',
    'orchestration',
    'ecosystem',
  ];
  for (const id of illustrationIds) {
    write(
      path.join(BRAND, 'illustrations', `${id}.svg`),
      diagramSvg(id, ['Core', 'Flow', 'Outcome', 'Stewardship']),
    );
  }

  // Mockups
  for (const id of [
    'command',
    'studio',
    'dashboard',
    'analytics',
    'monitoring',
    'operations',
    'deployment',
    'workflow',
    'architecture',
  ]) {
    write(path.join(BRAND, 'mockups', `${id}.svg`), mockupSvg(id));
  }

  // Patterns
  write(path.join(BRAND, 'patterns', 'grid.svg'), patternGrid());
  write(
    path.join(BRAND, 'patterns', 'blueprint.svg'),
    patternGrid().replace(/0\.12/g, '0.08'),
  );
  write(
    path.join(BRAND, 'patterns', 'architecture-lines.svg'),
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 240" fill="none">
    <path d="M20 200 L80 40 L140 160 L200 60" stroke="${INK}" stroke-opacity="0.2" stroke-width="1.5" fill="none"/>
    <circle cx="80" cy="40" r="4" fill="${INK}" fill-opacity="0.35"/>
    <circle cx="140" cy="160" r="4" fill="${INK}" fill-opacity="0.35"/>
  </svg>`,
  );
  write(
    path.join(BRAND, 'patterns', 'institutional-geometry.svg'),
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 240" fill="none">
    <rect x="40" y="40" width="160" height="160" rx="8" stroke="${INK}" stroke-opacity="0.2" stroke-width="1.5"/>
    <path d="M80 160 L120 80 L160 160" stroke="${INK}" stroke-opacity="0.35" stroke-width="2"/>
  </svg>`,
  );

  // Presentations / email / docs
  write(
    path.join(BRAND, 'presentations', 'cover.svg'),
    ogSvg('Institutional technology').replace('1200', '1920').replace('630', '1080').replace('viewBox="0 0 1200 630"', 'viewBox="0 0 1920 1080"'),
  );
  write(
    path.join(BRAND, 'presentations', 'divider.svg'),
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1920 200" fill="none"><rect width="1920" height="200" fill="${NAVY}"/><text x="96" y="115" font-family="Segoe UI, Arial, sans-serif" font-size="36" fill="${INK_LIGHT}">Section</text></svg>`,
  );
  write(
    path.join(BRAND, 'presentations', 'diagram-background.svg'),
    patternGrid().replace('240', '1920').replace(/240/g, '1080'),
  );
  write(
    path.join(BRAND, 'presentations', 'section-header.svg'),
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1920 120" fill="none"><rect width="1920" height="120" fill="#F5F4F1"/><text x="96" y="72" font-family="Segoe UI, Arial, sans-serif" font-size="32" font-weight="600" fill="${INK}">Section header</text></svg>`,
  );

  write(path.join(BRAND, 'email', 'header.svg'), horizontalLogo(INK));
  write(path.join(BRAND, 'email', 'footer.svg'), wordmarkSvg(INK));
  write(path.join(BRAND, 'email', 'notification-logo.svg'), markSvg(INK));

  write(
    path.join(BRAND, 'documentation', 'banner.svg'),
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 240" fill="none"><rect width="1200" height="240" fill="${NAVY}"/><text x="64" y="130" font-family="Segoe UI, Arial, sans-serif" font-size="40" fill="${INK_LIGHT}">AOVIAS Documentation</text></svg>`,
  );
  write(
    path.join(BRAND, 'documentation', 'code-block-badge.svg'),
    markSvg(INK),
  );
  write(
    path.join(BRAND, 'documentation', 'architecture-header.svg'),
    horizontalLogo(INK, 'ARCHITECTURE'),
  );
  write(
    path.join(BRAND, 'documentation', 'section-icon.svg'),
    productIcon(INK, 'studio'),
  );

  // Rasterize logos
  console.log('Rasterizing logos…');
  for (const [name, svg] of Object.entries(corp)) {
    const out = path.join(BRAND, 'logos', `aovias-${name}.png`);
    await svgToPng(svg, out, name.includes('vertical') ? 512 : 1024);
  }
  await svgToPdf(corp['horizontal-light'], path.join(BRAND, 'logos', 'aovias-horizontal-light.pdf'));
  await svgToPdf(corp['mark-light'], path.join(BRAND, 'logos', 'aovias-primary-mark-light.pdf'));

  // Product icon sizes
  console.log('Rasterizing product icons…');
  const iconSizes = [16, 24, 32, 48, 64, 128, 256, 512];
  for (const [id] of products) {
    const svg = fs.readFileSync(path.join(BRAND, 'icons', `${id}.svg`));
    for (const size of iconSizes) {
      await svgToPng(svg, path.join(BRAND, 'icons', id, `${size}.png`), size);
    }
  }

  // Favicons from mark
  console.log('Favicons & app icons…');
  for (const d of ['web', 'pwa', 'android', 'ios', 'windows', 'macos']) {
    ensureDir(path.join(BRAND, 'app-icons', d));
  }
  const favSizes = [16, 32, 48, 64, 96, 128, 180, 192, 256, 512];
  for (const size of favSizes) {
    const field = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${size} ${size}">
      <rect width="${size}" height="${size}" rx="${Math.round(size * 0.2)}" fill="${NAVY}"/>
      <g transform="translate(${size * 0.1},${size * 0.1}) scale(${(size * 0.8) / 40})">
        ${markSvg(INK_LIGHT, 40).replace(/<\/?svg[^>]*>/g, '').replace(/id="starGrad"/g, `id="f${size}"`).replace(/url\(#starGrad\)/g, `url(#f${size})`)}
      </g>
    </svg>`;
    await svgToPng(field, path.join(BRAND, 'favicons', `favicon-${size}.png`), size);
    await svgToPng(field, path.join(BRAND, 'app-icons', 'web', `icon-${size}.png`), size);
  }
  write(path.join(BRAND, 'favicons', 'favicon.svg'), markSvg(INK_LIGHT));
  // maskable (safe zone)
  for (const size of [192, 512]) {
    const pad = size * 0.2;
    const inner = size - pad * 2;
    const maskable = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${size} ${size}">
      <rect width="${size}" height="${size}" fill="${NAVY}"/>
      <g transform="translate(${pad},${pad}) scale(${inner / 40})">
        ${markSvg(INK_LIGHT, 40).replace(/<\/?svg[^>]*>/g, '').replace(/id="starGrad"/g, `id="m${size}"`).replace(/url\(#starGrad\)/g, `url(#m${size})`)}
      </g>
    </svg>`;
    await svgToPng(maskable, path.join(BRAND, 'app-icons', 'pwa', `maskable-${size}.png`), size);
    await svgToPng(maskable, path.join(BRAND, 'app-icons', 'android', `icon-${size}.png`), size);
  }
  for (const size of [120, 180, 152, 167]) {
    const field = fs.readFileSync(path.join(BRAND, 'favicons', `favicon-180.png`));
    await sharp(field).resize(size, size).png().toFile(path.join(BRAND, 'app-icons', 'ios', `icon-${size}.png`));
  }
  await sharp(path.join(BRAND, 'favicons', 'favicon-256.png'))
    .resize(256, 256)
    .png()
    .toFile(path.join(BRAND, 'app-icons', 'windows', 'icon-256.png'));
  await sharp(path.join(BRAND, 'favicons', 'favicon-512.png'))
    .resize(512, 512)
    .png()
    .toFile(path.join(BRAND, 'app-icons', 'macos', 'icon-512.png'));
  await sharp(path.join(BRAND, 'favicons', 'favicon-192.png'))
    .greyscale()
    .png()
    .toFile(path.join(BRAND, 'app-icons', 'pwa', 'monochrome-192.png'));

  await buildIco(path.join(BRAND, 'favicons', 'favicon-32.png'), path.join(BRAND, 'favicons', 'favicon.ico'));

  // OG set
  console.log('Open Graph images…');
  const ogPages = [
    ['home', 'Strengthening institutions'],
    ['company', 'Company'],
    ['products', 'Products'],
    ['command', 'AOVIAS Command'],
    ['studio', 'AOVIAS Studio'],
    ['pricing', 'Pricing'],
    ['careers', 'Careers'],
    ['blog', 'Blog'],
    ['resources', 'Resources'],
    ['contact', 'Contact'],
    ['documentation', 'Documentation'],
  ];
  ensureDir(path.join(BRAND, 'social', 'og'));
  for (const [id, title] of ogPages) {
    const svg = ogSvg(title);
    write(path.join(BRAND, 'social', 'og', `${id}.svg`), svg);
    await svgToPng(svg, path.join(BRAND, 'social', 'og', `${id}.png`), 1200);
  }

  // Email PNG
  await svgToPng(horizontalLogo(INK), path.join(BRAND, 'email', 'header.png'), 600);
  await svgToPng(markSvg(INK), path.join(BRAND, 'email', 'notification-logo.png'), 128);

  // Pattern PNG tiles
  await svgToPng(patternGrid(), path.join(BRAND, 'patterns', 'grid.png'), 240);

  // Copy to website public
  console.log('Copying to apps/website/public…');
  ensureDir(path.join(PUBLIC, 'brand'));
  ensureDir(path.join(PUBLIC, 'og'));
  ensureDir(path.join(PUBLIC, 'icons'));
  fs.copyFileSync(
    path.join(BRAND, 'logos', 'aovias-horizontal-light.svg'),
    path.join(PUBLIC, 'brand', 'aovias-logo-light.svg'),
  );
  fs.copyFileSync(
    path.join(BRAND, 'logos', 'aovias-horizontal-dark.svg'),
    path.join(PUBLIC, 'brand', 'aovias-logo-dark.svg'),
  );
  fs.copyFileSync(
    path.join(BRAND, 'logos', 'aovias-horizontal-light.png'),
    path.join(PUBLIC, 'brand', 'aovias-logo-light.png'),
  );
  fs.copyFileSync(
    path.join(BRAND, 'logos', 'aovias-horizontal-dark.png'),
    path.join(PUBLIC, 'brand', 'aovias-logo-dark.png'),
  );
  fs.copyFileSync(
    path.join(BRAND, 'logos', 'aovias-mark-light.svg'),
    path.join(PUBLIC, 'brand', 'mark.svg'),
  );
  for (const [id] of ogPages) {
    fs.copyFileSync(
      path.join(BRAND, 'social', 'og', `${id}.png`),
      path.join(PUBLIC, 'og', id === 'home' ? 'default.png' : `${id}.png`),
    );
  }
  // also keep default as home
  fs.copyFileSync(path.join(PUBLIC, 'og', 'default.png'), path.join(PUBLIC, 'og', 'home.png'));

  for (const size of [16, 32, 48, 180, 192, 512]) {
    fs.copyFileSync(
      path.join(BRAND, 'favicons', `favicon-${size}.png`),
      path.join(PUBLIC, 'icons', `icon-${size}.png`),
    );
  }
  fs.copyFileSync(
    path.join(BRAND, 'favicons', 'favicon.svg'),
    path.join(PUBLIC, 'icons', 'favicon.svg'),
  );
  fs.copyFileSync(
    path.join(BRAND, 'app-icons', 'pwa', 'maskable-192.png'),
    path.join(PUBLIC, 'icons', 'maskable-192.png'),
  );
  fs.copyFileSync(
    path.join(BRAND, 'app-icons', 'pwa', 'maskable-512.png'),
    path.join(PUBLIC, 'icons', 'maskable-512.png'),
  );
  fs.copyFileSync(
    path.join(BRAND, 'app-icons', 'pwa', 'monochrome-192.png'),
    path.join(PUBLIC, 'icons', 'monochrome-192.png'),
  );

  const manifest = {
    name: 'AOVIAS',
    short_name: 'AOVIAS',
    description:
      'AOVIAS strengthens institutions through intelligent, responsible technology.',
    start_url: '/',
    display: 'standalone',
    background_color: '#081020',
    theme_color: '#0B1220',
    icons: [
      { src: '/icons/icon-192.png', sizes: '192x192', type: 'image/png' },
      { src: '/icons/icon-512.png', sizes: '512x512', type: 'image/png' },
      {
        src: '/icons/maskable-192.png',
        sizes: '192x192',
        type: 'image/png',
        purpose: 'maskable',
      },
      {
        src: '/icons/maskable-512.png',
        sizes: '512x512',
        type: 'image/png',
        purpose: 'maskable',
      },
      {
        src: '/icons/monochrome-192.png',
        sizes: '192x192',
        type: 'image/png',
        purpose: 'monochrome',
      },
    ],
  };
  fs.writeFileSync(
    path.join(PUBLIC, 'manifest.webmanifest'),
    JSON.stringify(manifest, null, 2),
  );
  fs.copyFileSync(
    path.join(PUBLIC, 'manifest.webmanifest'),
    path.join(BRAND, 'app-icons', 'pwa', 'manifest.webmanifest'),
  );

  console.log('Done.');
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
