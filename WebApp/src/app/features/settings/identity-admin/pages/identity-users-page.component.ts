import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { ROLE_IDS, ROLE_LABELS } from '../../../../core/auth/roles/role-definitions';
import { NotificationService } from '../../../../core/ui/services/notification.service';
import { UserContextService } from '../../../../core/services/user-context.service';
import { HasPermissionDirective } from '../../../../shared/authorization/has-permission.directive';
import { AuthorizationService } from '../../../../shared/authorization/authorization.service';
import { SettingsComingSoonBadgeComponent } from '../../components/settings-coming-soon-badge.component';
import { SettingsPageHeaderComponent } from '../../components/settings-page-header.component';
import { AdminPageStateComponent } from '../../../../shared/ui/administration/admin-page-state.component';
import { IdentityConfirmDialogComponent } from '../components/identity-confirm-dialog.component';
import { IdentityUserFormDialogComponent } from '../components/identity-user-form-dialog.component';
import { IdentityUserStatusChipComponent } from '../components/identity-user-status-chip.component';
import { IdentityUserListItem } from '../models/identity-user-admin.model';
import { IdentityAdminApiService } from '../services/identity-admin-api.service';

@Component({
  selector: 'app-identity-users-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule,
    MatPaginatorModule,
    MatIconModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    HasPermissionDirective,
    IdentityUserStatusChipComponent,
    SettingsComingSoonBadgeComponent,
    SettingsPageHeaderComponent,
    AdminPageStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="users-page">
      <div class="users-page__top">
        <app-settings-page-header
          title="Users"
          description="Manage organization users, roles, and account status."
        />
        <div class="users-page__header-actions">
          <button mat-stroked-button disabled title="Coming soon">Bulk Import</button>
          <button mat-stroked-button disabled title="Coming soon">Export</button>
          <button
            mat-flat-button
            color="primary"
            *hasPermission="'Users.Create'"
            (click)="openCreateDialog()"
          >
            <mat-icon>person_add</mat-icon> Add User
          </button>
        </div>
      </div>

      <section class="users-page__toolbar" aria-label="User filters">
        <mat-form-field appearance="outline" class="users-page__search">
          <mat-label>Search</mat-label>
          <input matInput [formControl]="searchControl" placeholder="Name, email, designation" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Role</mat-label>
          <mat-select [formControl]="roleControl">
            <mat-option value="">All roles</mat-option>
            @for (role of roles; track role) {
              <mat-option [value]="role">{{ roleLabels[role] }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Status</mat-label>
          <mat-select [formControl]="statusControl">
            <mat-option value="">Active only</mat-option>
            <mat-option value="Active">Active</mat-option>
            <mat-option value="Suspended">Suspended</mat-option>
            <mat-option value="Archived">Archived</mat-option>
          </mat-select>
        </mat-form-field>
        <button mat-stroked-button type="button" (click)="reload()">
          <mat-icon>refresh</mat-icon> Refresh
        </button>
      </section>

      @if (loading()) {
        <app-admin-page-state state="loading" message="Loading users…" [showRetry]="false" />
      } @else if (!users().length) {
        <app-admin-page-state
          state="empty"
          icon="group_off"
          message="No users found. Add a user to get started with identity administration."
          [showRetry]="false"
        />
        <div class="users-page__empty-action">
          <button
            mat-flat-button
            color="primary"
            *hasPermission="'Users.Create'"
            (click)="openCreateDialog()"
          >
            Add User
          </button>
        </div>
      } @else {
        <div class="users-page__table-wrap">
          <table mat-table [dataSource]="users()" class="users-table">
            <ng-container matColumnDef="avatar">
              <th mat-header-cell *matHeaderCellDef></th>
              <td mat-cell *matCellDef="let row">
                @if (row.profilePicUrl) {
                  <img class="avatar" [src]="row.profilePicUrl" alt="" />
                } @else {
                  <span class="avatar avatar--placeholder">{{ initials(row) }}</span>
                }
              </td>
            </ng-container>
            <ng-container matColumnDef="fullName">
              <th mat-header-cell *matHeaderCellDef>Name</th>
              <td mat-cell *matCellDef="let row">
                <div class="name-cell">
                  <strong>{{ row.fullName }}</strong>
                  <span>{{ row.designation || '—' }}</span>
                </div>
              </td>
            </ng-container>
            <ng-container matColumnDef="email">
              <th mat-header-cell *matHeaderCellDef>Email</th>
              <td mat-cell *matCellDef="let row">{{ row.email || '—' }}</td>
            </ng-container>
            <ng-container matColumnDef="userName">
              <th mat-header-cell *matHeaderCellDef>Username</th>
              <td mat-cell *matCellDef="let row">{{ row.userName || '—' }}</td>
            </ng-container>
            <ng-container matColumnDef="department">
              <th mat-header-cell *matHeaderCellDef>Department</th>
              <td mat-cell *matCellDef="let row">{{ row.departmentName || '—' }}</td>
            </ng-container>
            <ng-container matColumnDef="role">
              <th mat-header-cell *matHeaderCellDef>Role</th>
              <td mat-cell *matCellDef="let row">{{ roleLabel(row.role) }}</td>
            </ng-container>
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let row">
                <app-identity-user-status-chip [status]="row.accountStatus" />
              </td>
            </ng-container>
            <ng-container matColumnDef="lastLogin">
              <th mat-header-cell *matHeaderCellDef>Last login</th>
              <td mat-cell *matCellDef="let row">
                {{ row.lastLoginAt ? (row.lastLoginAt | date: 'medium') : '—' }}
              </td>
            </ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef></th>
              <td mat-cell *matCellDef="let row">
                <button mat-icon-button [matMenuTriggerFor]="menu" aria-label="User actions">
                  <mat-icon>more_vert</mat-icon>
                </button>
                <mat-menu #menu="matMenu">
                  <button
                    mat-menu-item
                    *hasPermission="'Users.Update'"
                    (click)="openEditDialog(row)"
                  >
                    Edit
                  </button>
                  <button
                    mat-menu-item
                    *hasPermission="'Users.ResetPassword'"
                    (click)="confirmResetPassword(row)"
                  >
                    Reset password
                  </button>
                  @if (row.accountStatus === 'Active') {
                    <button
                      mat-menu-item
                      *hasPermission="'Users.Disable'"
                      (click)="confirmDisable(row)"
                    >
                      Disable
                    </button>
                  } @else if (
                    row.accountStatus === 'Disabled' || row.accountStatus === 'Suspended'
                  ) {
                    <button mat-menu-item *hasPermission="'Users.Update'" (click)="activate(row)">
                      Enable
                    </button>
                  }
                  <button
                    mat-menu-item
                    *hasPermission="'Users.Delete'"
                    (click)="confirmDelete(row)"
                  >
                    Delete
                  </button>
                </mat-menu>
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="displayedColumns; sticky: true"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
          </table>
        </div>
        <mat-paginator
          [length]="totalCount()"
          [pageIndex]="pageIndex()"
          [pageSize]="pageSize()"
          [pageSizeOptions]="[10, 20, 50]"
          (page)="onPage($event)"
        />
      }

      <section class="users-page__future">
        <app-settings-coming-soon-badge />
        <p>
          Invite User, Identity Providers, External Users, and API Users are planned for a future
          release.
        </p>
      </section>
    </div>
  `,
  styles: [
    `
      .users-page {
        display: flex;
        flex-direction: column;
        gap: 16px;
      }
      .users-page__top,
      .users-page__toolbar {
        display: flex;
        justify-content: space-between;
        gap: 16px;
        flex-wrap: wrap;
        align-items: flex-start;
      }
      .users-page__future p {
        margin: 0;
        color: var(--color-text-secondary, #6b7280);
      }
      .users-page__header-actions,
      .users-page__toolbar {
        gap: 8px;
        align-items: center;
      }
      .users-page__search {
        min-width: 260px;
        flex: 1;
      }
      .users-page__empty-action {
        display: flex;
        justify-content: center;
        margin-top: -16px;
        margin-bottom: 24px;
      }
      .users-page__table-wrap {
        overflow: auto;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        border-radius: 16px;
      }
      .users-table {
        width: 100%;
      }
      .avatar {
        width: 36px;
        height: 36px;
        border-radius: 999px;
        object-fit: cover;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        background: var(--color-primary-50, #eef3ff);
        color: var(--color-primary-700, #1e4bc7);
        font-weight: 600;
      }
      .avatar--placeholder {
        font-size: 12px;
      }
      .name-cell {
        display: flex;
        flex-direction: column;
        gap: 2px;
      }
      .name-cell span {
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
      .users-page__future {
        padding: 16px;
        border-radius: 16px;
        background: color-mix(in srgb, var(--color-bg-muted, #f3f4f6) 50%, transparent);
      }
      @media (max-width: 640px) {
        .users-page__top {
          flex-direction: column;
        }
        .users-page__header-actions {
          width: 100%;
          flex-wrap: wrap;
        }
      }
    `,
  ],
})
export class IdentityUsersPageComponent implements OnInit {
  private readonly api = inject(IdentityAdminApiService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);
  private readonly userContext = inject(UserContextService);
  private readonly authz = inject(AuthorizationService);

  readonly roles = ROLE_IDS;
  readonly roleLabels = ROLE_LABELS;
  readonly displayedColumns = [
    'avatar',
    'fullName',
    'email',
    'userName',
    'department',
    'role',
    'status',
    'lastLogin',
    'actions',
  ];

  readonly users = signal<IdentityUserListItem[]>([]);
  readonly loading = signal(false);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly canCreate = signal(false);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly roleControl = new FormControl('', { nonNullable: true });
  readonly statusControl = new FormControl('', { nonNullable: true });

  ngOnInit(): void {
    this.authz.can('Users.Create').subscribe((allowed) => this.canCreate.set(allowed));
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => {
        this.pageIndex.set(0);
        this.reload();
      });
    this.roleControl.valueChanges.subscribe(() => {
      this.pageIndex.set(0);
      this.reload();
    });
    this.statusControl.valueChanges.subscribe(() => {
      this.pageIndex.set(0);
      this.reload();
    });
    this.reload();
  }

  reload(): void {
    const orgId = this.userContext.getOrganization()?.id;
    if (!orgId) {
      this.notify.showError('Organization context is not available.');
      return;
    }

    const status = this.statusControl.value;
    this.loading.set(true);
    this.api
      .listUsers({
        search: this.searchControl.value || undefined,
        role: this.roleControl.value || undefined,
        membershipStatus: status || undefined,
        includeInactive: !!status,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (page) => {
          this.users.set(page.items);
          this.totalCount.set(page.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.notify.showError('Unable to load users.');
        },
      });

    this.api.getDepartmentTree(orgId).subscribe({ error: () => undefined });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.reload();
  }

  openCreateDialog(): void {
    const orgId = this.userContext.getOrganization()?.id;
    if (!orgId) return;
    this.api.getDepartmentTree(orgId).subscribe({
      next: (departments) => {
        const ref = this.dialog.open(IdentityUserFormDialogComponent, {
          data: { mode: 'create', departments },
          width: '720px',
        });
        ref.afterClosed().subscribe((result) => {
          if (!result || result.action !== 'create') return;
          this.api.createUser(result.payload).subscribe({
            next: () => {
              this.notify.showSuccess('User created');
              this.reload();
            },
            error: () => this.notify.showError('Failed to create user'),
          });
        });
      },
    });
  }

  openEditDialog(row: IdentityUserListItem): void {
    const orgId = this.userContext.getOrganization()?.id;
    if (!orgId) return;
    this.api.getDepartmentTree(orgId).subscribe({
      next: (departments) => {
        this.api.getUser(row.userId).subscribe({
          next: (user) => {
            const ref = this.dialog.open(IdentityUserFormDialogComponent, {
              data: { mode: 'edit', user, departments },
              width: '720px',
            });
            ref.afterClosed().subscribe((result) => {
              if (!result || result.action !== 'update') return;
              this.api.updateUser(row.userId, result.payload).subscribe({
                next: () => {
                  this.notify.showSuccess('User updated');
                  this.reload();
                },
                error: () => this.notify.showError('Failed to update user'),
              });
            });
          },
        });
      },
    });
  }

  confirmResetPassword(row: IdentityUserListItem): void {
    const ref = this.dialog.open(IdentityConfirmDialogComponent, {
      data: {
        title: 'Reset password',
        message: `Reset password for ${row.fullName}? A temporary password will be set and the user will be required to change it on next login.`,
        confirmLabel: 'Reset password',
      },
    });
    ref.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;
      this.api.resetPassword(row.userId).subscribe({
        next: () => this.notify.showSuccess('Password reset successfully'),
        error: () => this.notify.showError('Password reset failed'),
      });
    });
  }

  confirmDisable(row: IdentityUserListItem): void {
    const ref = this.dialog.open(IdentityConfirmDialogComponent, {
      data: {
        title: 'Disable user',
        message: `Disable ${row.fullName}?`,
        confirmLabel: 'Disable',
        destructive: true,
      },
    });
    ref.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;
      this.api.disableUser(row.userId).subscribe({
        next: () => {
          this.notify.showSuccess('User disabled');
          this.reload();
        },
        error: () => this.notify.showError('Failed to disable user'),
      });
    });
  }

  activate(row: IdentityUserListItem): void {
    this.api.activateUser(row.userId).subscribe({
      next: () => {
        this.notify.showSuccess('User enabled');
        this.reload();
      },
      error: () => this.notify.showError('Failed to enable user'),
    });
  }

  confirmDelete(row: IdentityUserListItem): void {
    const ref = this.dialog.open(IdentityConfirmDialogComponent, {
      data: {
        title: 'Delete user',
        message: `Soft-delete ${row.fullName}? This archives their membership.`,
        confirmLabel: 'Delete',
        destructive: true,
      },
    });
    ref.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;
      this.api.deleteUser(row.userId).subscribe({
        next: () => {
          this.notify.showSuccess('User deleted');
          this.reload();
        },
        error: () => this.notify.showError('Failed to delete user'),
      });
    });
  }

  initials(row: IdentityUserListItem): string {
    const parts = row.fullName.split(' ').filter(Boolean);
    return (parts[0]?.[0] ?? '?') + (parts[1]?.[0] ?? '');
  }

  roleLabel(role: string): string {
    return ROLE_LABELS[role as keyof typeof ROLE_LABELS] ?? role;
  }
}
