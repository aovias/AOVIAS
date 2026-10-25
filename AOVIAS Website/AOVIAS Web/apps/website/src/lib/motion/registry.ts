import type { Variants } from 'framer-motion';

import { fadeSystem } from './fade';
import { hoverMotions, type HoverMotionName } from './hover';
import {
  fadeDown,
  fadeIn,
  fadeOut,
  fadeUp,
  pageTransition,
  reveal,
  scaleIn,
  scaleOut,
  slideLeft,
  slideRight,
  staggerContainer,
  staggerItem,
} from './variants';

/**
 * Motion Registry — stable approved animation names.
 *
 * Future components MUST import named entries from this registry
 * instead of inventing new variant objects. This prevents motion drift.
 */

export type RegistryVariantEntry = {
  kind: 'variant';
  name: string;
  description: string;
  usage: string;
  variants: Variants;
};

export type RegistryHoverEntry = {
  kind: 'hover';
  name: HoverMotionName;
  description: string;
  usage: string;
  hover: (typeof hoverMotions)[HoverMotionName];
};

export type MotionRegistryEntry = RegistryVariantEntry | RegistryHoverEntry;

const variant = (
  name: string,
  description: string,
  usage: string,
  variants: Variants,
): RegistryVariantEntry => ({
  kind: 'variant',
  name,
  description,
  usage,
  variants,
});

const hover = (
  name: HoverMotionName,
  description: string,
  usage: string,
): RegistryHoverEntry => ({
  kind: 'hover',
  name,
  description,
  usage,
  hover: hoverMotions[name],
});

/**
 * Canonical registry keyed by stable public names.
 * Add new motions here only — never as one-off inline objects in features.
 */
export const motionRegistry = {
  fadeIn: variant(
    'fadeIn',
    'Opacity entrance for soft appearance.',
    'Dialogs overlays, inline components, list fades.',
    fadeIn,
  ),
  fadeOut: variant(
    'fadeOut',
    'Opacity exit for dismissal.',
    'Dismissible layers and cross-fades.',
    fadeOut,
  ),
  fadeUp: variant(
    'fadeUp',
    'Fade with subtle upward travel — primary entrance.',
    'Sections, cards, general content reveal.',
    fadeUp,
  ),
  fadeDown: variant(
    'fadeDown',
    'Fade with subtle downward travel.',
    'Captions, secondary disclosures.',
    fadeDown,
  ),
  slideLeft: variant(
    'slideLeft',
    'Horizontal reveal from the right.',
    'Directional panels and split layouts.',
    slideLeft,
  ),
  slideRight: variant(
    'slideRight',
    'Horizontal reveal from the left.',
    'Directional panels and split layouts.',
    slideRight,
  ),
  scaleIn: variant(
    'scaleIn',
    'Subtle scale entrance (≤2%).',
    'Dialogs and focused surfaces.',
    scaleIn,
  ),
  scaleOut: variant(
    'scaleOut',
    'Subtle scale exit.',
    'Dialog dismissal pairing with scaleIn.',
    scaleOut,
  ),
  staggerContainer: variant(
    'staggerContainer',
    'Parent orchestrator for staggered children.',
    'Wrap lists; pair with staggerItem.',
    staggerContainer,
  ),
  staggerItem: variant(
    'staggerItem',
    'Child item for stagger containers.',
    'List rows, feature grids (when composed later).',
    staggerItem,
  ),
  reveal: variant(
    'reveal',
    'Generic progressive disclosure (fadeUp).',
    'Default scroll-reveal / section reveal.',
    reveal,
  ),
  sectionReveal: variant(
    'sectionReveal',
    'Section-level scroll reveal.',
    'Page sections via MotionReveal / fadeSystem.section.',
    fadeSystem.section,
  ),
  pageTransition: variant(
    'pageTransition',
    'Route-level enter/exit shell.',
    'Future App Router transition wrappers only.',
    pageTransition,
  ),
  cardHover: hover(
    'card',
    'Subtle lift on interactive cards.',
    'Apply via getHoverMotion("card") on motion elements.',
  ),
  buttonHover: hover(
    'button',
    'Micro scale feedback for buttons.',
    'Apply via getHoverMotion("button").',
  ),
  linkHover: hover(
    'link',
    'Soft opacity cue for text links.',
    'Apply via getHoverMotion("link").',
  ),
  navItemHover: hover(
    'navItem',
    'Soft opacity cue for navigation items.',
    'Apply via getHoverMotion("navItem").',
  ),
  interactiveHover: hover(
    'interactive',
    'Generic interactive affordance.',
    'Apply via getHoverMotion("interactive").',
  ),
} as const;

export type MotionRegistryName = keyof typeof motionRegistry;

export function getRegistryEntry(name: MotionRegistryName): MotionRegistryEntry {
  return motionRegistry[name];
}

/** Resolve variants from a registry name (variant entries only). */
export function getRegistryVariants(name: MotionRegistryName): Variants {
  const entry = motionRegistry[name];
  if (entry.kind !== 'variant') {
    throw new Error(
      `Motion registry entry "${name}" is a hover motion, not a variant. Use getHoverMotion instead.`,
    );
  }
  return entry.variants;
}

/** Documented inventory for tooling / showcase. */
export function listMotionRegistry(): ReadonlyArray<{
  name: MotionRegistryName;
  kind: 'variant' | 'hover';
  description: string;
  usage: string;
}> {
  return (Object.keys(motionRegistry) as MotionRegistryName[]).map((name) => {
    const entry = motionRegistry[name];
    return {
      name,
      kind: entry.kind,
      description: entry.description,
      usage: entry.usage,
    };
  });
}
