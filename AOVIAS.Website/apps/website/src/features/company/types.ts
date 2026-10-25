/**
 * Shared metadata for Company content modules.
 * Modules own copy and composition; the page only orchestrates order.
 */
export type CompanyModuleMeta = {
  id: string;
  /** Anchor-friendly section id. */
  sectionId: string;
  title: string;
};
