import type { BreadcrumbItem } from '@/lib/navigation/breadcrumb-types';

const base: BreadcrumbItem[] = [
  { label: 'Home', href: '/' },
  { label: 'Technology', href: '/technology' },
];

export const trustPagesSeo = {
  security: {
    title: 'Security',
    description:
      'How AOVIAS engineers security as a continuous platform practice — secure by design, operational discipline, and continuous improvement without fear-based messaging.',
    path: '/technology/trust',
    keywords: [
      'enterprise security',
      'secure platform',
      'security by design',
      'AOVIAS',
      'responsible technology',
    ],
  },
  privacy: {
    title: 'Privacy & Trust',
    description:
      'AOVIAS privacy and trust philosophy: transparency, privacy by design, responsible data stewardship, governance, and human responsibility — distinct from legal privacy policy.',
    path: '/technology/trust/privacy',
    keywords: [
      'privacy by design',
      'enterprise trust',
      'data stewardship',
      'AOVIAS',
    ],
  },
  identity: {
    title: 'Identity Platform',
    description:
      'Identity as a shared AOVIAS platform service: authentication, authorization, SSO readiness, and secure access management — conceptual, without vendors or protocols.',
    path: '/technology/trust/identity',
    keywords: [
      'identity platform',
      'enterprise identity',
      'secure access',
      'AOVIAS',
    ],
  },
  compliance: {
    title: 'Compliance Readiness',
    description:
      'How the AOVIAS platform supports regulated industries with auditability, traceability, and accountability — without claiming certifications. Compliance depends on customer implementation.',
    path: '/technology/trust/compliance',
    keywords: [
      'compliance readiness',
      'enterprise governance',
      'audit support',
      'AOVIAS',
    ],
  },
  dataProtection: {
    title: 'Data Protection',
    description:
      'Data protection principles for the AOVIAS platform: minimization, secure handling, encryption philosophy, lifecycle awareness, confidentiality, integrity, and availability.',
    path: '/technology/trust/data-protection',
    keywords: [
      'data protection',
      'enterprise security',
      'privacy by design',
      'AOVIAS',
    ],
  },
} as const;

export function trustSecurityBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Security', href: '/technology/trust', current: true },
  ];
}

export function trustPrivacyBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Trust', href: '/technology/trust' },
    { label: 'Privacy & Trust', href: '/technology/trust/privacy', current: true },
  ];
}

export function trustIdentityBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Trust', href: '/technology/trust' },
    { label: 'Identity Platform', href: '/technology/trust/identity', current: true },
  ];
}

export function trustComplianceBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Trust', href: '/technology/trust' },
    {
      label: 'Compliance Readiness',
      href: '/technology/trust/compliance',
      current: true,
    },
  ];
}

export function trustDataProtectionBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Trust', href: '/technology/trust' },
    {
      label: 'Data Protection',
      href: '/technology/trust/data-protection',
      current: true,
    },
  ];
}

export const trustSitemapPaths = [
  '/technology/trust',
  '/technology/trust/privacy',
  '/technology/trust/identity',
  '/technology/trust/compliance',
  '/technology/trust/data-protection',
] as const;
