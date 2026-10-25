import type { ReactNode } from 'react';

/**
 * Trust Data Model — structured entities for the Trust Component Library.
 * Update data, not UI, when certifications, controls, or references change.
 */

export type TrustBadgeVariant = 'default' | 'security' | 'compliance' | 'quality';
export type TrustBadgeSize = 'sm' | 'md' | 'lg';

/** Compact trust signal (badge row items). */
export type TrustSignal = {
  id: string;
  label: string;
  description?: string;
  icon?: ReactNode;
  variant?: TrustBadgeVariant;
};

export type SecurityControl = {
  id: string;
  title: string;
  description: string;
  icon?: ReactNode;
  category?: string;
};

export type ComplianceFramework = {
  id: string;
  name: string;
  description: string;
  icon?: ReactNode;
  href?: string;
};

export type CertificationStatus = 'active' | 'in-progress' | 'planned';

export type Certification = {
  id: string;
  title: string;
  description?: string;
  status?: CertificationStatus;
  /** Logo node; omit for placeholder label. */
  logo?: ReactNode;
  href?: string;
};

export type CustomerReference = {
  id: string;
  name: string;
  /** Logo mark for logo grids. */
  logo?: ReactNode;
  industry?: string;
  href?: string;
};

export type TrustMetric = {
  id: string;
  value: string;
  label: string;
  description?: string;
  trend?: 'up' | 'down' | 'neutral';
  trendLabel?: string;
};

export type TestimonialReference = {
  id: string;
  quote: string;
  author: string;
  role?: string;
  organization?: string;
  avatar?: ReactNode;
  avatarInitials?: string;
  /** Optional 1–5 rating for display. */
  rating?: 1 | 2 | 3 | 4 | 5;
};

export type FaqItem = {
  id: string;
  question: string;
  answer: string;
  category?: string;
  /** Free-text keywords for future search filtering. */
  keywords?: readonly string[];
};

export type TrustTimelineEvent = {
  id: string;
  date?: string;
  title: string;
  description?: string;
  /** Milestone emphasis for styling consumers. */
  milestone?: boolean;
};
