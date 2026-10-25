import type {
  BillingCycle,
  Currency,
  EnterpriseOffering,
  Feature,
  FeatureCategory,
  PricingBannerMessage,
  PricingFaqItem,
  PricingPlan,
  Product,
} from '@/components/pricing';

/**
 * AOVIAS Pricing catalog — configuration-driven commerce domain.
 * Update data here; Pricing UI remains in `@/components/pricing`.
 */

export const pricingCurrencies: readonly Currency[] = [
  { code: 'USD', symbol: '$', locale: 'en-US' },
] as const;

export const pricingBillingCycles: readonly BillingCycle[] = [
  { id: 'monthly', label: 'Monthly', months: 1 },
  {
    id: 'annual',
    label: 'Annual',
    months: 12,
    discountLabel: 'Annual billing aligns to yearly procurement calendars.',
  },
] as const;

export const pricingProducts: readonly Product[] = [
  {
    id: 'studio',
    name: 'AOVIAS Studio',
    description:
      'Subscription plans for teams designing and delivering institutional solutions with shared standards.',
    planIds: ['studio-standard', 'studio-professional', 'studio-enterprise'],
  },
  {
    id: 'command',
    name: 'AOVIAS Command',
    description:
      'Licensing for operational visibility and control — suited to healthcare and enterprise purchasing models.',
    planIds: ['command-professional', 'command-enterprise'],
  },
] as const;

export const pricingPlans: readonly PricingPlan[] = [
  {
    id: 'studio-standard',
    name: 'Evaluate',
    description: 'For teams validating Studio for institutional delivery practice.',
    badge: 'Evaluate',
    prices: [
      {
        cycleId: 'monthly',
        amount: null,
        currencyCode: 'USD',
        displayOverride: 'Contact us to get quote.',
      },
      {
        cycleId: 'annual',
        amount: null,
        currencyCode: 'USD',
        displayOverride: 'Contact us to get quote.',
      },
    ],
    highlights: [
      'Core delivery workspace',
      'Shared libraries',
      'Standard support',
    ],
    features: [
      { featureId: 'studio-workspace', availability: 'included' },
      { featureId: 'studio-libraries', availability: 'included' },
      { featureId: 'studio-collaboration', availability: 'limited', note: 'Team' },
      { featureId: 'sso', availability: 'excluded' },
      { featureId: 'audit', availability: 'limited', note: '90 days' },
      { featureId: 'support', availability: 'included' },
      { featureId: 'implementation', availability: 'excluded' },
      { featureId: 'dedicated-success', availability: 'excluded' },
    ],
    cta: {
      label: 'Start checkout',
      href: '/checkout?productId=studio&planId=studio-standard&cycleId=annual',
    },
    secondaryCta: {
      label: 'Request a demo',
      href: '/company/contact#form-demo',
    },
  },
  {
    id: 'studio-professional',
    name: 'Professional',
    description: 'For organizations scaling Studio across multiple delivery teams.',
    prices: [
      {
        cycleId: 'monthly',
        amount: null,
        currencyCode: 'USD',
        displayOverride: 'Contact us to get quote.',
      },
      {
        cycleId: 'annual',
        amount: null,
        currencyCode: 'USD',
        displayOverride: 'Contact us to get quote.',
      },
    ],
    highlights: [
      'Everything in Evaluate',
      'Broader collaboration',
      'SSO options',
      'Priority support',
    ],
    features: [
      { featureId: 'studio-workspace', availability: 'included' },
      { featureId: 'studio-libraries', availability: 'included' },
      { featureId: 'studio-collaboration', availability: 'included' },
      { featureId: 'sso', availability: 'included' },
      { featureId: 'audit', availability: 'included' },
      { featureId: 'support', availability: 'included' },
      { featureId: 'implementation', availability: 'addon', note: 'Available' },
      { featureId: 'dedicated-success', availability: 'excluded' },
    ],
    cta: {
      label: 'Start checkout',
      href: '/checkout?productId=studio&planId=studio-professional&cycleId=annual',
    },
    secondaryCta: {
      label: 'Request a demo',
      href: '/company/contact#form-demo',
    },
  },
  {
    id: 'studio-enterprise',
    name: 'Enterprise',
    description: 'Custom licensing, governance, and success for complex institutions.',
    variant: 'enterprise',
    badge: 'Enterprise',
    prices: [
      {
        cycleId: 'monthly',
        amount: null,
        currencyCode: 'USD',
        displayOverride: 'Contact us to get quote.',
      },
      {
        cycleId: 'annual',
        amount: null,
        currencyCode: 'USD',
        displayOverride: 'Contact us to get quote.',
      },
    ],
    highlights: [
      'Organization-wide licensing',
      'Advanced governance options',
      'Dedicated success',
      'Implementation services',
    ],
    features: [
      { featureId: 'studio-workspace', availability: 'included' },
      { featureId: 'studio-libraries', availability: 'included' },
      { featureId: 'studio-collaboration', availability: 'included' },
      { featureId: 'sso', availability: 'included' },
      { featureId: 'audit', availability: 'included' },
      { featureId: 'support', availability: 'contact', note: 'Dedicated' },
      { featureId: 'implementation', availability: 'included' },
      { featureId: 'dedicated-success', availability: 'included' },
    ],
    cta: {
      label: 'Contact enterprise',
      href: '/company/contact#form-enterprise',
    },
  },
  {
    id: 'command-professional',
    name: 'Professional',
    description:
      'Operational Command for institutions ready for structured visibility and control.',
    badge: 'Professional',
    prices: [
      {
        cycleId: 'monthly',
        amount: null,
        currencyCode: 'USD',
        displayOverride: 'Contact us to get quote.',
      },
      {
        cycleId: 'annual',
        amount: null,
        currencyCode: 'USD',
        displayOverride: 'Contact us to get quote.',
      },
    ],
    highlights: [
      'Operational visibility',
      'Structured control workflows',
      'Standard compliance tooling',
      'Priority support',
    ],
    features: [
      { featureId: 'command-visibility', availability: 'included' },
      { featureId: 'command-control', availability: 'included' },
      { featureId: 'command-integrations', availability: 'limited', note: 'Core' },
      { featureId: 'sso', availability: 'included' },
      { featureId: 'audit', availability: 'included' },
      { featureId: 'support', availability: 'included' },
      { featureId: 'implementation', availability: 'addon', note: 'Available' },
      { featureId: 'dedicated-success', availability: 'excluded' },
    ],
    cta: {
      label: 'Start checkout',
      href: '/checkout?productId=command&planId=command-professional&cycleId=annual',
    },
    secondaryCta: {
      label: 'Request a demo',
      href: '/company/contact#form-demo',
    },
  },
  {
    id: 'command-enterprise',
    name: 'Enterprise',
    description:
      'Custom enterprise licensing for healthcare and multi-site institutional deployments.',
    variant: 'enterprise',
    badge: 'Enterprise',
    prices: [
      {
        cycleId: 'monthly',
        amount: null,
        currencyCode: 'USD',
        displayOverride: 'Contact us to get quote.',
      },
      {
        cycleId: 'annual',
        amount: null,
        currencyCode: 'USD',
        displayOverride: 'Contact us to get quote.',
      },
    ],
    highlights: [
      'Volume and site licensing',
      'Dedicated support',
      'Implementation services',
      'Security and procurement review support',
    ],
    features: [
      { featureId: 'command-visibility', availability: 'included' },
      { featureId: 'command-control', availability: 'included' },
      { featureId: 'command-integrations', availability: 'included' },
      { featureId: 'sso', availability: 'included' },
      { featureId: 'audit', availability: 'included' },
      { featureId: 'support', availability: 'contact', note: 'Dedicated' },
      { featureId: 'implementation', availability: 'included' },
      { featureId: 'dedicated-success', availability: 'included' },
    ],
    cta: {
      label: 'Contact enterprise',
      href: '/company/contact#form-enterprise',
    },
  },
] as const;

export const pricingFeatures: readonly Feature[] = [
  {
    id: 'studio-workspace',
    name: 'Delivery workspace',
    description: 'Disciplined space to design and deliver solutions.',
    categoryId: 'studio',
  },
  {
    id: 'studio-libraries',
    name: 'Shared libraries',
    categoryId: 'studio',
  },
  {
    id: 'studio-collaboration',
    name: 'Team collaboration',
    categoryId: 'studio',
  },
  {
    id: 'command-visibility',
    name: 'Operational visibility',
    categoryId: 'command',
  },
  {
    id: 'command-control',
    name: 'Structured control',
    categoryId: 'command',
  },
  {
    id: 'command-integrations',
    name: 'Institutional integrations',
    categoryId: 'command',
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
  {
    id: 'implementation',
    name: 'Implementation services',
    categoryId: 'success',
  },
  {
    id: 'dedicated-success',
    name: 'Dedicated success',
    categoryId: 'success',
  },
] as const;

export const pricingFeatureCategories: readonly FeatureCategory[] = [
  { id: 'studio', name: 'Studio', order: 1 },
  { id: 'command', name: 'Command', order: 2 },
  { id: 'security', name: 'Security & governance', order: 3 },
  { id: 'success', name: 'Support & success', order: 4 },
] as const;

export const pricingBanner: PricingBannerMessage = {
  id: 'transparency',
  title: 'Transparent commercial guidance',
  message:
    'Published list prices are for evaluation. Enterprise agreements are scoped to volume, sites, and institutional requirements — without artificial urgency.',
  tone: 'info',
  href: '#pricing-faq',
  linkLabel: 'Read pricing FAQ',
};

export const enterpriseOffering: EnterpriseOffering = {
  id: 'enterprise-offering',
  title: 'Enterprise engagement',
  description:
    'Custom pricing, volume licensing, dedicated support, and implementation services — structured for long-term institutional partnership rather than short sales cycles.',
  primaryCta: {
    label: 'Contact enterprise',
    href: '/company/contact#form-enterprise',
  },
  secondaryCta: {
    label: 'Request a demo',
    href: '/company/contact#form-demo',
  },
  tertiaryCta: {
    label: 'Talk with AOVIAS',
    href: '/company/contact#form-contact',
  },
};

export const enterpriseCapabilities = [
  {
    id: 'custom-pricing',
    title: 'Custom pricing',
    description:
      'Agreements shaped to institutional scope, environments, and procurement cycles.',
  },
  {
    id: 'volume-licensing',
    title: 'Volume licensing',
    description:
      'Site, capacity, and organization-wide models when scale requires clarity.',
  },
  {
    id: 'dedicated-support',
    title: 'Dedicated support',
    description:
      'Named success and support pathways aligned to operational criticality.',
  },
  {
    id: 'implementation',
    title: 'Implementation services',
    description:
      'Structured onboarding and delivery support for responsible rollouts.',
  },
  {
    id: 'consultation',
    title: 'Enterprise consultation',
    description:
      'Architecture and licensing conversations before commitments are made.',
  },
] as const;

export const pricingFaqItems: readonly PricingFaqItem[] = [
  {
    id: 'licensing',
    question: 'How does AOVIAS licensing work?',
    answer:
      'Studio is offered primarily as subscription plans with published list prices. Command supports professional list pricing and enterprise licensing for institutional purchasing. Final terms reflect product, seats or capacity, and deployment scope.',
    category: 'Licensing',
    keywords: ['license', 'subscription', 'seats', 'list price'],
  },
  {
    id: 'list-prices',
    question: 'Are the prices on this page binding?',
    answer:
      'Published amounts are transparent list prices for evaluation. Checkout confirms plan and cycle for self-serve purchases. Enterprise agreements may differ when volume, sites, security review, or implementation scope require a tailored contract.',
    category: 'Licensing',
    keywords: ['list price', 'quote', 'binding', 'evaluation'],
  },
  {
    id: 'billing',
    question: 'What billing cycles are available?',
    answer:
      'Monthly and annual cycles are available for published plans. Annual billing aligns to yearly procurement calendars. Enterprise agreements may define custom invoicing.',
    category: 'Billing',
    keywords: ['monthly', 'annual', 'invoice'],
  },
  {
    id: 'support',
    question: 'What support is included?',
    answer:
      'Published plans include standard or priority support as listed on each plan. Enterprise engagements can include dedicated success pathways. Response expectations are confirmed in the agreement for that engagement — we do not invent SLAs on this page.',
    category: 'Support',
    keywords: ['support', 'success', 'priority'],
  },
  {
    id: 'implementation',
    question: 'How does implementation work?',
    answer:
      'Implementation services cover structured onboarding, configuration guidance, and rollout support when listed as included or available. Scope is agreed before work begins so institutions know what is delivery, what is advisory, and what remains internal responsibility.',
    category: 'Implementation',
    keywords: ['onboarding', 'rollout', 'services', 'implementation'],
  },
  {
    id: 'upgrades',
    question: 'Can we change plans later?',
    answer:
      'Yes. You can move to a higher published plan or an enterprise agreement as needs grow. Transitions are handled clearly — we prefer honest commercial conversations over pressure-driven upgrades.',
    category: 'Upgrades',
    keywords: ['upgrade', 'change plan', 'downgrade'],
  },
  {
    id: 'enterprise-purchasing',
    question: 'How do enterprise and healthcare purchasing work?',
    answer:
      'Enterprise engagement supports security reviews, volume licensing, multi-site deployments, and procurement documentation. Start with an enterprise inquiry or demo request. We respond with a practical next step based on your institutional context.',
    category: 'Enterprise',
    keywords: ['healthcare', 'procurement', 'volume', 'security review'],
  },
  {
    id: 'multi-product',
    question: 'Can we license Studio and Command together?',
    answer:
      'Yes. Many institutions evaluate both products. Published plans can be purchased separately; enterprise agreements can combine products when a single commercial relationship is clearer for procurement.',
    category: 'Licensing',
    keywords: ['studio', 'command', 'bundle', 'multi-product'],
  },
  {
    id: 'evaluation',
    question: 'How do we evaluate AOVIAS before purchasing?',
    answer:
      'Request a demo for a focused product walkthrough aligned to your stakeholders and scope. For complex environments, Contact enterprise to discuss evaluation structure, security review, and whether a guided pilot makes sense. We align fit before commercial commitment.',
    category: 'Evaluation',
    keywords: ['demo', 'evaluate', 'pilot', 'trial'],
  },
  {
    id: 'taxes',
    question: 'Do list prices include taxes?',
    answer:
      'Published list prices are shown exclusive of applicable taxes. Tax treatment depends on jurisdiction and purchase path, and is confirmed during checkout or in the enterprise agreement.',
    category: 'Billing',
    keywords: ['tax', 'VAT', 'GST'],
  },
] as const;

export function getPricingProduct(id: string): Product | undefined {
  return pricingProducts.find((product) => product.id === id);
}
