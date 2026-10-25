import type { Attributes } from '../core/attributes';
import type { EventName } from './event-names';
import { getEventDefinition } from './catalog';

export type EventValidationResult = {
  ok: boolean;
  missing: readonly string[];
};

/**
 * Validate required attributes against the event registry (no `any`).
 */
export function validateEventAttributes(
  name: EventName,
  attributes: Attributes,
): EventValidationResult {
  const definition = getEventDefinition(name);
  const missing = definition.requiredAttributes.filter((key) => {
    const value = attributes[key];
    return value === undefined || value === null || value === '';
  });

  return { ok: missing.length === 0, missing };
}
