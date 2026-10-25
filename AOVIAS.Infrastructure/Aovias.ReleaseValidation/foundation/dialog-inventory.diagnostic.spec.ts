import { test } from '../src/runtime.js';

/**
 * F1.5E — ownership diagnostic for duplicate `context-dialog` instances.
 *
 * Read-only. Performs the activation affordance chain, then inventories every matching element
 * without selecting anything. Establishes whether duplicates are attached-but-hidden (operator
 * scope bug) or genuinely visible (product defect) before any repair is attempted.
 */
const CREDENTIALS = {
  email: process.env['RV_USER'] ?? '',
  password: process.env['RV_PASSWORD'] ?? '',
};

test('F1.5E diagnostic: context-dialog DOM inventory', async ({ auth, page }) => {
  test.skip(!CREDENTIALS.email, 'RV_USER / RV_PASSWORD not provided');

  const marks: { event: string; at: number }[] = [];
  const mark = (event: string) => marks.push({ event, at: Date.now() });

  await auth.login(CREDENTIALS);
  mark('authenticated');

  await page.goto('/app/audit/findings', { waitUntil: 'domcontentloaded' });
  mark('navigated');

  // Inventory BEFORE invoking the affordance — proves whether the route alone renders a dialog.
  const beforeActivation = await inventory(page);

  const trigger = page.getByTestId('profile-menu-trigger');
  await trigger.waitFor({ state: 'visible' });
  await trigger.click();
  mark('profile-menu-opened');

  const activate = page.getByTestId('workspace-activate');
  await activate.waitFor({ state: 'visible' });
  await activate.click();
  mark('activate-clicked');

  await page.getByTestId('context-dialog').first().waitFor({ state: 'attached' });
  mark('dialog-attached');

  const afterActivation = await inventory(page);

  console.log('\n===== F1.5E DIALOG INVENTORY =====');
  console.log('BEFORE activation:', JSON.stringify(beforeActivation, null, 2));
  console.log('AFTER  activation:', JSON.stringify(afterActivation, null, 2));
  console.log('LIFECYCLE:', JSON.stringify(
    marks.map((m, i) => ({ ...m, deltaMs: i === 0 ? 0 : m.at - marks[i - 1]!.at })),
    null,
    2,
  ));
  console.log('==================================\n');
});

async function inventory(page: import('@playwright/test').Page) {
  return page.evaluate(() => {
    const dialogs = Array.from(document.querySelectorAll('[data-testid="context-dialog"]'));

    return {
      dialogCount: dialogs.length,
      totalHospitalOptions: document.querySelectorAll('[data-testid="context-hospital-option"]').length,
      totalJourneyOptions: document.querySelectorAll('[data-testid="context-framework-option"]').length,
      dialogs: dialogs.map((el, index) => {
        const rect = el.getBoundingClientRect();
        const style = getComputedStyle(el);
        const overlay = el.closest('.cdk-overlay-pane, [id^="cdk-overlay"]');
        return {
          index,
          visible: rect.width > 0 && rect.height > 0 && style.visibility !== 'hidden' && style.display !== 'none',
          rect: { w: Math.round(rect.width), h: Math.round(rect.height), x: Math.round(rect.x), y: Math.round(rect.y) },
          display: style.display,
          visibility: style.visibility,
          opacity: style.opacity,
          zIndex: style.zIndex,
          ariaHidden: el.closest('[aria-hidden]')?.getAttribute('aria-hidden') ?? null,
          parentTag: el.parentElement?.tagName ?? null,
          parentClass: el.parentElement?.className ?? null,
          cdkOverlayId: overlay?.id ?? null,
          cdkOverlayClass: overlay?.className ?? null,
          hospitalOptions: el.querySelectorAll('[data-testid="context-hospital-option"]').length,
          journeyOptions: el.querySelectorAll('[data-testid="context-framework-option"]').length,
        };
      }),
    };
  });
}
