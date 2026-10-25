import type { Transition } from 'framer-motion';

import { motionDurations } from './durations';
import { motionEasing } from './easing';
import { motionTokens } from './tokens';
import { motionTransitions } from './transitions';

/**
 * Global motion configuration for the AOVIAS website.
 * Enterprise Motion Language (Phase V10) — calm, purposeful, GPU-friendly.
 */
export const motionConfig = {
  library: 'framer-motion' as const,
  philosophy: [
    'confidence',
    'stability',
    'precision',
    'clarity',
    'calmness',
    'professionalism',
  ] as const,
  /** Every animation must serve at least one purpose. */
  purposes: [
    'guide-attention',
    'explain-relationships',
    'confirm-interaction',
    'reduce-cognitive-load',
    'reinforce-hierarchy',
    'smooth-context-changes',
  ] as const,
  /** Prefer GPU-friendly properties only. */
  preferredProperties: ['opacity', 'transform'] as const,
  /** Properties to avoid for decorative motion. */
  avoidedProperties: ['width', 'height', 'top', 'left', 'margin', 'padding'] as const,
  reducedMotion: {
    /** Framer MotionConfig value — honor OS preference. */
    strategy: 'user' as const,
    disableDecorative: true,
    preserveEssentialFeedback: true,
  },
  defaults: {
    transition: motionTransitions.standard as Transition,
    duration: motionDurations.standard,
    ease: motionEasing.standard,
    distance: motionTokens.distance.md,
    stagger: motionTokens.stagger.default,
  },
  scrollReveal: {
    once: true,
    amount: 0.2,
    margin: '0px 0px -40px 0px',
  },
  /** Card lift ceiling — 2–4px (enterprise, not spectacle). */
  cardLiftPx: 3,
} as const;

export type MotionConfig = typeof motionConfig;
