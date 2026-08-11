import { NabhAssessmentPhase, OperationalMode } from '../models/nabh.enums';

/** Map operational mode to NABH assessment decision rules (6th ed.). */
export function mapLifecycleToNabhAssessmentPhase(mode: OperationalMode): NabhAssessmentPhase {
  if (mode === OperationalMode.Surveillance) return 'surveillance';
  if (mode === OperationalMode.ReAccreditation) return 're_accreditation';
  return 'final';
}
