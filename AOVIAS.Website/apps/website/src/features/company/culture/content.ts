import type { CompanyModuleMeta } from '@/features/company/types';

export const cultureMeta = {
  id: 'culture',
  sectionId: 'culture',
  title: 'Culture',
} as const satisfies CompanyModuleMeta;

export const cultureContent = {
  eyebrow: 'Culture',
  heading: 'An engineering culture of ownership',
  description:
    'How we work is inseparable from what we ship — craftsmanship, learning, and responsibility in every engagement.',
  principles: [
    {
      id: 'craft',
      title: 'Craftsmanship',
      description:
        'We take pride in precise work: clear interfaces, honest quality, and systems others can maintain.',
    },
    {
      id: 'learning',
      title: 'Learning',
      description:
        'Curiosity is operational. We improve through evidence, review, and disciplined reflection.',
    },
    {
      id: 'ownership',
      title: 'Ownership',
      description:
        'Teams own outcomes end to end — including the consequences of shortcuts and the benefits of care.',
    },
    {
      id: 'collaboration',
      title: 'Collaboration',
      description:
        'Shared standards and respectful critique make complex work safer and more coherent.',
    },
    {
      id: 'responsibility',
      title: 'Responsibility',
      description:
        'We remember who depends on institutional systems — and design with that duty in mind.',
    },
  ],
};
