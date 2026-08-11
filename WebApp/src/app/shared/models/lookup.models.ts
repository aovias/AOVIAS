export interface DepartmentLookup {
  id: string;
  name: string;
  code?: string;
  category?: string;
  displayOrder?: number | null;
}

/** Row from GET /frameworks/all-frameworks `data[]` (gateway envelope). */
export interface FrameworkLookup {
  /**
   * Stable server id (e.g. UUID). Not used for checkbox selection; use `code`.
   * Order: API `platformId` → `frameworkId` / legacy `FrameworkId`.
   */
  platformId: string;
  name: string;
  /**
   * Text for the card subtitle (`<p>`): prefer API `authority` / `Authority`;
   * if absent, `code` is used until the backend exposes authority (documented interim).
   */
  authority: string;
  /** Business code from API; used as checkbox value and persisted selection key. */
  code: string;
  /** Optional key from API for dynamic Lucide resolution (`iconKey`, `icon`, …). */
  iconKey?: string | null;
  isActive?: boolean;
}
