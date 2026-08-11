import { ObjectiveElement } from '../models/objective-element.model';

export function naRequiresJustification(oe: Pick<ObjectiveElement, 'isNa' | 'naJustification'>): boolean {
  return oe.isNa && !(oe.naJustification?.trim());
}

export function coreOeBelowThreshold(oe: ObjectiveElement, minScore = 4): boolean {
  if (oe.isNa) return false;
  return oe.classification === 'core' && oe.score < minScore;
}
