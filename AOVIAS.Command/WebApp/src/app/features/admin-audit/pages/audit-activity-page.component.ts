import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { catchError, of } from 'rxjs';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { SettingsComingSoonBadgeComponent } from '../../settings/components/settings-coming-soon-badge.component';
import { NotificationStatCardComponent } from '../../notifications/components/notification-stat-card.component';
import { NotificationPageStateComponent } from '../../../shared/notifications/components/notification-ui.components';
import { HasPermissionDirective } from '../../../shared/authorization/has-permission.directive';
import {
  AdministrationAuditApiService,
  PlatformActivityEntry,
  PlatformActivitySummary,
} from '../services/administration-audit-api.service';
import { AuditActivityDetailDialogComponent } from '../components/audit-activity-detail-dialog.component';

@Component({
  selector: 'app-audit-activity-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatTooltipModule,
    MatDialogModule,
    SettingsPageHeaderComponent,
    SettingsComingSoonBadgeComponent,
    NotificationStatCardComponent,
    NotificationPageStateComponent,
    HasPermissionDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="audit-page">
      <app-settings-page-header
        title="Audit & Activity"
        description="Review administrative activity, configuration changes and security events across the platform."
      />

      <div class="audit-page__live-hint" role="status">
        <mat-icon aria-hidden="true">sync</mat-icon>
        <span>Live updates ready — SignalR subscription will activate when platform event streaming is enabled.</span>
      </div>

      @if (loading()) {
        <app-notification-page-state state="loading" message="Loading audit activity…" [showRetry]="false" />
      } @else if (error()) {
        <app-notification-page-state state="error" [message]="error()!" (retry)="load()" />
      } @else {
        <section class="audit-page__stats" aria-label="Activity summary">
          <app-notification-stat-card label="Administrative actions" [value]="summary().administrativeActions" />
          <app-notification-stat-card label="Security events" [value]="summary().securityEvents" />
          <app-notification-stat-card label="Configuration changes" [value]="summary().configurationChanges" />
          <app-notification-stat-card label="User management" [value]="summary().userManagementEvents" />
          <app-notification-stat-card label="Integration events" [value]="summary().integrationEvents" />
          <app-notification-stat-card label="Notification events" [value]="summary().notificationEvents" />
          <app-notification-stat-card label="Recent errors" [value]="summary().recentErrors" />
        </section>

        <section class="audit-toolbar" aria-label="Filters">
          <input class="audit-toolbar__search" type="search" [(ngModel)]="search" (ngModelChange)="onFilterChange()" placeholder="Search activity…" aria-label="Search activity" />
          <input type="date" [(ngModel)]="fromDate" (ngModelChange)="onFilterChange()" aria-label="From date" />
          <input type="date" [(ngModel)]="toDate" (ngModelChange)="onFilterChange()" aria-label="To date" />
          <select [(ngModel)]="module" (ngModelChange)="onFilterChange()" aria-label="Module filter">
            <option value="">All modules</option>
            @for (m of modules; track m) { <option [value]="m">{{ m }}</option> }
          </select>
          <select [(ngModel)]="category" (ngModelChange)="onFilterChange()" aria-label="Category filter">
            <option value="">All categories</option>
            @for (c of categories; track c) { <option [value]="c">{{ c }}</option> }
          </select>
          <select [(ngModel)]="status" (ngModelChange)="onFilterChange()" aria-label="Status filter">
            <option value="">All statuses</option>
            @for (s of statuses; track s) { <option [value]="s">{{ s }}</option> }
          </select>
          <select [(ngModel)]="sort" (ngModelChange)="onFilterChange()" aria-label="Sort order">
            <option value="desc">Newest first</option>
            <option value="asc">Oldest first</option>
          </select>
          <button mat-stroked-button type="button" (click)="load()"><mat-icon>refresh</mat-icon> Refresh</button>
          <div class="audit-toolbar__exports" *hasPermission="'Audit.Export'">
            <button mat-stroked-button type="button" disabled matTooltip="Coming soon">Export CSV</button>
            <button mat-stroked-button type="button" disabled matTooltip="Coming soon">Export Excel</button>
            <button mat-stroked-button type="button" disabled matTooltip="Coming soon">Export PDF</button>
          </div>
        </section>

        <div class="audit-toolbar audit-toolbar--placeholder">
          <app-settings-coming-soon-badge label="Saved filters — Coming Soon" />
        </div>

        @if (!items().length) {
          <app-notification-page-state state="empty" message="No activity matches your filters." [showRetry]="false" />
        } @else {
          <div class="audit-table-wrap">
            <table mat-table [dataSource]="items()" class="audit-table" aria-label="Activity timeline">
              <ng-container matColumnDef="timestamp">
                <th mat-header-cell *matHeaderCellDef sticky>Timestamp</th>
                <td mat-cell *matCellDef="let row">{{ row.timestampUtc | date: 'medium' }}</td>
              </ng-container>
              <ng-container matColumnDef="category">
                <th mat-header-cell *matHeaderCellDef>Category</th>
                <td mat-cell *matCellDef="let row">{{ row.category }}</td>
              </ng-container>
              <ng-container matColumnDef="action">
                <th mat-header-cell *matHeaderCellDef>Action</th>
                <td mat-cell *matCellDef="let row">{{ row.action }}</td>
              </ng-container>
              <ng-container matColumnDef="module">
                <th mat-header-cell *matHeaderCellDef>Module</th>
                <td mat-cell *matCellDef="let row">{{ row.module }}</td>
              </ng-container>
              <ng-container matColumnDef="user">
                <th mat-header-cell *matHeaderCellDef>User</th>
                <td mat-cell *matCellDef="let row">{{ row.userId ?? '—' }}</td>
              </ng-container>
              <ng-container matColumnDef="target">
                <th mat-header-cell *matHeaderCellDef>Target</th>
                <td mat-cell *matCellDef="let row">{{ row.target ?? '—' }}</td>
              </ng-container>
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let row"><span class="status-chip" [attr.data-status]="row.status.toLowerCase()">{{ row.status }}</span></td>
              </ng-container>
              <ng-container matColumnDef="ip">
                <th mat-header-cell *matHeaderCellDef>IP</th>
                <td mat-cell *matCellDef="let row">{{ row.ipAddress ?? '—' }}</td>
              </ng-container>
              <ng-container matColumnDef="correlation">
                <th mat-header-cell *matHeaderCellDef>Correlation</th>
                <td mat-cell *matCellDef="let row" class="mono">{{ row.correlationId ?? '—' }}</td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef></th>
                <td mat-cell *matCellDef="let row">
                  <button mat-button type="button" (click)="openDetail(row)">View details</button>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="displayedColumns; sticky: true"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns" class="audit-row"></tr>
            </table>
          </div>
          <div class="audit-pagination">
            <button mat-stroked-button type="button" [disabled]="page() <= 1" (click)="setPage(page() - 1)">Previous</button>
            <span>Page {{ page() }} · {{ totalCount() }} events</span>
            <button mat-stroked-button type="button" [disabled]="page() * pageSize() >= totalCount()" (click)="setPage(page() + 1)">Next</button>
          </div>
        }

        <section class="audit-future">
          <h2>Future capabilities</h2>
          <div class="audit-future__grid">
            @for (item of futurePlaceholders; track item) {
              <div class="audit-future__card">
                <strong>{{ item }}</strong>
                <app-settings-coming-soon-badge />
              </div>
            }
          </div>
        </section>
      }
    </div>
  `,
  styles: [
    `
      .audit-page { display: flex; flex-direction: column; gap: 20px; }
      .audit-page__live-hint {
        display: flex; align-items: center; gap: 8px; padding: 10px 14px; border-radius: 12px;
        background: color-mix(in srgb, var(--color-primary) 8%, transparent);
        color: var(--color-text-secondary, #6b7280); font-size: 13px;
      }
      .audit-page__stats {
        display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 12px;
      }
      .audit-toolbar {
        display: flex; flex-wrap: wrap; gap: 8px; align-items: center;
        padding: 12px; border-radius: 12px; border: 1px solid #e5e7eb; background: #fff;
      }
      .audit-toolbar--placeholder { border-style: dashed; background: #f9fafb; }
      .audit-toolbar__search { flex: 1; min-width: 200px; padding: 8px 12px; border-radius: 8px; border: 1px solid #e5e7eb; }
      .audit-toolbar__exports { display: flex; gap: 8px; margin-left: auto; }
      select, input[type='date'] { padding: 8px 10px; border-radius: 8px; border: 1px solid #e5e7eb; }
      .audit-table-wrap { overflow: auto; border: 1px solid #e5e7eb; border-radius: 12px; max-height: 62vh; }
      .audit-table { width: 100%; min-width: 1100px; }
      .audit-table th { background: #f9fafb; font-size: 12px; text-transform: uppercase; letter-spacing: 0.04em; }
      .audit-table td { font-size: 13px; }
      .audit-row:hover { background: color-mix(in srgb, var(--color-primary) 4%, transparent); }
      .status-chip { padding: 2px 8px; border-radius: 999px; font-size: 12px; font-weight: 600; background: #f3f4f6; }
      .status-chip[data-status*='success'], .status-chip[data-status*='completed'] { background: #dcfce7; color: #166534; }
      .status-chip[data-status*='fail'], .status-chip[data-status*='error'] { background: #fee2e2; color: #991b1b; }
      .mono { font-family: ui-monospace, monospace; font-size: 12px; }
      .audit-pagination { display: flex; justify-content: flex-end; align-items: center; gap: 12px; }
      .audit-future h2 { margin: 0 0 12px; font-size: var(--font-size-section-title); }
      .audit-future__grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 10px; }
      .audit-future__card {
        display: flex; flex-direction: column; gap: 8px; padding: 14px; border-radius: 12px;
        border: 1px dashed #e5e7eb; background: #f9fafb;
      }
    `,
  ],
})
export class AuditActivityPageComponent implements OnInit {
  private readonly api = inject(AdministrationAuditApiService);
  private readonly dialog = inject(MatDialog);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<PlatformActivityEntry[]>([]);
  readonly summary = signal<PlatformActivitySummary>({
    administrativeActions: 0,
    securityEvents: 0,
    configurationChanges: 0,
    userManagementEvents: 0,
    integrationEvents: 0,
    notificationEvents: 0,
    recentErrors: 0,
  });
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(25);

  search = '';
  fromDate = '';
  toDate = '';
  module = '';
  category = '';
  status = '';
  sort: 'asc' | 'desc' = 'desc';

  readonly modules = ['Identity', 'Organization', 'Notifications', 'Enterprise Integrations', 'Compliance', 'AI'];
  readonly categories = ['Security', 'Configuration', 'User Management', 'Integration', 'Notification', 'Compliance', 'AI'];
  readonly statuses = ['Success', 'Failure', 'Failed', 'Completed', 'Pending'];
  readonly displayedColumns = ['timestamp', 'category', 'action', 'module', 'user', 'target', 'status', 'ip', 'correlation', 'actions'];
  readonly futurePlaceholders = ['Compliance Timeline', 'Cross-service Trace', 'Distributed Request Flow', 'Event Replay', 'Platform Diagnostics'];

  ngOnInit(): void {
    this.load();
  }

  onFilterChange(): void {
    this.page.set(1);
    this.load();
  }

  setPage(next: number): void {
    this.page.set(next);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api
      .getActivity({
        search: this.search.trim() || undefined,
        from: this.fromDate ? new Date(this.fromDate).toISOString() : undefined,
        to: this.toDate ? new Date(`${this.toDate}T23:59:59`).toISOString() : undefined,
        module: this.module || undefined,
        category: this.category || undefined,
        status: this.status || undefined,
        sort: this.sort,
        page: this.page(),
        pageSize: this.pageSize(),
      })
      .pipe(catchError(() => {
        this.error.set('Unable to load audit activity. You may not have permission to view audit history.');
        return of(null);
      }))
      .subscribe((response) => {
        this.loading.set(false);
        if (!response) return;
        this.items.set(response.items);
        this.summary.set(response.summary);
        this.totalCount.set(response.totalCount);
      });
  }

  openDetail(entry: PlatformActivityEntry): void {
    this.dialog.open(AuditActivityDetailDialogComponent, {
      width: '640px',
      maxWidth: '95vw',
      data: entry,
    });
  }
}
