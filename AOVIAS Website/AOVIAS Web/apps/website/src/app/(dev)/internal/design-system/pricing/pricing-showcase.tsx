'use client';

import { useMemo, useState } from 'react';

import {
  BillingToggle,
  EnterpriseCta,
  FeatureComparison,
  PlanSelector,
  PricingBanner,
  PricingFaq,
  PricingGrid,
  type BillingCycle,
  type Currency,
  type EnterpriseOffering,
  type Feature,
  type FeatureCategory,
  type PricingBannerMessage,
  type PricingFaqItem,
  type PricingPlan,
  type Product,
} from '@/components/pricing';
import {
  Heading,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';

const currencies: Currency[] = [
  { code: 'USD', symbol: '$', locale: 'en-US' },
];

const cycles: BillingCycle[] = [
  { id: 'monthly', label: 'Monthly', months: 1 },
  {
    id: 'annual',
    label: 'Annual',
    months: 12,
    discountLabel: 'Annual billing includes two months equivalent value.',
  },
];

const products: Product[] = [
  {
    id: 'command',
    name: 'Sample Product A',
    description: 'QA sample — not a real product catalog entry.',
    planIds: ['starter', 'growth', 'enterprise'],
  },
  {
    id: 'studio',
    name: 'Sample Product B',
    description: 'QA sample for multi-product selector expansion.',
    planIds: ['studio-std', 'studio-ent'],
  },
];

const plans: PricingPlan[] = [
  {
    id: 'starter',
    name: 'Starter',
    description: 'For teams evaluating institutional workflows.',
    badge: 'Popular entry',
    prices: [
      { cycleId: 'monthly', amount: 49, currencyCode: 'USD' },
      { cycleId: 'annual', amount: 470, currencyCode: 'USD' },
    ],
    highlights: ['Core workflows', 'Standard support', 'Workspace controls'],
    features: [
      { featureId: 'workflows', availability: 'included' },
      { featureId: 'sso', availability: 'excluded' },
      { featureId: 'audit', availability: 'limited', note: '30 days' },
      { featureId: 'support', availability: 'included' },
    ],
    cta: { label: 'Start inquiry', href: '#contact' },
  },
  {
    id: 'growth',
    name: 'Growth',
    description: 'For growing teams with stronger controls.',
    featured: true,
    badge: 'Featured',
    prices: [
      { cycleId: 'monthly', amount: 129, currencyCode: 'USD' },
      { cycleId: 'annual', amount: 1240, currencyCode: 'USD' },
    ],
    highlights: ['Everything in Starter', 'SSO options', 'Priority support'],
    features: [
      { featureId: 'workflows', availability: 'included' },
      { featureId: 'sso', availability: 'included' },
      { featureId: 'audit', availability: 'included' },
      { featureId: 'support', availability: 'included' },
    ],
    cta: { label: 'Talk to sales', href: '#contact' },
    secondaryCta: { label: 'Compare plans', href: '#comparison' },
  },
  {
    id: 'enterprise',
    name: 'Enterprise',
    description: 'Custom terms for complex institutions.',
    variant: 'enterprise',
    badge: 'Enterprise',
    prices: [
      {
        cycleId: 'monthly',
        amount: null,
        currencyCode: 'USD',
        displayOverride: 'Custom',
      },
      {
        cycleId: 'annual',
        amount: null,
        currencyCode: 'USD',
        displayOverride: 'Custom',
      },
    ],
    highlights: [
      'Dedicated success',
      'Advanced compliance options',
      'Custom licensing',
    ],
    features: [
      { featureId: 'workflows', availability: 'included' },
      { featureId: 'sso', availability: 'included' },
      { featureId: 'audit', availability: 'included' },
      { featureId: 'support', availability: 'contact', note: 'Dedicated' },
    ],
    cta: { label: 'Contact enterprise', href: '#enterprise' },
  },
  {
    id: 'studio-std',
    name: 'Standard',
    description: 'Sample plan for Product B.',
    prices: [
      { cycleId: 'monthly', amount: 79, currencyCode: 'USD' },
      { cycleId: 'annual', amount: 760, currencyCode: 'USD' },
    ],
    highlights: ['Creator seats', 'Shared libraries'],
    features: [
      { featureId: 'workflows', availability: 'included' },
      { featureId: 'sso', availability: 'excluded' },
      { featureId: 'audit', availability: 'excluded' },
      { featureId: 'support', availability: 'included' },
    ],
    cta: { label: 'Start inquiry', href: '#contact' },
  },
  {
    id: 'studio-ent',
    name: 'Enterprise',
    description: 'Sample enterprise plan for Product B.',
    variant: 'enterprise',
    prices: [
      {
        cycleId: 'monthly',
        amount: null,
        currencyCode: 'USD',
        displayOverride: 'Custom',
      },
      {
        cycleId: 'annual',
        amount: null,
        currencyCode: 'USD',
        displayOverride: 'Custom',
      },
    ],
    highlights: ['Org-wide licensing', 'Security review support'],
    features: [
      { featureId: 'workflows', availability: 'included' },
      { featureId: 'sso', availability: 'included' },
      { featureId: 'audit', availability: 'included' },
      { featureId: 'support', availability: 'contact' },
    ],
    cta: { label: 'Contact enterprise', href: '#enterprise' },
  },
];

const features: Feature[] = [
  {
    id: 'workflows',
    name: 'Core workflows',
    description: 'Sample capability row.',
    categoryId: 'platform',
  },
  {
    id: 'sso',
    name: 'SSO',
    categoryId: 'security',
  },
  {
    id: 'audit',
    name: 'Audit retention',
    categoryId: 'security',
  },
  {
    id: 'support',
    name: 'Support',
    categoryId: 'success',
  },
];

const categories: FeatureCategory[] = [
  { id: 'platform', name: 'Platform', order: 1 },
  { id: 'security', name: 'Security', order: 2 },
  { id: 'success', name: 'Success', order: 3 },
];

const faqItems: PricingFaqItem[] = [
  {
    id: 'billing',
    question: 'Can billing cycles change later?',
    answer: 'Yes. The BillingToggle is data-driven so new cycles can be added without UI rewrites.',
    category: 'Billing',
    keywords: ['annual', 'monthly'],
  },
  {
    id: 'enterprise',
    question: 'How does enterprise pricing work?',
    answer: 'Enterprise plans use contact pricing. Amounts stay null with a display override.',
    category: 'Enterprise',
  },
  {
    id: 'products',
    question: 'Can multiple products share this library?',
    answer: 'Yes. Product and PricingPlan models keep the UI product-agnostic.',
    category: 'Products',
  },
];

const banner: PricingBannerMessage = {
  id: 'notice',
  title: 'Sample notice',
  message: 'QA pricing figures are illustrative only and not commercial offers.',
  tone: 'info',
  href: '#faq',
  linkLabel: 'Read FAQ',
};

const enterprise: EnterpriseOffering = {
  id: 'ent',
  title: 'Need an enterprise agreement?',
  description:
    'Transparent evaluation with security review support — no pressure tactics.',
  primaryCta: { label: 'Contact sales', href: '#contact' },
  secondaryCta: { label: 'Request demo', href: '#demo' },
  tertiaryCta: { label: 'Book consultation', href: '#consult' },
};

function ShowcaseBlock({
  id,
  title,
  notes,
  children,
}: {
  id: string;
  title: string;
  notes?: string;
  children: React.ReactNode;
}) {
  return (
    <section id={id} className="border-border scroll-mt-20 border-b py-12">
      <Stack gap={6}>
        <Stack gap={2}>
          <Heading level={2} size="h2">
            {title}
          </Heading>
          {notes ? <Paragraph tone="muted">{notes}</Paragraph> : null}
        </Stack>
        {children}
      </Stack>
    </section>
  );
}

const nav = [
  { href: '#banner', label: 'Banner' },
  { href: '#selector', label: 'Plan selector' },
  { href: '#toggle', label: 'Billing toggle' },
  { href: '#grid', label: 'Pricing grid' },
  { href: '#comparison', label: 'Comparison' },
  { href: '#enterprise', label: 'Enterprise CTA' },
  { href: '#faq', label: 'FAQ' },
] as const;

export function PricingShowcase() {
  const [productId, setProductId] = useState(products[0]!.id);
  const [planId, setPlanId] = useState('growth');
  const [cycleId, setCycleId] = useState(cycles[1]!.id);

  const cycle = cycles.find((item) => item.id === cycleId) ?? cycles[0]!;
  const visiblePlans = useMemo(() => {
    const product = products.find((item) => item.id === productId);
    if (!product) return plans.slice(0, 3);
    return product.planIds
      .map((id) => plans.find((plan) => plan.id === id))
      .filter((plan): plan is PricingPlan => Boolean(plan));
  }, [productId]);

  return (
    <div>
      <nav
        aria-label="Pricing components"
        className="border-border mb-8 border-b pb-6"
      >
        <ul className="flex flex-wrap gap-x-4 gap-y-2">
          {nav.map((item) => (
            <li key={item.href}>
              <a
                href={item.href}
                className="text-muted-foreground hover:text-foreground text-small underline-offset-4 hover:underline"
              >
                {item.label}
              </a>
            </li>
          ))}
        </ul>
      </nav>

      <ShowcaseBlock
        id="banner"
        title="Pricing Banner"
        notes="Informational notices only — no countdown pressure patterns."
      >
        <PricingBanner message={banner} />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="selector"
        title="Plan Selector"
        notes="Product tabs + plan radios. Expandable for future products."
      >
        <PlanSelector
          products={products}
          plans={plans}
          productId={productId}
          planId={planId}
          onProductChange={(id) => {
            setProductId(id);
            const next = products.find((item) => item.id === id);
            if (next?.planIds[0]) setPlanId(next.planIds[0]);
          }}
          onPlanChange={setPlanId}
        />
        <Text as="p" size="caption" tone="muted">
          Active: {productId} / {planId}
        </Text>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="toggle"
        title="Billing Toggle"
        notes="Controlled cycle selection drives PricingGrid price resolution."
      >
        <BillingToggle
          cycles={cycles}
          value={cycleId}
          onChange={setCycleId}
        />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="grid"
        title="Pricing Grid + Cards"
        notes="Featured and enterprise variants. Sample amounts are QA-only."
      >
        <PricingGrid
          plans={visiblePlans}
          cycle={cycle}
          currencies={currencies}
          contained={false}
        />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="comparison"
        title="Feature Comparison"
        notes="Desktop table with categories; mobile stacked cards."
      >
        <FeatureComparison
          plans={visiblePlans}
          features={features}
          categories={categories}
        />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="enterprise"
        title="Enterprise CTA"
        notes="Contact, demo, and consultation actions from EnterpriseOffering."
      >
        <EnterpriseCta offering={enterprise} background="subtle" />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="faq"
        title="Pricing FAQ"
        notes="Composes Trust FAQ — searchable and categorized."
      >
        <PricingFaq items={faqItems} />
      </ShowcaseBlock>
    </div>
  );
}
