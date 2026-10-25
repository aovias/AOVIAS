/**
 * Security & Trust section copy — Sprint T5.
 * Philosophy and governance. No certifications, vendors, protocols, or implementation.
 * Theme: Trust is a platform principle — earned through engineering.
 */

export const TRUST_GUIDING_LINE =
  'Security is a continuous engineering practice, not a feature.';

export const TRUST_RECURRING_THEME =
  'Trust is earned through engineering — and designed into the platform.';

export const trustSecurityContent = {
  hero: {
    eyebrow: 'Security',
    title: 'Security engineered from the beginning',
    description:
      'AOVIAS treats security as a continuous engineering practice. Controls, review, and operational discipline are part of how the platform is built — not an optional add-on.',
    primaryCta: {
      label: 'Privacy & trust',
      href: '/technology/trust/privacy',
    },
    secondaryCta: {
      label: 'Identity platform',
      href: '/technology/trust/identity',
    },
  },
  guidingLine: TRUST_GUIDING_LINE,
  secureByDesign: {
    eyebrow: 'Secure by design',
    title: 'Built into the platform — not bolted on later',
    description:
      'Security expectations travel with shared platform capabilities. Products inherit a disciplined foundation so institutions do not rebuild trust from scratch for every application.',
  },
  principles: [
    {
      title: 'Least privilege in spirit',
      description:
        'Access and capability are shaped so people and systems receive what they need for their duty — not unbounded reach.',
    },
    {
      title: 'Defense in layers',
      description:
        'No single control is the whole story. Platform, product, and operational practices reinforce each other.',
    },
    {
      title: 'Clarity over obscurity',
      description:
        'Understandable boundaries and responsibilities strengthen confidence more than hidden complexity.',
    },
    {
      title: 'Enable, do not obstruct',
      description:
        'Security should help organizations work safely — not invent friction that slows responsible progress.',
    },
  ],
  platformSecurity: {
    eyebrow: 'Platform security',
    title: 'Shared controls for a multi-product foundation',
    description:
      'Identity, data stewardship, and operational boundaries are platform concerns. Consistency across products reduces surprise for operators and leaders.',
  },
  operationalSecurity: {
    eyebrow: 'Operational security',
    title: 'Practice that continues after delivery',
    description:
      'Secure outcomes depend on how systems are operated: monitoring, change discipline, and accountable response when something needs attention.',
  },
  continuous: {
    eyebrow: 'Continuous improvement',
    title: 'Security never finishes',
    description:
      'Threats, regulations, and institutional needs change. Engineering practice improves through review, learning, and refinement — calmly and without fear-based messaging.',
  },
} as const;

export const trustPrivacyContent = {
  hero: {
    eyebrow: 'Privacy & trust',
    title: 'Responsible stewardship of information',
    description:
      'Privacy is a responsibility. This page explains how AOVIAS approaches trust and stewardship as engineering principles — distinct from legal privacy policy texts.',
    primaryCta: {
      label: 'Data protection',
      href: '/technology/trust/data-protection',
    },
    secondaryCta: {
      label: 'Legal privacy',
      href: '/legal/privacy',
    },
  },
  distinction:
    'For contractual and regulatory privacy notices, see Trust & Governance → Privacy. This page describes platform philosophy and stewardship intent.',
  topics: [
    {
      title: 'Trust philosophy',
      description:
        'Trust is a platform principle. Institutions should understand how information is respected as they adopt AOVIAS capabilities.',
    },
    {
      title: 'Transparency',
      description:
        'Clarity about purpose and responsibility strengthens confidence. We prefer explainable stewardship over opaque claims.',
    },
    {
      title: 'Privacy by design',
      description:
        'Privacy expectations are considered when capabilities are shaped — not deferred until after launch.',
    },
    {
      title: 'Responsible data stewardship',
      description:
        'Sensitive institutional information is treated with restraint across products that share the platform.',
    },
    {
      title: 'Governance',
      description:
        'Clear ownership of how data is used inside products and deployments is part of trustworthy delivery.',
    },
    {
      title: 'Human responsibility',
      description:
        'People and institutions remain accountable for how information is used. Technology supports that duty; it does not erase it.',
    },
  ],
} as const;

export const trustIdentityContent = {
  hero: {
    eyebrow: 'Identity platform',
    title: 'Identity as a shared platform service',
    description:
      'Secure collaboration starts with knowing who is acting and what they are permitted to do. Identity is foundational — described here conceptually, without protocols or vendors.',
    primaryCta: {
      label: 'Security',
      href: '/technology/trust',
    },
    secondaryCta: {
      label: 'Compliance readiness',
      href: '/technology/trust/compliance',
    },
  },
  topics: [
    {
      title: 'Authentication',
      description:
        'Confirming that a person or system is who they claim to be before access is granted.',
    },
    {
      title: 'Authorization',
      description:
        'Determining what an authenticated actor may do — aligned to role, duty, and policy.',
    },
    {
      title: 'Single sign-on readiness',
      description:
        'Designed so institutions can connect familiar access experiences across products — without prescribing a specific identity vendor.',
    },
    {
      title: 'Shared platform service',
      description:
        'Identity capabilities serve multiple products consistently instead of fragmenting into one-off access models.',
    },
    {
      title: 'Secure access management',
      description:
        'Access is stewarded over time: granted, reviewed, and adjusted as responsibilities change.',
    },
  ],
} as const;

export const trustComplianceContent = {
  hero: {
    eyebrow: 'Compliance readiness',
    title: 'Designed to support regulated industries',
    description:
      'AOVIAS builds platform capabilities that help institutions pursue their compliance obligations — with audit support, traceability, and accountability in mind. We do not claim certifications or approvals on this page.',
    primaryCta: {
      label: 'Data protection',
      href: '/technology/trust/data-protection',
    },
    secondaryCta: {
      label: 'Security',
      href: '/technology/trust',
    },
  },
  disclaimer:
    'Compliance depends on how customers implement, configure, and operate the platform under applicable regulations. AOVIAS does not claim that use of the platform alone constitutes compliance with any specific law or standard.',
  topics: [
    {
      title: 'Healthcare context',
      description:
        'Platform patterns for access, stewardship, and auditability support healthcare institutions pursuing their own regulatory duties.',
    },
    {
      title: 'Government context',
      description:
        'Clarity, accountability, and controlled access matter for public-sector technology programs.',
    },
    {
      title: 'Enterprise governance',
      description:
        'Shared controls help enterprises apply policy consistently across products and teams.',
    },
    {
      title: 'Audit support',
      description:
        'Institutions need the ability to examine how work proceeded. The platform is designed with reviewability in mind.',
    },
    {
      title: 'Traceability',
      description:
        'Important actions should be attributable within operational context for later examination.',
    },
    {
      title: 'Accountability',
      description:
        'People and organizations remain responsible for compliance outcomes. Technology supports that responsibility.',
    },
  ],
} as const;

export const trustDataProtectionContent = {
  hero: {
    eyebrow: 'Data protection',
    title: 'Engineering intent for protecting information',
    description:
      'Data protection at AOVIAS is expressed as principles: minimize what is collected, handle it securely, respect the lifecycle, and uphold confidentiality, integrity, and availability as engineering goals.',
    primaryCta: {
      label: 'Privacy & trust',
      href: '/technology/trust/privacy',
    },
    secondaryCta: {
      label: 'Security',
      href: '/technology/trust',
    },
  },
  topics: [
    {
      title: 'Data minimization',
      description:
        'Collect and retain what is needed for a clear purpose — not everything that is conveniently available.',
    },
    {
      title: 'Secure handling',
      description:
        'Information is treated with care across workflows that products compose on the platform.',
    },
    {
      title: 'Encryption philosophy',
      description:
        'Protecting data in transit and at rest is part of responsible platform design — described as intent, not as a catalogue of mechanisms.',
    },
    {
      title: 'Data lifecycle awareness',
      description:
        'Creation, use, retention, and retirement are considered when capabilities are shaped.',
    },
    {
      title: 'Confidentiality',
      description:
        'Sensitive information should be accessible only to those with a legitimate need.',
    },
    {
      title: 'Integrity',
      description:
        'Institutions depend on information remaining accurate and unaltered except through authorized change.',
    },
    {
      title: 'Availability',
      description:
        'Protected information must remain usable for authorized work when institutions need it.',
    },
  ],
} as const;
