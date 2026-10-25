'use client';

import { FileCheck, Lock, Scale, Shield, Users } from 'lucide-react';
import type { ReactNode } from 'react';

import {
  CertificationsBlock,
  ComplianceGrid,
  CustomerLogoGrid,
  Faq,
  SecuritySection,
  SuccessMetrics,
  Testimonial,
  TrustBadge,
  TrustTimeline,
  type Certification,
  type ComplianceFramework,
  type CustomerReference,
  type FaqItem,
  type SecurityControl,
  type TestimonialReference,
  type TrustMetric,
  type TrustSignal,
  type TrustTimelineEvent,
} from '@/components/trust';
import {
  Caption,
  Heading,
  Paragraph,
  Section,
  Stack,
  Text,
} from '@/components/primitives';

function ShowcaseBlock({
  id,
  title,
  notes,
  children,
}: {
  id: string;
  title: string;
  notes: string;
  children: ReactNode;
}) {
  return (
    <Section id={id} spacing="lg" className="border-border border-b">
      <Stack gap={8}>
        <Stack gap={2}>
          <Heading level={2} size="h3">
            {title}
          </Heading>
          <Paragraph size="small" tone="muted">
            {notes}
          </Paragraph>
        </Stack>
        {children}
      </Stack>
    </Section>
  );
}

const signals: TrustSignal[] = [
  {
    id: 'b1',
    label: 'Security-first design',
    description: 'Sample signal — not a certification claim.',
    icon: <Shield />,
    variant: 'security',
  },
  {
    id: 'b2',
    label: 'Compliance-ready posture',
    description: 'Sample signal for layout QA.',
    icon: <Scale />,
    variant: 'compliance',
  },
  {
    id: 'b3',
    label: 'Quality practices',
    icon: <FileCheck />,
    variant: 'quality',
  },
];

const controls: SecurityControl[] = [
  {
    id: 'c1',
    category: 'Access',
    title: 'Access control',
    description: 'Sample security control description for composition QA.',
    icon: <Lock />,
  },
  {
    id: 'c2',
    category: 'Data',
    title: 'Data protection',
    description: 'Sample security control description for composition QA.',
    icon: <Shield />,
  },
  {
    id: 'c3',
    category: 'Ops',
    title: 'Operational assurance',
    description: 'Sample security control description for composition QA.',
    icon: <Users />,
  },
];

const frameworks: ComplianceFramework[] = [
  {
    id: 'f1',
    name: 'Framework A',
    description: 'Sample compliance framework entry.',
    icon: <Scale />,
    href: '/legal/preview',
  },
  {
    id: 'f2',
    name: 'Framework B',
    description: 'Sample compliance framework entry.',
    icon: <FileCheck />,
  },
  {
    id: 'f3',
    name: 'Framework C',
    description: 'Sample compliance framework entry.',
    icon: <Shield />,
  },
];

const certifications: Certification[] = [
  {
    id: 'cert1',
    title: 'Certification Alpha',
    description: 'Placeholder certification — status for UI only.',
    status: 'active',
  },
  {
    id: 'cert2',
    title: 'Certification Beta',
    description: 'Placeholder certification — in progress state.',
    status: 'in-progress',
  },
  {
    id: 'cert3',
    title: 'Certification Gamma',
    description: 'Placeholder certification — planned state.',
    status: 'planned',
  },
];

const customers: CustomerReference[] = [
  { id: 'cu1', name: 'Institution A' },
  { id: 'cu2', name: 'Institution B' },
  { id: 'cu3', name: 'Institution C' },
  { id: 'cu4', name: 'Institution D' },
];

const metrics: TrustMetric[] = [
  {
    id: 'm1',
    value: '—',
    label: 'Sample metric',
    description: 'Replace with verified figures.',
    trend: 'neutral',
    trendLabel: 'Steady',
  },
  {
    id: 'm2',
    value: '—',
    label: 'Sample metric',
    description: 'Replace with verified figures.',
    trend: 'up',
    trendLabel: 'Improved',
  },
  {
    id: 'm3',
    value: '—',
    label: 'Sample metric',
    description: 'Replace with verified figures.',
  },
];

const testimonials: TestimonialReference[] = [
  {
    id: 't1',
    quote: 'Sample quotation for attribution layout — not a customer endorsement.',
    author: 'Jordan Lee',
    role: 'Head of Risk',
    organization: 'Example Institution',
    avatarInitials: 'JL',
    rating: 5,
  },
];

const faqs: FaqItem[] = [
  {
    id: 'q1',
    category: 'Security',
    question: 'How should security questions be structured?',
    answer: 'Pass verified answers via FaqItem models — keep claims accurate and restrained.',
    keywords: ['security', 'controls'],
  },
  {
    id: 'q2',
    category: 'Security',
    question: 'Where do attestations live?',
    answer: 'In Certification and ComplianceFramework data, not inside component code.',
  },
  {
    id: 'q3',
    category: 'General',
    question: 'Can FAQ be searched?',
    answer: 'Yes — enable searchable and optionally supply keywords on each item.',
    keywords: ['search', 'faq'],
  },
];

const timeline: TrustTimelineEvent[] = [
  {
    id: 'e1',
    date: '2024',
    title: 'Foundation milestone',
    description: 'Sample trust timeline event.',
    milestone: true,
  },
  {
    id: 'e2',
    date: '2025',
    title: 'Assurance expansion',
    description: 'Sample trust timeline event.',
  },
  {
    id: 'e3',
    date: '2026',
    title: 'Institutional programs',
    description: 'Sample trust timeline event.',
  },
];

export function TrustShowcase() {
  return (
    <>
      <nav aria-label="Trust components" className="border-border mb-8 border-b pb-6">
        <Stack direction="horizontal" gap={3} wrap>
          {[
            'badges',
            'security',
            'compliance',
            'certifications',
            'logos',
            'metrics',
            'testimonial',
            'faq',
            'timeline',
          ].map((id) => (
            <a
              key={id}
              href={`#${id}`}
              className="text-muted-foreground hover:text-foreground text-small focus-visible:ring-ring rounded-sm underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:outline-none"
            >
              {id}
            </a>
          ))}
        </Stack>
      </nav>

      <ShowcaseBlock
        id="badges"
        title="Trust Badge"
        notes="Variants: default · security · compliance · quality · sizes sm/md/lg."
      >
        <Stack gap={4}>
          <Stack direction="horizontal" gap={3} wrap>
            {signals.map((signal) => (
              <TrustBadge key={signal.id} signal={signal} />
            ))}
          </Stack>
          <Stack direction="horizontal" gap={3} wrap align="center">
            <TrustBadge signal={signals[0]!} size="sm" />
            <TrustBadge signal={signals[0]!} size="md" />
            <TrustBadge signal={signals[0]!} size="lg" />
          </Stack>
        </Stack>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="security"
        title="Security Section"
        notes="Heading · highlights · SecurityControl cards · CTA."
      >
        <SecuritySection
          heading="Security posture"
          description="Sample security section copy for layout QA — supply verified language on real pages."
          highlights={[
            'Sample highlight one',
            'Sample highlight two',
            'Sample highlight three',
          ]}
          controls={controls}
          cta={{ label: 'View security overview', href: '/legal/preview' }}
          reveal={false}
        />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="compliance"
        title="Compliance Grid"
        notes="Consumes ComplianceFramework models."
      >
        <ComplianceGrid items={frameworks} />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="certifications"
        title="Certifications Block"
        notes="Logo placeholder · status badge · Certification model."
      >
        <CertificationsBlock items={certifications} />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="logos"
        title="Customer Logo Grid"
        notes="Maps CustomerReference → LogoCloud (theme-aware placeholders)."
      >
        <CustomerLogoGrid customers={customers} />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="metrics"
        title="Success Metrics"
        notes="TrustMetric → StatisticCard. Use verified figures only in production."
      >
        <SuccessMetrics metrics={metrics} />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="testimonial"
        title="Testimonial"
        notes="default / emphasized · optional rating · TestimonialReference model."
      >
        <Stack gap={6}>
          <Testimonial testimonial={testimonials[0]!} />
          <Testimonial testimonial={testimonials[0]!} variant="emphasized" />
        </Stack>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="faq"
        title="FAQ"
        notes="Accordion · categories · searchable filter over question/answer/keywords."
      >
        <Faq items={faqs} groupByCategory searchable allowMultiple />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="timeline"
        title="Trust Timeline"
        notes="Vertical and horizontal — TrustTimelineEvent models."
      >
        <Stack gap={8}>
          <TrustTimeline events={timeline} />
          <Caption>Horizontal</Caption>
          <TrustTimeline events={timeline} orientation="horizontal" />
        </Stack>
      </ShowcaseBlock>

      <Section spacing="lg">
        <Stack gap={2}>
          <Text size="small" weight="medium">
            Accessibility notes
          </Text>
          <Paragraph size="small" tone="muted">
            FAQ uses buttons with aria-expanded/controls and heading wrappers. Ratings expose
            text alternatives. Certification status uses visible Badge labels (not color alone).
            Prefer keyboard checks on FAQ and linked compliance cards.
          </Paragraph>
        </Stack>
      </Section>
    </>
  );
}
