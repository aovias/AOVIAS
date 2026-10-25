import type { CompanyModuleMeta } from '@/features/company/types';

export const visionMeta = {
  id: 'vision',
  sectionId: 'vision',
  title: 'Vision',
} as const satisfies CompanyModuleMeta;

export const visionContent = {
  eyebrow: 'Vision',
  heading: 'A future of stronger institutions',
  description:
    'We work toward systems that make critical organizations more capable — and more worthy of the trust placed in them.',
  statement:
    'AOVIAS envisions a world where institutional technology is calm, accountable, and built to endure — so healthcare, public service, education, and enterprise can focus on their duty to people.',
  pillars: [
    {
      id: 'systems',
      principle: 'Better systems',
      description:
        'Architectures that reduce ambiguity, surface truth, and support careful decision-making.',
    },
    {
      id: 'institutions',
      principle: 'Stronger institutions',
      description:
        'Technology that reinforces governance, stewardship, and operational reliability.',
    },
    {
      id: 'society',
      principle: 'Societal impact',
      description:
        'Progress measured by whether essential organizations can serve more effectively over time.',
    },
  ],
};
