import type { Locator, Page, TestInfo } from '@playwright/test';

/**
 * Best-effort diagnostic collection.
 *
 * The governing principle, inherited from the server-log invariant: **diagnostics must explain a
 * failure even when the workflow cannot complete.** F1.5E proved the cost of ignoring it — a
 * diagnostic written to inventory the DOM after activation died on a click and produced nothing at
 * all, leaving ownership unknown after a full run.
 *
 * Every method here is failure-tolerant by construction. Nothing throws; every capture returns
 * either its data or a recorded reason for its absence. A collector that can only report on success
 * is not a collector.
 */

export interface InteractionRecord {
  readonly locator: string;
  readonly startedAt: number;
  readonly completedAt: number;
  readonly durationMs: number;
  readonly elementCount: number;
  readonly visible: boolean | null;
  readonly enabled: boolean | null;
  readonly stable: boolean | null;
  readonly outcome: 'succeeded' | 'failed';
  readonly failureReason?: string;
}

export interface LifecycleMark {
  readonly event: string;
  readonly at: number;
  readonly deltaMs: number;
}

export interface OverlayInventory {
  readonly dialogCount: number;
  readonly totalHospitalOptions: number;
  readonly totalJourneyOptions: number;
  readonly dialogs: readonly DialogSnapshot[];
}

export interface DialogSnapshot {
  readonly index: number;
  readonly visible: boolean;
  readonly rect: { w: number; h: number; x: number; y: number };
  readonly display: string;
  readonly visibility: string;
  readonly opacity: string;
  readonly zIndex: string;
  readonly ariaHidden: string | null;
  readonly cdkOverlayId: string | null;
  readonly hospitalOptions: number;
  readonly journeyOptions: number;
}

/** Every capture returns this shape, so an absent capture is still a recorded fact. */
type Captured<T> = { ok: true; value: T } | { ok: false; reason: string };

export class DiagnosticCollector {
  private readonly marks: LifecycleMark[] = [];
  private readonly interactions: InteractionRecord[] = [];
  private lastMarkAt: number | null = null;

  constructor(private readonly page: Page) {}

  // ── Lifecycle ───────────────────────────────────────────────────────────────────────────────

  captureLifecycle(event: string): void {
    const at = Date.now();
    this.marks.push({ event, at, deltaMs: this.lastMarkAt === null ? 0 : at - this.lastMarkAt });
    this.lastMarkAt = at;
  }

  get lifecycle(): readonly LifecycleMark[] {
    return this.marks;
  }

  get interactionLog(): readonly InteractionRecord[] {
    return this.interactions;
  }

  // ── Instrumented interaction ────────────────────────────────────────────────────────────────

  /**
   * Clicks a locator, recording its observable state **before** the attempt and the outcome after.
   *
   * State is read first so that a timeout still yields a full record — the failure reason alone
   * ("Timeout 20000ms exceeded") does not distinguish "element absent" from "element present but
   * never stable", which is precisely the distinction F1.5C and F1.5D turned on.
   */
  async instrumentedClick(locator: Locator, description: string): Promise<boolean> {
    const startedAt = Date.now();

    const elementCount = await locator.count().catch(() => -1);
    const visible = elementCount === 1 ? await locator.isVisible().catch(() => null) : null;
    const enabled = elementCount === 1 ? await locator.isEnabled().catch(() => null) : null;
    const stable = elementCount === 1 ? await this.isStable(locator) : null;

    try {
      await locator.click();
      this.record(description, startedAt, elementCount, visible, enabled, stable, 'succeeded');
      return true;
    } catch (error) {
      this.record(
        description,
        startedAt,
        elementCount,
        visible,
        enabled,
        stable,
        'failed',
        (error as Error).message.split('\n')[0],
      );
      return false;
    }
  }

  /** Two bounding-box samples; equality implies the element is not animating. */
  private async isStable(locator: Locator): Promise<boolean | null> {
    try {
      const first = await locator.boundingBox();
      await this.page.waitForTimeout(0); // yields a frame without being a timing dependency
      const second = await locator.boundingBox();
      if (!first || !second) return null;
      return first.x === second.x && first.y === second.y && first.width === second.width;
    } catch {
      return null;
    }
  }

  private record(
    locator: string,
    startedAt: number,
    elementCount: number,
    visible: boolean | null,
    enabled: boolean | null,
    stable: boolean | null,
    outcome: 'succeeded' | 'failed',
    failureReason?: string,
  ): void {
    const completedAt = Date.now();
    this.interactions.push({
      locator,
      startedAt,
      completedAt,
      durationMs: completedAt - startedAt,
      elementCount,
      visible,
      enabled,
      stable,
      outcome,
      ...(failureReason ? { failureReason } : {}),
    });
  }

  // ── Captures — none of these throw ──────────────────────────────────────────────────────────

  async captureStorage(): Promise<Captured<Record<string, string | null>>> {
    return this.safely('storage', () =>
      this.page.evaluate(() => ({
        access_token: localStorage.getItem('access_token') ? 'present' : null,
        refresh_token: localStorage.getItem('refresh_token') ? 'present' : null,
        hospital_id: localStorage.getItem('hospital_id'),
        selected_hospital_id: sessionStorage.getItem('selected_hospital_id'),
        selected_framework_id: sessionStorage.getItem('selected_framework_id'),
        selected_framework_journey_id: sessionStorage.getItem('selected_framework_journey_id'),
        url: location.href,
      })),
    );
  }

  async captureOverlayInventory(): Promise<Captured<OverlayInventory>> {
    return this.safely('overlay-inventory', () =>
      this.page.evaluate(() => {
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
              visible:
                rect.width > 0 && rect.height > 0 &&
                style.visibility !== 'hidden' && style.display !== 'none',
              rect: { w: Math.round(rect.width), h: Math.round(rect.height), x: Math.round(rect.x), y: Math.round(rect.y) },
              display: style.display,
              visibility: style.visibility,
              opacity: style.opacity,
              zIndex: style.zIndex,
              ariaHidden: el.closest('[aria-hidden]')?.getAttribute('aria-hidden') ?? null,
              cdkOverlayId: overlay?.id ?? null,
              hospitalOptions: el.querySelectorAll('[data-testid="context-hospital-option"]').length,
              journeyOptions: el.querySelectorAll('[data-testid="context-framework-option"]').length,
            };
          }),
        };
      }),
    );
  }

  /** Presence of the affordance chain's controls — answers "did it fail before the dialog existed?" */
  async captureAffordanceState(): Promise<Captured<Record<string, { count: number; visible: boolean }>>> {
    return this.safely('affordance-state', () =>
      this.page.evaluate(() => {
        const probe = (testId: string) => {
          const nodes = Array.from(document.querySelectorAll(`[data-testid="${testId}"]`));
          const visible = nodes.some(n => {
            const r = n.getBoundingClientRect();
            return r.width > 0 && r.height > 0;
          });
          return { count: nodes.length, visible };
        };
        return {
          'profile-menu-trigger': probe('profile-menu-trigger'),
          'workspace-activate': probe('workspace-activate'),
          'context-dialog': probe('context-dialog'),
          'context-confirm': probe('context-confirm'),
        };
      }),
    );
  }

  async captureDOM(maxChars = 400_000): Promise<Captured<string>> {
    return this.safely('dom', async () => (await this.page.content()).slice(0, maxChars));
  }

  async capturePlaywrightState(): Promise<Captured<{ url: string; title: string; closed: boolean }>> {
    return this.safely('playwright-state', async () => ({
      url: this.page.url(),
      title: await this.page.title().catch(() => '(unavailable)'),
      closed: this.page.isClosed(),
    }));
  }

  /**
   * Writes the whole package and attaches it to the report. Call from a `finally`, so it runs
   * whether the workflow completed or not.
   */
  async captureEvidence(testInfo: TestInfo, label = 'diagnostic'): Promise<void> {
    const bundle = {
      label,
      capturedAt: new Date().toISOString(),
      lifecycle: this.marks,
      interactions: this.interactions,
      affordance: await this.captureAffordanceState(),
      storage: await this.captureStorage(),
      overlay: await this.captureOverlayInventory(),
      playwright: await this.capturePlaywrightState(),
    };

    await testInfo
      .attach(`${label}.json`, { body: JSON.stringify(bundle, null, 2), contentType: 'application/json' })
      .catch(() => undefined);

    const dom = await this.captureDOM();
    if (dom.ok) {
      await testInfo.attach(`${label}-dom.html`, { body: dom.value, contentType: 'text/html' }).catch(() => undefined);
    }

    await this.page
      .screenshot({ fullPage: true })
      .then(body => testInfo.attach(`${label}.png`, { body, contentType: 'image/png' }))
      .catch(() => undefined);

    // Console-visible summary so a CI log explains the failure without opening artifacts.
    console.log(`\n===== ${label.toUpperCase()} =====`);
    console.log(JSON.stringify({ lifecycle: bundle.lifecycle, interactions: bundle.interactions, affordance: bundle.affordance, storage: bundle.storage, overlay: bundle.overlay }, null, 2));
    console.log('='.repeat(label.length + 12) + '\n');
  }

  private async safely<T>(name: string, fn: () => Promise<T>): Promise<Captured<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (error) {
      return { ok: false, reason: `${name} capture failed: ${(error as Error).message.split('\n')[0]}` };
    }
  }
}
