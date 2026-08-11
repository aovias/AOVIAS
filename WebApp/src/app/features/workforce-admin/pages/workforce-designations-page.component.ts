import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { SessionService } from '../../../core/auth/session.service';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { OrganizationApiService } from '../../organization/services/organization-api.service';
import { EnterpriseDirectoryApiService } from '../../enterprise-directory/services/enterprise-directory-api.service';
import { WorkforceAdminPageStateComponent } from '../components/workforce-admin-page-state.component';
import { catchError, forkJoin, of } from 'rxjs';

interface DesignationRow {
  name: string;
  category: string;
  status: string;
  usageCount: number;
}

@Component({
  selector: 'app-workforce-designations-page',
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
        title="Designations"
        description="Designations observed across membership and enterprise directory records. Catalog management remains owned by OrganizationService membership data."
      />
      <mat-card class="wf-page__card">
        <mat-card-content>
          @if (loading()) {
            <app-workforce-admin-page-state state="loading" message="Loading designations…" [showRetry]="false" />
          } @else if (error()) {
            <app-workforce-admin-page-state state="error" [message]="error()!" (retry)="reload()" />
          } @else if (!rows().length) {
            <app-workforce-admin-page-state
              state="empty"
              icon="badge"
              message="No designations have been assigned yet."
              [showRetry]="false"
            />
          } @else {
            <div class="wf-toolbar">
              <mat-form-field appearance="outline" class="wf-toolbar__search">
                <mat-label>Search designations</mat-label>
                <input matInput [ngModel]="search()" (ngModelChange)="search.set($event)" />
                <mat-icon matSuffix aria-hidden="true">search</mat-icon>
              </mat-form-field>
            </div>
            <div class="wf-table-wrap">
              <table class="wf-table">
                <thead>
                  <tr>
                    <th>Designation</th>
                    <th>Category</th>
                    <th>Status</th>
                    <th>Usage Count</th>
                  </tr>
                </thead>
                <tbody>
                  @for (d of filtered(); track d.name) {
                    <tr>
                      <td><strong>{{ d.name }}</strong></td>
                      <td>{{ d.category }}</td>
                      <td><span class="wf-chip">{{ d.status }}</span></td>
                      <td>{{ d.usageCount }}</td>
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
        font-size: 12px; font-weight: 600; background: var(--color-primary-50, #eef3ff); color: var(--color-primary-700, #1e4bc7);
      }
    `,
  ],
})
export class WorkforceDesignationsPageComponent implements OnInit {
  private readonly session = inject(SessionService);
  private readonly orgApi = inject(OrganizationApiService);
  private readonly directoryApi = inject(EnterpriseDirectoryApiService);

  readonly rows = signal<DesignationRow[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly search = signal('');

  readonly filtered = computed(() => {
    const q = this.search().trim().toLowerCase();
    if (!q) return this.rows();
    return this.rows().filter(
      (d) => d.name.toLowerCase().includes(q) || d.category.toLowerCase().includes(q),
    );
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    const organizationId = this.session.getOrganizationId();
    if (!organizationId) {
      this.error.set('Organization context is required.');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    forkJoin({
      memberships: this.orgApi.listMemberships(organizationId, 1, 200, { includeInactive: true }).pipe(catchError(() => of(null))),
      facets: this.directoryApi.facets(organizationId).pipe(catchError(() => of(null))),
    }).subscribe({
      next: ({ memberships, facets }) => {
        const counts = new Map<string, number>();
        for (const m of memberships?.items ?? []) {
          const name = (m.designation || '').trim();
          if (!name) continue;
          counts.set(name, (counts.get(name) ?? 0) + 1);
        }
        for (const f of facets?.['designation'] ?? facets?.['Designation'] ?? []) {
          if (!f.value) continue;
          counts.set(f.value, Math.max(counts.get(f.value) ?? 0, f.count ?? 0));
        }
        this.rows.set(
          Array.from(counts.entries())
            .map(([name, usageCount]) => ({
              name,
              category: this.categorize(name),
              status: 'In Use',
              usageCount,
            }))
            .sort((a, b) => b.usageCount - a.usageCount || a.name.localeCompare(b.name)),
        );
        this.loading.set(false);
      },
      error: () => {
        this.rows.set([]);
        this.loading.set(false);
        this.error.set('Unable to load designations.');
      },
    });
  }

  private categorize(name: string): string {
    const value = name.toLowerCase();
    if (/(doctor|physician|surgeon|consultant)/.test(value)) return 'Clinical';
    if (/(nurse|nursing)/.test(value)) return 'Nursing';
    if (/(admin|manager|director|officer)/.test(value)) return 'Administration';
    if (/(tech|technician|lab)/.test(value)) return 'Technical';
    return 'General';
  }
}
