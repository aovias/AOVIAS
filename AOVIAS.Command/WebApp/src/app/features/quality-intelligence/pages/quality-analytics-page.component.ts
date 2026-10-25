import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { QualityApiService } from '../services/quality-api.service';
import type { QualityAnalytics } from '../models/quality-indicator.models';
import { formatQualityLabel } from '../models/quality-indicator.models';

@Component({
  selector: 'app-quality-analytics-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/quality-intelligence-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header"><div><h1>Quality Analytics</h1><p class="subtitle">Analytics Hub integration — {{ analytics()?.analyticsSource }}</p></div>
        <a class="btn-secondary" routerLink="/app/analytics">Open Analytics Hub</a>
      </header>
      @if (analytics(); as a) {
        <div class="ps-kpis">
          <div class="kpi kpi--score"><span class="kpi__v">{{ a.overallQualityScore }}</span><span class="kpi__l">Quality Score</span></div>
          <div class="kpi"><span class="kpi__v">{{ a.journeyReadiness }}</span><span class="kpi__l">Journey Readiness</span></div>
          <div class="kpi"><span class="kpi__v">{{ a.capaEffectiveness }}%</span><span class="kpi__l">CAPA Effectiveness</span></div>
        </div>
        <div class="ps-breakdown">
          <div class="breakdown"><h4>Indicator Health</h4>@for (row of a.indicatorHealth; track row.label) { <div class="row"><span>{{ formatLabel(row.label) }}</span><span>{{ row.count }}</span></div> }</div>
          <div class="breakdown"><h4>Department Heatmap</h4>@for (row of a.departmentHeatmap; track row.label) { <div class="row"><span>{{ formatLabel(row.label) }}</span><span>{{ row.count }}</span></div> }</div>
        </div>
      }
    </div>
  `,
})
export class QualityAnalyticsPageComponent implements OnInit {
  private readonly api = inject(QualityApiService);
  private readonly aiContext = inject(AiPageContextService);
  readonly analytics = signal<QualityAnalytics | null>(null);
  readonly formatLabel = formatQualityLabel;
  ngOnInit(): void { this.aiContext.setPageContext({ pageContext: 'QualityIntelligence', label: 'Quality Analytics' }); this.api.getAnalytics().subscribe((a) => this.analytics.set(a)); }
}
