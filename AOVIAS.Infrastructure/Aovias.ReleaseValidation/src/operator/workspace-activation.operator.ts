import type { Locator, Page } from '@playwright/test';

/**
 * Workspace activation, implementing ADR-015.
 *
 * The lifecycle this operator serves:
 *
 *   Authentication → Identity Context → Workspace Activation → Operational Ready
 *
 * Authentication yields a hospital (a JWT claim, not a choice). It does not yield a workspace.
 * ADR-015 §3 establishes that nothing exits `Workspace Activation Required` on its own — F1.3A
 * proved the activation dialog has only user-initiated openers — so this operator invokes the same
 * affordance chain a human uses rather than waiting for the product to prompt.
 *
 * Replaces ContextOperator. Public API is business-only; no UI vocabulary escapes it.
 */

/** Storage keys the product writes. Names are the product's; meanings are ADR-015 §4's. */
const STORAGE = {
  /** Identity Context — localStorage, written by SessionService from the JWT claim. */
  hospital: 'hospital_id',
  /** Derived Context — resolved from the journey (ADR-014). */
  framework: 'selected_framework_id',
  /** Workspace Context — hybrid key name, holds a JOURNEY id. */
  journey: 'selected_framework_journey_id',
} as const;

/** Why a workspace is not operational. Ordered by lifecycle position. */
export type ActivationBlocker =
  | 'AuthenticationRequired'
  | 'IdentityContextMissing'
  | 'JourneyMissing'
  | 'FrameworkResolutionFailed';

/**
 * The lifecycle position of the workspace. A single discriminated value rather than scattered
 * booleans, so an impossible combination cannot be represented.
 */
export type WorkspaceActivationState =
  | { readonly stage: 'AuthenticationRequired' }
  | { readonly stage: 'WorkspaceActivationRequired'; readonly blockers: readonly ActivationBlocker[] }
  | { readonly stage: 'FrameworkResolutionFailed'; readonly journeyId: string }
  | { readonly stage: 'OperationalReady'; readonly context: OperationalContext };

export interface OperationalContext {
  readonly hospitalId: string;
  readonly journeyId: string;
  readonly frameworkId: string;
}

interface RawContext {
  readonly authenticated: boolean;
  readonly hospitalId: string | null;
  readonly journeyId: string | null;
  readonly frameworkId: string | null;
}

export class WorkspaceActivationOperator {
  constructor(private readonly page: Page) {}

  // ── Discovery ───────────────────────────────────────────────────────────────────────────────

  /**
   * Determines the lifecycle position from application state alone. Performs no UI interaction and
   * makes no change — an unactivated workspace is a legitimate state to report, not an error
   * (ADR-015 §5).
   */
  async discoverRequirement(): Promise<WorkspaceActivationState> {
    const raw = await this.readContext();

    if (!raw.authenticated) {
      return { stage: 'AuthenticationRequired' };
    }

    if (raw.hospitalId && raw.journeyId && raw.frameworkId) {
      return {
        stage: 'OperationalReady',
        context: { hospitalId: raw.hospitalId, journeyId: raw.journeyId, frameworkId: raw.frameworkId },
      };
    }

    // A journey without a framework is its own state: the product removed the framework rather
    // than substituting the journey id (ADR-014; app-context.service.ts:124). It is terminal for
    // that journey, not a transient step, so it must not be reported as "activation required".
    if (raw.journeyId && !raw.frameworkId) {
      return { stage: 'FrameworkResolutionFailed', journeyId: raw.journeyId };
    }

    const blockers: ActivationBlocker[] = [];
    if (!raw.hospitalId) blockers.push('IdentityContextMissing');
    if (!raw.journeyId) blockers.push('JourneyMissing');
    if (!raw.frameworkId) blockers.push('FrameworkResolutionFailed');

    return { stage: 'WorkspaceActivationRequired', blockers };
  }

  // ── Activation ──────────────────────────────────────────────────────────────────────────────

  /**
   * Activates a workspace through the documented affordance chain:
   *
   *   profile-menu-trigger → workspace-activate → context-dialog
   *     → hospital selection → journey selection → context-confirm
   *
   * Idempotent: returns immediately when already operational.
   */
  async activateWorkspace(): Promise<OperationalContext> {
    const requirement = await this.discoverRequirement();

    if (requirement.stage === 'OperationalReady') return requirement.context;

    if (requirement.stage === 'AuthenticationRequired') {
      throw new Error(
        'Cannot activate a workspace before authentication. ADR-015: Identity Context is ' +
          'established by SessionService from the JWT and is a precondition of activation.',
      );
    }

    if (requirement.stage === 'FrameworkResolutionFailed') {
      throw new Error(
        `Journey ${requirement.journeyId} is selected but resolves to no framework. ` +
          'The product removes the framework rather than substituting the journey id (ADR-014). ' +
          'Select a different journey, or omit framework-scoped operations.',
      );
    }

    await this.openActivationSurface();
    await this.selectFirstEligibleHospital();
    const journeyId = await this.selectFirstAvailableJourney();
    await this.confirm();

    const state = await this.discoverRequirement();
    if (state.stage !== 'OperationalReady') {
      throw new Error(
        `Activation completed but the workspace is not operational (journey ${journeyId}). ` +
          `State: ${JSON.stringify(state)}`,
      );
    }
    return state.context;
  }

  // ── Verification ────────────────────────────────────────────────────────────────────────────

  /**
   * Asserts the ADR-015 §3 invariant. Reports the specific member that failed, never a generic
   * failure — RC2's expensive defects were all diagnosis problems, not detection problems.
   */
  async verifyOperationalReadiness(): Promise<OperationalContext> {
    const raw = await this.readContext();
    const missing: string[] = [];

    if (!raw.authenticated) missing.push('authentication (no access token)');
    if (!raw.hospitalId) missing.push(`Identity Context (${STORAGE.hospital})`);
    if (!raw.journeyId) missing.push(`Workspace journey (${STORAGE.journey})`);
    if (!raw.frameworkId) missing.push(`Derived framework (${STORAGE.framework})`);

    if (missing.length > 0) {
      throw new Error(
        `Workspace is not operational. Missing: ${missing.join(', ')}. ` +
          `Observed: ${JSON.stringify(raw)}`,
      );
    }

    // Derivation must not have regressed to substitution. ADR-014 Phase 2 item 1: a field named
    // frameworkId once carried a journey id, which "caused every framework-filtered audit query to
    // silently match nothing". This is the guard against that returning.
    if (raw.frameworkId === raw.journeyId) {
      throw new Error(
        `Framework id equals journey id (${raw.frameworkId}). Derivation has regressed to ` +
          'substitution — see ADR-014. The framework must be resolved via ' +
          'OrganizationService.getFrameworkIdForJourney(), never copied from the journey.',
      );
    }

    return {
      hospitalId: raw.hospitalId!,
      journeyId: raw.journeyId!,
      frameworkId: raw.frameworkId!,
    };
  }

  // ── Affordance chain ────────────────────────────────────────────────────────────────────────

  /** profile-menu-trigger → workspace-activate → context-dialog visible. */
  private async openActivationSurface(): Promise<void> {
    const dialog = this.page.getByTestId('context-dialog');
    if (await dialog.isVisible().catch(() => false)) return;

    const trigger = this.page.getByTestId('profile-menu-trigger');
    await trigger.waitFor({ state: 'visible' });
    await trigger.click();

    const activate = this.page.getByTestId('workspace-activate');
    await activate.waitFor({ state: 'visible' });
    await activate.click();

    await dialog.waitFor({ state: 'visible' });
  }

  private async selectFirstEligibleHospital(): Promise<string> {
    const options = await this.readOptions('context-hospital-option', 'data-hospital-id');
    const eligible = options.find(o => o.attrs['data-onboarded'] === 'true');

    if (!eligible) {
      throw new Error(
        `No onboarded hospital is selectable. Observed: ${JSON.stringify(options.map(o => o.id))}. ` +
          'Hospitals that are not onboarded are rendered disabled by the product.',
      );
    }

    await this.selectIfNotAlready('context-hospital-option', 'data-hospital-id', eligible);
    return eligible.id;
  }

  private async selectFirstAvailableJourney(): Promise<string> {
    const options = await this.readOptions('context-framework-option', 'data-journey-id');
    const first = options[0];

    if (!first) {
      const empty = await this.page.getByTestId('context-framework-empty').isVisible().catch(() => false);
      throw new Error(
        empty
          ? 'The selected hospital has no framework enrolments (product rendered context-framework-empty).'
          : 'No journeys are selectable for the selected hospital.',
      );
    }

    await this.selectIfNotAlready('context-framework-option', 'data-journey-id', first);
    return first.id;
  }

  /**
   * Discover → verify → act only if required → verify again.
   *
   * An operator never changes state it has already observed to be correct. Beyond being
   * semantically right, this removes a real failure: the hospital arrives pre-selected, and clicking
   * it made Playwright wait for element stability that the dialog's entry animation never granted —
   * five identical 20s timeouts in F1.5C, one step from a working activation.
   *
   * The same shape applies to every stateful control this platform will drive: checkboxes, toggles,
   * dropdowns, approval and workflow actions.
   */
  private async selectIfNotAlready(
    testId: string,
    identityAttribute: string,
    option: { id: string; attrs: Record<string, string> },
  ): Promise<void> {
    const locator = this.optionByIdentity(testId, identityAttribute, option.id);

    if (option.attrs['data-selected'] === 'true') {
      // Already correct — verify and continue, never re-assert.
      await locator.waitFor({ state: 'attached' });
      return;
    }

    await locator.click();
    await locator.and(this.page.locator('[data-selected="true"]')).waitFor({ state: 'attached' });
  }

  /** Commits, then waits on application state — never on elapsed time. */
  private async confirm(): Promise<void> {
    const confirm = this.page.getByTestId('context-confirm');
    await confirm.waitFor({ state: 'visible' });
    await confirm.click();

    await this.page.getByTestId('context-dialog').waitFor({ state: 'hidden' });

    // The framework appears only after the product derives it from the journey — an asynchronous
    // step with no DOM signal, so the wait is on the product's own persisted state.
    await this.page.waitForFunction(
      (keys: typeof STORAGE) =>
        !!localStorage.getItem(keys.hospital) &&
        !!sessionStorage.getItem(keys.journey) &&
        !!sessionStorage.getItem(keys.framework),
      STORAGE,
    );
  }

  // ── State access ────────────────────────────────────────────────────────────────────────────

  private async readContext(): Promise<RawContext> {
    return this.page.evaluate((keys: typeof STORAGE) => ({
      authenticated: !!localStorage.getItem('access_token'),
      hospitalId: localStorage.getItem(keys.hospital),
      journeyId: sessionStorage.getItem(keys.journey),
      frameworkId: sessionStorage.getItem(keys.framework),
    }), STORAGE);
  }

  private optionByIdentity(testId: string, identityAttribute: string, value: string): Locator {
    return this.page
      .getByTestId(testId)
      .and(this.page.locator(`[${identityAttribute}="${value}"]`));
  }

  private async readOptions(
    testId: string,
    identityAttribute: string,
  ): Promise<{ id: string; attrs: Record<string, string> }[]> {
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
    return raw.filter(o => o.id !== '');
  }
}
