import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { SessionService } from '../../../core/auth/session.service';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import {
  DepartmentTreeNode,
  MembershipItem,
  OrganizationApiService,
} from '../../organization/services/organization-api.service';
import { WorkforceAdminPageStateComponent } from '../components/workforce-admin-page-state.component';

interface DepartmentRow {
  id: string;
  name: string;
  code: string;
  head: string;
  status: string;
  memberCount: number;
  depth: number;
}

@Component({
  selector: 'app-workforce-departments-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    SettingsPageHeaderComponent,
    WorkforceAdminPageStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="wf-page">
      <app-settings-page-header
        title="Departments"
        description="Organization department hierarchy from OrganizationService, with membership coverage."
      />
      <mat-card class="wf-page__card">
        <mat-card-content>
          @if (loading()) {
            <app-workforce-admin-page-state state="loading" message="Loading departments…" [showRetry]="false" />
          } @else if (error()) {
            <app-workforce-admin-page-state state="error" [message]="error()!" (retry)="reload()" />
          } @else if (!rows().length) {
            <app-workforce-admin-page-state
              state="empty"
              icon="apartment"
              message="No departments are configured for this organization."
              [showRetry]="false"
            />
          } @else {
            <div class="wf-toolbar">
              <mat-form-field appearance="outline" class="wf-toolbar__search">
                <mat-label>Search departments</mat-label>
                <input matInput [ngModel]="search()" (ngModelChange)="search.set($event)" />
                <mat-icon matSuffix aria-hidden="true">search</mat-icon>
              </mat-form-field>
            </div>
            <div class="wf-table-wrap">
              <table class="wf-table">
                <thead>
                  <tr>
                    <th>Department Name</th>
                    <th>Code</th>
                    <th>Head</th>
                    <th>Status</th>
                    <th>Member Count</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  @for (d of paged(); track d.id) {
                    <tr>
                      <td>
                        <strong [style.padding-left.px]="d.depth * 16">{{ d.name }}</strong>
                      </td>
                      <td>{{ d.code || '—' }}</td>
                      <td>{{ d.head }}</td>
                      <td><span class="wf-chip">{{ d.status }}</span></td>
                      <td>{{ d.memberCount }}</td>
                      <td class="wf-actions">
                        <span>View</span>
                        <span class="wf-actions--muted">Edit</span>
                        <span class="wf-actions--muted">Deactivate</span>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
            <div class="wf-pager">
              <button type="button" (click)="prev()" [disabled]="page() <= 1">Previous</button>
              <span>Page {{ page() }} of {{ totalPages() }}</span>
              <button type="button" (click)="next()" [disabled]="page() >= totalPages()">Next</button>
            </div>
          }
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [
    `
      .wf-page__card { border-radius: 16px; }
      .wf-toolbar { margin-bottom: 12px; }
      .wf-toolbar__search { width: min(360px, 100%); }
      .wf-table-wrap { overflow-x: auto; }
      .wf-table { width: 100%; border-collapse: collapse; font-size: 13px; }
      .wf-table th, .wf-table td {
        text-align: left; padding: 12px 10px;
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);
      }
      .wf-table th { font-size: 12px; color: var(--color-text-secondary, #6b7280); }
      .wf-chip {
        display: inline-block; padding: 2px 10px; border-radius: 999px;
        font-size: 12px; font-weight: 600; background: #ecfdf5; color: #047857;
      }
      .wf-actions { display: flex; gap: 10px; font-size: 12px; font-weight: 600; color: var(--color-primary); }
      .wf-actions--muted { color: #9ca3af; font-weight: 500; }
      .wf-pager { display: flex; gap: 12px; align-items: center; margin-top: 12px; font-size: 13px; }
      .wf-pager button {
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        background: #fff; border-radius: 8px; padding: 6px 10px; cursor: pointer;
      }
      .wf-pager button:disabled { opacity: 0.5; cursor: not-allowed; }
    `,
  ],
})
export class WorkforceDepartmentsPageComponent implements OnInit {
  private readonly session = inject(SessionService);
  private readonly api = inject(OrganizationApiService);

  readonly rows = signal<DepartmentRow[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 20;

  readonly filtered = computed(() => {
    const q = this.search().trim().toLowerCase();
    if (!q) return this.rows();
    return this.rows().filter(
      (d) => d.name.toLowerCase().includes(q) || d.code.toLowerCase().includes(q) || d.head.toLowerCase().includes(q),
    );
  });

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.filtered().length / this.pageSize)));
  readonly paged = computed(() => {
    const start = (this.page() - 1) * this.pageSize;
    return this.filtered().slice(start, start + this.pageSize);
  });

  ngOnInit(): void {
    this.reload();
  }

  prev(): void {
    if (this.page() > 1) this.page.update((p) => p - 1);
  }

  next(): void {
    if (this.page() < this.totalPages()) this.page.update((p) => p + 1);
  }

  reload(): void {
    const organizationId = this.session.getOrganizationId();
    if (!organizationId) {
      this.error.set('Organization context is required.');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.api.getDepartmentTree(organizationId).subscribe({
      next: (tree) => {
        this.api.listMemberships(organizationId, 1, 200, { includeInactive: true }).subscribe({
          next: (memberships) => {
            this.rows.set(this.flatten(tree ?? [], memberships.items ?? []));
            this.page.set(1);
            this.loading.set(false);
          },
          error: () => {
            this.rows.set(this.flatten(tree ?? [], []));
            this.loading.set(false);
          },
        });
      },
      error: () => {
        this.rows.set([]);
        this.loading.set(false);
        this.error.set('Unable to load departments.');
      },
    });
  }

  private flatten(nodes: DepartmentTreeNode[], memberships: MembershipItem[], depth = 0): DepartmentRow[] {
    const rows: DepartmentRow[] = [];
    for (const node of nodes) {
      const memberCount = memberships.filter((m) => m.departmentId === node.id).length;
      rows.push({
        id: node.id,
        name: node.name,
        code: node.code,
        head: '—',
        status: 'Active',
        memberCount,
        depth,
      });
      if (node.children?.length) {
        rows.push(...this.flatten(node.children, memberships, depth + 1));
      }
    }
    return rows;
  }
}
