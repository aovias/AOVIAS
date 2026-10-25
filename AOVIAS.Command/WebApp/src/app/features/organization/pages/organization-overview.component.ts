import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideBuilding2,
  lucideClipboardList,
  lucideGitBranch,
  lucideHospital,
  lucideNetwork,
  lucidePlus,
  lucideUsers,
} from '@ng-icons/lucide';
import { catchError, forkJoin, map, of } from 'rxjs';
import { SessionService } from '../../../core/auth/session.service';
import {
  PageBreadcrumbComponent,
  type PageBreadcrumbItem,
} from '../../../shared/ui/page-breadcrumb.component';
import { OeOwnershipApiService } from '../services/oe-ownership-api.service';
import { OrganizationApiService, type DepartmentTreeNode } from '../services/organization-api.service';
import { computeKpis, flattenDepartments } from './oe-ownership-matrix.utils';

@Component({
  selector: 'app-organization-overview',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, NgIcon, PageBreadcrumbComponent],
  viewProviders: [
    provideIcons({
      lucideHospital,
      lucideBuilding2,
      lucideGitBranch,
      lucideUsers,
      lucideClipboardList,
      lucideNetwork,
      lucidePlus,
    }),
  ],
  template: `
    <div class="aov-page">
      <app-page-breadcrumb [items]="crumbs" />
      <header class="aov-page__header">
        <div class="aov-page__titles">
          <h1 class="aov-page__title">Organization</h1>
          <p class="aov-page__subtitle">
            Administration console for structure, people, compliance ownership, and insights.
          </p>
        </div>
      </header>

      @if (error()) {
        <p class="aov-alert aov-alert--warning" role="status">{{ error() }}</p>
      }

      <section class="aov-card org-ov__metrics" aria-label="Organization metrics">
        <div class="aov-card__body org-ov__metric-row">
          <div class="org-ov__metric">
            <span class="aov-num org-ov__metric-num">{{ hospitals() }}</span>
            <span class="aov-text-caption">Hospitals</span>
          </div>
          <div class="org-ov__metric">
            <span class="aov-num org-ov__metric-num">{{ departments() }}</span>
            <span class="aov-text-caption">Departments</span>
          </div>
          <div class="org-ov__metric">
            <span class="aov-num org-ov__metric-num">{{ members() }}</span>
            <span class="aov-text-caption">Members</span>
          </div>
          <div class="org-ov__metric">
            <span class="aov-num org-ov__metric-num">
              @if (oeTotal() > 0) {
                {{ oeAssigned() }}/{{ oeTotal() }}
              } @else {
                —
              }
            </span>
            <span class="aov-text-caption">Assigned OEs</span>
          </div>
          <div class="org-ov__metric">
            <span class="aov-num org-ov__metric-num">{{ assignmentsLabel() }}</span>
            <span class="aov-text-caption">Assignments</span>
          </div>
          @if (healthPct() !== null) {
            <div class="org-ov__metric">
              <span class="aov-num org-ov__metric-num">{{ healthPct() }}%</span>
              <span class="aov-text-caption">Organization Health</span>
            </div>
          }
        </div>
      </section>

      <section class="aov-page__section" aria-label="Quick actions">
        <h2 class="aov-text-micro">Quick Actions</h2>
        <div class="org-ov__actions">
          <a class="aov-btn aov-btn--secondary aov-btn--sm" routerLink="/app/organization/hospitals">
            <ng-icon name="lucidePlus" aria-hidden="true" />
            Add Hospital
          </a>
          <a class="aov-btn aov-btn--secondary aov-btn--sm" routerLink="/app/organization/departments">
            <ng-icon name="lucidePlus" aria-hidden="true" />
            Add Department
          </a>
          <a class="aov-btn aov-btn--secondary aov-btn--sm" routerLink="/app/organization/membership">
            Manage Membership
          </a>
          <a class="aov-btn aov-btn--primary aov-btn--sm" routerLink="/app/organization/oe-ownership">
            Open OE Ownership
          </a>
        </div>
      </section>

      <section class="aov-page__section" aria-label="Structure">
        <h2 class="aov-text-micro">Structure</h2>
        <div class="org-ov__cards">
          <a class="aov-card aov-card--interactive org-ov__card" routerLink="/app/organization/hospitals">
            <div class="aov-card__body">
              <ng-icon name="lucideHospital" class="org-ov__card-icon" aria-hidden="true" />
              <h3 class="aov-card__title">Hospitals</h3>
              <p class="aov-text-caption">{{ hospitals() }} Hospitals</p>
              <span class="aov-btn aov-btn--text aov-btn--sm">Manage →</span>
            </div>
          </a>
          <a class="aov-card aov-card--interactive org-ov__card" routerLink="/app/organization/facility">
            <div class="aov-card__body">
              <ng-icon name="lucideBuilding2" class="org-ov__card-icon" aria-hidden="true" />
              <h3 class="aov-card__title">Facility</h3>
              <p class="aov-text-caption">Campus → Building → Floor hierarchy</p>
              <span class="aov-btn aov-btn--text aov-btn--sm">Manage →</span>
            </div>
          </a>
          <a class="aov-card aov-card--interactive org-ov__card" routerLink="/app/organization/departments">
            <div class="aov-card__body">
              <ng-icon name="lucideGitBranch" class="org-ov__card-icon" aria-hidden="true" />
              <h3 class="aov-card__title">Departments</h3>
              <p class="aov-text-caption">{{ departments() }} Departments</p>
              <span class="aov-btn aov-btn--text aov-btn--sm">Manage →</span>
            </div>
          </a>
        </div>
      </section>

      <section class="aov-page__section" aria-label="People">
        <h2 class="aov-text-micro">People</h2>
        <div class="org-ov__cards">
          <a class="aov-card aov-card--interactive org-ov__card" routerLink="/app/organization/membership">
            <div class="aov-card__body">
              <ng-icon name="lucideUsers" class="org-ov__card-icon" aria-hidden="true" />
              <h3 class="aov-card__title">Membership</h3>
              <p class="aov-text-caption">{{ members() }} Members</p>
              <span class="aov-btn aov-btn--text aov-btn--sm">Manage →</span>
            </div>
          </a>
        </div>
      </section>

      <section class="aov-page__section" aria-label="Compliance">
        <h2 class="aov-text-micro">Compliance</h2>
        <div class="org-ov__cards">
          <a class="aov-card aov-card--interactive org-ov__card" routerLink="/app/organization/oe-ownership">
            <div class="aov-card__body">
              <ng-icon name="lucideClipboardList" class="org-ov__card-icon" aria-hidden="true" />
              <h3 class="aov-card__title">OE Ownership</h3>
              <p class="aov-text-caption">
                @if (oeTotal() > 0) {
                  {{ oeAssigned() }} Assigned
                } @else {
                  Assign ownership
                }
              </p>
              <span class="aov-btn aov-btn--text aov-btn--sm">Continue →</span>
            </div>
          </a>
          <a class="aov-card aov-card--interactive org-ov__card" routerLink="/app/organization/assignments">
            <div class="aov-card__body">
              <ng-icon name="lucideClipboardList" class="org-ov__card-icon" aria-hidden="true" />
              <h3 class="aov-card__title">Assignments</h3>
              <p class="aov-text-caption">{{ assignmentsLabel() }}</p>
              <span class="aov-btn aov-btn--text aov-btn--sm">Review →</span>
            </div>
          </a>
        </div>
      </section>

      <section class="aov-page__section" aria-label="Insights">
        <h2 class="aov-text-micro">Insights</h2>
        <div class="org-ov__cards">
          <a class="aov-card aov-card--interactive org-ov__card" routerLink="/app/organization/graph">
            <div class="aov-card__body">
              <ng-icon name="lucideNetwork" class="org-ov__card-icon" aria-hidden="true" />
              <h3 class="aov-card__title">Graph</h3>
              <p class="aov-text-caption">Organization structure graph</p>
              <span class="aov-btn aov-btn--text aov-btn--sm">Explore →</span>
            </div>
          </a>
        </div>
      </section>
    </div>
  `,
  styles: [
    `
      .org-ov__metrics {
        margin-block-end: var(--page-section-gap, var(--space-5));
      }
      .org-ov__metric-row {
        display: flex;
        flex-wrap: wrap;
        gap: var(--space-5);
      }
      .org-ov__metric {
        display: grid;
        gap: var(--space-1);
        min-inline-size: 5rem;
      }
      .org-ov__metric-num {
        font-size: var(--font-size-section-title);
        font-weight: var(--font-weight-bold);
      }
      .org-ov__actions {
        display: flex;
        flex-wrap: wrap;
        gap: var(--space-2);
        margin-block-start: var(--space-2);
      }
      .org-ov__cards {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(14rem, 1fr));
        gap: var(--space-3);
        margin-block-start: var(--space-2);
      }
      .org-ov__card {
        text-decoration: none;
        color: inherit;
      }
      .org-ov__card-icon {
        font-size: 1.25rem;
        color: var(--color-primary);
        margin-block-end: var(--space-2);
      }
      h2.aov-text-micro {
        margin: 0;
      }
    `,
  ],
})
export class OrganizationOverviewComponent implements OnInit {
  private readonly orgApi = inject(OrganizationApiService);
  private readonly oeApi = inject(OeOwnershipApiService);
  private readonly session = inject(SessionService);

  readonly crumbs: PageBreadcrumbItem[] = [{ label: 'Organization' }];

  readonly hospitals = signal(0);
  readonly departments = signal(0);
  readonly members = signal(0);
  readonly oeAssigned = signal(0);
  readonly oeTotal = signal(0);
  readonly assignmentsPending = signal<number | null>(null);
  readonly error = signal<string | null>(null);
  readonly loading = signal(true);

  readonly healthPct = computed(() => {
    const t = this.oeTotal();
    if (t <= 0) return null;
    return Math.round((this.oeAssigned() / t) * 100);
  });

  assignmentsLabel(): string {
    const p = this.assignmentsPending();
    if (p === null) return '—';
    return `${p} Pending`;
  }

  ngOnInit(): void {
    const orgId = this.session.getOrganizationId();
    if (!orgId) {
      this.error.set('Organization context is required.');
      this.loading.set(false);
      return;
    }

    const hospitalId = this.session.getHospitalId() || '';

    forkJoin({
      hospitals: this.orgApi.listHospitals(orgId).pipe(catchError(() => of([]))),
      depts: this.orgApi.getDepartmentTree(orgId, hospitalId || undefined).pipe(
        catchError(() => of([] as DepartmentTreeNode[])),
      ),
      members: this.orgApi.listMemberships(orgId, 1, 1).pipe(
        catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: 1 })),
      ),
    }).subscribe({
      next: ({ hospitals, depts, members }) => {
        this.hospitals.set(hospitals?.length ?? 0);
        this.departments.set(flattenDepartments(depts).length);
        this.members.set(members?.totalCount ?? 0);
        this.loading.set(false);

        const hid = hospitalId || hospitals?.[0]?.id;
        if (hid) this.loadOeMetrics(hid);
      },
      error: () => {
        this.error.set('Some organization metrics could not be loaded.');
        this.loading.set(false);
      },
    });
  }

  private loadOeMetrics(hospitalId: string): void {
    this.oeApi
      .listHospitalFrameworks(hospitalId)
      .pipe(
        catchError(() => of([])),
        map((fws) => fws?.[0]?.frameworkId as string | undefined),
      )
      .subscribe((frameworkId) => {
        if (!frameworkId) return;
        this.oeApi.listMatrix(hospitalId, frameworkId).pipe(catchError(() => of([]))).subscribe((rows) => {
          const k = computeKpis(rows);
          this.oeTotal.set(k.total);
          this.oeAssigned.set(k.assigned);
        });
      });
  }
}
