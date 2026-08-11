import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { QualityApiService } from '../services/quality-api.service';
import { IndicatorDashboardComponent } from '../components/indicator-dashboard.component';
import type { QualityDashboard } from '../models/quality-indicator.models';

@Component({
  selector: 'app-quality-intelligence-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterLink, IndicatorDashboardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/quality-intelligence-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header"><div><h1>Quality Intelligence Dashboard</h1><p class="subtitle">Continuous clinical quality measurement and accreditation readiness</p></div></header>
      <app-indicator-dashboard [dashboard]="dashboard()" />
    </div>
  `,
})
export class QualityIntelligenceDashboardPageComponent implements OnInit {
  private readonly api = inject(QualityApiService);
  private readonly aiContext = inject(AiPageContextService);
  readonly dashboard = signal<QualityDashboard | null>(null);

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'QualityIntelligence', label: 'Quality Dashboard' });
    this.api.getDashboard().subscribe((d) => this.dashboard.set(d));
  }
}
