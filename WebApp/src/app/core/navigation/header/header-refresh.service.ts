import { Injectable, signal } from '@angular/core';

/**
 * Presentation bus for the shell Refresh action (NAV-2).
 * Pages may subscribe later; this sprint only publishes ticks.
 */
@Injectable({ providedIn: 'root' })
export class HeaderRefreshService {
  private readonly tick = signal(0);

  readonly refreshTick = this.tick.asReadonly();

  requestRefresh(): void {
    this.tick.update((n) => n + 1);
  }
}
