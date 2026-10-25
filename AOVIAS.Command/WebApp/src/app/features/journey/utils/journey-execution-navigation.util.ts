import type { ObjectiveElement } from '../models/objective-element.model';

export interface JourneyOeNavigationContext {
  oeId: string;
  oeCode: string;
  chapterId?: string | null;
  standardId?: string | null;
}

/** Build deep-link query params shared by Evidence module (oe-wizard source). */
export function evidenceQueryForOe(ctx: JourneyOeNavigationContext): Record<string, string> {
  const q: Record<string, string> = {
    source: 'journey-execution',
    objectiveElementId: ctx.oeId,
    objectiveElementCode: ctx.oeCode,
  };
  if (ctx.chapterId) q['chapterId'] = ctx.chapterId;
  if (ctx.standardId) q['standardId'] = ctx.standardId;
  return q;
}

export function tasksQueryForOe(ctx: JourneyOeNavigationContext): Record<string, string> {
  return { search: ctx.oeCode };
}

/** Issues & Risks (deficiency command center) — search by OE code. */
export function deficienciesQueryForOe(ctx: JourneyOeNavigationContext): Record<string, string> {
  return { search: ctx.oeCode };
}

export function reviewQueueQueryForOe(ctx: JourneyOeNavigationContext): Record<string, string> {
  return {
    objectiveElementId: ctx.oeId,
    search: ctx.oeCode,
  };
}

export function oeNavigationFromStore(
  oeId: string,
  findOe: (id: string) => { oe: ObjectiveElement; chapter: { id: string }; standard: { id: string } } | null,
): JourneyOeNavigationContext | null {
  const found = findOe(oeId);
  if (!found) return null;
  return {
    oeId,
    oeCode: found.oe.code,
    chapterId: found.chapter.id,
    standardId: found.standard.id,
  };
}
