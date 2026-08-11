import { Injectable, signal, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type OperationalMode = 'self-assessment' | 'internal-audit' | 'final-audit';

const STORAGE_KEY = 'praman-operational-mode';

const LABELS: Record<OperationalMode, string> = {
  'self-assessment': 'Self Assessment',
  'internal-audit': 'Internal Audit',
  'final-audit': 'Final Audit',
};

@Injectable({
  providedIn: 'root',
})
export class ShellOperationalModeService {
  private readonly platformId = inject(PLATFORM_ID);

  /** Current operational mode (session-backed in browser). */
  readonly mode = signal<OperationalMode>(this.readInitial());

  label(mode: OperationalMode): string {
    return LABELS[mode];
  }

  currentLabel(): string {
    return LABELS[this.mode()];
  }

  setMode(next: OperationalMode): void {
    this.mode.set(next);
    if (isPlatformBrowser(this.platformId)) {
      sessionStorage.setItem(STORAGE_KEY, next);
    }
  }

  cycleModes(): OperationalMode[] {
    return ['self-assessment', 'internal-audit', 'final-audit'];
  }

  private readInitial(): OperationalMode {
    if (!isPlatformBrowser(this.platformId)) {
      return 'self-assessment';
    }
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (raw === 'internal-audit' || raw === 'final-audit' || raw === 'self-assessment') {
      return raw;
    }
    return 'self-assessment';
  }
}
