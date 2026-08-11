import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import type { QualityDashboard } from '../models/quality-indicator.models';
import { formatQualityLabel } from '../models/quality-indicator.models';

@Component({
  selector: 'app-indicator-dashboard',
  standalone: true,
  imports: [CommonModule, DecimalPipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/quality-intelligence-workspace.layout.scss',
  template: `
    @if (dashboard(); as d) {
      <div class="ps-kpis">
        <div class="kpi kpi--score"><span class="kpi__v">{{ d.overallQualityScore | number: '1.0-1' }}</span><span class="kpi__l">Quality Score</span></div>
        <div class="kpi"><span class="kpi__v">{{ d.indicatorsWithinTarget }}</span><span class="kpi__l">Within Target</span></div>
        <div class="kpi"><span class="kpi__v">{{ d.warningIndicators }}</span><span class="kpi__l">Warning</span></div>
        <div class="kpi kpi--critical"><span class="kpi__v">{{ d.criticalIndicators }}</span><span class="kpi__l">Critical</span></div>
        <div class="kpi"><span class="kpi__v">{{ d.improvingTrends }}</span><span class="kpi__l">Improving</span></div>
        <div class="kpi"><span class="kpi__v">{{ d.decliningTrends }}</span><span class="kpi__l">Declining</span></div>
        <div class="kpi"><span class="kpi__v">{{ d.activeIndicators }}</span><span class="kpi__l">Active Indicators</span></div>
        <div class="kpi kpi--score"><span class="kpi__v">{{ d.patientSafetyScore | number: '1.0-1' }}</span><span class="kpi__l">Patient Safety</span></div>
        <div class="kpi kpi--critical"><span class="kpi__v">{{ d.accreditationRiskScore | number: '1.0-1' }}</span><span class="kpi__l">Accreditation Risk</span></div>
        <div class="kpi"><span class="kpi__v">{{ d.capaEffectivenessScore | number: '1.0-1' }}%</span><span class="kpi__l">CAPA Effectiveness</span></div>
      </div>
      <div class="ps-breakdown">
        @if (d.byCategory.length) {
          <div class="breakdown"><h4>By Category</h4>@for (row of d.byCategory.slice(0, 6); track row.label) { <div class="row"><span>{{ formatLabel(row.label) }}</span><span>{{ row.count }}</span></div> }</div>
        }
        @if (d.byDepartment.length) {
          <div class="breakdown"><h4>By Department</h4>@for (row of d.byDepartment.slice(0, 5); track row.label) { <div class="row"><span>{{ formatLabel(row.label) }}</span><span>{{ row.count }}</span></div> }</div>
        }
        @if (d.byThresholdStatus.length) {
          <div class="breakdown"><h4>Threshold Status</h4>@for (row of d.byThresholdStatus; track row.label) { <div class="row"><span>{{ formatLabel(row.label) }}</span><span>{{ row.count }}</span></div> }</div>
        }
      </div>
      @if (showShortcuts()) {
        <div class="shortcuts">
          <a routerLink="/app/quality-intelligence/indicators">Clinical Indicators</a>
          <a routerLink="/app/quality-intelligence/scorecards">Department Scorecards</a>
          <a routerLink="/app/quality-intelligence/executive">Executive Summary</a>
          <a routerLink="/app/patient-safety/dashboard">Patient Safety</a>
        </div>
      }
    }
  `,
})
export class IndicatorDashboardComponent {
  readonly dashboard = input<QualityDashboard | null>(null);
  readonly showShortcuts = input(true);
  readonly formatLabel = formatQualityLabel;
}
