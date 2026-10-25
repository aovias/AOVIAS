import type { CompanyModuleMeta } from '@/features/company/types';

export const storyMeta = {
  id: 'story',
  sectionId: 'story',
  title: 'Our Story',
} as const satisfies CompanyModuleMeta;

export const storyContent = {
  eyebrow: 'Our story',
  heading: 'Why AOVIAS exists',
  description:
    'Institutions carry work that society depends on. Technology that supports them must be steady, transparent, and worthy of that responsibility.',
  paragraphs: [
    'AOVIAS was founded because critical organizations often inherit software that is fragile, opaque, or optimized for attention rather than duty. We build for the opposite outcome: clarity, integrity, and continuity.',
    'The shortage is not more applications. It is technology designed for accountability, longevity, and the real constraints of healthcare, government, education, and enterprise environments.',
    'Progress is measured in capability earned over time — stronger systems, clearer ownership, and partnerships that outlast release calendars.',
  ],
};
