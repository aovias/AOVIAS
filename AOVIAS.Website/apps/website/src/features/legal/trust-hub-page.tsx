import { MarketingCtaLink, SectionHeader } from '@/components/marketing';
import { ResourceGrid } from '@/components/resources';
import { TrustBadge } from '@/components/trust';
import { Heading, Paragraph, Stack, Text } from '@/components/primitives';
import { getLegalHubCards } from '@/features/legal/domain/documents';
import { ROUTES } from '@/constants/routes';

/**
 * Trust & Governance Center hub — entry to all legal documents.
 */
export function TrustHubPage() {
  const cards = getLegalHubCards();

  return (
    <Stack gap={10}>
      <header className="max-w-prose">
        <Text
          as="p"
          size="caption"
          tone="muted"
          className="tracking-wide uppercase"
        >
          AOVIAS
        </Text>
        <Heading level={1} size="display-l" className="mt-2">
          Trust & Governance
        </Heading>
        <Paragraph size="body-lg" tone="muted" className="mt-4">
          Public drafts on privacy, terms, cookies, accessibility, and security —
          written for clarity and institutional confidence. Documents are for
          counsel review; they are not legal advice and not counsel-approved.
        </Paragraph>
      </header>

      <ul className="flex flex-wrap gap-2" aria-label="Trust signals">
        <li>
          <TrustBadge
            size="sm"
            signal={{
              id: 'privacy',
              label: 'Privacy',
              variant: 'compliance',
            }}
          />
        </li>
        <li>
          <TrustBadge
            size="sm"
            signal={{
              id: 'accessibility',
              label: 'Accessibility',
              variant: 'quality',
            }}
          />
        </li>
        <li>
          <TrustBadge
            size="sm"
            signal={{
              id: 'security',
              label: 'Security',
              variant: 'security',
            }}
          />
        </li>
        <li>
          <TrustBadge
            size="sm"
            signal={{
              id: 'transparency',
              label: 'Transparency',
              variant: 'default',
            }}
          />
        </li>
      </ul>

      <Stack gap={4}>
        <SectionHeader
          eyebrow="Documents"
          heading="Governance documents"
          description="Each document includes a version, last-updated date, and clear sections. Open a document to read the full draft."
        />
        <ResourceGrid
          items={cards}
          cols={2}
          reveal={false}
          label="Trust and governance documents"
        />
      </Stack>

      <div className="border-border max-w-prose space-y-4 border-t pt-8">
        <SectionHeader
          eyebrow="Engage"
          heading="Need to talk to a person?"
          description="Privacy, security, accessibility, and enterprise questions are welcome through Contact."
        />
        <MarketingCtaLink
          label="Contact AOVIAS"
          href={ROUTES.CONTACT}
          variant="primary"
        />
      </div>
    </Stack>
  );
}
