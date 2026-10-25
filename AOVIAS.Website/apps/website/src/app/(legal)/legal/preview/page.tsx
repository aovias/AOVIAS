import { PageHeader, Section } from '@/components/layout';
import { MarketingCtaLink } from '@/components/marketing';
import { ROUTES } from '@/constants/routes';

export default function LegalLayoutPreviewPage() {
  return (
    <Section spacing="sm">
      <PageHeader
        eyebrow="Layout preview"
        title="Legal layout"
        description="Narrow structural shell for Trust & Governance documents. Public content lives under /legal."
      />
      <MarketingCtaLink
        label="Open Trust & Governance"
        href={ROUTES.LEGAL}
        variant="primary"
      />
    </Section>
  );
}
