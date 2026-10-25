import { OperationalMode } from '../models/nabh.enums';
import { ObjectiveElement } from '../models/objective-element.model';

export function filterOesByLifecycle(
  oes: readonly ObjectiveElement[],
  mode: OperationalMode,
): ObjectiveElement[] {
  const list = [...oes];
  switch (mode) {
    case OperationalMode.FinalAudit:
    case OperationalMode.ReAccreditation:
      return list.filter((o) => o.classification === 'core' || o.classification === 'commitment');
    case OperationalMode.Surveillance:
      return list.filter(
        (o) =>
          o.classification === 'core' ||
          o.classification === 'commitment' ||
          o.classification === 'achievement',
      );
    default:
      return list;
  }
}

/** Deterministic ordering by OE code (lexicographic; suffix letters sort predictably). */
export function sortOesByCode(oes: readonly ObjectiveElement[]): ObjectiveElement[] {
  return [...oes].sort((a, b) => a.code.localeCompare(b.code, undefined, { numeric: true }));
}
