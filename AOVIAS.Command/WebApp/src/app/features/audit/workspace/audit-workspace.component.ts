import { ChangeDetectionStrategy, Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, startWith } from 'rxjs';
import { PageTabsComponent } from '../../../shared/ui/navigation/page-tabs/page-tabs.component';
import { ContextDrawerComponent } from '../../../shared/ui/context-drawer/context-drawer.component';
import { AuditContextualActionsComponent } from './components/audit-contextual-actions.component';
import { AuditWorkflowStatusBarComponent } from './components/audit-workflow-status-bar.component';
import { AUDIT_WORKSPACE_TABS, resolveAuditTabFromUrl } from './models/audit-workspace-tab.model';
import { AuditContextDrawerRegistrar } from './services/audit-context-drawer.registrar';
import { AuditWorkspaceStateService } from './services/audit-workspace-state.service';

/**
 * Enterprise Audit Workspace — the single entry point for the Audit domain.
 *
 * Phase 1 composed the existing page components behind one capability tab strip.
 * Phase 3 adds the workflow layer above the outlet — the lifecycle progress bar
 * and the contextual action toolbar — turning the shell from a container into
 * the thing that tells the user where they are and what to do next.
 *
 * The shell is instantiated once for `/app/audit/**`. Switching tabs swaps only
 * the routed content below it — the chrome is never re-created, so workspace
 * context survives navigation.
 *
 * Visual state of the tabs is owned by `routerLinkActive` inside
 * {@link PageTabsComponent} (navigation mode). The URL subscription below feeds
 * {@link AuditWorkspaceStateService} instead, which holds workspace-level
 * context rather than driving the strip.
 */
@Component({
  selector: 'app-audit-workspace',
  standalone: true,
  imports: [
    RouterOutlet,
    PageTabsComponent,
    AuditWorkflowStatusBarComponent,
    AuditContextualActionsComponent,
    ContextDrawerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './audit-workspace.component.html',
  styleUrl: './audit-workspace.component.scss',
})
export class AuditWorkspaceComponent {
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly state = inject(AuditWorkspaceStateService);
  /**
   * Injected purely to instantiate it: the registrar's constructor effect is
   * what keeps the drawer's sections in step with page participation, and
   * nothing else in the module depends on it.
   */
  private readonly drawerSections = inject(AuditContextDrawerRegistrar);

  protected readonly tabs = AUDIT_WORKSPACE_TABS;

  constructor() {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        startWith(null),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.state.setActiveTab(resolveAuditTabFromUrl(this.router.url));
      });
  }
}
