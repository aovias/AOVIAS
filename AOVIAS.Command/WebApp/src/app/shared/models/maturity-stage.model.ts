/**
 * Authoritative maturity stage state from StageUnlockingService.
 * Shared by Journey and Compliance Maturity surfaces (Sprint 1 — H2).
 */

export type MaturityStageId = 'core' | 'commitment' | 'achievement' | 'excellence';

export type MaturityStageStatus = 'completed' | 'inProgress' | 'available' | 'locked';

export interface MaturityStageStateModel {
  stage: MaturityStageId;
  label: string;
  score: number;
  status: MaturityStageStatus;
  gapCount?: number;
  unlockThreshold: number;
  unlockMessage?: string;
  progressPercent?: number;
  readinessPercent?: number;
  unlockReadinessPercent?: number;
  blockingReasons?: string[];
  recommendations?: string[];
  nextMilestone?: string | null;
}

export interface MaturityStageStateApiDto {
  stage?: string | null;
  label?: string | null;
  score?: number | null;
  status?: string | null;
  gapCount?: number | null;
  unlockThreshold?: number | null;
  unlockMessage?: string | null;
  progressPercent?: number | null;
  readinessPercent?: number | null;
  unlockReadinessPercent?: number | null;
  blockingReasons?: string[] | null;
  recommendations?: string[] | null;
  nextMilestone?: string | null;
}

const VALID_STAGES = new Set<MaturityStageId>(['core', 'commitment', 'achievement', 'excellence']);

export function normMaturityStageId(raw: string | null | undefined): MaturityStageId | null {
  const v = (raw ?? '').toLowerCase() as MaturityStageId;
  return VALID_STAGES.has(v) ? v : null;
}

export function normMaturityStageStatus(raw: string | null | undefined): MaturityStageStatus {
  const v = (raw ?? '').toLowerCase();
  if (v === 'completed') return 'completed';
  if (v === 'inprogress') return 'inProgress';
  if (v === 'available') return 'available';
  return 'locked';
}

export function mapMaturityStageState(dto: MaturityStageStateApiDto): MaturityStageStateModel | null {
  const stage = normMaturityStageId(dto.stage ?? null);
  if (!stage) return null;
  return {
    stage,
    label: dto.label ?? stage,
    score: dto.score ?? 0,
    status: normMaturityStageStatus(dto.status),
    gapCount: dto.gapCount ?? undefined,
    unlockThreshold: dto.unlockThreshold ?? 0,
    unlockMessage: dto.unlockMessage ?? undefined,
    progressPercent: dto.progressPercent ?? undefined,
    readinessPercent: dto.readinessPercent ?? undefined,
    unlockReadinessPercent: dto.unlockReadinessPercent ?? undefined,
    blockingReasons: dto.blockingReasons ?? undefined,
    recommendations: dto.recommendations ?? undefined,
    nextMilestone: dto.nextMilestone ?? undefined,
  };
}

export function maturityStageStatusLabel(status: MaturityStageStatus): string {
  switch (status) {
    case 'completed':
      return 'Complete';
    case 'inProgress':
      return 'In Progress';
    case 'available':
      return 'Available';
    default:
      return 'Locked';
  }
}

export function isMaturityStageAccessible(status: MaturityStageStatus): boolean {
  return status === 'completed' || status === 'inProgress' || status === 'available';
}
