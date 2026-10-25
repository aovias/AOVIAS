import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { debounceTime, distinctUntilChanged, switchMap, of } from 'rxjs';
import { NotificationService } from '../../../../core/ui/services/notification.service';
import { SettingsComingSoonBadgeComponent } from '../../components/settings-coming-soon-badge.component';
import { SettingsPageHeaderComponent } from '../../components/settings-page-header.component';
import { IdentityConfirmDialogComponent } from '../components/identity-confirm-dialog.component';
import { IdentityUserListItem } from '../models/identity-user-admin.model';
import { IdentityAdminApiService } from '../services/identity-admin-api.service';

@Component({
  selector: 'app-identity-password-management-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatAutocompleteModule,
    MatButtonModule,
    MatIconModule,
    SettingsPageHeaderComponent,
    SettingsComingSoonBadgeComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="password-admin">
      <app-settings-page-header
        title="Password Management"
        description="Administrative password operations for organization users."
      />

      <mat-card class="password-admin__card">
        <h3>Select user</h3>
        <mat-form-field appearance="outline" class="password-admin__search">
          <mat-label>Search users</mat-label>
          <input
            matInput
            [formControl]="searchControl"
            [matAutocomplete]="auto"
            placeholder="Name or email"
          />
          <mat-autocomplete
            #auto="matAutocomplete"
            (optionSelected)="selectUser($event.option.value)"
          >
            @for (user of suggestions(); track user.userId) {
              <mat-option [value]="user">{{ user.fullName }} — {{ user.email }}</mat-option>
            }
          </mat-autocomplete>
        </mat-form-field>

        @if (selectedUser()) {
          <div class="password-admin__selected">
            <div>
              <strong>{{ selectedUser()!.fullName }}</strong>
              <div class="password-admin__meta">
                {{ selectedUser()!.email }} · {{ selectedUser()!.accountStatus }}
              </div>
            </div>
            <div class="password-admin__actions">
              <button mat-stroked-button type="button" (click)="resetPassword()">
                Reset Password
              </button>
              <button mat-stroked-button type="button" (click)="forceChange()">
                Force Password Change
              </button>
              <button mat-stroked-button type="button" (click)="unlock()">Unlock Account</button>
            </div>
          </div>
        }
      </mat-card>

      <div class="password-admin__placeholders">
        <mat-card class="password-admin__card password-admin__card--muted">
          <h3>Expire Password <app-settings-coming-soon-badge /></h3>
          <p>Force password expiry policies for selected users.</p>
        </mat-card>
        <mat-card class="password-admin__card password-admin__card--muted">
          <h3>Bulk Password Reset <app-settings-coming-soon-badge /></h3>
          <p>Reset passwords for multiple users in one operation.</p>
        </mat-card>
      </div>
    </div>
  `,
  styles: [
    `
      .password-admin {
        display: flex;
        flex-direction: column;
        gap: 16px;
      }
      .password-admin__card {
        padding: 24px;
        border-radius: 16px;
      }
      .password-admin__card--muted {
        background: color-mix(in srgb, var(--color-bg-muted, #f3f4f6) 50%, transparent);
      }
      .password-admin__card h3 {
        margin: 0 0 12px;
        display: flex;
        align-items: center;
        gap: 8px;
      }
      .password-admin__search {
        width: 100%;
        max-width: 520px;
      }
      .password-admin__selected {
        display: flex;
        justify-content: space-between;
        gap: 16px;
        flex-wrap: wrap;
        margin-top: 16px;
        padding-top: 16px;
        border-top: 1px solid var(--color-border-subtle, #e5e7eb);
      }
      .password-admin__meta {
        color: var(--color-text-secondary, #6b7280);
        font-size: 13px;
      }
      .password-admin__actions {
        display: flex;
        gap: 8px;
        flex-wrap: wrap;
      }
      .password-admin__placeholders {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
        gap: 16px;
      }
    `,
  ],
})
export class IdentityPasswordManagementPageComponent implements OnInit {
  private readonly api = inject(IdentityAdminApiService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly suggestions = signal<IdentityUserListItem[]>([]);
  readonly selectedUser = signal<IdentityUserListItem | null>(null);

  ngOnInit(): void {
    this.searchControl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((term) => {
          if (!term.trim()) return of({ items: [] as IdentityUserListItem[] });
          return this.api.listUsers({ search: term, page: 1, pageSize: 10, includeInactive: true });
        }),
      )
      .subscribe({
        next: (page) => this.suggestions.set(page.items),
        error: () => this.suggestions.set([]),
      });
  }

  selectUser(user: IdentityUserListItem): void {
    this.selectedUser.set(user);
    this.searchControl.setValue(user.fullName, { emitEvent: false });
  }

  resetPassword(): void {
    const user = this.selectedUser();
    if (!user) return;
    const ref = this.dialog.open(IdentityConfirmDialogComponent, {
      data: {
        title: 'Reset password',
        message: `Reset password for ${user.fullName}? The new password will not be shown in the UI.`,
        confirmLabel: 'Reset password',
      },
    });
    ref.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;
      this.api.resetPassword(user.userId).subscribe({
        next: () => this.notify.showSuccess('Password reset successfully'),
        error: () => this.notify.showError('Password reset failed'),
      });
    });
  }

  forceChange(): void {
    const user = this.selectedUser();
    if (!user) return;
    this.api.forcePasswordChange(user.userId).subscribe({
      next: () => this.notify.showSuccess('User must change password on next login'),
      error: () => this.notify.showError('Failed to force password change'),
    });
  }

  unlock(): void {
    const user = this.selectedUser();
    if (!user) return;
    this.api.unlockPasswordManagement(user.userId).subscribe({
      next: () => this.notify.showSuccess('Account unlocked'),
      error: () => this.notify.showError('Unlock failed'),
    });
  }
}
