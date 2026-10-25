import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, of, switchMap } from 'rxjs';
import { AppContextService } from '../../../core/services/app-context.service';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import {
  OrganizationHospital,
  OrganizationService,
} from '../../../core/services/organization.service';
import {
  RoleExperienceHome,
  RoleExperienceService,
} from '../../../core/services/role-experience.service';
import { AnalyticsApiService } from '../../analytics/services/analytics-api.service';
import { AuditManagementApiService } from '../../audit/services/audit-management-api.service';
import { HospitalDashboardService } from '../../dashboard/services/hospital-dashboard.service';
import {
  KpiCardComponent,
  PriorityNbaStripComponent,
  type PriorityNbaItem,
} from '../../../shared/ui/analytics';

interface HospitalCard {
  id: string;
  name: string;
  code: string;
  readiness: number | null;
  openCapas: number | null;
  auditSessions: number | null;
  frameworks: string[];
  active: boolean;
}

@Component({
  selector: 'app-consultant-portfolio',
  standalone: true,
  imports: [CommonModule, RouterLink, KpiCardComponent, PriorityNbaStripComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="portfolio">
      <header class="portfolio__header">
        <div>
          <h1>Consultant Portfolio</h1>
          <p class="portfolio__subtitle">Multi-hospital readiness, CAPA progress, and audit calendar</p>
        </div>
        <div class="portfolio__actions">
          <a class="btn btn--primary" routerLink="/app/audit/planning">Audit calendar</a>
          <a class="btn" routerLink="/app/compliance-maturity">Maturity</a>
          <a class="btn" routerLink="/app/reports/readiness">Readiness reports</a>
        </div>
      </header>

      <app-priority-nba-strip title="Portfolio priorities" [items]="nbaItems()" />

      @if (error()) {
        <div class="banner error">{{ error() }}</div>
      }

      <div class="kpi-row">
        <app-kpi-card label="Hospitals" [value]="hospitals().length" />
        <app-kpi-card label="Avg readiness" [value]="avgReadinessLabel()" />
        <app-kpi-card label="Open CAPAs (active)" [value]="activeOpenCapas()" tone="warning" />
        <app-kpi-card label="Audit sessions (active)" [value]="activeAuditSessions()" />
      </div>

      <section class="panel">
        <header class="panel__head">
          <h2>Hospital switcher</h2>
          <span class="muted">{{ hospitals().length }} in organization</span>
        </header>
        @if (loading()) {
          <p class="muted">Loading portfolio…</p>
        } @else if (!hospitals().length) {
          <p class="muted">No hospitals found for this organization session.</p>
        } @else {
          <div class="cards">
            @for (h of hospitals(); track h.id) {
              <article class="card" [class.card--active]="h.active">
                <header>
                  <h3>{{ h.name }}</h3>
                  <span class="code">{{ h.code || '—' }}</span>
                </header>
                <dl>
                  <div><dt>Readiness</dt><dd>{{ h.readiness == null ? '—' : (h.readiness | number: '1.0-0') + '%' }}</dd></div>
                  <div><dt>Open CAPAs</dt><dd>{{ h.openCapas ?? '—' }}</dd></div>
                  <div><dt>Audit progress</dt><dd>{{ h.auditSessions ?? '—' }} sessions</dd></div>
                </dl>
                @if (h.frameworks.length) {
                  <p class="frameworks">{{ h.frameworks.join(' · ') }}</p>
                }
                <div class="card__actions">
                  <button type="button" class="btn btn--primary" (click)="switchHospital(h.id)" [disabled]="h.active">
                    {{ h.active ? 'Active' : 'Switch' }}
                  </button>
                  <a class="btn" routerLink="/app/compliance-maturity">Maturity</a>
                  <a class="btn" routerLink="/app/audit/planning">Calendar</a>
                </div>
              </article>
            }
          </div>
        }
      </section>
    </div>
  `,
  styles: [
    `
      .portfolio { padding: 8px 4px 24px; }
      .portfolio__header {
        display: flex; justify-content: space-between; gap: 16px; flex-wrap: wrap; margin-bottom: 16px;
      }
      .portfolio__header h1 { margin: 0; font-size: var(--font-size-page-title); font-weight: 700; }
      .portfolio__subtitle { margin: 4px 0 0; color: var(--text-secondary, #6b7280); font-size: var(--font-size-subtitle); }
      .portfolio__actions, .card__actions { display: flex; gap: 8px; flex-wrap: wrap; }
      .btn {
        padding: 8px 12px; border-radius: 8px; border: 1px solid var(--border-color, #e5e7eb);
        background: #fff; text-decoration: none; color: inherit; font-size: 13px; cursor: pointer;
      }
      .btn--primary { background: var(--color-primary-hover); color: #fff; border-color: var(--color-primary-hover); }
      .btn:disabled { opacity: 0.6; cursor: default; }
      .kpi-row {
        display: grid; grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
        gap: 10px; margin: 12px 0 18px;
      }
      .panel {
        background: var(--card-bg, #fff); border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 10px; padding: 14px 16px;
      }
      .panel__head { display: flex; justify-content: space-between; margin-bottom: 12px; }
      .panel__head h2 { margin: 0; font-size: var(--font-size-section-title); }
      .cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 12px; }
      .card {
        border: 1px solid var(--border-color, #e5e7eb); border-radius: 10px; padding: 14px;
        display: flex; flex-direction: column; gap: 10px;
      }
      .card--active { border-color: var(--color-primary-hover); box-shadow: 0 0 0 1px var(--color-primary-hover) inset; }
      .card h3 { margin: 0; font-size: 15px; }
      .code { font-size: 11px; color: var(--text-secondary, #6b7280); }
      .card dl { margin: 0; display: grid; gap: 6px; }
      .card dl div { display: flex; justify-content: space-between; font-size: 13px; }
      .card dt { color: var(--text-secondary, #6b7280); }
      .card dd { margin: 0; font-weight: 600; }
      .frameworks { margin: 0; font-size: 12px; color: var(--text-secondary, #6b7280); }
      .muted { color: var(--text-secondary, #6b7280); font-size: 12px; }
      .banner.error { background: #fef2f2; color: #b91c1c; padding: 10px 12px; border-radius: 8px; margin-bottom: 12px; }
    `,
  ],
})
export class ConsultantPortfolioComponent implements OnInit {
  private readonly org = inject(OrganizationService);
  private readonly appContext = inject(AppContextService);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly roleExperience = inject(RoleExperienceService);
  private readonly analytics = inject(AnalyticsApiService);
  private readonly auditApi = inject(AuditManagementApiService);
  private readonly hospitalDashboard = inject(HospitalDashboardService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly hospitals = signal<HospitalCard[]>([]);
  readonly nbaItems = signal<PriorityNbaItem[]>([]);
  readonly home = signal<RoleExperienceHome | null>(null);

  ngOnInit(): void {
    this.roleExperience.getHome().subscribe((h) => {
      this.home.set(h);
      this.nbaItems.set(
        (h.priorities ?? []).map((p) => ({
          title: p.title,
          rationale: p.rationale,
          href: p.href,
          severity: p.severity,
        })),
      );
    });

    this.reloadPortfolio();
  }

  avgReadinessLabel(): string {
    const scores = this.hospitals()
      .map((h) => h.readiness)
      .filter((v): v is number => v != null);
    if (!scores.length) return '—';
    return `${Math.round(scores.reduce((a, b) => a + b, 0) / scores.length)}%`;
  }

  activeOpenCapas(): number | string {
    const active = this.hospitals().find((h) => h.active);
    return active?.openCapas ?? '—';
  }

  activeAuditSessions(): number | string {
    const active = this.hospitals().find((h) => h.active);
    return active?.auditSessions ?? '—';
  }

  switchHospital(hospitalId: string): void {
    const hospital = this.org.getHospitals().find((h) => h.id === hospitalId);
    const journeyId = hospital?.journey?.[0]?.journeyId;
    if (journeyId) {
      this.appContext.switchContext(
        hospitalId,
        journeyId,
        this.org.getFrameworkIdForJourney(hospitalId, journeyId),
      );
    } else {
      this.appContext.switchHospital(hospitalId);
    }
    this.reloadPortfolio();
  }

  private reloadPortfolio(): void {
    this.loading.set(true);
    this.error.set(null);

    this.org
      .loadOrganizationDetails()
      .pipe(
        switchMap((org) => {
          const list = org?.hospitals ?? [];
          const activeId = this.complianceContext.getHospitalId();
          const frameworkId = this.complianceContext.getFrameworkId() ?? undefined;

          return forkJoin({
            list: of(list),
            activeId: of(activeId),
            overview: this.analytics.getOverview(frameworkId).pipe(catchError(() => of(null))),
            capa: this.analytics.getCapa(frameworkId).pipe(catchError(() => of(null))),
            operational: this.analytics.getOperational(frameworkId).pipe(catchError(() => of(null))),
            dash: activeId
              ? this.hospitalDashboard.getHospitalDashboard(activeId).pipe(catchError(() => of(null)))
              : of(null),
            calendar: this.auditApi.getCalendar({ hospitalId: activeId || undefined }).pipe(
              catchError(() => of([])),
            ),
          });
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: ({ list, activeId, overview, capa, operational, dash, calendar }) => {
          const cards = list.map((h: OrganizationHospital) => {
            const active = h.id === activeId;
            return {
              id: h.id,
              name: h.name,
              code: h.code,
              readiness: active
                ? (overview?.readinessScore ?? dash?.complianceScore ?? null)
                : null,
              openCapas: active
                ? (capa?.pendingReviews ?? operational?.overdueWork?.overdueCapas ?? null)
                : null,
              auditSessions: active
                ? (operational?.auditSessionsTotal ?? calendar?.length ?? null)
                : null,
              frameworks: (h.journey ?? []).map((j) => j.frameworkCode || j.frameworkName).filter(Boolean),
              active,
            } satisfies HospitalCard;
          });
          this.hospitals.set(cards);
        },
        error: () => this.error.set('Unable to load consultant portfolio.'),
      });
  }
}
