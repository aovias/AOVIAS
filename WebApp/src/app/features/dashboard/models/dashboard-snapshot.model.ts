/**
 * Dashboard Program Overview Snapshot
 *
 * Endpoints:
 *   GET /dashboard/program-overview-snapshot                      — org-level overview
 *   GET /dashboard/{hospitalId}/program-overview-snapshot         — hospital-specific
 *
 * Query params (both routes):
 *   ?hospitalIdQuery={guid}   — alternative hospital filter (when not in route)
 *   ?frameworkId={guid}       — filter by framework
 *
 * Note: The API returns an ApiResponse<DashboardSnapshot> envelope.
 * The apiResponseInterceptor unwraps it, so services receive DashboardSnapshot directly.
 */

// ── Query Params ──────────────────────────────────────

/** Optional filters for the program-overview-snapshot endpoint */
export interface SnapshotQueryParams {
  /** Hospital ID — when provided, uses the route-based endpoint */
  hospitalId?: string | null;
  /** Framework ID — sent as query parameter */
  frameworkId?: string | null;
}

// ── Root ──────────────────────────────────────────────

export interface DashboardSnapshot {
  /** Compliance program overview snapshot (from ComplianceSnapshots read model). */
  isSnapshotAvailable?: boolean;
  /** True when rows come from ComplianceSnapshots table; false when scores were computed on read. */
  snapshotMaterialized?: boolean;
  overallScore?: number;
  auditConfidenceScore?: number;
  complianceJourneyId?: string | null;
  frameworkId?: string | null;
  calculatedAt?: string | null;

  tenantId?: string;
  organizationName?: string;
  generatedAt?: string;
  snapshotRange?: SnapshotRange;
  summary?: SnapshotSummary;
  frameworks?: FrameworkSnapshot[];
  hospitals?: HospitalSnapshot[];
  scoreTrends?: ScoreTrend[];
  tasksSummary?: TasksSummary;
  milestones?: MilestoneSnapshot[];
  alerts?: AlertSnapshot[];
  filters?: SnapshotFilters;
}

// ── Summary ───────────────────────────────────────────

export interface SnapshotRange {
  from: string;
  to: string;
}

export interface SnapshotSummary {
  totalHospitals: number;
  onboardedHospitals: number;
  totalJourneys: number;
  activeJourneys: number;
  upcomingExpiries: number;
  overdueTasks: number;
  /** Percentage 0 – 100 (canonical scale, matches avgComplianceScore) */
  completionRate: number;
  /** Score 0 – 100 */
  avgComplianceScore: number;
  frameworksActive: number;
}

// ── Frameworks ────────────────────────────────────────

export interface FrameworkSnapshot {
  frameworkCode: string;
  frameworkName: string;
  activeHospitals: number;
  journeys: number;
  avgScore: number;
  statusCounts: Record<string, number>;
  upcomingMilestones: MilestoneSnapshot[];
  recentActivity: ActivitySnapshot[];
}

export interface ActivitySnapshot {
  activityId: string;
  type: string;
  actor: string;
  createdAt: string;
  summary: string;
}

// ── Hospitals ─────────────────────────────────────────

export interface HospitalSnapshot {
  hospitalId: string;
  name: string;
  code: string;
  isOnboarded: boolean;
  nabhLevel: string | null;
  openTasks: number;
  overdueTasks: number;
  lastUpdated: string;
  journeys: JourneySnapshot[];
}

export interface JourneySnapshot {
  journeyId: string;
  frameworkCode: string;
  frameworkName: string;
  startDate: string | null;
  expiryDate: string | null;
  status: string;
  score: number;
  /** Percentage 0 – 100 (canonical scale, matches score) */
  progressPercent: number;
}

// ── Score Trends ──────────────────────────────────────

export interface ScoreTrend {
  date: string;
  avgScore: number;
  /** Map of frameworkCode → score */
  frameworks: Record<string, number>;
}

// ── Tasks ─────────────────────────────────────────────

export interface TasksSummary {
  total: number;
  open: number;
  overdue: number;
  dueSoon: number;
  byPriority: Record<string, number>;
  byType: Record<string, number>;
  byFramework: TasksByFramework[];
}

export interface TasksByFramework {
  frameworkCode: string;
  open: number;
  overdue: number;
}

// ── Milestones ────────────────────────────────────────

export interface MilestoneSnapshot {
  milestoneId: string;
  title: string;
  frameworkCode?: string;
  owner?: string;
  dueDate: string;
  status: string;
}

// ── Alerts ────────────────────────────────────────────

export interface AlertSnapshot {
  alertId: string;
  type: string;
  severity: string;
  message: string;
  entityType: string;
  entityId: string;
  createdAt: string;
  acknowledged: boolean;
}

// ── Filters ───────────────────────────────────────────

export interface SnapshotFilters {
  frameworks: string[];
  hospitals: FilterHospital[];
  dateRange: SnapshotRange;
}

export interface FilterHospital {
  hospitalId: string;
  name: string;
}
