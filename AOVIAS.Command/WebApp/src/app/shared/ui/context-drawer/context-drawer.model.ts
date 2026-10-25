import type { Type } from '@angular/core';

/**
 * Enterprise Context Drawer — section contract.
 *
 * A section is one contextual experience inside the drawer: AI guidance, linked
 * documents, activity history. Modules contribute sections; the drawer knows
 * nothing about what any of them contain.
 *
 * `load` is a dynamic import rather than a component reference so a section's
 * code is only fetched when the user opens it. That is the whole point of
 * Step 10: a workspace that never opens the drawer should not pay to render —
 * or download — its contents.
 */
export interface ContextDrawerSection {
  readonly id: string;
  /** Rail label, also the accessible name of its tab. */
  readonly label: string;
  /**
   * Short glyph for the toolbar entry point and the rail. Text rather than an
   * icon font keeps the drawer dependency-free for any consuming module.
   */
  readonly glyph: string;
  /** Resolves the section's component the first time it is shown. */
  readonly load: () => Promise<Type<unknown>>;
  /**
   * Optional count shown on the rail — "3 documents". Omit when a count would
   * be meaningless; a zero badge reads as an error rather than as emptiness.
   */
  readonly badge?: number | null;
}
