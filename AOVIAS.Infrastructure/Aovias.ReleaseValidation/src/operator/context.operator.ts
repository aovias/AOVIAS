import type { Locator, Page } from '@playwright/test';

/**
 * Compliance context selection.
 *
 * Implements TESTID-CONTRACT v2.0 against the domain model in ADR-014. The single fact that governs
 * this file: **the dialog presents a Framework and selects a Journey.** Rows are therefore
 * identified by `data-journey-id`, and the framework id is never read from the DOM — the product
 * derives it via `OrganizationService.getFrameworkIdForJourney()` and persists the result, which is
 * what this operator observes (Resolution Rule, contract §2.4).
 *
 * Public API is business-only: no dialog, no button, no click appears in a method name.
 */

/** Storage keys the application writes. Names are the product's; meanings are ADR-014's. */
const STORAGE = {
  hospital: 'hospital_id',
  framework: 'selected_framework_id',
  /** Hybrid product key name — holds a JOURNEY id (contract §5.4). */
  journey: 'selected_framework_journey_id',
} as const;

export interface ComplianceContext {
  readonly hospitalId: string | null;
  /** Derived by the product from the journey; never read from the DOM. */
  readonly frameworkId: string | null;
  readonly journeyId: string | null;
}

export interface ContextRequirement {
  readonly contextComplete: boolean;
  readonly dialogPresent: boolean;
  readonly hospitalSelectionRequired: boolean;
  readonly journeySelectionRequired: boolean;
}

export interface AvailableHospital {
  readonly hospitalId: string;
  readonly onboarded: boolean;
  readonly selected: boolean;
}

export interface AvailableJourney {
  readonly journeyId: string;
  readonly frameworkCode: string | null;
  readonly selected: boolean;
}

export class ContextOperator {
  constructor(private readonly page: Page) {}

  // ── Business API ────────────────────────────────────────────────────────────────────────────

  /**
   * Ensures a usable compliance context exists, choosing the first onboarded hospital and its first
   * journey. Idempotent: returns immediately when context is already complete.
   */
  async selectDefaultContext(): Promise<ComplianceContext> {
    const requirement = await this.discoverRequirement();
    if (requirement.contextComplete) return this.currentContext();

    if (!requirement.dialogPresent) {
      throw new Error(
        'Compliance context is incomplete but the selection dialog is not present. ' +
          'The dialog is raised by routes that require framework scope; navigate to such a route ' +
          'before selecting context. Current context: ' +
          JSON.stringify(await this.currentContext()),
      );
    }

    const hospitals = await this.availableHospitals();
    const hospital = hospitals.find(h => h.onboarded);
    if (!hospital) {
      throw new Error(
        `No onboarded hospital is selectable. Available: ${JSON.stringify(hospitals)}. ` +
          'A hospital that is not onboarded is rendered disabled by the product.',
      );
    }
    await this.selectHospital(hospital.hospitalId);

    const journeys = await this.availableJourneys();
    const journey = journeys[0];
    if (!journey) {
      const empty = await this.page.getByTestId('context-framework-empty').isVisible();
      throw new Error(
        empty
          ? `Hospital ${hospital.hospitalId} has no frameworks (product rendered context-framework-empty).`
          : `No journeys are selectable for hospital ${hospital.hospitalId}.`,
      );
    }
    await this.selectJourney(journey.journeyId);

    return this.confirm();
  }

  /** Selects a hospital by its domain identity. */
  async selectHospital(hospitalId: string): Promise<void> {
    const option = this.hospitalOption(hospitalId);
    await option.waitFor({ state: 'visible' });

    if ((await option.getAttribute('data-onboarded')) !== 'true') {
      throw new Error(`Hospital ${hospitalId} is not onboarded and cannot be selected.`);
    }

    await option.click();
    // Await the product's own state, not a duration.
    await option.and(this.page.locator('[data-selected="true"]')).waitFor({ state: 'visible' });
  }

  /**
   * Selects a journey by its domain identity.
   *
   * Named for what it selects. The surface calls this column "Framework"; the identity is the
   * journey's (ADR-014 Phase 1).
   */
  async selectJourney(journeyId: string): Promise<void> {
    const option = this.journeyOption(journeyId);
    await option.waitFor({ state: 'visible' });
    await option.click();
    await option.and(this.page.locator('[data-selected="true"]')).waitFor({ state: 'visible' });
  }

  /** Commits the selection and waits for the product to persist a complete context. */
  async confirm(): Promise<ComplianceContext> {
    const confirm = this.page.getByTestId('context-confirm');
    await confirm.waitFor({ state: 'visible' });
    await confirm.click();

    await this.page.getByTestId('context-dialog').waitFor({ state: 'hidden' });
    await this.waitUntilReady();
    return this.currentContext();
  }

  async cancel(): Promise<void> {
    await this.page.getByTestId('context-cancel').click();
    await this.page.getByTestId('context-dialog').waitFor({ state: 'hidden' });
  }

  /**
   * Blocks until the product has persisted a complete context.
   *
   * Waits on application state rather than elapsed time — the framework id appears only after the
   * product resolves it from the journey, which is asynchronous and has no DOM signal.
   */
  async waitUntilReady(): Promise<void> {
    await this.page.waitForFunction(
      (keys: typeof STORAGE) =>
        !!localStorage.getItem(keys.hospital) &&
        !!sessionStorage.getItem(keys.framework) &&
        !!sessionStorage.getItem(keys.journey),
      STORAGE,
    );
  }

  /** True when hospital, framework and journey are all persisted. */
  async verifyContext(): Promise<boolean> {
    const c = await this.currentContext();
    return !!c.hospitalId && !!c.frameworkId && !!c.journeyId;
  }

  // ── Readiness discovery ─────────────────────────────────────────────────────────────────────

  /** What, if anything, the operator must do. Inferred from application state, not from the route. */
  async discoverRequirement(): Promise<ContextRequirement> {
    const context = await this.currentContext();
    const contextComplete = !!context.hospitalId && !!context.frameworkId && !!context.journeyId;
    const dialogPresent = await this.page.getByTestId('context-dialog').isVisible().catch(() => false);

    if (!dialogPresent) {
      return {
        contextComplete,
        dialogPresent: false,
        hospitalSelectionRequired: false,
        journeySelectionRequired: false,
      };
    }

    const hospitals = await this.availableHospitals();
    const journeys = await this.availableJourneys();

    return {
      contextComplete,
      dialogPresent: true,
      hospitalSelectionRequired: !hospitals.some(h => h.selected),
      journeySelectionRequired: !journeys.some(j => j.selected),
    };
  }

  /** The context the application actually holds. */
  async currentContext(): Promise<ComplianceContext> {
    return this.page.evaluate(
      (keys: typeof STORAGE) => ({
        hospitalId: localStorage.getItem(keys.hospital),
        frameworkId: sessionStorage.getItem(keys.framework),
        journeyId: sessionStorage.getItem(keys.journey),
      }),
      STORAGE,
    );
  }

  async availableHospitals(): Promise<AvailableHospital[]> {
    return this.readOptions('context-hospital-option', 'data-hospital-id', el => ({
      hospitalId: el.id,
      onboarded: el.attrs['data-onboarded'] === 'true',
      selected: el.attrs['data-selected'] === 'true',
    }));
  }

  async availableJourneys(): Promise<AvailableJourney[]> {
    return this.readOptions('context-framework-option', 'data-journey-id', el => ({
      journeyId: el.id,
      frameworkCode: el.attrs['data-framework-code'] ?? null,
      selected: el.attrs['data-selected'] === 'true',
    }));
  }

  // ── Locators — testid plus companion attribute only ─────────────────────────────────────────

  private hospitalOption(hospitalId: string): Locator {
    return this.page.getByTestId('context-hospital-option').and(
      this.page.locator(`[data-hospital-id="${hospitalId}"]`),
    );
  }

  private journeyOption(journeyId: string): Locator {
    return this.page.getByTestId('context-framework-option').and(
      this.page.locator(`[data-journey-id="${journeyId}"]`),
    );
  }

  /** Reads every option of a kind with its companion attributes, in one page call. */
  private async readOptions<T>(
    testId: string,
    identityAttribute: string,
    project: (el: { id: string; attrs: Record<string, string> }) => T,
  ): Promise<T[]> {
    const raw = await this.page.evaluate(
      ({ testId, identityAttribute }: { testId: string; identityAttribute: string }) =>
        Array.from(document.querySelectorAll(`[data-testid="${testId}"]`)).map(node => {
          const attrs: Record<string, string> = {};
          for (const attr of Array.from(node.attributes)) {
            if (attr.name.startsWith('data-')) attrs[attr.name] = attr.value;
          }
          return { id: attrs[identityAttribute] ?? '', attrs };
        }),
      { testId, identityAttribute },
    );

    return raw.filter(el => el.id !== '').map(project);
  }
}
