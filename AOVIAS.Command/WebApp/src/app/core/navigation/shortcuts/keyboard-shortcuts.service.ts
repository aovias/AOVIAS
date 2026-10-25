import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser, DOCUMENT } from '@angular/common';
import { Router } from '@angular/router';
import { NavigationQueryService } from '../engine/navigation-query.service';
import { CommandPaletteService } from '../search/command-palette.service';

/** Extension point for future AI / NL command registration. */
export interface NavigationShortcutExtension {
  readonly id: string;
  readonly chord: string;
  readonly description: string;
  execute(): void;
}

/**
 * Global keyboard shortcuts for intelligent navigation (NAV-4).
 * Ctrl/Cmd+K → command palette; Alt+1/2/3 → Dashboard / Audit / Reports.
 */
@Injectable({ providedIn: 'root' })
export class KeyboardShortcutsService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);
  private readonly palette = inject(CommandPaletteService);
  private readonly query = inject(NavigationQueryService);

  private readonly extensions = new Map<string, NavigationShortcutExtension>();
  private bound = false;
  private readonly onKey = (event: KeyboardEvent) => this.handle(event);

  /** Call once from shell bootstrap. */
  start(): void {
    if (!isPlatformBrowser(this.platformId) || this.bound) return;
    this.document.addEventListener('keydown', this.onKey);
    this.bound = true;
  }

  stop(): void {
    if (!this.bound) return;
    this.document.removeEventListener('keydown', this.onKey);
    this.bound = false;
  }

  /** Future AI commands register here — no-op until wired. */
  registerExtension(ext: NavigationShortcutExtension): void {
    this.extensions.set(ext.id, ext);
  }

  unregisterExtension(id: string): void {
    this.extensions.delete(id);
  }

  private handle(event: KeyboardEvent): void {
    const target = event.target as HTMLElement | null;
    const inEditable =
      !!target &&
      (target.tagName === 'INPUT' ||
        target.tagName === 'TEXTAREA' ||
        target.tagName === 'SELECT' ||
        target.isContentEditable);

    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      this.palette.toggle();
      return;
    }

    if (event.key === 'Escape' && this.palette.isOpen()) {
      event.preventDefault();
      this.palette.close();
      return;
    }

    if (inEditable || this.palette.isOpen()) return;

    if (event.altKey && !event.ctrlKey && !event.metaKey) {
      if (event.key === '1') {
        event.preventDefault();
        void this.router.navigateByUrl('/app/dashboard');
        return;
      }
      if (event.key === '2') {
        event.preventDefault();
        const audit = this.query.findModule('module.audit');
        void this.router.navigateByUrl(audit?.defaultRoute ?? '/app/audit');
        return;
      }
      if (event.key === '3') {
        event.preventDefault();
        const reports = this.query.findModule('module.reports');
        void this.router.navigateByUrl(reports?.defaultRoute ?? '/app/reports/compliance');
        return;
      }
    }
  }
}
