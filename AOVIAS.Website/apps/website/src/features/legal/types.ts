import type { ContentItem, TocItem } from '@/components/content';
import type { SecurityControl } from '@/components/trust';

/**
 * Legal Document Framework — versioned public governance documents.
 * Content is informational for the website experience; not legal advice.
 */

export type LegalDocumentId =
  | 'privacy'
  | 'terms'
  | 'cookies'
  | 'accessibility'
  | 'security';

export type LegalBodySection = {
  id: string;
  heading: string;
  paragraphs: readonly string[];
  /** Optional unordered list under the section. */
  bullets?: readonly string[];
};

export type LegalDocumentMeta = {
  id: LegalDocumentId;
  slug: LegalDocumentId;
  title: string;
  description: string;
  path: string;
  /** Semver-style document version for future versioning. */
  version: string;
  /** ISO-8601 date (YYYY-MM-DD). */
  lastUpdated: string;
  keywords: readonly string[];
};

export type LegalDocument = LegalDocumentMeta & {
  body: readonly LegalBodySection[];
  toc: readonly TocItem[];
  /** Related document ids for ResourceGrid. */
  relatedIds: readonly LegalDocumentId[];
  /** Optional trust controls for Security Overview. */
  securityControls?: readonly SecurityControl[];
  securityHighlights?: readonly string[];
};

export type LegalDocumentCard = ContentItem;

export function buildTocFromBody(
  body: readonly LegalBodySection[],
): TocItem[] {
  return body.map((section) => ({
    id: section.id,
    title: section.heading,
    level: 2 as const,
    href: `#${section.id}`,
  }));
}

export function formatLegalDate(isoDate: string): string {
  const date = new Date(`${isoDate}T00:00:00.000Z`);
  if (Number.isNaN(date.getTime())) return isoDate;
  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    timeZone: 'UTC',
  }).format(date);
}
