export const durations = {
  instant: '100ms',
  fast: '150ms',
  hover: '200ms',
  standard: '250ms',
  dropdown: '260ms',
  slow: '350ms',
  overlay: '350ms',
  deliberate: '450ms',
  hero: '700ms',
  diagram: '800ms',
  signature: '800ms',
} as const;

export const easings = {
  standard: 'cubic-bezier(0.2, 0, 0, 1)',
  enter: 'cubic-bezier(0.16, 1, 0.3, 1)',
  exit: 'cubic-bezier(0.4, 0, 1, 1)',
} as const;

export const transitions = {
  color: `color ${durations.fast} ${easings.standard}, background-color ${durations.fast} ${easings.standard}, border-color ${durations.fast} ${easings.standard}`,
  transform: `transform ${durations.hover} ${easings.standard}`,
  opacity: `opacity ${durations.fast} ${easings.standard}`,
  shadow: `box-shadow ${durations.hover} ${easings.standard}`,
  interactive: `color ${durations.fast} ${easings.standard}, background-color ${durations.fast} ${easings.standard}, border-color ${durations.fast} ${easings.standard}, box-shadow ${durations.hover} ${easings.standard}, transform ${durations.hover} ${easings.standard}`,
  overlay: `opacity ${durations.overlay} ${easings.enter}, transform ${durations.overlay} ${easings.enter}`,
  nav: `color ${durations.hover} ${easings.standard}, background-color ${durations.hover} ${easings.standard}, border-color ${durations.hover} ${easings.standard}, box-shadow ${durations.dropdown} ${easings.standard}, backdrop-filter ${durations.dropdown} ${easings.standard}`,
} as const;

/**
 * Enterprise Motion Language (Phase V10).
 * Every animation must guide attention, explain relationships, confirm
 * interaction, reduce cognitive load, reinforce hierarchy, or smooth context.
 */
export const motion = {
  durations,
  easings,
  transitions,
  staggerMax: '50ms',
  purposes: [
    'guide-attention',
    'explain-relationships',
    'confirm-interaction',
    'reduce-cognitive-load',
    'reinforce-hierarchy',
    'smooth-context-changes',
  ] as const,
} as const;
