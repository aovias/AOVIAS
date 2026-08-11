import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';

import { CommonModule } from '@angular/common';

import { EvidenceStatusBadgeComponent } from '../../../../shared/ui/evidence-status-badge/evidence-status-badge.component';

import { type DeficiencyWorkspaceItem } from '../models/deficiency-command-center.model';
import { PlatformConfigurationService } from '../../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../../core/configuration/audit-configuration.catalogs';

@Component({
  selector: 'app-dcc-grid',

  standalone: true,

  imports: [CommonModule, EvidenceStatusBadgeComponent],

  changeDetection: ChangeDetectionStrategy.OnPush,

  template: `
    <div class="aov-table-wrap praman-scrollbar praman-scrollbar--dense">
      <table class="aov-table aov-table--wide">
        <thead>
          <tr>
            <th scope="col" (click)="sort.emit('severity')">Deficiency</th>

            <th scope="col" (click)="sort.emit('severity')">Severity</th>

            <th scope="col" (click)="sort.emit('stage')">Stage</th>

            <th scope="col">Chapter</th>

            <th scope="col">Standard</th>

            <th scope="col">Objective Element</th>

            <th scope="col" (click)="sort.emit('status')">Lifecycle</th>

            <th scope="col">Risk</th>

            <th scope="col">Primary Task</th>

            <th scope="col">Evidence</th>

            <th scope="col">Verification</th>

            <th scope="col">Owner</th>

            <th scope="col">Department</th>

            <th scope="col" (click)="sort.emit('duedate')">Target Date</th>

            <th scope="col">SLA Status</th>

            <th scope="col">Days Left</th>

            <th scope="col">Escalation</th>

            <th scope="col" (click)="sort.emit('updated')">Updated</th>

            <th scope="col">Blocking</th>
          </tr>
        </thead>

        <tbody>
          @for (row of items(); track row.id) {
            <tr
              class="is-selectable"
              [class.overdue]="row.isOverdue"
              [attr.aria-selected]="selectedId() === row.id"
              (click)="select.emit(row.id)"
            >
              <td class="title-cell">
                <div class="code">{{ row.deficiencyCode }}</div>
                @if (row.capaNumber) {
                  <div class="capa-num">{{ row.capaNumber }}</div>
                }

                <div class="title">{{ row.title }}</div>
              </td>

              <td>
                <span [class]="severityClass(row.severity)">{{
                  formatSeverity(row.severity)
                }}</span>
              </td>

              <td>{{ formatStage(row.maturityLevel) }}</td>

              <td>{{ row.chapterCode }}</td>

              <td>{{ row.standardCode }}</td>

              <td>{{ row.objectiveElementCode }}</td>

              <td>
                <span class="status-pill">{{
                  row.lifecycleStatus || formatStatus(row.status)
                }}</span>
              </td>

              <td>
                @if (row.assessedRiskLevel) {
                  <span class="risk" [class]="riskClass(row.assessedRiskLevel)">{{
                    row.assessedRiskLevel
                  }}</span>
                } @else {
                  <span class="muted">—</span>
                }
              </td>

              <td>
                @if (row.primaryTaskTitle) {
                  <span class="task-ref">{{ row.primaryTaskTitle }}</span>

                  @if (row.primaryTaskStatus) {
                    <span class="task-status">{{ row.primaryTaskStatus }}</span>
                  }
                } @else {
                  <span class="muted">—</span>
                }
              </td>

              <td><app-evidence-status-badge [status]="row.evidenceStatus" /></td>

              <td><app-evidence-status-badge [status]="row.verificationStatus" /></td>

              <td>{{ row.assignedOwner || '—' }}</td>

              <td>{{ row.department || '—' }}</td>

              <td [class.overdue-text]="row.isOverdue">
                {{
                  row.targetResolutionDate || row.dueDate
                    ? (row.targetResolutionDate || row.dueDate | date: 'mediumDate')
                    : '—'
                }}
              </td>

              <td>
                @if (row.slaStatus) {
                  <span [class]="slaClass(row.slaStatus)">{{
                    formatSlaStatus(row.slaStatus)
                  }}</span>
                } @else {
                  <span class="muted">—</span>
                }
              </td>

              <td>{{ row.daysRemaining ?? '—' }}</td>

              <td>
                @if (row.escalationLevel) {
                  L{{ row.escalationLevel }} · {{ row.escalationState || '—' }}
                } @else {
                  <span class="muted">—</span>
                }
              </td>

              <td>{{ row.updatedAt | date: 'mediumDate' }}</td>

              <td>
                <span
                  class="blocking"
                  [class.blocking--yes]="row.blockingStageLabel.startsWith('Blocks')"
                >
                  {{ row.blockingStageLabel }}
                </span>
              </td>
            </tr>
          } @empty {
            <tr>
              <td colspan="19" class="empty">No deficiencies match the current filters.</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,

  styles: [
    `
      /* 19 columns need more room than the shared --wide default. */
      .aov-table {
        min-inline-size: 80rem;
      }

      :host {
        display: block;

        width: 100%;
      }

      th {
        padding: 10px 12px;

        text-align: left;

        font-size: 11px;

        text-transform: uppercase;

        letter-spacing: 0.04em;

        color: var(--text-secondary, #6b7280);

        background: var(--bg-primary, #f9fafb);

        border-bottom: 1px solid var(--border-color, #e5e7eb);

        cursor: pointer;

        white-space: nowrap;

        overflow: hidden;

        text-overflow: ellipsis;
      }

      td {
        padding: 12px;

        border-bottom: 1px solid var(--border-color, #e5e7eb);

        vertical-align: top;

        overflow: hidden;

        text-overflow: ellipsis;
      }

      .title-cell {
        overflow: visible;

        text-overflow: clip;
      }

      tbody tr.overdue {
        background: rgba(239, 68, 68, 0.04);
      }

      .title-cell .capa-num {
        font-size: 11px;
        font-weight: 600;
        color: var(--color-primary-hover);
        margin-top: 2px;
      }

      .title-cell .code {
        font-size: 11px;

        color: var(--text-secondary);
      }

      .title-cell .title {
        font-weight: 500;
      }

      .sev {
        font-weight: 600;

        font-size: 12px;
      }

      .sev--critical {
        color: #dc2626;
      }

      .sev--major {
        color: #ea580c;
      }

      .sev--moderate {
        color: #ca8a04;
      }

      .sev--minor {
        color: #6b7280;
      }

      .risk {
        font-size: 11px;

        font-weight: 600;

        padding: 2px 6px;

        border-radius: 4px;
      }

      .risk--low {
        background: #ecfdf5;
        color: #047857;
      }

      .risk--medium {
        background: #fef9c3;
        color: #a16207;
      }

      .risk--high {
        background: #ffedd5;
        color: #c2410c;
      }

      .risk--critical {
        background: #fef2f2;
        color: #b91c1c;
      }

      .status-pill {
        font-size: 12px;
      }

      .task-ref {
        display: block;
        font-size: 12px;
      }

      .task-status {
        font-size: 11px;
        color: var(--text-secondary);
      }

      .blocking--yes {
        color: #b45309;
        font-weight: 600;
      }

      .overdue-text {
        color: #dc2626;
        font-weight: 600;
      }

      .sla {
        font-size: 11px;
        font-weight: 600;
        padding: 2px 6px;
        border-radius: 4px;
        white-space: nowrap;
      }

      .sla--ontrack {
        background: #d1fae5;
        color: #047857;
      }

      .sla--warning {
        background: #fef3c7;
        color: #b45309;
      }

      .sla--atrisk {
        background: #ffedd5;
        color: #c2410c;
      }

      .sla--breached {
        background: #fee2e2;
        color: #b91c1c;
      }

      .sla--closed {
        background: #f3f4f6;
        color: #6b7280;
      }

      .muted,
      .empty {
        color: var(--text-secondary);
      }

      .empty {
        text-align: center;
        padding: 32px;
      }
    `,
  ],
})
export class DccGridComponent {
  readonly items = input<DeficiencyWorkspaceItem[]>([]);

  readonly selectedId = input<string | null>(null);

  readonly select = output<string>();

  readonly sort = output<string>();

  private readonly configuration = inject(PlatformConfigurationService);

  formatStage(stage: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.maturityStage, stage, stage);
  }

  formatStatus(status: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.deficiencyStatus, status, status);
  }

  formatSeverity(severity: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.deficiencySeverity, severity, severity);
  }

  /**
   * Severity's visual identity, from configuration rather than from its code — the template built
   * this as `'sev--' + severity.toLowerCase()`, which rendered unstyled for any code that was not
   * already a valid CSS token.
   */
  severityClass(severity: string): string {
    const token = this.configuration.attribute(
      CONFIGURATION_CATEGORY.deficiencySeverity,
      severity,
      'badgeToken',
      '',
    );
    return token ? `sev sev--${token}` : 'sev';
  }

  riskClass(level: string): string {
    return this.configuration.attribute(CONFIGURATION_CATEGORY.riskLevel, level, 'badgeClass', '');
  }

  /**
   * SLA badge class. An unrecognised state resolves to the category's designated fallback rather
   * than to the last arm of an `if` ladder, which silently displayed every new state as "closed".
   */
  slaClass(status: string): string {
    return this.configuration.attributeOrFallback(
      CONFIGURATION_CATEGORY.slaStatus,
      status,
      'badgeClass',
    );
  }

  formatSlaStatus(status: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.slaStatus, status, status);
  }
}
