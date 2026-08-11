import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { injectAuditWorkspace } from '../services/audit-workspace-participation.service';
import { AuditCapabilityPlaceholderComponent } from './audit-capability-placeholder.component';

/** Administration capability — placeholder host (Phase 1). */
@Component({
  selector: 'app-audit-administration-page',
  standalone: true,
  imports: [AuditCapabilityPlaceholderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-audit-capability-placeholder
      capability="Audit Administration"
      description="Configuration that audit planning and execution depend on."
      [planned]="planned"
    />
  `,
})
export class AuditAdministrationPageComponent implements OnInit {
  /** Withdrawn automatically on destroy. */
  private readonly workspace = injectAuditWorkspace();

  /**
   * Declares a subject even though the capability is not built yet, so the
   * panel and the assistant name where the user is instead of falling back to
   * the generic workspace context.
   */
  ngOnInit(): void {
    this.workspace.set({
      stage: null,
      subject: { kind: 'none', title: 'Audit administration' },
    });
  }

  protected readonly planned = [
    'Audit types',
    'Checklist templates',
    'Prep templates',
    'Audit teams',
  ];
}
