import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { QualityApiService } from '../services/quality-api.service';
import type { QualityTrends } from '../models/quality-indicator.models';
import { formatQualityLabel } from '../models/quality-indicator.models';

@Component({
  selector: 'app-quality-trends-page',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/quality-intelligence-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header"><div><h1>Quality Trends</h1><p class="subtitle">Trend engine — improving, stable, declining, volatile</p></div>
        <select [(ngModel)]="period" (ngModelChange)="load()"><option value="Monthly">Monthly</option><option value="Quarterly">Quarterly</option><option value="Weekly">Weekly</option></select>
      </header>
      @for (series of trends()?.series ?? []; track series.indicatorId) {
        <section class="card" style="margin-bottom:16px">
          <h3>{{ series.name }} <span class="muted">({{ formatLabel(series.trend) }})</span></h3>
          @if (series.movingAverage != null) { <p class="muted">Moving avg: {{ series.movingAverage }}</p> }
          @for (pt of series.points.slice(0, 8); track pt.id) {
            <div class="row"><span>{{ pt.periodEnd | date: 'MMM yyyy' }}</span><span>{{ pt.value }}</span></div>
          }
        </section>
      }
    </div>
  `,
})
export class QualityTrendsPageComponent implements OnInit {
  private readonly api = inject(QualityApiService);
  private readonly aiContext = inject(AiPageContextService);
  readonly trends = signal<QualityTrends | null>(null);
  period = 'Monthly';
  readonly formatLabel = formatQualityLabel;

  ngOnInit(): void { this.aiContext.setPageContext({ pageContext: 'QualityIntelligence', label: 'Quality Trends' }); this.load(); }
  load(): void { this.api.getTrends(this.period).subscribe((t) => this.trends.set(t)); }
}
