import { expect } from '@playwright/test';
import type { ContextOperator } from '../operator/context.operator.js';

/**
 * Context assertions, stated in domain terms.
 *
 * `expectFrameworkResolved` is the one that matters most: it proves the product performed the
 * journey → framework derivation described in ADR-014 rather than the automation substituting one id
 * for the other. A framework id that is present but equal to the journey id is the exact defect the
 * ADR exists to prevent, so it is asserted explicitly.
 */

export async function expectContextReady(context: ContextOperator): Promise<void> {
  const current = await context.currentContext();
  expect(
    await context.verifyContext(),
    `Compliance context is incomplete: ${JSON.stringify(current)}`,
  ).toBe(true);
}

export async function expectHospitalSelected(
  context: ContextOperator,
  hospitalId?: string,
): Promise<void> {
  const current = await context.currentContext();
  expect(current.hospitalId, 'No hospital selected').toBeTruthy();
  if (hospitalId) {
    expect(current.hospitalId, 'A different hospital is selected').toBe(hospitalId);
  }
}

export async function expectJourneySelected(
  context: ContextOperator,
  journeyId?: string,
): Promise<void> {
  const current = await context.currentContext();
  expect(current.journeyId, 'No journey selected').toBeTruthy();
  if (journeyId) {
    expect(current.journeyId, 'A different journey is selected').toBe(journeyId);
  }
}

/**
 * The framework id must exist and must differ from the journey id.
 *
 * ADR-014 Phase 2 item 1: a field named `frameworkId` once carried a journey id, which "caused every
 * framework-filtered audit query to silently match nothing". If derivation ever regresses to
 * substitution, this assertion is what catches it.
 */
export async function expectFrameworkResolved(context: ContextOperator): Promise<void> {
  const current = await context.currentContext();

  expect(
    current.frameworkId,
    'No framework id was resolved. The product derives it from the journey via ' +
      'OrganizationService.getFrameworkIdForJourney(); it is never present in the DOM.',
  ).toBeTruthy();

  expect(
    current.frameworkId,
    'The framework id equals the journey id — derivation has regressed to substitution (ADR-014).',
  ).not.toBe(current.journeyId);
}

/** Context survives a reload of the same tab. */
export async function expectContextPersisted(
  context: ContextOperator,
  reload: () => Promise<void>,
): Promise<void> {
  const before = await context.currentContext();
  await reload();
  const after = await context.currentContext();

  expect(after.hospitalId, 'Hospital did not survive reload').toBe(before.hospitalId);
  expect(after.frameworkId, 'Framework did not survive reload').toBe(before.frameworkId);
  expect(after.journeyId, 'Journey did not survive reload').toBe(before.journeyId);
}
