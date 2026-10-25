/** Brand, neutral, and semantic color primitives (hex). Themes map these to semantic CSS vars. */

/** Chromatic enterprise blue — primary brand (R1 visual review). */
export const primaryColors = {
  50: '#EFF5FB',
  100: '#DDE9F7',
  200: '#BAD2EE',
  300: '#8AB2E0',
  400: '#5A8FD0',
  500: '#3672BE',
  600: '#285CA4',
  700: '#204A86',
  800: '#1A3A69',
  900: '#142B4E',
  950: '#0C1B33',
} as const;

/** Indigo — secondary brand (restrained accents only). */
export const secondaryColors = {
  50: '#F2F3FB',
  100: '#E3E5F6',
  200: '#C7CAED',
  300: '#A2A7E0',
  400: '#7B81D0',
  500: '#5B62C0',
  600: '#474DA4',
  700: '#393E85',
  800: '#303468',
  900: '#282B52',
  950: '#181A33',
} as const;

/** Professional cyan — AI / analytics / insights only (never page-dominant). */
export const accentColors = {
  50: '#EBF9FD',
  100: '#D0F0F9',
  200: '#A5E2F2',
  300: '#6BCCE8',
  400: '#30AFD6',
  500: '#1292BC',
  600: '#0F759D',
  700: '#135E7F',
  800: '#174D68',
  900: '#174058',
  950: '#0E2A3B',
} as const;

/** Emerald — supporting brand (aligned with success). */
export const supportingColors = {
  50: '#EDF9F2',
  100: '#D4F0E2',
  200: '#ACE2C8',
  300: '#79CCA4',
  400: '#45B07E',
  500: '#2BA06A',
  600: '#1F8254',
  700: '#1A6743',
  800: '#175338',
  900: '#13442F',
  950: '#0A261A',
} as const;

export const brandColors = {
  primary: primaryColors,
  secondary: secondaryColors,
  accent: accentColors,
  supporting: supportingColors,
} as const;

/**
 * Neutrals — no pure black for canvases.
 * `0` is a named near-white extreme (not default card fill); surfaces use 50+.
 */
export const neutralColors = {
  0: '#FCFCFD',
  50: '#F6F7F9',
  100: '#EEEFF3',
  200: '#D8DCE4',
  300: '#B6BECB',
  400: '#8B95A5',
  500: '#6A7485',
  600: '#535C6C',
  700: '#424957',
  800: '#2E343F',
  900: '#1B2028',
  950: '#12161C',
  1000: '#0B0E13',
} as const;

export const semanticColors = {
  success: {
    50: '#EDF9F2',
    100: '#D4F0E2',
    500: '#2BA06A',
    600: '#1F8254',
    700: '#1A6743',
  },
  warning: {
    50: '#FBF6ED',
    100: '#F4E7CF',
    500: '#C48A1A',
    600: '#A37114',
    700: '#825B12',
  },
  /** Danger — aliased as `error` in CSS/theme for compatibility. */
  error: {
    50: '#FBF1F1',
    100: '#F4DADA',
    500: '#C23B3B',
    600: '#A12F2F',
    700: '#812626',
  },
  info: {
    50: '#EFF5FB',
    100: '#DDE9F7',
    500: '#3672BE',
    600: '#285CA4',
    700: '#204A86',
  },
} as const;

/**
 * Product identity accents (R1.DS.3.1) — restrained; never page-dominant.
 * Platform=Indigo · Command=Blue · Studio=Purple · Future=Slate
 */
export const productAccentColors = {
  platform: '#5B62C0',
  command: '#285CA4',
  studio: '#7050B0',
  future: '#5C6B80',
} as const;

/**
 * Technology identity accents.
 * Platform=Indigo · Engineering=Blue · AI=Cyan · Security=Emerald ·
 * Identity=Teal · Architecture=Violet
 */
export const technologyAccentColors = {
  platform: '#5B62C0',
  engineering: '#285CA4',
  ai: '#1292BC',
  security: '#2BA06A',
  identity: '#178080',
  architecture: '#7A5FC0',
} as const;

/**
 * Industry accents — very restrained.
 * Healthcare=Emerald · Government=Navy · Education=Royal Indigo · Enterprise=Slate Blue
 */
export const industryAccentColors = {
  healthcare: '#1F8254',
  government: '#204A86',
  education: '#474DA4',
  enterprise: '#285CA4',
} as const;

/** Trust signal accents — emerald family. */
export const trustAccentColors = {
  security: '#1F8254',
  compliance: '#1A6743',
  privacy: '#178080',
  verified: '#2BA06A',
} as const;

export const colors = {
  brand: brandColors,
  neutral: neutralColors,
  semantic: semanticColors,
  product: productAccentColors,
  technology: technologyAccentColors,
  industry: industryAccentColors,
  trust: trustAccentColors,
} as const;

export type BrandColorScale = typeof brandColors.primary;
export type NeutralColorScale = typeof neutralColors;
export type SemanticColorGroup = typeof semanticColors;
