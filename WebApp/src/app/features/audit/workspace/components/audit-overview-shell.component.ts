import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import type { PageTabItem } from '../../../../shared/ui/navigation/page-tabs/page-tabs.component';
import { UserContextService } from '../../../../core/services/user-context.service';
import { AuditCapabilityShellComponent } from './audit-capability-shell.component';
import { AuditWorkflowEngine } from '../services/audit-workflow.engine';

/**
 * Overview capability.
 *
 * Phase 2 surfaces the external Auditor Home here. It was previously reachable
 * only through its own sidebar entry (`workspace.dashboard.auditor-home`) and
 * became undiscoverable once Phase 1 collapsed the Audit children — a hidden
 * capability (Step 6). It is a genuinely different dashboard from the internal
 * programme view, so it is a sibling rather than a duplicate.
 *
 * Internal users see a single section and therefore no sub-tab strip.
 */
@Component({
  selector: 'app-audit-overview-shell',
  standalone: true,
  imports: [AuditCapabilityShellComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<app-audit-capability-shell [tabs]="tabs()" ariaLabel="Audit overview sections" />`,
})
export class AuditOverviewShellComponent {
  private readonly userContext = inject(UserContextService);
  private readonly workflow = inject(AuditWorkflowEngine);

  /**
   * Which surfaces this user may reach is a workflow decision, so the engine makes it.
   *
   * This component previously held the list *and* the role check, as did the Execution and Reports
   * shells — three copies of the same rule, each free to drift from the route guards that enforce
   * it.
   */
  protected readonly tabs = computed<readonly PageTabItem[]>(() =>
    this.workflow.surfacesFor('overview', this.userContext.getUserRoles()),
  );
}
