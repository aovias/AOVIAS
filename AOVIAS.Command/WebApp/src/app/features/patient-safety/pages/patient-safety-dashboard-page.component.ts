import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { IncidentApiService } from '../services/incident-api.service';
import { IncidentDashboardComponent } from '../components/incident-dashboard.component';
import type { IncidentDashboard } from '../models/incident.models';

@Component({
  selector: 'app-patient-safety-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterLink, IncidentDashboardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/patient-safety-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header">
        <div>
          <h1>Patient Safety Dashboard</h1>
          <p class="subtitle">Operational heartbeat of hospital patient safety</p>
        </div>
        <a routerLink="/app/patient-safety/command-center" class="btn-primary">Open Command Center</a>
      </header>
      <app-incident-dashboard [dashboard]="dashboard()" />
      <div class="shortcuts">
        <a routerLink="/app/patient-safety/investigations">Investigations</a>
        <a routerLink="/app/patient-safety/rca">Root Cause Analysis</a>
        <a routerLink="/app/patient-safety/near-misses">Near Misses</a>
        <a routerLink="/app/patient-safety/sentinel-events">Sentinel Events</a>
        <a routerLink="/app/patient-safety/feedback">Patient Feedback</a>
        <a routerLink="/app/patient-safety/lessons">Lessons Learned</a>
        <a routerLink="/app/patient-safety/analytics">Analytics</a>
      </div>
    </div>
  `,
})
export class PatientSafetyDashboardPageComponent implements OnInit {
  private readonly api = inject(IncidentApiService);
  private readonly aiContext = inject(AiPageContextService);
  readonly dashboard = signal<IncidentDashboard | null>(null);

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'PatientSafety' });
    this.api.getDashboard().subscribe((d) => this.dashboard.set(d));
  }
}
