import { MarketingCtaLink, SectionHeader } from '@/components/marketing';
import {
  ResourceGrid,
  TableOfContents,
} from '@/components/resources';
import { SecuritySection } from '@/components/trust';
import {
  Heading,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';
import {
  getLegalDocument,
  getRelatedLegalCards,
} from '@/features/legal/domain/documents';
import type { LegalDocumentId } from '@/features/legal/types';
import { formatLegalDate } from '@/features/legal/types';
import { ROUTES } from '@/constants/routes';

export type LegalDocumentPageProps = {
  documentId: LegalDocumentId;
};

/**
 * Shared legal document page — hero, version meta, TOC, sections, related, CTA.
 */
export function LegalDocumentPage({ documentId }: LegalDocumentPageProps) {
  const doc = getLegalDocument(documentId);
  const related = getRelatedLegalCards(doc);

  return (
    <article className="w-full" aria-labelledby="legal-document-title">
      <Stack gap={10}>
        <header className="max-w-prose">
          <Text
            as="p"
            size="caption"
            tone="muted"
            className="tracking-wide uppercase"
          >
            Trust & Governance
          </Text>
          <Heading
            id="legal-document-title"
            level={1}
            size="display-l"
            className="mt-2"
          >
            {doc.title}
          </Heading>
          <Paragraph size="body-lg" tone="muted" className="mt-4">
            {doc.description}
          </Paragraph>
          <Text as="p" size="small" tone="muted" className="mt-4">
            Last updated {formatLegalDate(doc.lastUpdated)} · Version{' '}
            {doc.version}
          </Text>
          <Text as="p" size="caption" tone="muted" className="mt-2">
            Public draft for counsel review — not legal advice and not
            counsel-approved.
          </Text>
        </header>

        <nav aria-label="Table of contents" className="max-w-sm">
          <TableOfContents items={doc.toc} sticky={false} title="On this page" />
        </nav>

        {documentId === 'security' && doc.securityControls ? (
          <SecuritySection
            heading="Security posture at a glance"
            description="High-level principles that guide how AOVIAS protects public services and institutional trust."
            {...(doc.securityHighlights
              ? { highlights: doc.securityHighlights }
              : {})}
            controls={doc.securityControls}
            reveal={false}
            className="!px-0 !py-0"
            cta={{
              label: 'Contact security',
              href: ROUTES.CONTACT,
            }}
          />
        ) : null}

        <div className="max-w-prose space-y-10">
          {doc.body.map((section) => (
            <section
              key={section.id}
              id={section.id}
              aria-labelledby={`${section.id}-heading`}
            >
              <Heading id={`${section.id}-heading`} level={2} size="h2">
                {section.heading}
              </Heading>
              <Stack gap={4} className="mt-4">
                {section.paragraphs.map((paragraph) => (
                  <Paragraph key={paragraph.slice(0, 64)} tone="muted">
                    {paragraph}
                  </Paragraph>
                ))}
                {section.bullets && section.bullets.length > 0 ? (
                  <ul className="text-muted-foreground list-disc space-y-2 ps-5">
                    {section.bullets.map((bullet) => (
                      <li key={bullet.slice(0, 64)} className="text-body">
                        {bullet}
                      </li>
                    ))}
                  </ul>
                ) : null}
              </Stack>
            </section>
          ))}
        </div>

        {related.length > 0 ? (
          <Stack gap={4}>
            <SectionHeader
              eyebrow="Related"
              heading="Related resources"
              description="Continue through the Trust & Governance Center."
            />
            <ResourceGrid
              items={related}
              cols={2}
              reveal={false}
              label="Related legal documents"
            />
          </Stack>
        ) : null}

        <div className="border-border max-w-prose space-y-4 border-t pt-8">
          <SectionHeader
            eyebrow="Questions"
            heading="Contact us"
            description="Reach the appropriate team for privacy, security, accessibility, or legal questions."
          />
          <div className="flex flex-wrap gap-3">
            <MarketingCtaLink
              label="Contact AOVIAS"
              href={ROUTES.CONTACT}
              variant="primary"
            />
            <MarketingCtaLink
              label="Trust & Governance hub"
              href={ROUTES.LEGAL}
              variant="outline"
            />
          </div>
        </div>
      </Stack>
    </article>
  );
}
