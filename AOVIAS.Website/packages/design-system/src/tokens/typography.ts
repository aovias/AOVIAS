export const fontFamilies = {
  sans: '"IBM Plex Sans", ui-sans-serif, system-ui, sans-serif',
  mono: '"IBM Plex Mono", ui-monospace, SFMono-Regular, Menlo, monospace',
} as const;

/**
 * Absolute scale (desktop reference). CSS applies fluid clamps for display/titles.
 */
export const fontSizes = {
  'display-xl': '4.5rem',
  'display-l': '3.75rem',
  display: '3.75rem',
  'hero-title': '3rem',
  'page-title': '3rem',
  'section-title': '2.5rem',
  'card-title': '1.5rem',
  subheading: '1.25rem',
  lead: '1.125rem',
  eyebrow: '0.75rem',
  label: '0.875rem',
  helper: '0.875rem',
  h1: '3rem',
  h2: '2.5rem',
  h3: '2rem',
  h4: '1.5rem',
  h5: '1.25rem',
  h6: '1.125rem',
  'body-lg': '1.125rem',
  body: '1rem',
  small: '0.875rem',
  caption: '0.75rem',
} as const;

export const fontWeights = {
  regular: '400',
  medium: '500',
  semibold: '600',
  bold: '700',
} as const;

export const lineHeights = {
  none: '1',
  tight: '1.2',
  snug: '1.35',
  normal: '1.5',
  relaxed: '1.65',
  loose: '1.8',
} as const;

export const letterSpacings = {
  tighter: '-0.04em',
  tight: '-0.02em',
  normal: '0em',
  wide: '0.02em',
  wider: '0.04em',
} as const;

/** Semantic type roles for documentation / recipe alignment. */
export const typeRoles = [
  'display',
  'hero-title',
  'page-title',
  'section-title',
  'card-title',
  'subheading',
  'lead',
  'body',
  'caption',
  'eyebrow',
  'label',
  'helper',
  'quote',
  'link',
] as const;

export const typography = {
  fontFamilies,
  fontSizes,
  fontWeights,
  lineHeights,
  letterSpacings,
  typeRoles,
} as const;
