import { PageHeader, Section } from '@/components/layout';

export default function CompanyLayoutPreviewPage() {
  return (
    <Section spacing="sm">
      <PageHeader
        eyebrow="Layout preview"
        title="Company (marketing layout)"
        description="Company routes inherit MarketingLayout. Content is out of scope."
      />
    </Section>
  );
}
