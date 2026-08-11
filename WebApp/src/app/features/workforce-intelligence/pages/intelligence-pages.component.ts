import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { SessionService } from '../../../core/auth/session.service';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { AdminPageStateComponent } from '../../../shared/ui/administration/admin-page-state.component';
import {
  WorkforceIntelligenceApiService,
  SkillMatrix,
  TrainingMatrix,
  CredentialDashboard,
  ForecastData,
  RecommendationList,
} from '../services/workforce-intelligence-api.service';

function useOrgId(session: SessionService): string | null {
  return session.getOrganizationId();
}

@Component({
  selector: 'app-skill-matrix',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, SettingsPageHeaderComponent, AdminPageStateComponent],
  template: `
    <section class="wi-subpage">
      <app-settings-page-header title="Skill Matrix" description="Skill coverage and critical gaps across the workforce." />
      <a class="wi-back" routerLink="..">Back to Intelligence</a>
      <mat-card class="wi-card">
        <mat-card-content>
          @if (!data()) {
            <app-admin-page-state state="loading" message="Loading skill matrix…" [showRetry]="false" />
          } @else {
            <p class="wi-muted">Coverage: {{ data()!.coveragePercent | number: '1.0-1' }}%</p>
            <div class="wi-table-wrap">
              <table class="wi-table">
                <thead>
                  <tr><th>Skill</th><th>Level</th><th>Gap</th><th>Critical</th></tr>
                </thead>
                <tbody>
                  @for (r of data()!.rows; track r.skillName) {
                    <tr>
                      <td>{{ r.skillName }}</td>
                      <td>{{ r.currentLevel }}</td>
                      <td>{{ r.gap || '—' }}</td>
                      <td>{{ r.isCritical ? 'Yes' : 'No' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </mat-card-content>
      </mat-card>
    </section>
  `,
  styles: [`.wi-subpage{display:flex;flex-direction:column;gap:12px}.wi-back{font-size:13px;color:var(--color-primary);text-decoration:none;font-weight:600}.wi-card{border-radius:16px}.wi-muted{color:#6b7280;font-size:13px}.wi-table-wrap{overflow-x:auto}.wi-table{width:100%;border-collapse:collapse;font-size:13px}.wi-table th,.wi-table td{text-align:left;padding:10px;border-bottom:1px solid #e5e7eb}.wi-table th{font-size:12px;color:#6b7280}`],
})
export class SkillMatrixComponent implements OnInit {
  private api = inject(WorkforceIntelligenceApiService);
  private session = inject(SessionService);
  data = signal<SkillMatrix | null>(null);
  ngOnInit(): void {
    const orgId = useOrgId(this.session);
    if (!orgId) return;
    this.api.getSkills(orgId).subscribe({ next: (d) => this.data.set(d) });
  }
}

@Component({
  selector: 'app-competency-matrix',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, SettingsPageHeaderComponent, AdminPageStateComponent],
  template: `
    <section class="wi-subpage">
      <app-settings-page-header title="Competency Matrix" description="Role competency coverage for the organization." />
      <a class="wi-back" routerLink="..">Back to Intelligence</a>
      <mat-card class="wi-card"><mat-card-content>
        @if (!data()) { <app-admin-page-state state="loading" message="Loading competencies…" [showRetry]="false" /> }
        @else { <pre class="wi-pre">{{ data() | json }}</pre> }
      </mat-card-content></mat-card>
    </section>
  `,
  styles: [`.wi-subpage{display:flex;flex-direction:column;gap:12px}.wi-back{font-size:13px;color:#2f6bff;text-decoration:none;font-weight:600}.wi-card{border-radius:16px}.wi-pre{margin:0;font-size:12px;overflow:auto}`],
})
export class CompetencyMatrixComponent implements OnInit {
  private api = inject(WorkforceIntelligenceApiService);
  private session = inject(SessionService);
  data = signal<unknown>(null);
  ngOnInit(): void {
    const orgId = useOrgId(this.session);
    if (!orgId) return;
    this.api.getCompetencies(orgId).subscribe({ next: (d) => this.data.set(d) });
  }
}

@Component({
  selector: 'app-training-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, SettingsPageHeaderComponent, AdminPageStateComponent],
  template: `
    <section class="wi-subpage">
      <app-settings-page-header title="Training Dashboard" description="Mandatory training compliance by course." />
      <a class="wi-back" routerLink="..">Back to Intelligence</a>
      <mat-card class="wi-card"><mat-card-content>
        @if (!data()) { <app-admin-page-state state="loading" message="Loading training…" [showRetry]="false" /> }
        @else {
          <p class="wi-muted">Compliance: {{ data()!.organizationCompliancePercent }}%</p>
          <ul>@for (r of data()!.rows; track r.courseName) { <li>{{ r.courseName }} — {{ r.status }}</li> }</ul>
        }
      </mat-card-content></mat-card>
    </section>
  `,
  styles: [`.wi-subpage{display:flex;flex-direction:column;gap:12px}.wi-back{font-size:13px;color:#2f6bff;text-decoration:none;font-weight:600}.wi-card{border-radius:16px}.wi-muted{color:#6b7280;font-size:13px}`],
})
export class TrainingDashboardComponent implements OnInit {
  private api = inject(WorkforceIntelligenceApiService);
  private session = inject(SessionService);
  data = signal<TrainingMatrix | null>(null);
  ngOnInit(): void {
    const orgId = useOrgId(this.session);
    if (!orgId) return;
    this.api.getTraining(orgId).subscribe({ next: (d) => this.data.set(d) });
  }
}

@Component({
  selector: 'app-certification-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, SettingsPageHeaderComponent, AdminPageStateComponent],
  template: `
    <section class="wi-subpage">
      <app-settings-page-header title="Certification Dashboard" description="Active, expiring, and expired certifications." />
      <a class="wi-back" routerLink="..">Back to Intelligence</a>
      <mat-card class="wi-card"><mat-card-content>
        @if (!data()) { <app-admin-page-state state="loading" message="Loading certifications…" [showRetry]="false" /> }
        @else { <p>Active {{ data()!.active }} | Expiring {{ data()!.expiring }} | Expired {{ data()!.expired }}</p> }
      </mat-card-content></mat-card>
    </section>
  `,
  styles: [`.wi-subpage{display:flex;flex-direction:column;gap:12px}.wi-back{font-size:13px;color:#2f6bff;text-decoration:none;font-weight:600}.wi-card{border-radius:16px}`],
})
export class CertificationDashboardComponent implements OnInit {
  private api = inject(WorkforceIntelligenceApiService);
  private session = inject(SessionService);
  data = signal<CredentialDashboard | null>(null);
  ngOnInit(): void {
    const orgId = useOrgId(this.session);
    if (!orgId) return;
    this.api.getCertifications(orgId).subscribe({ next: (d) => this.data.set(d) });
  }
}

@Component({
  selector: 'app-license-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, SettingsPageHeaderComponent, AdminPageStateComponent],
  template: `
    <section class="wi-subpage">
      <app-settings-page-header title="License Dashboard" description="Active, expiring, and expired professional licenses." />
      <a class="wi-back" routerLink="..">Back to Intelligence</a>
      <mat-card class="wi-card"><mat-card-content>
        @if (!data()) { <app-admin-page-state state="loading" message="Loading licenses…" [showRetry]="false" /> }
        @else { <p>Active {{ data()!.active }} | Expiring {{ data()!.expiring }} | Expired {{ data()!.expired }}</p> }
      </mat-card-content></mat-card>
    </section>
  `,
  styles: [`.wi-subpage{display:flex;flex-direction:column;gap:12px}.wi-back{font-size:13px;color:#2f6bff;text-decoration:none;font-weight:600}.wi-card{border-radius:16px}`],
})
export class LicenseDashboardComponent implements OnInit {
  private api = inject(WorkforceIntelligenceApiService);
  private session = inject(SessionService);
  data = signal<CredentialDashboard | null>(null);
  ngOnInit(): void {
    const orgId = useOrgId(this.session);
    if (!orgId) return;
    this.api.getLicenses(orgId).subscribe({ next: (d) => this.data.set(d) });
  }
}

@Component({
  selector: 'app-availability-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, SettingsPageHeaderComponent, AdminPageStateComponent],
  template: `
    <section class="wi-subpage">
      <app-settings-page-header title="Availability Dashboard" description="Workforce availability coverage analytics." />
      <a class="wi-back" routerLink="..">Back to Intelligence</a>
      <mat-card class="wi-card"><mat-card-content>
        @if (!data()) { <app-admin-page-state state="loading" message="Loading availability…" [showRetry]="false" /> }
        @else { <pre class="wi-pre">{{ data() | json }}</pre> }
      </mat-card-content></mat-card>
    </section>
  `,
  styles: [`.wi-subpage{display:flex;flex-direction:column;gap:12px}.wi-back{font-size:13px;color:#2f6bff;text-decoration:none;font-weight:600}.wi-card{border-radius:16px}.wi-pre{margin:0;font-size:12px;overflow:auto}`],
})
export class AvailabilityDashboardComponent implements OnInit {
  private api = inject(WorkforceIntelligenceApiService);
  private session = inject(SessionService);
  data = signal<unknown>(null);
  ngOnInit(): void {
    const orgId = useOrgId(this.session);
    if (!orgId) return;
    this.api.getAvailability(orgId).subscribe({ next: (d) => this.data.set(d) });
  }
}

@Component({
  selector: 'app-organization-analytics',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, SettingsPageHeaderComponent, AdminPageStateComponent],
  template: `
    <section class="wi-subpage">
      <app-settings-page-header title="Organization Analytics" description="Workforce distribution and organizational structure analytics." />
      <a class="wi-back" routerLink="..">Back to Intelligence</a>
      <mat-card class="wi-card"><mat-card-content>
        @if (!data()) { <app-admin-page-state state="loading" message="Loading organization analytics…" [showRetry]="false" /> }
        @else { <pre class="wi-pre">{{ data() | json }}</pre> }
      </mat-card-content></mat-card>
    </section>
  `,
  styles: [`.wi-subpage{display:flex;flex-direction:column;gap:12px}.wi-back{font-size:13px;color:#2f6bff;text-decoration:none;font-weight:600}.wi-card{border-radius:16px}.wi-pre{margin:0;font-size:12px;overflow:auto}`],
})
export class OrganizationAnalyticsComponent implements OnInit {
  private api = inject(WorkforceIntelligenceApiService);
  private session = inject(SessionService);
  data = signal<unknown>(null);
  ngOnInit(): void {
    const orgId = useOrgId(this.session);
    if (!orgId) return;
    this.api.getOrganization(orgId).subscribe({ next: (d) => this.data.set(d) });
  }
}

@Component({
  selector: 'app-responsibility-analytics',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, SettingsPageHeaderComponent, AdminPageStateComponent],
  template: `
    <section class="wi-subpage">
      <app-settings-page-header title="Responsibility Analytics" description="Responsibility assignment coverage and gaps." />
      <a class="wi-back" routerLink="..">Back to Intelligence</a>
      <mat-card class="wi-card"><mat-card-content>
        @if (!data()) { <app-admin-page-state state="loading" message="Loading responsibilities…" [showRetry]="false" /> }
        @else { <pre class="wi-pre">{{ data() | json }}</pre> }
      </mat-card-content></mat-card>
    </section>
  `,
  styles: [`.wi-subpage{display:flex;flex-direction:column;gap:12px}.wi-back{font-size:13px;color:#2f6bff;text-decoration:none;font-weight:600}.wi-card{border-radius:16px}.wi-pre{margin:0;font-size:12px;overflow:auto}`],
})
export class ResponsibilityAnalyticsComponent implements OnInit {
  private api = inject(WorkforceIntelligenceApiService);
  private session = inject(SessionService);
  data = signal<unknown>(null);
  ngOnInit(): void {
    const orgId = useOrgId(this.session);
    if (!orgId) return;
    this.api.getResponsibilities(orgId).subscribe({ next: (d) => this.data.set(d) });
  }
}

@Component({
  selector: 'app-committee-analytics',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, SettingsPageHeaderComponent, AdminPageStateComponent],
  template: `
    <section class="wi-subpage">
      <app-settings-page-header title="Committee Analytics" description="Committee participation and action backlog." />
      <a class="wi-back" routerLink="..">Back to Intelligence</a>
      <mat-card class="wi-card"><mat-card-content>
        @if (!data()) { <app-admin-page-state state="loading" message="Loading committees…" [showRetry]="false" /> }
        @else { <pre class="wi-pre">{{ data() | json }}</pre> }
      </mat-card-content></mat-card>
    </section>
  `,
  styles: [`.wi-subpage{display:flex;flex-direction:column;gap:12px}.wi-back{font-size:13px;color:#2f6bff;text-decoration:none;font-weight:600}.wi-card{border-radius:16px}.wi-pre{margin:0;font-size:12px;overflow:auto}`],
})
export class CommitteeAnalyticsComponent implements OnInit {
  private api = inject(WorkforceIntelligenceApiService);
  private session = inject(SessionService);
  data = signal<unknown>(null);
  ngOnInit(): void {
    const orgId = useOrgId(this.session);
    if (!orgId) return;
    this.api.getCommittees(orgId).subscribe({ next: (d) => this.data.set(d) });
  }
}

@Component({
  selector: 'app-forecast-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, SettingsPageHeaderComponent, AdminPageStateComponent],
  template: `
    <section class="wi-subpage">
      <app-settings-page-header title="Forecast Dashboard" description="Workforce forecasting for credential and capacity planning." />
      <a class="wi-back" routerLink="..">Back to Intelligence</a>
      <mat-card class="wi-card"><mat-card-content>
        @if (!data()) { <app-admin-page-state state="loading" message="Loading forecast…" [showRetry]="false" /> }
        @else {
          <p class="wi-muted">{{ data()!.forecastType }} v{{ data()!.version }}</p>
          <ul>@for (p of data()!.dataPoints; track p.label) { <li>{{ p.label }}: {{ p.count }}</li> }</ul>
        }
      </mat-card-content></mat-card>
    </section>
  `,
  styles: [`.wi-subpage{display:flex;flex-direction:column;gap:12px}.wi-back{font-size:13px;color:#2f6bff;text-decoration:none;font-weight:600}.wi-card{border-radius:16px}.wi-muted{color:#6b7280;font-size:13px}`],
})
export class ForecastDashboardComponent implements OnInit {
  private api = inject(WorkforceIntelligenceApiService);
  private session = inject(SessionService);
  data = signal<ForecastData | null>(null);
  ngOnInit(): void {
    const orgId = useOrgId(this.session);
    if (!orgId) return;
    this.api.getForecast(orgId, 'CertificationExpiry').subscribe({ next: (d) => this.data.set(d) });
  }
}

@Component({
  selector: 'app-recommendations',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, SettingsPageHeaderComponent, AdminPageStateComponent],
  template: `
    <section class="wi-subpage">
      <app-settings-page-header title="AI Recommendations" description="Priority workforce recommendations from intelligence analytics." />
      <a class="wi-back" routerLink="..">Back to Intelligence</a>
      <mat-card class="wi-card"><mat-card-content>
        @if (!loaded()) { <app-admin-page-state state="loading" message="Loading recommendations…" [showRetry]="false" /> }
        @else if (!items().length) { <app-admin-page-state state="empty" icon="insights" message="No recommendations available." [showRetry]="false" /> }
        @else {
          <ul>@for (r of items(); track r.id) {
            <li><strong>{{ r.title }}</strong> ({{ r.priority }}) — {{ r.explanation }}</li>
          }</ul>
        }
      </mat-card-content></mat-card>
    </section>
  `,
  styles: [`.wi-subpage{display:flex;flex-direction:column;gap:12px}.wi-back{font-size:13px;color:#2f6bff;text-decoration:none;font-weight:600}.wi-card{border-radius:16px}`],
})
export class RecommendationsComponent implements OnInit {
  private api = inject(WorkforceIntelligenceApiService);
  private session = inject(SessionService);
  items = signal<RecommendationList['items']>([]);
  loaded = signal(false);
  ngOnInit(): void {
    const orgId = useOrgId(this.session);
    if (!orgId) return;
    this.api.getRecommendations(orgId).subscribe({
      next: (d) => {
        this.items.set(d.items);
        this.loaded.set(true);
      },
      error: () => this.loaded.set(true),
    });
  }
}

@Component({
  selector: 'app-workforce-search',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, SettingsPageHeaderComponent],
  template: `
    <section class="wi-subpage">
      <app-settings-page-header title="Workforce Search" description="Search people by skill, certification, and name." />
      <a class="wi-back" routerLink="..">Back to Intelligence</a>
      <mat-card class="wi-card"><mat-card-content>
        <label class="wi-sr-only" for="wi-search">Search workforce</label>
        <input id="wi-search" #q (keyup.enter)="search(q.value)" placeholder="Search skills, certs, names..." aria-label="Search workforce" />
        <ul>@for (h of hits(); track h.personId) { <li>{{ h.displayName }}</li> }</ul>
      </mat-card-content></mat-card>
    </section>
  `,
  styles: [`.wi-subpage{display:flex;flex-direction:column;gap:12px}.wi-back{font-size:13px;color:#2f6bff;text-decoration:none;font-weight:600}.wi-card{border-radius:16px}.wi-sr-only{position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);border:0}input{width:min(420px,100%);padding:8px 12px;border:1px solid #e5e7eb;border-radius:8px}`],
})
export class WorkforceSearchComponent {
  private api = inject(WorkforceIntelligenceApiService);
  private session = inject(SessionService);
  hits = signal<{ personId: string; displayName: string }[]>([]);
  search(query: string): void {
    const orgId = useOrgId(this.session);
    if (!orgId) return;
    this.api.search(orgId, query).subscribe({ next: (r) => this.hits.set(r.hits) });
  }
}
