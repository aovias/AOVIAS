import { type ClassValue, clsx } from 'clsx';
import { extendTailwindMerge } from 'tailwind-merge';

/**
 * Design-system typography size tokens use `text-*` names that would otherwise
 * collide with color utilities (e.g. `text-body` vs `text-primary-foreground`)
 * inside default tailwind-merge grouping.
 */
const twMerge = extendTailwindMerge({
  extend: {
    classGroups: {
      'font-size': [
        {
          text: [
            'display-xl',
            'display-l',
            'display',
            'hero-title',
            'page-title',
            'section-title',
            'card-title',
            'subheading',
            'lead',
            'eyebrow',
            'label',
            'helper',
            'h1',
            'h2',
            'h3',
            'h4',
            'h5',
            'h6',
            'body-lg',
            'body',
            'small',
            'caption',
          ],
        },
      ],
    },
  },
});

/** Merge Tailwind class names without dropping tokenized text colors. */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}
