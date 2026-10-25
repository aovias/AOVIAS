/**
 * Careers domain — configuration-driven open roles.
 */

export type EmploymentType =
  | 'full-time'
  | 'part-time'
  | 'contract'
  | 'internship';

export type RoleStatus = 'open' | 'upcoming' | 'filled' | 'closed';

export type OpenRole = {
  id: string;
  slug: string;
  title: string;
  department: string;
  employmentType: EmploymentType;
  /** Human-readable location (e.g. Remote, Hybrid — Bengaluru). */
  location: string;
  status: RoleStatus;
  /** Short summary shown on the Careers page. */
  summary: string;
  /** Longer description for JobPosting schema readiness. */
  description: string;
  /** Apply destination (mailto or external URL). */
  applyHref: string;
  /** ISO-8601 date when the role was published. */
  publishedAt: string;
  /** Optional ISO country code for JobPosting. */
  addressCountry?: string;
  /** When true, JobPosting uses TELECOMMUTE. */
  remote?: boolean;
};
