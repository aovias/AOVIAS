export * from './colors';
export * from './typography';
export * from './spacing';
export * from './radius';
export * from './shadows';
export * from './borders';
export * from './layout';
export * from './breakpoints';
export * from './motion';
export * from './z-index';
export * from './opacity';
export * from './blur';
export * from './icons';

import { colors } from './colors';
import { typography } from './typography';
import { spacing } from './spacing';
import { radius } from './radius';
import { shadows, elevation } from './shadows';
import { borders } from './borders';
import { layout } from './layout';
import { breakpoints } from './breakpoints';
import { motion } from './motion';
import { zIndex } from './z-index';
import { opacity } from './opacity';
import { blur } from './blur';
import { iconSizes } from './icons';

/** Aggregated token map — single TypeScript source of truth. */
export const tokens = {
  colors,
  typography,
  spacing,
  radius,
  shadows,
  elevation,
  borders,
  layout,
  breakpoints,
  motion,
  zIndex,
  opacity,
  blur,
  iconSizes,
} as const;

export type DesignTokens = typeof tokens;
