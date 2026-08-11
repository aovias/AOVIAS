import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { SessionService } from '../../../core/auth/session.service';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { MembershipItem, OrganizationApiService } from '../../organization/services/organization-api.service';
import { WorkforceAdminPageStateComponent } from '../components/workforce-admin-page-state.component';

@Component({
  selector: 'app-workforce-memberships-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatButtonModule,
    SettingsPageHeaderComponent,
    WorkforceAdminPageStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="wf-page">
      <app-settings-page-header
        title="Membership"
        description="Organization membership records including role, department, status, and created date from OrganizationService."
      />
      <mat-card class="wf-page__card">
        <mat-card-content>
          @if (loading()) {
            <app-workforce-admin-page-state state="loading" message="Loading memberships…" [showRetry]="false" />
          } @else if (error()) {
            <app-workforce-admin-page-state state="error" [message]="error()!" (retry)="reload()" />
          } @else if (!items().length) {
            <app-workforce-admin-page-state
              state="empty"
              icon="groups"
              message="No memberships found for this organization."
              [showRetry]="false"
            />
          } @else {
            <div class="wf-toolbar">
              <mat-form-field appearance="outline">
                <mat-label>Search</mat-label>
                <input matInput [(ngModel)]="searchTerm" (keyup.enter)="reload(true)" />
                <mat-icon matSuffix aria-hidden="true">search</mat-icon>
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Status</mat-label>
                <mat-select [(ngModel)]="statusFilter" (selectionChange)="reload(true)">
                  <mat-option value="">All</mat-option>
                  <mat-option value="Active">Active</mat-option>
                  <mat-option value="Inactive">Inactive</mat-option>
                  <mat-option value="Pending">Pending</mat-option>
                </mat-select>
              </mat-form-field>
              <button mat-stroked-button type="button" (click)="reload(true)">Apply</button>
              <button mat-stroked-button type="button" (click)="prev()" [disabled]="page() <= 1">Previous</button>
              <button mat-stroked-button type="button" (click)="next()" [disabled]="!hasMore()">Next</button>
              <span class="wf-muted">Page {{ page() }} · {{ total() }} total</span>
            </div>
            <div class="wf-table-wrap">
              <table class="wf-table">
                <thead>
                  <tr>
                    <th>Organization</th>
                    <th>Member</th>
                    <th>Role</th>
                    <th>Department</th>
                    <th>Status</th>
                    <th>Created Date</th>
                  </tr>
                </thead>
                <tbody>
                  @for (m of items(); track m.id) {
                    <tr>
                      <td>{{ m.organizationId }}</td>
                      <td>
                        <strong>{{ memberName(m) }}</strong>
                        <span class="wf-muted-block">{{ m.email || m.userId }}</span>
                      </td>
                      <td>{{ m.role }}</td>
                      <td>{{ m.departmentName || '—' }}</td>
                      <td><span class="wf-chip">{{ m.membershipStatus }}</span></td>
                      <td>{{ (m.createdAt || m.effectiveFrom) | date: 'mediumDate' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [
    `
      .wf-page__card { border-radius: 16px; }
      .wf-toolbar {
        display: grid;
        grid-template-columns: minmax(180px, 1fr) minmax(140px, 180px) auto auto auto auto;
        gap: 10px;
        align-items: center;
        margin-bottom: 12px;
      }
      .wf-table-wrap { overflow-x: auto; }
      .wf-table { width: 100%; border-collapse: collapse; font-size: 13px; }
      .wf-table th, .wf-table td {
        text-align: left; padding: 12px 10px;
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb); vertical-align: top;
      }
      .wf-table th { font-size: 12px; color: var(--color-text-secondary, #6b7280); }
      .wf-chip {
        display: inline-block; padding: 2px 10px; border-radius: 999px;
        font-size: 12px; font-weight: 600; background: var(--color-primary-50, #eef3ff); color: var(--color-primary-700, #1e4bc7);
      }
      .wf-muted, .wf-muted-block { color: var(--color-text-secondary, #6b7280); font-size: 12px; }
      .wf-muted-block { display: block; margin-top: 2px; word-break: break-all; }
      @media (max-width: 900px) {
        .wf-toolbar { grid-template-columns: 1fr 1fr; }
      }
    `,
  ],
})
export class WorkforceMembershipsPageComponent implements OnInit {
  private readonly session = inject(SessionService);
  private readonly api = inject(OrganizationApiService);

  readonly items = signal<MembershipItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly total = signal(0);
  searchTerm = '';
  statusFilter = '';

  ngOnInit(): void {
    this.reload();
  }

  hasMore(): boolean {
    return this.page() * this.pageSize < this.total();
  }

  prev(): void {
    if (this.page() <= 1) return;
    this.page.update((p) => p - 1);
    this.reload();
  }

  next(): void {
    if (!this.hasMore()) return;
    this.page.update((p) => p + 1);
    this.reload();
  }

  memberName(m: MembershipItem): string {
    const name = [m.firstName, m.lastName].filter(Boolean).join(' ').trim();
    return name || m.email || m.userId;
  }

  reload(resetPage = false): void {
    if (resetPage) this.page.set(1);
    const organizationId = this.session.getOrganizationId();
    if (!organizationId) {
      this.error.set('Organization context is required.');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.api
      .listMemberships(organizationId, this.page(), this.pageSize, {
        search: this.searchTerm.trim() || undefined,
        membershipStatus: this.statusFilter || undefined,
        includeInactive: true,
      })
      .subscribe({
        next: (page) => {
          this.items.set(page.items ?? []);
          this.total.set(page.totalCount ?? 0);
          this.loading.set(false);
        },
        error: () => {
          this.items.set([]);
          this.loading.set(false);
          this.error.set('Unable to load memberships.');
        },
      });
  }
}
