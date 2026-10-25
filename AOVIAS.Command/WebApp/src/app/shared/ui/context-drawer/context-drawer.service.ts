import { Injectable, computed, signal } from '@angular/core';
import type { ContextDrawerSection } from './context-drawer.model';

const PINNED_KEY = 'aovias.contextDrawer.pinned.v1';
const SECTION_KEY = 'aovias.contextDrawer.section.v1';

/**
 * Enterprise Context Drawer state — Stage 1.2.
 *
 * Platform-level, not Audit-level: any workspace registers its sections here
 * and gets the same contextual interaction model (Step 12).
 *
 * Two pieces of state with deliberately different lifetimes. Which section was
 * last open and whether the drawer is pinned are *user preferences*, so they
 * persist in localStorage across sessions. Whether the drawer is currently open
 * is not — a workspace should load with the full width the user came for, and
 * reopening is one click. Pinned is the exception the user has explicitly asked
 * for, so a pinned drawer does reopen.
 */
@Injectable({ providedIn: 'root' })
export class ContextDrawerService {
  private readonly sectionsState = signal<readonly ContextDrawerSection[]>([]);
  private readonly activeIdState = signal<string | null>(null);
  private readonly openState = signal(false);
  private readonly pinnedState = signal(this.restorePinned());

  readonly sections = this.sectionsState.asReadonly();
  readonly pinned = this.pinnedState.asReadonly();

  /**
   * Pinned implies open. Keeping that as derived state means the two can never
   * contradict each other — a pinned-but-closed drawer would just be a gap in
   * the layout.
   */
  readonly isOpen = computed(() => this.openState() || this.pinnedState());

  readonly activeSection = computed<ContextDrawerSection | null>(() => {
    const all = this.sectionsState();
    if (!all.length) return null;
    const id = this.activeIdState();
    return all.find((s) => s.id === id) ?? all[0];
  });

  /** True when the host workspace has contributed nothing to show. */
  readonly hasSections = computed(() => this.sectionsState().length > 0);

  /**
   * Publish the sections available for the current workspace.
   *
   * Called as the active page changes, so the drawer's contents track the
   * workspace. The remembered section is honoured when the new workspace still
   * offers it — a user who works out of "Documents" stays there while moving
   * between records, rather than being reset to the first tab each time.
   */
  setSections(sections: readonly ContextDrawerSection[]): void {
    this.sectionsState.set(sections);

    if (!sections.length) {
      this.activeIdState.set(null);
      return;
    }

    const remembered = this.activeIdState() ?? this.restoreSection();
    const stillOffered = remembered && sections.some((s) => s.id === remembered);
    this.activeIdState.set(stillOffered ? remembered : sections[0].id);
  }

  /** Open the drawer at a specific section — the toolbar entry points use this. */
  open(sectionId?: string): void {
    if (sectionId) this.select(sectionId);
    this.openState.set(true);
  }

  /** Close, unless pinned — a pinned drawer is dismissed by unpinning. */
  close(): void {
    this.openState.set(false);
  }

  /** Toolbar buttons toggle: pressing the active section's button closes it. */
  toggle(sectionId: string): void {
    if (this.isOpen() && this.activeIdState() === sectionId) {
      if (this.pinnedState()) {
        this.setPinned(false);
      }
      this.openState.set(false);
      return;
    }
    this.open(sectionId);
  }

  select(sectionId: string): void {
    if (!this.sectionsState().some((s) => s.id === sectionId)) return;
    this.activeIdState.set(sectionId);
    this.persist(SECTION_KEY, sectionId);
  }

  setPinned(pinned: boolean): void {
    this.pinnedState.set(pinned);
    this.persist(PINNED_KEY, pinned ? '1' : '0');
    if (pinned) this.openState.set(true);
  }

  togglePinned(): void {
    this.setPinned(!this.pinnedState());
  }

  private persist(key: string, value: string): void {
    try {
      localStorage.setItem(key, value);
    } catch {
      /* storage unavailable — preference stays in memory for this session */
    }
  }

  private restorePinned(): boolean {
    try {
      // Default unpinned: Stage 1.2 exists because the workspace should start
      // at full width. Pinning is an explicit opt-in.
      return localStorage.getItem(PINNED_KEY) === '1';
    } catch {
      return false;
    }
  }

  private restoreSection(): string | null {
    try {
      return localStorage.getItem(SECTION_KEY);
    } catch {
      return null;
    }
  }
}
