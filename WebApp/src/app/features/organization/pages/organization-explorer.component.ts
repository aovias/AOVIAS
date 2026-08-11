import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  DepartmentTreeNode,
  OrganizationApiService,
  OrganizationalUnitNode,
  OrganizationalUnitType,
  HospitalSummary,
} from '../services/organization-api.service';
import { SessionService } from '../../../core/auth/session.service';
import {
  PageBreadcrumbComponent,
  type PageBreadcrumbItem,
} from '../../../shared/ui/page-breadcrumb.component';

/** @deprecated Prefer OrganizationOverviewComponent via /overview */
@Component({
  selector: 'app-organization-explorer',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="org-shell">
      <p class="aov-text-caption">Redirecting to Organization Overview…</p>
      <a routerLink="/app/organization/overview">Go to Overview</a>
    </section>
  `,
  styles: [
    `
      .org-shell {
        padding: var(--page-padding);
      }
    `,
  ],
})
export class OrganizationExplorerComponent {}

@Component({
  selector: 'app-membership-explorer',
  standalone: true,
  imports: [CommonModule, RouterLink, PageBreadcrumbComponent],
  template: `
    <section class="pad aov-page">
      <app-page-breadcrumb [items]="crumbs" />
      <header class="aov-page__header">
        <div class="aov-page__titles">
          <h1 class="aov-page__title">Membership</h1>
        </div>
      </header>
      @if (error()) {
        <p class="err">{{ error() }}</p>
      } @else if (loading()) {
        <p>Loading memberships…</p>
      } @else if (items().length === 0) {
        <p>No memberships found for this organization.</p>
      } @else {
        <div class="aov-table-wrap">
          <table class="aov-table">
            <thead>
              <tr>
                <th>User</th>
                <th>Role</th>
                <th>Status</th>
                <th>Primary</th>
              </tr>
            </thead>
            <tbody>
              @for (m of items(); track m.id) {
                <tr>
                  <td>{{ m.email || m.userId }}</td>
                  <td>{{ m.role }}</td>
                  <td>{{ m.membershipStatus }}</td>
                  <td>{{ m.isPrimary ? 'Yes' : 'No' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `,
  styles: [
    `.pad { padding: var(--page-padding); } .err { color: var(--color-status-danger-fg); }`,
  ],
})
export class MembershipExplorerComponent implements OnInit {
  private api = inject(OrganizationApiService);
  private session = inject(SessionService);
  readonly crumbs: PageBreadcrumbItem[] = [
    { label: 'Organization', route: '/app/organization/overview' },
    { label: 'Membership' },
  ];
  items = signal<{ id: string; userId: string; role: string; membershipStatus: string; isPrimary: boolean; email?: string }[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const orgId = this.session.getOrganizationId() ?? undefined;
    this.api.listMemberships(orgId).subscribe({
      next: (p) => {
        this.items.set(p.items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load memberships.');
        this.loading.set(false);
      },
    });
  }
}

@Component({
  selector: 'app-department-hierarchy',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, PageBreadcrumbComponent],
  template: `
    <section class="pad aov-page">
      <app-page-breadcrumb [items]="crumbs" />
      <header class="aov-page__header">
        <div class="aov-page__titles">
          <h1 class="aov-page__title">Departments</h1>
        </div>
      </header>

      @if (error()) {
        <p class="err">{{ error() }}</p>
      }
      @if (loading()) {
        <p>Loading departments…</p>
      } @else if (tree().length === 0) {
        <p>No departments yet. Create the first department below.</p>
      } @else {
        <ul class="tree">
          <ng-container *ngTemplateOutlet="deptNodes; context: { $implicit: tree(), depth: 0 }"></ng-container>
        </ul>
      }

      <ng-template #deptNodes let-nodes let-depth="depth">
        @for (n of nodes; track n.id) {
          <li [style.marginLeft.px]="depth * 16">
            <strong>{{ n.code }}</strong> — {{ n.name }}
            <span class="badge" [class.off]="n.isActive === false">{{ n.isActive === false ? 'Inactive' : 'Active' }}</span>
            @if (n.children?.length) {
              <ul>
                <ng-container *ngTemplateOutlet="deptNodes; context: { $implicit: n.children, depth: depth + 1 }"></ng-container>
              </ul>
            }
          </li>
        }
      </ng-template>

      <form class="create" (ngSubmit)="create()">
        <h2>Create department</h2>
        <label>Code <input name="code" [(ngModel)]="code" required /></label>
        <label>Name <input name="name" [(ngModel)]="name" required /></label>
        <button type="submit" [disabled]="!orgId || !code || !name || saving()">Create</button>
      </form>
    </section>
  `,
  styles: [
    `
      .pad { padding: 1.25rem; max-width: 960px; }
      .err { color: #b91c1c; }
      .tree { list-style: none; padding-left: 0; }
      .badge { margin-left: 0.5rem; font-size: 0.75rem; padding: 0.1rem 0.4rem; border-radius: 4px; background: #dcfce7; }
      .badge.off { background: #fee2e2; }
      .create { margin-top: 1.5rem; display: grid; gap: 0.5rem; max-width: 420px; }
      label { display: grid; gap: 0.25rem; }
      input { padding: 0.4rem 0.5rem; }
      button { width: fit-content; padding: 0.45rem 0.9rem; }
    `,
  ],
})
export class DepartmentHierarchyComponent implements OnInit {
  private api = inject(OrganizationApiService);
  private session = inject(SessionService);
  readonly crumbs: PageBreadcrumbItem[] = [
    { label: 'Organization', route: '/app/organization/overview' },
    { label: 'Departments' },
  ];
  orgId = this.session.getOrganizationId();
  tree = signal<DepartmentTreeNode[]>([]);
  loading = signal(true);
  saving = signal(false);
  error = signal<string | null>(null);
  code = '';
  name = '';

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    if (!this.orgId) {
      this.error.set('Organization context is not available.');
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.api.getDepartmentTree(this.orgId).subscribe({
      next: (t) => {
        this.tree.set(t ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load department tree.');
        this.loading.set(false);
      },
    });
  }

  create(): void {
    if (!this.orgId || !this.code || !this.name) return;
    this.saving.set(true);
    this.api
      .createDepartment({ organizationId: this.orgId, code: this.code.trim(), name: this.name.trim() })
      .subscribe({
        next: () => {
          this.code = '';
          this.name = '';
          this.saving.set(false);
          this.reload();
        },
        error: () => {
          this.error.set('Unable to create department.');
          this.saving.set(false);
        },
      });
  }
}

@Component({
  selector: 'app-hospital-hierarchy',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, PageBreadcrumbComponent],
  template: `
    <section class="pad aov-page">
      <app-page-breadcrumb [items]="crumbs" />
      <header class="aov-page__header">
        <div class="aov-page__titles">
          <h1 class="aov-page__title">Hospitals &amp; Facility</h1>
        </div>
      </header>

      @if (error()) {
        <p class="err">{{ error() }}</p>
      }

      <section>
        <h2>Hospitals</h2>
        @if (hospitalsLoading()) {
          <p>Loading hospitals…</p>
        } @else if (hospitals().length === 0) {
          <p>No hospitals returned for this organization (list API may be unavailable).</p>
        } @else {
          <ul>
            @for (h of hospitals(); track h.id) {
              <li><strong>{{ h.name }}</strong> <span class="muted">{{ h.code || h.id }}</span></li>
            }
          </ul>
        }
      </section>

      <section>
        <div class="row">
          <h2>Physical facility units</h2>
          <label class="chk"
            ><input type="checkbox" [ngModel]="includeInactive()" (ngModelChange)="toggleInactive($event)" /> Include
            inactive</label
          >
        </div>

        @if (unitsLoading()) {
          <p>Loading facility tree…</p>
        } @else if (units().length === 0) {
          <p>No facility units yet. Create a Campus or Facility at the root.</p>
        } @else {
          <ul class="tree">
            <ng-container *ngTemplateOutlet="unitNodes; context: { $implicit: units(), depth: 0 }"></ng-container>
          </ul>
        }

        <ng-template #unitNodes let-nodes let-depth="depth">
          @for (n of nodes; track n.id) {
            <li [style.marginLeft.px]="depth * 16">
              <span class="type">{{ n.unitTypeCode }}</span>
              <strong>{{ n.code }}</strong> — {{ n.name }}
              <span class="badge" [class.off]="n.status !== 'Active'">{{ n.status }}</span>
              <button type="button" class="link" (click)="selectParent(n)">Set as parent</button>
              @if (n.status === 'Active') {
                <button type="button" class="link" (click)="deactivate(n)">Deactivate</button>
              } @else {
                <button type="button" class="link" (click)="activate(n)">Activate</button>
              }
              @if (n.children?.length) {
                <ul>
                  <ng-container *ngTemplateOutlet="unitNodes; context: { $implicit: n.children, depth: depth + 1 }"></ng-container>
                </ul>
              }
            </li>
          }
        </ng-template>
      </section>

      <form class="create" (ngSubmit)="createUnit()">
        <h2>Create organizational unit</h2>
        <p class="muted">
          Parent: <strong>{{ selectedParent()?.name || 'Organization (root)' }}</strong>
          @if (selectedParent()) {
            <button type="button" class="link" (click)="clearParent()">Clear</button>
          }
        </p>
        <label
          >Type
          <select name="type" [(ngModel)]="selectedTypeId" required>
            <option value="" disabled>Select type</option>
            @for (t of types(); track t.id) {
              <option [value]="t.id">{{ t.name }} ({{ t.code }})</option>
            }
          </select>
        </label>
        <label>Code <input name="ucode" [(ngModel)]="unitCode" required /></label>
        <label>Name <input name="uname" [(ngModel)]="unitName" required /></label>
        <button type="submit" [disabled]="!orgId || !selectedTypeId || !unitCode || !unitName || saving()">Create</button>
      </form>
    </section>
  `,
  styles: [
    `
      .pad { padding: 1.25rem; max-width: 1080px; }
      .err { color: #b91c1c; }
      .muted { color: #6b7280; font-size: 0.9rem; }
      .row { display: flex; align-items: center; justify-content: space-between; gap: 1rem; }
      .chk { font-size: 0.9rem; }
      .tree { list-style: none; padding-left: 0; }
      .type {
        display: inline-block;
        min-width: 4.5rem;
        font-size: 0.7rem;
        text-transform: uppercase;
        color: var(--color-primary-hover);
        margin-right: 0.35rem;
      }
      .badge { margin-left: 0.35rem; font-size: 0.75rem; padding: 0.1rem 0.4rem; border-radius: 4px; background: #dcfce7; }
      .badge.off { background: #fee2e2; }
      .link { background: none; border: none; color: var(--color-primary-hover); cursor: pointer; text-decoration: underline; margin-left: 0.35rem; padding: 0; }
      .create { margin-top: 1.5rem; display: grid; gap: 0.5rem; max-width: 460px; }
      label { display: grid; gap: 0.25rem; }
      input, select { padding: 0.4rem 0.5rem; }
      button[type='submit'] { width: fit-content; padding: 0.45rem 0.9rem; }
    `,
  ],
})
export class HospitalHierarchyComponent implements OnInit {
  private api = inject(OrganizationApiService);
  private session = inject(SessionService);
  readonly crumbs: PageBreadcrumbItem[] = [
    { label: 'Organization', route: '/app/organization/overview' },
    { label: 'Hospitals' },
  ];
  orgId = this.session.getOrganizationId();

  hospitals = signal<HospitalSummary[]>([]);
  hospitalsLoading = signal(true);
  units = signal<OrganizationalUnitNode[]>([]);
  unitsLoading = signal(true);
  types = signal<OrganizationalUnitType[]>([]);
  includeInactive = signal(false);
  selectedParent = signal<OrganizationalUnitNode | null>(null);
  selectedTypeId = '';
  unitCode = '';
  unitName = '';
  saving = signal(false);
  error = signal<string | null>(null);

  ngOnInit(): void {
    if (!this.orgId) {
      this.error.set('Organization context is not available.');
      this.hospitalsLoading.set(false);
      this.unitsLoading.set(false);
      return;
    }
    this.api.getOrganizationalUnitTypes().subscribe({
      next: (t) => this.types.set(t ?? []),
      error: () => this.error.set('Unable to load unit types. Restart OrganizationService to seed UnitType lookups.'),
    });
    this.api.listHospitals(this.orgId).subscribe({
      next: (h) => {
        this.hospitals.set(h ?? []);
        this.hospitalsLoading.set(false);
      },
      error: () => {
        this.hospitals.set([]);
        this.hospitalsLoading.set(false);
      },
    });
    this.reloadUnits();
  }

  reloadUnits(): void {
    if (!this.orgId) return;
    this.unitsLoading.set(true);
    this.api.getOrganizationalUnitTree(this.orgId, undefined, this.includeInactive()).subscribe({
      next: (u) => {
        this.units.set(u ?? []);
        this.unitsLoading.set(false);
      },
      error: () => {
        this.error.set('Unable to load facility unit tree.');
        this.unitsLoading.set(false);
      },
    });
  }

  toggleInactive(v: boolean): void {
    this.includeInactive.set(v);
    this.reloadUnits();
  }

  selectParent(n: OrganizationalUnitNode): void {
    this.selectedParent.set(n);
  }

  clearParent(): void {
    this.selectedParent.set(null);
  }

  createUnit(): void {
    if (!this.orgId || !this.selectedTypeId || !this.unitCode || !this.unitName) return;
    this.saving.set(true);
    this.api
      .createOrganizationalUnit({
        organizationId: this.orgId,
        parentUnitId: this.selectedParent()?.id,
        unitTypeValueId: this.selectedTypeId,
        code: this.unitCode.trim(),
        name: this.unitName.trim(),
      })
      .subscribe({
        next: () => {
          this.unitCode = '';
          this.unitName = '';
          this.saving.set(false);
          this.reloadUnits();
        },
        error: (err) => {
          const msg = err?.error?.errors?.[0]?.message || err?.error?.message || 'Unable to create unit (check hierarchy rules).';
          this.error.set(msg);
          this.saving.set(false);
        },
      });
  }

  activate(n: OrganizationalUnitNode): void {
    this.api.activateOrganizationalUnit(n.id).subscribe({ next: () => this.reloadUnits() });
  }

  deactivate(n: OrganizationalUnitNode): void {
    this.api.deactivateOrganizationalUnit(n.id).subscribe({ next: () => this.reloadUnits() });
  }
}

@Component({
  selector: 'app-membership-history',
  standalone: true,
  imports: [CommonModule, PageBreadcrumbComponent],
  template: `
    <section class="pad aov-page">
      <app-page-breadcrumb [items]="crumbs" />
      <header class="aov-page__header">
        <div class="aov-page__titles">
          <h1 class="aov-page__title">Membership History</h1>
        </div>
      </header>
      <p>
        Open a membership from Membership to inspect transitions (API: /membership/history).
      </p>
    </section>
  `,
  styles: [`.pad { padding: var(--page-padding); }`],
})
export class MembershipHistoryComponent {
  readonly crumbs: PageBreadcrumbItem[] = [
    { label: 'Organization', route: '/app/organization/overview' },
    { label: 'Membership', route: '/app/organization/membership' },
    { label: 'History' },
  ];
}

@Component({
  selector: 'app-assignment-timeline',
  standalone: true,
  imports: [CommonModule, PageBreadcrumbComponent],
  template: `
    <section class="pad aov-page">
      <app-page-breadcrumb [items]="crumbs" />
      <header class="aov-page__header">
        <div class="aov-page__titles">
          <h1 class="aov-page__title">Assignments</h1>
        </div>
      </header>
      <p>Workforce department/hospital assignments aggregate (read-only).</p>
    </section>
  `,
  styles: [`.pad { padding: var(--page-padding); }`],
})
export class AssignmentTimelineComponent {
  readonly crumbs: PageBreadcrumbItem[] = [
    { label: 'Organization', route: '/app/organization/overview' },
    { label: 'Assignments' },
  ];
}

@Component({
  selector: 'app-organization-graph',
  standalone: true,
  imports: [CommonModule, PageBreadcrumbComponent],
  template: `
    <section class="pad aov-page">
      <app-page-breadcrumb [items]="crumbs" />
      <header class="aov-page__header">
        <div class="aov-page__titles">
          <h1 class="aov-page__title">Organization Graph</h1>
        </div>
      </header>
      @if (error()) {
        <p class="err">{{ error() }}</p>
      } @else if (loading()) {
        <p>Loading graph…</p>
      } @else if (nodes().length === 0) {
        <p>No graph nodes for this organization.</p>
      } @else {
        <ul>
          @for (n of nodes(); track n.id) {
            <li>{{ n.nodeType }}: {{ n.label }}</li>
          }
        </ul>
      }
    </section>
  `,
  styles: [`.pad { padding: var(--page-padding); } .err { color: var(--color-status-danger-fg); }`],
})
export class OrganizationGraphComponent implements OnInit {
  private api = inject(OrganizationApiService);
  private session = inject(SessionService);
  readonly crumbs: PageBreadcrumbItem[] = [
    { label: 'Organization', route: '/app/organization/overview' },
    { label: 'Graph' },
  ];
  nodes = signal<{ id: string; label: string; nodeType: string }[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const orgId = this.session.getOrganizationId();
    if (!orgId) {
      this.error.set('Organization context is not available.');
      this.loading.set(false);
      return;
    }
    this.api.getOrganizationGraph(orgId).subscribe({
      next: (g) => {
        this.nodes.set(g?.nodes ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load organization graph.');
        this.loading.set(false);
      },
    });
  }
}
