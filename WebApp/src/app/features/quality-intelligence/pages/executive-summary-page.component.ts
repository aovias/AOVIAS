import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { QualityApiService } from '../services/quality-api.service';
import type { QualityExecutive } from '../models/quality-indicator.models';

@Component({
  selector: 'app-executive-summary-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/quality-intelligence-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header"><h1>Executive Intelligence</h1><p class="subtitle">Read-only management review pack</p></header>
      @if (exec(); as e) {
        <div class="ps-kpis">
          <div class="kpi kpi--score"><span class="kpi__v">{{ e.overallQualityScore }}</span><span class="kpi__l">Quality Score</span></div>
          <div class="kpi kpi--critical"><span class="kpi__v">{{ e.accreditationRiskScore }}</span><span class="kpi__l">Accreditation Risk</span></div>
        </div>
        <p class="context">{{ e.monthlyQualitySummary }}</p>
        <div class="sections">
          <section><h3>Top Risks</h3>@for (r of e.topRisks; track r.title) { <div class="doc-link"><strong>{{ r.title }}</strong><small>{{ r.department }} · {{ r.severity }}</small></div> }</section>
          <section><h3>Top Improvements</h3>@for (r of e.topImprovements; track r.title) { <div class="doc-link"><strong>{{ r.title }}</strong><small>{{ r.department }}</small></div> }</section>
          <section><h3>Departments Needing Attention</h3>@for (d of e.departmentsNeedingAttention; track d) { <div class="row"><span>{{ d }}</span></div> }</section>
        </div>
      }
    </div>
  `,
})
export class ExecutiveSummaryPageComponent implements OnInit {
  private readonly api = inject(QualityApiService);
  private readonly aiContext = inject(AiPageContextService);
  readonly exec = signal<QualityExecutive | null>(null);
  ngOnInit(): void { this.aiContext.setPageContext({ pageContext: 'QualityIntelligence', label: 'Executive Summary' }); this.api.getExecutive().subscribe((e) => this.exec.set(e)); }
}
