import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { JourneyWorkspaceStore, MaturityStageFilter } from '../state/journey-workspace.store';
import { UserContextService } from '../../../core/services/user-context.service';
import {
  PageTabsComponent,
  type PageTabItem,
} from '../../../shared/ui/navigation/page-tabs/page-tabs.component';

/** Auditor / regulator role ids — same constant as {@link journeyRoleRedirectGuard}. */
const AUDITOR_ROLES = new Set(['REGULATOR', 'AUTHORITY', 'REGULATORY_ADMIN']);
const VALID_STAGES = new Set<MaturityStageFilter>([
  'core',
  'commitment',
  'achievement',
  'excellence',
]);

/**
 * Wraps all `/app/journey/**` routes with a single {@link JourneyWorkspaceStore} so dashboard
 * and execution share one workspace instance (avoids aborting in-flight HTTP when switching child routes).
 *
 * Also renders a subtle role-aware nav strip so users can switch between the
 * maturity view (hospital users) and the operational view (auditors) if needed.
 */
@Component({
  selector: 'app-journey-feature-shell',
  standalone: true,
  imports: [RouterOutlet, PageTabsComponent],
  template: `
    <div class="aov-module-nav journey-shell-nav">
      <app-page-tabs
        [tabs]="tabs"
        variant="primary"
        [inset]="true"
        ariaLabel="Journey view switcher"
      />
      @if (isAuditor()) {
        <span class="aov-chip journey-shell-nav__role-badge">Auditor View</span>
      }
    </div>
    <router-outlet />
  `,
  styles: [
    `
      /* The role badge sits on the same baseline as the tab strip. */
      .journey-shell-nav {
        display: flex;
        align-items: center;
        gap: var(--space-3);
      }

      .journey-shell-nav app-page-tabs {
        flex: 1 1 auto;
        min-inline-size: 0;
      }

      .journey-shell-nav__role-badge {
        flex-shrink: 0;
        align-self: center;
      }
    `,
  ],
  providers: [JourneyWorkspaceStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JourneyFeatureShellComponent implements OnInit {
  private readonly store = inject(JourneyWorkspaceStore);
  private readonly userContext = inject(UserContextService);
  private readonly router = inject(Router);

  protected readonly tabs: readonly PageTabItem[] = [
    { id: 'execution', label: 'Execution Workspace', route: '/app/journey/execution' },
    { id: 'maturity', label: 'Execution Launchpad', route: '/app/journey/maturity' },
    { id: 'operational', label: 'Operational View', route: '/app/journey/operational' },
  ];

  readonly isAuditor = computed(() =>
    this.userContext.getUserRoles().some((r) => AUDITOR_ROLES.has(r)),
  );

  ngOnInit(): void {
    this.seedStageFilterFromUrl();
    this.store.initFromRoute();
  }

  /** Apply ?stage= or /execution/stage/:stage before the first workspace fetch. */
  private seedStageFilterFromUrl(): void {
    const parsed = this.router.parseUrl(this.router.url);
    let raw = parsed.queryParams['stage']?.toLowerCase();
    if (!raw) {
      const segments = this.router.url.split('?')[0].split('/').filter(Boolean);
      const stageIdx = segments.indexOf('stage');
      if (stageIdx >= 0 && segments[stageIdx + 1]) {
        raw = segments[stageIdx + 1].toLowerCase();
      }
    }
    if (raw && VALID_STAGES.has(raw as MaturityStageFilter)) {
      this.store.seedMaturityStageFilter(raw as MaturityStageFilter);
    }
  }
}
