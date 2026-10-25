/** Subtle elevation shadows — calm enterprise depth (R1.DS.3). */
export const shadows = {
  flat: 'none',
  surface: '0 1px 2px rgba(11, 21, 32, 0.06)',
  raised: '0 4px 12px rgba(11, 21, 32, 0.08)',
  /** Floating panels (menus, popovers) — alias of overlay. */
  floating: '0 12px 32px rgba(11, 21, 32, 0.12)',
  overlay: '0 12px 32px rgba(11, 21, 32, 0.12)',
  modal: '0 24px 48px rgba(11, 21, 32, 0.18)',
} as const;

export const elevation = {
  flat: 0,
  surface: 1,
  raised: 2,
  floating: 3,
  overlay: 3,
  modal: 4,
} as const;
