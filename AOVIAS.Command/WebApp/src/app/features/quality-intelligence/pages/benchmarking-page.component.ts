import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { QualityApiService } from '../services/quality-api.service';
import type { QualityBenchmarking } from '../models/quality-indicator.models';

@Component({
  selector: 'app-benchmarking-page',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/quality-intelligence-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header"><h1>Benchmarking</h1><p class="subtitle">{{ benchmarking()?.comparisonPeriod ?? 'Period' }} comparison</p></header>
      <div class="grid-wrap"><table class="data-table"><thead><tr><th>Indicator</th><th>Current</th><th>Previous</th><th>Change %</th><th>Target</th></tr></thead>
        <tbody>@for (item of benchmarking()?.items ?? []; track item.indicatorId) {
          <tr><td>{{ item.name }}</td><td>{{ item.currentValue ?? '—' }}</td><td>{{ item.previousValue ?? '—' }}</td><td>{{ item.changePercent ?? '—' }}</td><td>{{ item.targetValue ?? '—' }}</td></tr>
        }</tbody></table></div>
    </div>
  `,
})
export class BenchmarkingPageComponent implements OnInit {
  private readonly api = inject(QualityApiService);
  private readonly aiContext = inject(AiPageContextService);
  readonly benchmarking = signal<QualityBenchmarking | null>(null);
  ngOnInit(): void { this.aiContext.setPageContext({ pageContext: 'QualityIntelligence', label: 'Benchmarking' }); this.api.getBenchmarking('PreviousMonth').subscribe((b) => this.benchmarking.set(b)); }
}
