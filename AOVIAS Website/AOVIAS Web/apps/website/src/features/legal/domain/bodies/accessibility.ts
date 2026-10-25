import type { LegalBodySection } from '@/features/legal/types';

/**
 * Accessibility Statement body — public draft for counsel review.
 * Not legal advice; not counsel-approved. No invented certifications.
 */
export const accessibilityBody: readonly LegalBodySection[] = [
  {
    id: 'notice',
    heading: 'Public draft notice',
    paragraphs: [
      'This Accessibility Statement is a public draft published for transparency and counsel review. It is not legal advice and is not counsel-approved. It describes commitment and practice; it does not claim third-party certification.',
    ],
  },
  {
    id: 'commitment',
    heading: 'Accessibility commitment',
    paragraphs: [
      'AOVIAS believes public digital experiences should be usable by everyone. We design the public website to support clarity, keyboard access, semantic structure, and compatibility with assistive technologies.',
      'Accessibility is an ongoing engineering responsibility, not a one-time checkbox.',
    ],
  },
  {
    id: 'supported-standards',
    heading: 'Standards we aim for',
    paragraphs: [
      'We aim to conform to WCAG 2.2 Level AA for public website experiences. Our engineering foundations include semantic HTML, focus management, sufficient contrast tokens, and respect for reduced-motion preferences.',
    ],
    bullets: [
      'Keyboard navigation for interactive controls',
      'Screen-reader-friendly labels and landmarks',
      'Meaningful heading hierarchy',
      'Visible focus states',
    ],
  },
  {
    id: 'scope',
    heading: 'Scope',
    paragraphs: [
      'This statement covers the AOVIAS public website experiences maintained by AOVIAS. Third-party sites linked from our pages follow their own accessibility practices.',
      'Authenticated product interfaces and enterprise deployments may have additional accessibility requirements defined in separate agreements.',
    ],
  },
  {
    id: 'known-limitations',
    heading: 'Known limitations',
    paragraphs: [
      'Some third-party embeds, legacy assets, or rapidly evolving preview surfaces may not yet meet the same standard as primary public pages. We prioritize remediation based on user impact.',
      'If you encounter a barrier, please tell us. Specific page URLs and assistive technology details help us improve faster.',
    ],
  },
  {
    id: 'feedback-process',
    heading: 'Feedback process',
    paragraphs: [
      'We welcome accessibility feedback. Contact accessibility@aovias.com or use the Contact page. We review reports, acknowledge receipt, and work to resolve issues with clear communication.',
    ],
  },
  {
    id: 'continuous-improvement',
    heading: 'Continuous improvement',
    paragraphs: [
      'Accessibility reviews are part of our delivery discipline. As components and pages evolve, we re-validate critical journeys including navigation, forms, documentation, and trust content.',
    ],
  },
];
