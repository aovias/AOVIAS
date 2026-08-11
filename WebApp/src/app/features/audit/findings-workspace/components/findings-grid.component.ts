import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { type FindingsWorkspaceItem } from '../models/findings-workspace.model';
import { OrganizationService } from '../../../../core/services/organization.service';
import { PlatformConfigurationService } from '../../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../../core/configuration/audit-configuration.catalogs';

@Component({
  selector: 'app-findings-grid',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="aov-table-wrap praman-scrollbar praman-scrollbar--dense">
      <table class="aov-table aov-table--wide">
        <thead>
          <tr>
            <th scope="col" (click)="sort.emit('number')">Finding #</th>
            <th scope="col" (click)="sort.emit('type')">Type</th>
            <th scope="col">Source</th>
            <th scope="col" (click)="sort.emit('severity')">Severity</th>
            <th scope="col">Chapter</th>
            <th scope="col">Standard</th>
            <th scope="col">OE</th>
            <th scope="col">Department</th>
            <th scope="col">Owner</th>
            <th scope="col" (click)="sort.emit('status')">Status</th>
            <th scope="col">Promotion</th>
            <th scope="col" (click)="sort.emit('created')">Created</th>
            <th scope="col" (click)="sort.emit('updated')">Updated</th>
          </tr>
        </thead>
        <tbody>
          @for (row of items(); track row.id) {
            <tr
              class="is-selectable"
              [attr.aria-selected]="selectedId() === row.id"
              (click)="select.emit(row.id)"
            >
              <td>
                <div class="num">{{ row.findingNumber }}</div>
                <div class="title">{{ row.title }}</div>
              </td>
              <td>{{ formatType(row.classification) }}</td>
              <td>{{ formatSource(row.source) }}</td>
              <td>
                <span [class]="severityClass(row.severity)">{{
                  formatSeverity(row.severity)
                }}</span>
              </td>
              <td>{{ row.chapterCode || row.clauseReference || '—' }}</td>
              <td>{{ row.standardCode || '—' }}</td>
              <td>{{ row.objectiveElementCode || '—' }}</td>
              <td>{{ row.department }}</td>
              <td>{{ formatOwner(row.owner) }}</td>
              <td>{{ formatStatus(row.status) }}</td>
              <td>
                <span class="ready" [attr.data-ready]="row.promotionReadiness">{{
                  formatPromotionReadiness(row.promotionReadiness)
                }}</span>
              </td>
              <td>{{ row.createdDate | date: 'mediumDate' }}</td>
              <td>{{ row.lastUpdated | date: 'mediumDate' }}</td>
            </tr>
          } @empty {
            <tr>
              <td colspan="13" class="empty">No findings match the current filters.</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: [
    `
      /* 13 columns need more room than the shared --wide default. */
      .aov-table {
        min-inline-size: 72rem;
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
        color: var(--text-secondary);
        background: var(--bg-primary, #f9fafb);
        border-bottom: 1px solid var(--border-color, #e5e7eb);
        cursor: pointer;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }
      th:nth-child(1) {
        width: 14%;
      }
      th:nth-child(2) {
        width: 8%;
      }
      th:nth-child(3) {
        width: 9%;
      }
      th:nth-child(4) {
        width: 7%;
      }
      th:nth-child(5) {
        width: 5%;
      }
      th:nth-child(6) {
        width: 5%;
      }
      th:nth-child(7) {
        width: 5%;
      }
      th:nth-child(8) {
        width: 7%;
      }
      th:nth-child(9) {
        width: 7%;
      }
      th:nth-child(10) {
        width: 6%;
      }
      th:nth-child(11) {
        width: 10%;
      }
      th:nth-child(12) {
        width: 8%;
      }
      th:nth-child(13) {
        width: 9%;
      }
      td {
        padding: 12px;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
        vertical-align: top;
        overflow: hidden;
        text-overflow: ellipsis;
      }
      td:first-child {
        overflow: visible;
        text-overflow: clip;
      }
      .num {
        font-size: 11px;
        color: var(--text-secondary);
      }
      .title {
        font-weight: 500;
      }
      .sev--critical {
        color: #dc2626;
        font-weight: 600;
      }
      .sev--major {
        color: #ea580c;
        font-weight: 600;
      }
      .empty {
        text-align: center;
        color: var(--text-secondary);
        padding: 32px;
      }
      .ready {
        font-size: 11px;
        font-weight: 600;
        padding: 2px 6px;
        border-radius: 4px;
        background: #f3f4f6;
      }
      .ready[data-ready='Eligible'] {
        background: var(--color-primary-100);
        color: var(--color-primary-hover);
      }
      .ready[data-ready='Promoted'] {
        background: #d1fae5;
        color: #047857;
      }
      .ready[data-ready='Awaiting Acceptance'] {
        background: #fef3c7;
        color: #b45309;
      }
      .ready[data-ready='Not Eligible'] {
        background: #f3f4f6;
        color: #6b7280;
      }
    `,
  ],
})
export class FindingsGridComponent {
  private readonly organization = inject(OrganizationService);
  private readonly configuration = inject(PlatformConfigurationService);

  readonly items = input<FindingsWorkspaceItem[]>([]);
  readonly selectedId = input<string | null>(null);
  readonly select = output<string>();
  readonly sort = output<string>();

  /**
   * Classification label from configuration.
   *
   * Replaces a regex that inserted spaces before capitals and then string-replaced one long result
   * with "OFI" — a rule that read as a formatting quirk but was really a display-name decision, and
   * one no administrator could reach.
   */
  formatType(v: string): string {
    const category = CONFIGURATION_CATEGORY.findingClassification;
    const label = this.configuration.label(category, v, v);
    return this.configuration.attribute(category, v, 'shortLabel', label);
  }

  /**
   * The findings API returns `owner` as a bare user GUID, which reached the user verbatim.
   * Resolved against the organization member directory; unresolvable ids show the placeholder
   * rather than a raw identifier.
   */
  formatOwner(owner: string | null | undefined): string {
    return this.organization.getMemberDisplayName(owner) || '—';
  }

  /**
   * `source` arrives as the `FindingRaisedBy` ordinal string. Rows holding `0` are outside the enum
   * (it starts at 1) and previously rendered as a bare `0`; they show the unspecified marker.
   */
  formatSource(v: string): string {
    return this.configuration.labelByOrdinalOrCode(CONFIGURATION_CATEGORY.findingSource, v);
  }

  formatStatus(v: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.findingStatus, v);
  }

  formatSeverity(v: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.findingSeverity, v, v);
  }

  /**
   * Severity's visual identity, from configuration rather than from its code.
   *
   * The template built this as `'sev--' + severity.toLowerCase()`, so a severity whose code was not
   * already a valid CSS token rendered unstyled — invisible until someone configured one.
   */
  severityClass(v: string): string {
    const token = this.configuration.attribute(
      CONFIGURATION_CATEGORY.findingSeverity,
      v,
      'badgeToken',
      '',
    );
    return token ? `sev sev--${token}` : 'sev';
  }

  formatPromotionReadiness(v: string): string {
    return this.configuration.label(CONFIGURATION_CATEGORY.promotionReadiness, v, v);
  }
}
