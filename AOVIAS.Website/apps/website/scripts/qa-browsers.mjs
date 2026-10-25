/**
 * Cross-browser smoke via Playwright when installed; otherwise Chromium-only via chrome-launcher.
 * Safari/iOS remain RC checklist items on Windows hosts.
 */
import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);

const BASE = process.env.PORT
  ? `http://127.0.0.1:${process.env.PORT}`
  : 'http://127.0.0.1:3000';

const routes = ['/', '/products', '/company/contact', '/pricing', '/auth/sign-in'];

const outDir = path.resolve('.qa-browsers');
fs.mkdirSync(outDir, { recursive: true });

const results = {
  host: process.platform,
  safariNote:
    process.platform === 'win32'
      ? 'Safari/WebKit native unavailable on Windows — record under RC checklist (macOS/iOS device QA).'
      : 'Run Safari Technology Preview / WebKit where available for RC sign-off.',
  browsers: [],
};

async function runPlaywright() {
  const { chromium, firefox, webkit } = require('playwright');
  const projects = [
    { name: 'chromium', browserType: chromium },
    { name: 'firefox', browserType: firefox },
    { name: 'webkit', browserType: webkit },
  ];

  for (const project of projects) {
    let browser;
    try {
      browser = await project.browserType.launch({ headless: true });
      const page = await browser.newPage();
      const routeResults = [];
      for (const route of routes) {
        const response = await page.goto(`${BASE}${route}`, {
          waitUntil: 'domcontentloaded',
          timeout: 60000,
        });
        const title = await page.title();
        const hasMain = await page.locator('main#main-content').count();
        const reducedMotion = await page.evaluate(() =>
          window.matchMedia('(prefers-reduced-motion: reduce)').matches,
        );
        routeResults.push({
          route,
          status: response?.status() ?? 0,
          title,
          hasMain: hasMain > 0,
          ok: (response?.status() ?? 0) === 200 && hasMain > 0,
          reducedMotionQueryAvailable: typeof reducedMotion === 'boolean',
        });
      }
      results.browsers.push({ name: project.name, ok: routeResults.every((r) => r.ok), routes: routeResults });
      console.log(`${project.name}: ${routeResults.every((r) => r.ok) ? 'OK' : 'FAIL'}`);
    } catch (error) {
      results.browsers.push({ name: project.name, ok: false, error: error.message });
      console.warn(`${project.name}: ${error.message}`);
    } finally {
      try {
        await browser?.close();
      } catch {
        /* ignore */
      }
    }
  }
}

async function runChromiumOnly() {
  const chromeLauncher = require('chrome-launcher');
  let puppeteer;
  try {
    puppeteer = require('puppeteer-core');
  } catch {
    puppeteer = null;
  }

  const chrome = await chromeLauncher.launch({
    chromeFlags: ['--headless=new', '--no-sandbox', '--disable-gpu'],
  });

  try {
    if (!puppeteer) {
      const routeResults = [];
      for (const route of routes) {
        const res = await fetch(`${BASE}${route}`);
        routeResults.push({
          route,
          status: res.status,
          ok: res.status === 200,
          mode: 'http-only',
        });
      }
      results.browsers.push({
        name: 'chromium-http',
        ok: routeResults.every((r) => r.ok),
        routes: routeResults,
      });
      return;
    }

    const browser = await puppeteer.connect({
      browserURL: `http://127.0.0.1:${chrome.port}`,
    });
    const page = await browser.newPage();
    const routeResults = [];
    for (const route of routes) {
      const response = await page.goto(`${BASE}${route}`, {
        waitUntil: 'networkidle2',
        timeout: 60000,
      });
      const hasMain = await page.evaluate(
        () => Boolean(document.querySelector('main#main-content')),
      );
      routeResults.push({
        route,
        status: response?.status() ?? 0,
        hasMain,
        ok: (response?.status() ?? 0) === 200 && hasMain,
      });
    }
    results.browsers.push({
      name: 'chromium',
      ok: routeResults.every((r) => r.ok),
      routes: routeResults,
    });
    await browser.disconnect();
  } finally {
    await chrome.kill().catch(() => {});
  }
}

try {
  require.resolve('playwright');
  await runPlaywright();
} catch {
  console.warn('playwright not installed — Chromium-only smoke');
  await runChromiumOnly();
}

fs.writeFileSync(path.join(outDir, 'summary.json'), JSON.stringify(results, null, 2));
const failed = results.browsers.filter((b) => !b.ok);
console.log(`\nBrowser QA summary: ${path.join(outDir, 'summary.json')}`);
console.log(`Safari note: ${results.safariNote}`);
process.exit(failed.length ? 1 : 0);
