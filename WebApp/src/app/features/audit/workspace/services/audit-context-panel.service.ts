import { Injectable, computed, signal } from '@angular/core';
import {
  NO_CONTEXT_SUBJECT,
  type AuditContextSection,
  type AuditContextSubject,
} from '../models/audit-context-panel.model';

const PANEL_OPEN_KEY = 'aovias.audit.workspace.panelOpen.v1';

/**
 * Contextual side panel state — Phase 3, Steps 8 and 10.
 *
 * Two things live here: what the user is looking at (the subject) and what the
 * hosting page wants said about it (the sections).
 *
 * Sections are *contributed* by pages rather than computed here, for the same
 * reason the action toolbar works that way: only the page holding the loaded
 * record knows its related documents, previous findings and linked CAPAs. A
 * central resolver would have to re-fetch everything the page already has.
 *
 * The panel's open/closed state is a user preference, so it persists in
 * localStorage and outlives the working session — unlike the audit selection
 * context, which is per-session.
 */
@Injectable({ providedIn: 'root' })
export class AuditContextPanelService {
  private readonly subjectState = signal<AuditContextSubject>(NO_CONTEXT_SUBJECT);
  private readonly sectionState = signal<readonly AuditContextSection[]>([]);
  private readonly openState = signal<boolean>(this.restoreOpen());

  /** What the panel is currently describing. */
  readonly subject = this.subjectState.asReadonly();

  /** Sections contributed for the current subject. */
  readonly sections = this.sectionState.asReadonly();

  readonly isOpen = this.openState.asReadonly();

  /** True when there is something worth showing. */
  readonly hasContent = computed(
    () => this.subjectState().kind !== 'none' || this.sectionState().length > 0,
  );

  /**
   * Declare what the page is showing.
   *
   * Clears the previous sections: they described the old subject, and leaving
   * them up while the new page contributes its own would briefly attribute one
   * record's related documents to another.
   */
  setSubject(subject: AuditContextSubject): void {
    this.subjectState.set(subject);
    this.sectionState.set([]);
  }

  /** Publish the sections for the current subject, replacing any previous set. */
  setSections(sections: readonly AuditContextSection[]): void {
    this.sectionState.set(sections);
  }

  /**
   * Reset to "nothing selected". Pages call this on destroy so the panel does
   * not keep describing a record the user has navigated away from.
   */
  clear(): void {
    this.subjectState.set(NO_CONTEXT_SUBJECT);
    this.sectionState.set([]);
  }

  toggle(): void {
    this.setOpen(!this.openState());
  }

  setOpen(open: boolean): void {
    this.openState.set(open);
    try {
      localStorage.setItem(PANEL_OPEN_KEY, open ? '1' : '0');
    } catch {
      /* storage unavailable — preference stays in memory for this session */
    }
  }

  private restoreOpen(): boolean {
    try {
      // Default open: the panel is the workspace's context surface, and a
      // collapsed-by-default panel would be invisible to most users.
      return localStorage.getItem(PANEL_OPEN_KEY) !== '0';
    } catch {
      return true;
    }
  }
}
