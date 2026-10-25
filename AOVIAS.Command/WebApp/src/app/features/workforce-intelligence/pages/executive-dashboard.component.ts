import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { SessionService } from '../../../core/auth/session.service';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { WorkforceIntelligenceApiService, ExecutiveDashboard } from '../services/workforce-intelligence-api.service';
import { WorkforceAdminPageStateComponent } from '../../workforce-admin/components/workforce-admin-page-state.component';
import { WorkforceStatCardComponent } from '../../workforce-admin/components/workforce-stat-card.component';

@Component({
  selector: 'app-executive-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    SettingsPageHeaderComponent,
    WorkforceAdminPageStateComponent,
    WorkforceStatCardComponent,
  ],
  template: `
    <section class="wi-page">
      <app-settings-page-header
        title="Workforce Intelligence"
        description="Executive workforce analytics — headcount, compliance, credentials, and risk."
      />

      <nav class="wi-nav" aria-label="Workforce intelligence areas">
        <a routerLink="skills">Skills</a>
        <a routerLink="competencies">Competencies</a>
        <a routerLink="training">Training</a>
        <a routerLink="certifications">Certifications</a>
        <a routerLink="licenses">Licenses</a>
        <a routerLink="availability">Availability</a>
        <a routerLink="organization">Organization</a>
        <a routerLink="responsibilities">Responsibilities</a>
        <a routerLink="committees">Committees</a>
        <a routerLink="forecast">Forecast</a>
        <a routerLink="recommendations">Recommendations</a>
        <a routerLink="search">Search</a>
        <button mat-stroked-button type="button" (click)="refresh()" [disabled]="loading()">Refresh</button>
      </nav>

      @if (loading()) {
        <app-workforce-admin-page-state state="loading" message="Loading intelligence dashboard…" [showRetry]="false" />
      } @else if (error()) {
        <app-workforce-admin-page-state state="error" [message]="error()!" (retry)="load()" />
      } @else if (dashboard()) {
        <div class="wi-kpis">
          <app-workforce-stat-card label="Headcount" [value]="dashboard()!.headcount" />
          <app-workforce-stat-card
            label="Training compliance"
            [value]="(dashboard()!.trainingCompliancePercent | number: '1.0-1') + '%'"
          />
          <app-workforce-stat-card label="Credential risk" [value]="dashboard()!.credentialRiskCount" />
          <app-workforce-stat-card label="Skill gaps" [value]="dashboard()!.criticalSkillGaps" />
          <app-workforce-stat-card
            label="Availability"
            [value]="(dashboard()!.availabilityCoveragePercent | number: '1.0-1') + '%'"
          />
          <app-workforce-stat-card label="Vacancies" [value]="dashboard()!.openVacancies" />
        </div>
      } @else {
        <app-workforce-admin-page-state
          state="empty"
          icon="analytics"
          message="No workforce intelligence snapshot is available yet."
          [showRetry]="false"
        />
      }
    </section>
  `,
  styles: [
    `
      .wi-page {
        display: flex;
        flex-direction: column;
        gap: 16px;
      }
      .wi-nav {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
        align-items: center;
      }
      .wi-nav a {
        display: inline-flex;
        padding: 6px 12px;
        border-radius: 999px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        text-decoration: none;
        color: var(--color-text-secondary, #6b7280);
        font-size: 13px;
        background: #fff;
      }
      .wi-nav a:hover {
        color: var(--color-primary);
        border-color: color-mix(in srgb, var(--color-primary) 35%, transparent);
      }
      .wi-kpis {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
        gap: 12px;
      }
    `,
  ],
})
export class ExecutiveDashboardComponent implements OnInit {
  private readonly api = inject(WorkforceIntelligenceApiService);
  private readonly session = inject(SessionService);

  readonly dashboard = signal<ExecutiveDashboard | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const organizationId = this.session.getOrganizationId();
    if (!organizationId) {
      this.error.set('Organization context is required.');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.api.getDashboard(organizationId).subscribe({
      next: (d) => {
        this.dashboard.set(d);
        this.loading.set(false);
      },
      error: () => {
        this.dashboard.set(null);
        this.loading.set(false);
        this.error.set('Unable to load workforce intelligence.');
      },
    });
  }

  refresh(): void {
    const organizationId = this.session.getOrganizationId();
    if (!organizationId) return;
    this.api.refresh(organizationId).subscribe({
      next: () => this.load(),
      error: () => this.load(),
    });
  }
}
