import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { IncidentApiService } from '../services/incident-api.service';
import { formatIncidentLabel, type IncidentAnalytics } from '../models/incident.models';

@Component({
  selector: 'app-patient-safety-analytics-page',
  standalone: true,
  imports: [CommonModule, DecimalPipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/patient-safety-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header">
        <div>
          <h1>Patient Safety Analytics</h1>
          <p class="subtitle">Trends and conversion metrics — reuses Analytics Hub patterns</p>
        </div>
        <a routerLink="/app/analytics" class="btn-link">Open Analytics Hub</a>
      </header>
      @if (analytics(); as a) {
        <div class="kpis">
          <div class="kpi"><span class="v">{{ a.repeatIncidents }}</span><span class="l">Repeat Incidents</span></div>
          <div class="kpi"><span class="v">{{ a.averageResolutionDays | number: '1.0-1' }}</span><span class="l">Avg Resolution (days)</span></div>
          <div class="kpi"><span class="v">{{ a.lessonsLearnedCount }}</span><span class="l">Lessons Learned</span></div>
          <div class="kpi"><span class="v">{{ a.capaConversionRate | number: '1.0-1' }}%</span><span class="l">CAPA Conversion</span></div>
        </div>
        <div class="sections">
          <section>
            <h3>Severity Distribution</h3>
            @for (row of a.severityDistribution; track row.label) {
              <div class="row"><span>{{ formatLabel(row.label) }}</span><span>{{ row.count }}</span></div>
            }
          </section>
          <section>
            <h3>Department Heatmap</h3>
            @for (row of a.departmentHeatmap; track row.label) {
              <div class="row"><span>{{ formatLabel(row.label) }}</span><span>{{ row.count }}</span></div>
            }
          </section>
        </div>
      }
    </div>
  `,
})
export class PatientSafetyAnalyticsPageComponent implements OnInit {
  private readonly api = inject(IncidentApiService);
  private readonly aiContext = inject(AiPageContextService);
  readonly analytics = signal<IncidentAnalytics | null>(null);
  readonly formatLabel = formatIncidentLabel;

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'PatientSafety' });
    this.api.getAnalytics().subscribe((a) => this.analytics.set(a));
  }
}
