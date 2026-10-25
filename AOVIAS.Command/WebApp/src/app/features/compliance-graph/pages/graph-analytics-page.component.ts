import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { GraphApiService } from '../services/graph-api.service';
import type { GraphAnalytics } from '../models/compliance-graph.models';

@Component({
  selector: 'app-graph-analytics-page',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/compliance-graph-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header">
        <div><h1>Graph Analytics</h1><p class="subtitle">Relationship density, traceability completeness, and cross-domain coverage (Analytics Hub SSOT)</p></div>
      </header>
      @if (analytics(); as a) {
        <div class="kpi-grid">
          <div class="kpi-card"><span class="kpi-label">Total Nodes</span><strong>{{ a.totalNodes }}</strong></div>
          <div class="kpi-card"><span class="kpi-label">Relationships</span><strong>{{ a.totalRelationships }}</strong></div>
          <div class="kpi-card"><span class="kpi-label">Relationship Density</span><strong>{{ a.relationshipDensity }}</strong></div>
          <div class="kpi-card"><span class="kpi-label">Traceability Completeness</span><strong>{{ a.traceabilityCompleteness }}%</strong></div>
          <div class="kpi-card"><span class="kpi-label">Dependency Risk</span><strong>{{ a.dependencyRisk }}</strong></div>
        </div>
        <section class="panel"><h2>Domain Coverage</h2>
          @for (d of a.domainCoverage; track d.category) {
            <div class="timeline-item"><strong>{{ d.category }}</strong> — {{ d.count }} assets</div>
          }
        </section>
      }
    </div>
  `,
})
export class GraphAnalyticsPageComponent implements OnInit {
  private readonly api = inject(GraphApiService);
  private readonly aiContext = inject(AiPageContextService);
  readonly analytics = signal<GraphAnalytics | null>(null);

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'ComplianceGraph', label: 'Graph Analytics' });
    this.api.getAnalytics().subscribe((a) => this.analytics.set(a));
  }
}
