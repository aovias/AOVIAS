import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import type { IncidentListItem } from '../models/incident.models';
import { formatIncidentLabel } from '../models/incident.models';

@Component({
  selector: 'app-incident-grid',
  standalone: true,
  imports: [CommonModule, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/patient-safety-workspace.layout.scss',
  template: `
    <div class="aov-table-wrap praman-scrollbar praman-scrollbar--dense">
      <table class="aov-table aov-table--wide">
        <thead>
          <tr>
            <th scope="col" (click)="sort.emit('number')">Incident #</th>
            <th scope="col">Title</th>
            <th scope="col" (click)="sort.emit('severity')">Severity</th>
            <th scope="col">Category</th>
            <th scope="col">Department</th>
            <th scope="col">Owner</th>
            <th scope="col" (click)="sort.emit('status')">Status</th>
            <th scope="col">Investigation</th>
            <th scope="col">Date</th>
          </tr>
        </thead>
        <tbody>
          @for (row of items(); track row.id) {
            <tr
              class="is-selectable"
              [attr.aria-selected]="selectedId() === row.id"
              (click)="select.emit(row.id)"
            >
              <td class="num">{{ row.incidentNumber }}</td>
              <td>
                <div class="title">{{ row.title }}</div>
                @if (row.patientInvolved) {
                  <span class="badge">Patient</span>
                }
              </td>
              <td>
                <span [class]="'sev sev--' + row.severity.toLowerCase()">{{ row.severity }}</span>
              </td>
              <td>{{ formatLabel(row.category) }}</td>
              <td>{{ formatLabel(row.department) }}</td>
              <td>{{ row.ownerName || '—' }}</td>
              <td>{{ formatLabel(row.status) }}</td>
              <td>{{ formatLabel(row.investigationStatus) }}</td>
              <td>{{ row.incidentDateTime | date: 'mediumDate' }}</td>
            </tr>
          } @empty {
            <tr>
              <td colspan="9" class="empty">No incidents match the current filters.</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class IncidentGridComponent {
  readonly items = input<IncidentListItem[]>([]);
  readonly selectedId = input<string | null>(null);
  readonly sort = output<string>();
  readonly select = output<string>();
  readonly formatLabel = formatIncidentLabel;
}
