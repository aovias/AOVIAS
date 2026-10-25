import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { QualityApiService } from '../services/quality-api.service';
import { formatQualityLabel } from '../models/quality-indicator.models';
import type { DepartmentScorecard } from '../models/quality-indicator.models';

@Component({
  selector: 'app-department-scorecards-page',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/quality-intelligence-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header"><h1>Department Scorecards</h1><p class="subtitle">Quality performance projections by department</p></header>
      <div class="sections">
        @for (card of scorecards(); track card.department) {
          <section class="card">
            <h3>{{ formatLabel(card.department) }}</h3>
            <div class="row"><span>Quality Score</span><strong>{{ card.qualityScore }}%</strong></div>
            <div class="row"><span>Indicator Score</span><span>{{ card.clinicalIndicatorScore }}%</span></div>
            <div class="row"><span>Incident Rate</span><span>{{ card.incidentRate }}</span></div>
            <div class="row"><span>CAPA Completion</span><span>{{ card.capaCompletionPercent }}%</span></div>
            <div class="row"><span>SLA Health</span><span>{{ card.slaHealthPercent }}%</span></div>
            <div class="row"><span>Within Target</span><span>{{ card.indicatorsWithinTarget }}</span></div>
            <div class="row"><span>Critical</span><span>{{ card.criticalIndicators }}</span></div>
            <p class="muted">{{ card.executiveSummary }}</p>
          </section>
        }
      </div>
    </div>
  `,
})
export class DepartmentScorecardsPageComponent implements OnInit {
  private readonly api = inject(QualityApiService);
  private readonly aiContext = inject(AiPageContextService);
  readonly scorecards = signal<DepartmentScorecard[]>([]);
  readonly formatLabel = formatQualityLabel;

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'QualityIntelligence', label: 'Department Scorecards' });
    this.api.getScorecards().subscribe((s) => this.scorecards.set(s));
  }
}
