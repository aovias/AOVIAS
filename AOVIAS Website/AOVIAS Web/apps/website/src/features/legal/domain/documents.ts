import type {
  LegalDocument,
  LegalDocumentCard,
  LegalDocumentId,
  LegalDocumentMeta,
} from '@/features/legal/types';
import { buildTocFromBody } from '@/features/legal/types';
import { accessibilityBody } from '@/features/legal/domain/bodies/accessibility';
import { cookiesBody } from '@/features/legal/domain/bodies/cookies';
import { privacyBody } from '@/features/legal/domain/bodies/privacy';
import { securityBody } from '@/features/legal/domain/bodies/security';
import { termsBody } from '@/features/legal/domain/bodies/terms';

const LAST_UPDATED = '2026-07-20';

const metas: Record<LegalDocumentId, LegalDocumentMeta> = {
  privacy: {
    id: 'privacy',
    slug: 'privacy',
    title: 'Privacy Policy',
    description:
      'How AOVIAS collects, uses, and protects personal information on the public website and related public services. Public draft for counsel review.',
    path: '/legal/privacy',
    version: '1.0.0',
    lastUpdated: LAST_UPDATED,
    keywords: [
      'AOVIAS privacy',
      'privacy policy',
      'data protection',
      'personal information',
    ],
  },
  terms: {
    id: 'terms',
    slug: 'terms',
    title: 'Terms of Service',
    description:
      'Conditions for using the AOVIAS public website and related public services, including acceptable use and liability. Public draft for counsel review.',
    path: '/legal/terms',
    version: '1.0.0',
    lastUpdated: LAST_UPDATED,
    keywords: [
      'AOVIAS terms',
      'terms of service',
      'acceptable use',
      'website terms',
    ],
  },
  cookies: {
    id: 'cookies',
    slug: 'cookies',
    title: 'Cookie Policy',
    description:
      'How AOVIAS uses cookies and similar technologies on the public website, including categories and controls. Public draft for counsel review.',
    path: '/legal/cookies',
    version: '1.0.0',
    lastUpdated: LAST_UPDATED,
    keywords: [
      'AOVIAS cookies',
      'cookie policy',
      'tracking preferences',
      'website cookies',
    ],
  },
  accessibility: {
    id: 'accessibility',
    slug: 'accessibility',
    title: 'Accessibility Statement',
    description:
      'AOVIAS accessibility commitment, standards we aim for, known limitations, and how to share feedback. Public draft for counsel review.',
    path: '/legal/accessibility',
    version: '1.0.0',
    lastUpdated: LAST_UPDATED,
    keywords: [
      'AOVIAS accessibility',
      'WCAG',
      'accessibility statement',
      'inclusive design',
    ],
  },
  security: {
    id: 'security',
    slug: 'security',
    title: 'Security Overview',
    description:
      'Security principles, responsible disclosure, and high-level posture — without sensitive implementation detail or invented certifications. Public draft for counsel review.',
    path: '/legal/security',
    version: '1.0.0',
    lastUpdated: LAST_UPDATED,
    keywords: [
      'AOVIAS security',
      'responsible disclosure',
      'security overview',
      'trust',
    ],
  },
};

function documentFrom(
  id: LegalDocumentId,
  body: LegalDocument['body'],
  relatedIds: readonly LegalDocumentId[],
  extras?: Pick<LegalDocument, 'securityControls' | 'securityHighlights'>,
): LegalDocument {
  return {
    ...metas[id],
    body,
    toc: buildTocFromBody(body),
    relatedIds,
    ...extras,
  };
}

export const legalDocuments: readonly LegalDocument[] = [
  documentFrom('privacy', privacyBody, [
    'terms',
    'cookies',
    'security',
  ]),
  documentFrom('terms', termsBody, ['privacy', 'cookies', 'security']),
  documentFrom('cookies', cookiesBody, [
    'privacy',
    'terms',
    'accessibility',
  ]),
  documentFrom('accessibility', accessibilityBody, [
    'privacy',
    'security',
    'terms',
  ]),
  documentFrom(
    'security',
    securityBody,
    ['privacy', 'accessibility', 'terms'],
    {
      securityHighlights: [
        'Least-privilege access by default',
        'Encryption in transit for public services',
        'Responsible disclosure welcomed',
        'No sensitive implementation detail published here',
      ],
      securityControls: [
        {
          id: 'identity',
          title: 'Identity & access',
          description:
            'Access to production systems is limited to authorized roles with authentication controls appropriate to risk.',
          category: 'Access',
        },
        {
          id: 'data',
          title: 'Data protection',
          description:
            'Personal data is handled according to purpose limitation, retention discipline, and privacy commitments.',
          category: 'Privacy',
        },
        {
          id: 'ops',
          title: 'Operational resilience',
          description:
            'Monitoring, change discipline, and incident response processes support continuity of public services.',
          category: 'Operations',
        },
      ],
    },
  ),
] as const;

const byId = new Map(legalDocuments.map((doc) => [doc.id, doc]));

export function getLegalDocument(id: LegalDocumentId): LegalDocument {
  const doc = byId.get(id);
  if (!doc) {
    throw new Error(`Unknown legal document: ${id}`);
  }
  return doc;
}

export function getAllLegalDocuments(): readonly LegalDocument[] {
  return legalDocuments;
}

export function toLegalDocumentCard(doc: LegalDocument): LegalDocumentCard {
  return {
    id: doc.id,
    kind: 'resource',
    title: doc.title,
    description: doc.description,
    href: doc.path,
    category: {
      id: 'trust-governance',
      name: 'Trust & Governance',
      tone: 'info',
    },
    updatedAt: doc.lastUpdated,
  };
}

export function getRelatedLegalCards(
  doc: LegalDocument,
): readonly LegalDocumentCard[] {
  return doc.relatedIds
    .map((id) => byId.get(id))
    .filter((item): item is LegalDocument => Boolean(item))
    .map(toLegalDocumentCard);
}

export function getLegalHubCards(): readonly LegalDocumentCard[] {
  return legalDocuments.map(toLegalDocumentCard);
}
