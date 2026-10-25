import { Injectable, inject, signal } from '@angular/core';
import {
  NavigationBadgeKeys,
  type NavigationBadgeKey,
  type NavigationBadgeProvider,
  type NavigationBadgeSnapshot,
} from '../../models/badge.model';
import type { BadgeResolver } from './navigation.tokens';

/** Mutable Action Center badge signals (mock today; API later). */
@Injectable({ providedIn: 'root' })
export class NavigationBadgeStore {
  readonly findings = signal<NavigationBadgeSnapshot | null>(null);
  readonly issues = signal<NavigationBadgeSnapshot | null>(null);
  readonly alerts = signal<NavigationBadgeSnapshot | null>(null);

  seedMocks(): void {
    this.findings.set({ count: 3, variant: 'critical' });
    this.issues.set({ count: 2, variant: 'warning' });
    this.alerts.set(null);
  }
}

@Injectable({ providedIn: 'root' })
export class FindingsBadgeProvider implements NavigationBadgeProvider {
  readonly key = NavigationBadgeKeys.Findings;
  private readonly store = inject(NavigationBadgeStore);
  resolve(): NavigationBadgeSnapshot | null {
    return this.store.findings();
  }
}

@Injectable({ providedIn: 'root' })
export class IssuesBadgeProvider implements NavigationBadgeProvider {
  readonly key = NavigationBadgeKeys.Issues;
  private readonly store = inject(NavigationBadgeStore);
  resolve(): NavigationBadgeSnapshot | null {
    return this.store.issues();
  }
}

@Injectable({ providedIn: 'root' })
export class AlertsBadgeProvider implements NavigationBadgeProvider {
  readonly key = NavigationBadgeKeys.Alerts;
  private readonly store = inject(NavigationBadgeStore);
  resolve(): NavigationBadgeSnapshot | null {
    return this.store.alerts();
  }
}

@Injectable({ providedIn: 'root' })
export class CompositeBadgeResolver implements BadgeResolver {
  private readonly findings = inject(FindingsBadgeProvider);
  private readonly issues = inject(IssuesBadgeProvider);
  private readonly alerts = inject(AlertsBadgeProvider);

  private get providers(): readonly NavigationBadgeProvider[] {
    return [this.findings, this.issues, this.alerts];
  }

  resolve(key: NavigationBadgeKey): NavigationBadgeSnapshot | null {
    return this.providers.find((p) => p.key === key)?.resolve() ?? null;
  }
}
