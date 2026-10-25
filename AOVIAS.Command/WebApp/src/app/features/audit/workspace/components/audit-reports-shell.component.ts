import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import type { PageTabItem } from '../../../../shared/ui/navigation/page-tabs/page-tabs.component';
import { UserContextService } from '../../../../core/services/user-context.service';
import { AuditCapabilityShellComponent } from './audit-capability-shell.component';
import { AuditWorkflowEngine } from '../services/audit-workflow.engine';

/**
 * Reports capability — the single place every audit reporting surface lives
 * (Step 3).
 *
 * Consolidates three previously separate destinations:
 *  - `/app/audit/reports`      → Audit Reports (findings rollup + exports)
 *  - `/app/auditor/reports`    → Session Reports
 *  - `/app/auditor/history`    → History
 *
 * Simulation output stays in Execution rather than appearing here as well: the
 * simulation page both *runs* a mock audit and shows its result, so hosting it
 * in two tabs would breach "no capability under multiple tabs" (Step 10).
 *
 * Generated audit documents (`POST /api/v1/audit/generate`) are deliberately
 * absent — surfacing them needs new API wiring, which is out of Phase 2 scope.
 */
@Component({
  selector: 'app-audit-reports-shell',
  standalone: true,
  imports: [AuditCapabilityShellComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<app-audit-capability-shell [tabs]="tabs()" ariaLabel="Audit reporting sections" />`,
})
export class AuditReportsShellComponent {
  private readonly userContext = inject(UserContextService);
  private readonly workflow = inject(AuditWorkflowEngine);

  protected readonly tabs = computed<readonly PageTabItem[]>(() =>
    this.workflow.surfacesFor('reports', this.userContext.getUserRoles()),
  );
}
