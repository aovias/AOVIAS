/** 8-point spacing scale + semantic layout rhythm roles (R1.DS.3). */
export const spacing = {
  0: '0px',
  1: '4px',
  2: '8px',
  3: '12px',
  4: '16px',
  5: '24px',
  6: '32px',
  7: '40px',
  8: '48px',
  9: '64px',
  10: '80px',
  11: '96px',
  12: '128px',
  13: '160px',
} as const;

/**
 * Semantic spacing roles — map to scale steps / fluid clamps in CSS.
 * Components should prefer these names over ad-hoc rem values.
 */
export const spacingRoles = {
  'control-x': spacing[3],
  'control-y': spacing[2],
  'stack-xs': spacing[1],
  'stack-sm': spacing[2],
  'stack-md': spacing[3],
  'stack-lg': spacing[5],
  'stack-xl': spacing[6],
  'grid-gap': spacing[6],
  'grid-gap-lg': spacing[8],
  'card-padding': spacing[5],
  'section-y-md': spacing[8],
  'section-y-lg': spacing[9],
  'section-y-xl': spacing[10],
  'hero-y': spacing[9],
} as const;

export type SpacingToken = keyof typeof spacing;
export type SpacingRole = keyof typeof spacingRoles;
