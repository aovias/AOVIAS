import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import type { PageTabItem } from '../../../../shared/ui/navigation/page-tabs/page-tabs.component';
import { UserContextService } from '../../../../core/services/user-context.service';
import { AuditCapabilityShellComponent } from './audit-capability-shell.component';
import { AuditWorkflowEngine } from '../services/audit-workflow.engine';

/**
 * Execution capability — the single place audits are carried out.
 *
 * Phase 2 consolidation:
 *  - **Surveillance** is no longer an independent workspace or menu entry. It is
 *    an audit-type view inside Execution (Step 5).
 *  - **Sessions**, **Observations**, and the **session workspace** moved here
 *    from the separate `/app/auditor/**` tree, which now redirects.
 *  - **History** moved out to Reports, where the spec places it (Step 3).
 *
 * Only navigation and presentation are unified. Each hosted page keeps its own
 * component, service, and business logic.
 */
@Component({
  selector: 'app-audit-execution-shell',
  standalone: true,
  imports: [AuditCapabilityShellComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<app-audit-capability-shell [tabs]="tabs()" ariaLabel="Audit execution activities" />`,
})
export class AuditExecutionShellComponent {
  private readonly userContext = inject(UserContextService);
  private readonly workflow = inject(AuditWorkflowEngine);

  protected readonly tabs = computed<readonly PageTabItem[]>(() =>
    this.workflow.surfacesFor('execution', this.userContext.getUserRoles()),
  );
}
