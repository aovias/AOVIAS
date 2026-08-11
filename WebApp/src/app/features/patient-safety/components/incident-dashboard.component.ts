import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import type { IncidentDashboard } from '../models/incident.models';
import { formatIncidentLabel } from '../models/incident.models';

@Component({
  selector: 'app-incident-dashboard',
  standalone: true,
  imports: [CommonModule, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/patient-safety-workspace.layout.scss',
  template: `
    @if (dashboard(); as d) {
      <div class="ps-kpis">
        <div class="kpi"><span class="kpi__v">{{ d.openIncidents }}</span><span class="kpi__l">Open Incidents</span></div>
        <div class="kpi kpi--critical"><span class="kpi__v">{{ d.criticalIncidents }}</span><span class="kpi__l">Critical</span></div>
        <div class="kpi"><span class="kpi__v">{{ d.nearMisses }}</span><span class="kpi__l">Near Misses</span></div>
        <div class="kpi kpi--sentinel"><span class="kpi__v">{{ d.sentinelEvents }}</span><span class="kpi__l">Sentinel Events</span></div>
        <div class="kpi"><span class="kpi__v">{{ d.investigationsInProgress }}</span><span class="kpi__l">Investigations</span></div>
        <div class="kpi"><span class="kpi__v">{{ d.rcaPending }}</span><span class="kpi__l">RCA Pending</span></div>
        <div class="kpi"><span class="kpi__v">{{ d.repeatedIncidents }}</span><span class="kpi__l">Repeated</span></div>
        <div class="kpi"><span class="kpi__v">{{ d.departmentsAtRisk }}</span><span class="kpi__l">Depts at Risk</span></div>
        <div class="kpi"><span class="kpi__v">{{ d.averageClosureDays | number: '1.0-1' }}</span><span class="kpi__l">Avg Closure (days)</span></div>
        <div class="kpi"><span class="kpi__v">{{ d.openCapas }}</span><span class="kpi__l">Open CAPAs</span></div>
        <div class="kpi kpi--score"><span class="kpi__v">{{ d.patientSafetyScore | number: '1.0-1' }}</span><span class="kpi__l">Safety Score</span></div>
      </div>
      <div class="ps-breakdown">
        @if (d.byCategory.length) {
          <div class="breakdown">
            <h4>By Category</h4>
            @for (row of d.byCategory.slice(0, 6); track row.label) {
              <div class="row"><span>{{ formatLabel(row.label) }}</span><span>{{ row.count }}</span></div>
            }
          </div>
        }
        @if (d.byDepartment.length) {
          <div class="breakdown">
            <h4>By Department</h4>
            @for (row of d.byDepartment.slice(0, 5); track row.label) {
              <div class="row"><span>{{ formatLabel(row.label) }}</span><span>{{ row.count }}</span></div>
            }
          </div>
        }
        @if (d.bySeverity.length) {
          <div class="breakdown">
            <h4>By Severity</h4>
            @for (row of d.bySeverity; track row.label) {
              <div class="row"><span>{{ formatLabel(row.label) }}</span><span>{{ row.count }}</span></div>
            }
          </div>
        }
      </div>
    }
  `,
})
export class IncidentDashboardComponent {
  readonly dashboard = input<IncidentDashboard | null>(null);
  readonly formatLabel = formatIncidentLabel;
}
