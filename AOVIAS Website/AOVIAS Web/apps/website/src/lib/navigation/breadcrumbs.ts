import type { BreadcrumbItem } from '@/lib/navigation/breadcrumb-types';
import { buildBreadcrumbListSchema } from '@/lib/seo/schema';

export type { BreadcrumbItem } from '@/lib/navigation/breadcrumb-types';

const SEGMENT_LABELS: Record<string, string> = {
  products: 'Products',
  pricing: 'Pricing',
  checkout: 'Checkout',
  billing: 'Billing',
  confirm: 'Confirm',
  success: 'Success',
  solutions: 'Solutions',
  resources: 'Resources',
  company: 'Company',
  legal: 'Trust & Governance',
  auth: 'Account',
  preview: 'Preview',
  studio: 'Studio',
  command: 'Command',
  runtime: 'Runtime',
  sentinel: 'Sentinel',
  nexus: 'Nexus',
  insight: 'Insight',
  flow: 'Flow',
  framework: 'Framework',
  healthcare: 'Healthcare',
  government: 'Government',
  education: 'Education',
  enterprise: 'Enterprise',
  manufacturing: 'Manufacturing',
  'financial-services': 'Financial Services',
  energy: 'Energy',
  transportation: 'Transportation',
  'public-sector': 'Public Sector',
  blog: 'Blog',
  insights: 'Insights',
  docs: 'Documentation',
  library: 'Library',
  categories: 'Categories',
  search: 'Search',
  guides: 'Guides',
  changelog: 'Changelog',
  careers: 'Careers',
  contact: 'Contact',
  technology: 'Technology',
  architecture: 'Architecture',
  platform: 'Platform',
  overview: 'Overview',
  services: 'Services',
  ecosystem: 'Ecosystem',
  ai: 'AI',
  principles: 'Principles',
  capabilities: 'Capabilities',
  'human-in-the-loop': 'Human-in-the-Loop',
  responsible: 'Responsible AI',
  engineering: 'Engineering',
  'design-principles': 'Design Principles',
  reliability: 'Reliability',
  scalability: 'Scalability',
  observability: 'Observability',
  trust: 'Trust',
  identity: 'Identity Platform',
  compliance: 'Compliance Readiness',
  'data-protection': 'Data Protection',
  integrations: 'Integrations',
  apis: 'Platform APIs',
  developer: 'Developer Platform',
  roadmap: 'Technology Roadmap',
  why: 'Why AOVIAS Technology',
  'sign-in': 'Sign in',
  'sign-up': 'Create account',
  'forgot-password': 'Forgot password',
  'reset-password': 'Reset password',
  'verify-email': 'Verify email',
  privacy: 'Privacy',
  terms: 'Terms',
  cookies: 'Cookies',
  accessibility: 'Accessibility',
  security: 'Security',
};

function titleCaseSegment(segment: string): string {
  return segment
    .split('-')
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ');
}

function labelForSegment(segment: string, pathname: string): string {
  if (segment === 'ai' && pathname === '/technology/ai') {
    return 'AI at AOVIAS';
  }
  if (segment === 'engineering' && pathname === '/technology/engineering') {
    return 'Engineering Philosophy';
  }
  if (segment === 'trust' && pathname === '/technology/trust') {
    return 'Security';
  }
  if (segment === 'privacy' && pathname.startsWith('/technology/trust/')) {
    return 'Privacy & Trust';
  }
  if (segment === 'roadmap' && pathname.startsWith('/technology/ai/')) {
    return 'Roadmap';
  }
  return SEGMENT_LABELS[segment] ?? titleCaseSegment(segment);
}

/**
 * Build breadcrumb items from a pathname.
 * Shape is JSON-LD friendly (`label` + optional `href`).
 */
export function buildBreadcrumbsFromPath(pathname: string): BreadcrumbItem[] {
  const clean = pathname.split('?')[0]?.split('#')[0] ?? '/';
  const segments = clean.split('/').filter(Boolean);

  if (segments.length === 0) {
    return [{ label: 'Home', href: '/', current: true }];
  }

  const items: BreadcrumbItem[] = [{ label: 'Home', href: '/' }];
  let accumulated = '';

  segments.forEach((segment, index) => {
    accumulated += `/${segment}`;
    const isLast = index === segments.length - 1;
    const crumb: BreadcrumbItem = {
      label: labelForSegment(segment, accumulated),
      current: isLast,
    };
    if (!isLast) {
      crumb.href = accumulated;
    }
    items.push(crumb);
  });

  return items;
}

/** BreadcrumbList JSON-LD — delegates to the SEO schema framework. */
export function breadcrumbsToJsonLd(items: readonly BreadcrumbItem[]) {
  return buildBreadcrumbListSchema(items);
}
