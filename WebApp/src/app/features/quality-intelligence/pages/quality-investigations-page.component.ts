import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { QualityApiService } from '../services/quality-api.service';
import type { ClinicalIndicatorListItem } from '../models/quality-indicator.models';
import { formatQualityLabel, thresholdClass } from '../models/quality-indicator.models';

@Component({
  selector: 'app-quality-investigations-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/quality-intelligence-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header"><h1>Investigation Recommendations</h1><p class="subtitle">Advisory only — Copilot-aligned recommendations when thresholds breach</p></header>
      @for (item of breached(); track item.id) {
        <section class="card" style="margin-bottom:12px">
          <h3>{{ item.name }} <span [class]="thresholdClass(item.thresholdStatus)">{{ formatLabel(item.thresholdStatus) }}</span></h3>
          <p class="muted">{{ item.indicatorCode }} · {{ formatLabel(item.department) }}</p>
          <a class="btn-secondary" routerLink="/app/quality-intelligence/indicators" [queryParams]="{ selected: item.id }">Review Indicator</a>
          <a class="btn-link" routerLink="/app/patient-safety/investigations">Patient Safety Investigations</a>
        </section>
      } @empty { <p class="muted">No indicators currently require investigation recommendations.</p> }
    </div>
  `,
})
export class QualityInvestigationsPageComponent implements OnInit {
  private readonly api = inject(QualityApiService);
  private readonly aiContext = inject(AiPageContextService);
  readonly breached = signal<ClinicalIndicatorListItem[]>([]);
  readonly formatLabel = formatQualityLabel;
  readonly thresholdClass = thresholdClass;

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'QualityIntelligence', label: 'Investigation Recommendations' });
    this.api.getIndicators({ thresholdStatus: 'Red', pageSize: 50 }).subscribe((r) => {
      this.api.getIndicators({ thresholdStatus: 'Critical', pageSize: 50 }).subscribe((c) => {
        this.breached.set([...r.items, ...c.items]);
      });
    });
  }
}
