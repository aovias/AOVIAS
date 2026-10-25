import type { Variants } from 'framer-motion';

import { fadeDown, fadeIn, fadeOut, fadeUp, reducedFade, scaleIn } from './variants';

/**
 * Fade system — named fade presets for sections, components, dialogs, cards, lists, images.
 * All values derive from shared variants (no inline definitions).
 */
export const fadeSystem = {
  section: fadeUp,
  component: fadeIn,
  dialog: scaleIn,
  card: fadeUp,
  list: fadeIn,
  image: fadeIn,
  listItem: fadeUp,
  dismiss: fadeOut,
  overlay: fadeIn,
  caption: fadeDown,
} as const satisfies Record<string, Variants>;

export type FadeSystemName = keyof typeof fadeSystem;

export function getFadeVariant(name: FadeSystemName, reducedMotion = false): Variants {
  if (reducedMotion) return reducedFade;
  return fadeSystem[name];
}
