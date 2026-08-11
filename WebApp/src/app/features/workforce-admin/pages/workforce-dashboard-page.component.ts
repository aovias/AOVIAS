import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { catchError, forkJoin, of } from 'rxjs';
import { SessionService } from '../../../core/auth/session.service';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { SettingsComingSoonBadgeComponent } from '../../settings/components/settings-coming-soon-badge.component';
import { OrganizationApiService } from '../../organization/services/organization-api.service';
import { ProfessionalApiService } from '../../professional/services/professional-api.service';
import { EnterpriseDirectoryApiService } from '../../enterprise-directory/services/enterprise-directory-api.service';
import { WorkforceIntelligenceApiService } from '../../workforce-intelligence/services/workforce-intelligence-api.service';
import { WorkforceAdminPageStateComponent } from '../components/workforce-admin-page-state.component';
import { WorkforceStatCardComponent } from '../components/workforce-stat-card.component';
import {
  getWorkforceAdminCapabilityCards,
  getWorkforceAdminFuturePlaceholders,
} from '../models/workforce-admin-nav.model';

interface DashboardStats {
  totalWorkforce: string;
  activeMembers: string;
  departments: string;
  designations: string;
  professionalRegistrations: string;
  pendingMembershipRequests: string;
  inactiveMembers: string;
  recentlyJoined: string;
}

@Component({
  selector: 'app-workforce-dashboard-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatIconModule,
    SettingsPageHeaderComponent,
    SettingsComingSoonBadgeComponent,
    WorkforceStatCardComponent,
    WorkforceAdminPageStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="wf-dashboard">
      <app-settings-page-header
        title="Workforce Dashboard"
        description="Overview of workforce membership, departments, designations, and professional registrations across AOVIAS Command."
      />

      @if (loading()) {
        <app-workforce-admin-page-state
          state="loading"
          message="Loading workforce overview…"
          [showRetry]="false"
        />
      } @else if (error()) {
        <app-workforce-admin-page-state state="error" [message]="error()!" (retry)="reload()" />
      } @else {
        <section class="wf-dashboard__stats" aria-label="Workforce statistics">
          <app-workforce-stat-card
            label="Total Workforce"
            [value]="stats().totalWorkforce"
            [placeholder]="stats().totalWorkforce === '—'"
          />
          <app-workforce-stat-card
            label="Active Members"
            [value]="stats().activeMembers"
            [placeholder]="stats().activeMembers === '—'"
          />
          <app-workforce-stat-card
            label="Departments"
            [value]="stats().departments"
            [placeholder]="stats().departments === '—'"
          />
          <app-workforce-stat-card
            label="Designations"
            [value]="stats().designations"
            [placeholder]="stats().designations === '—'"
          />
          <app-workforce-stat-card
            label="Professional Registrations"
            [value]="stats().professionalRegistrations"
            [placeholder]="stats().professionalRegistrations === '—'"
          />
          <app-workforce-stat-card
            label="Pending Membership Requests"
            [value]="stats().pendingMembershipRequests"
            [placeholder]="stats().pendingMembershipRequests === '—'"
          />
          <app-workforce-stat-card
            label="Inactive Members"
            [value]="stats().inactiveMembers"
            [placeholder]="stats().inactiveMembers === '—'"
          />
          <app-workforce-stat-card
            label="Recently Joined"
            [value]="stats().recentlyJoined"
            [placeholder]="stats().recentlyJoined === '—'"
          />
        </section>
      }

      <section class="wf-dashboard__cards" aria-label="Workforce administration areas">
        @for (card of capabilityCards; track card.label) {
          <a class="wf-capability-card" [routerLink]="card.route">
            <mat-icon aria-hidden="true">{{ card.icon }}</mat-icon>
            <div>
              <strong>{{ card.label }}</strong>
              <span>{{ card.description }}</span>
            </div>
          </a>
        }
      </section>

      <section class="wf-dashboard__future" aria-label="Future workforce capabilities">
        <h2 class="wf-dashboard__future-title">Future Capabilities</h2>
        <div class="wf-dashboard__cards">
          @for (card of futureCards; track card.label) {
            <div class="wf-capability-card wf-capability-card--soon">
              <mat-icon aria-hidden="true">{{ card.icon }}</mat-icon>
              <div>
                <div class="wf-capability-card__title">
                  <strong>{{ card.label }}</strong>
                  <app-settings-coming-soon-badge />
                </div>
                <span>{{ card.description }}</span>
              </div>
            </div>
          }
        </div>
      </section>
    </div>
  `,
  styles: [
    `
      .wf-dashboard {
        display: flex;
        flex-direction: column;
        gap: 24px;
      }
      .wf-dashboard__stats {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
        gap: 12px;
      }
      .wf-dashboard__cards {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
        gap: 12px;
      }
      .wf-dashboard__future-title {
        margin: 0 0 12px;
        font-size: 16px;
        font-weight: 600;
      }
      .wf-capability-card {
        display: flex;
        gap: 12px;
        align-items: flex-start;
        padding: 16px;
        border-radius: 16px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        background: var(--color-bg-surface, #fff);
        text-decoration: none;
        color: inherit;
        transition: border-color 0.15s ease, box-shadow 0.15s ease, transform 0.15s ease;
      }
      .wf-capability-card:hover:not(.wf-capability-card--soon) {
        border-color: color-mix(in srgb, var(--color-primary) 40%, transparent);
        box-shadow: 0 4px 16px rgba(15, 23, 42, 0.06);
        transform: translateY(-1px);
      }
      .wf-capability-card mat-icon {
        color: var(--color-primary);
      }
      .wf-capability-card strong {
        display: block;
        font-size: 14px;
        margin-bottom: 4px;
      }
      .wf-capability-card span {
        display: block;
        font-size: 12px;
        line-height: 1.4;
        color: var(--color-text-secondary, #6b7280);
      }
      .wf-capability-card__title {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 8px;
        margin-bottom: 4px;
      }
      .wf-capability-card__title strong {
        margin-bottom: 0;
      }
      .wf-capability-card--soon {
        opacity: 0.92;
      }
    `,
  ],
})
export class WorkforceDashboardPageComponent implements OnInit {
  private readonly session = inject(SessionService);
  private readonly orgApi = inject(OrganizationApiService);
  private readonly professionalApi = inject(ProfessionalApiService);
  private readonly directoryApi = inject(EnterpriseDirectoryApiService);
  private readonly intelligenceApi = inject(WorkforceIntelligenceApiService);

  readonly capabilityCards = getWorkforceAdminCapabilityCards();
  readonly futureCards = getWorkforceAdminFuturePlaceholders();
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly stats = signal<DashboardStats>(this.emptyStats());

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    const organizationId = this.session.getOrganizationId();
    if (!organizationId) {
      this.error.set('Organization context is required to load the workforce dashboard.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const thirtyDaysAgo = new Date();
    thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);

    forkJoin({
      memberships: this.orgApi.listMemberships(organizationId, 1, 100, { includeInactive: true }).pipe(catchError(() => of(null))),
      departments: this.orgApi.getDepartmentTree(organizationId).pipe(catchError(() => of(null))),
      professionals: this.professionalApi.list(1, 1).pipe(catchError(() => of(null))),
      facets: this.directoryApi.facets(organizationId).pipe(catchError(() => of(null))),
      intelligence: this.intelligenceApi.getDashboard(organizationId).pipe(catchError(() => of(null))),
    }).subscribe({
      next: ({ memberships, departments, professionals, facets, intelligence }) => {
        const members = memberships?.items ?? [];
        const active = members.filter((m) => /active/i.test(m.membershipStatus || '')).length;
        const inactive = members.filter((m) => /inactive|disabled|terminated|left/i.test(m.membershipStatus || '')).length;
        const pending = members.filter((m) => /pending|invited|requested/i.test(m.membershipStatus || '')).length;
        const recentlyJoined = members.filter((m) => {
          const created = new Date(m.createdAt || m.effectiveFrom);
          return !Number.isNaN(created.getTime()) && created >= thirtyDaysAgo;
        }).length;

        const deptCount = departments ? this.countDepartments(departments) : null;
        const designationSet = new Set(
          members.map((m) => (m.designation || '').trim()).filter(Boolean),
        );
        const facetDesignations = facets?.['designation'] ?? facets?.['Designation'] ?? [];
        for (const f of facetDesignations) {
          if (f.value) designationSet.add(f.value);
        }

        this.stats.set({
          totalWorkforce:
            intelligence && typeof intelligence.headcount === 'number'
              ? String(intelligence.headcount)
              : memberships
                ? String(memberships.totalCount)
                : '—',
          activeMembers: memberships ? String(active) : '—',
          departments: deptCount == null ? '—' : String(deptCount),
          designations: String(designationSet.size),
          professionalRegistrations:
            professionals && typeof professionals.totalCount === 'number'
              ? String(professionals.totalCount)
              : '—',
          pendingMembershipRequests: memberships ? String(pending) : '—',
          inactiveMembers: memberships ? String(inactive) : '—',
          recentlyJoined: memberships ? String(recentlyJoined) : '—',
        });
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load workforce dashboard.');
        this.loading.set(false);
      },
    });
  }

  private countDepartments(
    nodes: { children?: unknown[] }[] | { children?: unknown[] },
  ): number {
    const list = Array.isArray(nodes) ? nodes : [nodes];
    let total = 0;
    const walk = (items: { children?: unknown[] }[]) => {
      for (const node of items) {
        total += 1;
        if (Array.isArray(node.children) && node.children.length) {
          walk(node.children as { children?: unknown[] }[]);
        }
      }
    };
    walk(list);
    return total;
  }

  private emptyStats(): DashboardStats {
    return {
      totalWorkforce: '—',
      activeMembers: '—',
      departments: '—',
      designations: '—',
      professionalRegistrations: '—',
      pendingMembershipRequests: '—',
      inactiveMembers: '—',
      recentlyJoined: '—',
    };
  }
}
