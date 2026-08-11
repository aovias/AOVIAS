import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../core/auth/auth.service';
import { UserSessionDto } from '../../../core/auth/models/login-response.model';
import { ROLE_LABELS } from '../../../core/auth/roles/role-definitions';
import { OrganizationService } from '../../../core/services/organization.service';
import { UserContextService } from '../../../core/services/user-context.service';
import { NotificationService } from '../../../core/ui/services/notification.service';
import { ChangePasswordFormComponent } from '../components/change-password-form.component';
import { SettingsPageHeaderComponent } from '../components/settings-page-header.component';
import { SettingsComingSoonBadgeComponent } from '../components/settings-coming-soon-badge.component';
import { NavigationPreferencesComponent } from '../../../core/navigation/personalization/navigation-preferences.component';

@Component({
  selector: 'app-settings-account-page',
  standalone: true,
  imports: [
    MatCardModule,
    MatIconModule,
    ChangePasswordFormComponent,
    SettingsPageHeaderComponent,
    SettingsComingSoonBadgeComponent,
    NavigationPreferencesComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <app-settings-page-header
        title="Account &amp; Security"
        description="Manage your profile, credentials, and security preferences."
      />

      <mat-card class="section">
        <h3>My Profile</h3>
        <dl class="profile-grid">
          <div>
            <dt>Name</dt>
            <dd>{{ profileName() }}</dd>
          </div>
          <div>
            <dt>Email</dt>
            <dd>{{ profileEmail() || '—' }}</dd>
          </div>
          <div>
            <dt>Role</dt>
            <dd>{{ profileRole() }}</dd>
          </div>
          <div>
            <dt>Organization</dt>
            <dd>{{ organizationName() || '—' }}</dd>
          </div>
        </dl>
        <p class="section__hint">
          Self-service profile editing is not available yet. Contact an administrator to update
          organization profile fields.
        </p>
      </mat-card>

      <mat-card class="section" id="navigation-preferences">
        <app-navigation-preferences />
      </mat-card>

      <mat-card class="section">
        <h3>Change Password</h3>
        <app-change-password-form />
      </mat-card>

      <mat-card class="section">
        <h3>Security</h3>
        <p class="section__hint">Active sessions for this account.</p>
        @for (s of sessions(); track s.sessionId) {
          <div class="session-row">
            <span>{{ s.userAgent || 'Unknown device' }} {{ s.isCurrent ? '(current)' : '' }}</span>
            @if (!s.isCurrent) {
              <button type="button" (click)="revoke(s.sessionId)">Revoke</button>
            }
          </div>
        }
        <button type="button" class="sign-out-all" (click)="logoutAll()">
          Sign out everywhere
        </button>
      </mat-card>

      <mat-card class="section section--muted">
        <h3>Multi-Factor Authentication</h3>
        <app-settings-coming-soon-badge />
        <p class="section__hint">Available in a future release.</p>
      </mat-card>

      <mat-card class="section section--muted">
        <h3>API Tokens</h3>
        <app-settings-coming-soon-badge />
        <p class="section__hint">Available in a future release.</p>
      </mat-card>
    </div>
  `,
  styles: [
    `
      .section {
        padding: 24px;
        margin-bottom: 16px;
        border-radius: 16px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
      }
      .section--muted {
        background: color-mix(in srgb, var(--color-bg-muted, #f3f4f6) 50%, transparent);
      }
      .section h3 {
        margin: 0 0 12px;
        font-size: 16px;
        font-weight: 600;
      }
      .section__hint {
        margin: 12px 0 0;
        font-size: 13px;
        color: var(--color-text-secondary, #6b7280);
      }
      .profile-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 12px 20px;
        margin: 0;
      }
      .profile-grid dt {
        margin: 0;
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
      .profile-grid dd {
        margin: 4px 0 0;
        font-size: 14px;
        font-weight: 500;
      }
      .session-row {
        display: flex;
        justify-content: space-between;
        gap: 12px;
        padding: 8px 0;
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);
        font-size: 14px;
      }
      .sign-out-all {
        margin-top: 12px;
      }
    `,
  ],
})
export class SettingsAccountPageComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly notify = inject(NotificationService);
  private readonly userContext = inject(UserContextService);
  private readonly organizationService = inject(OrganizationService);

  readonly sessions = signal<UserSessionDto[]>([]);
  readonly profileName = signal('—');
  readonly profileEmail = signal<string | null>(null);
  readonly profileRole = signal('—');
  readonly organizationName = signal<string | null>(null);

  ngOnInit(): void {
    this.loadSessions();
    this.loadProfile();
  }

  loadProfile(): void {
    const user = this.userContext.getUser();
    const org = this.userContext.getOrganization();
    this.profileEmail.set(user?.email ?? null);
    const role = user?.roles?.[0];
    this.profileRole.set(role ? (ROLE_LABELS[role as keyof typeof ROLE_LABELS] ?? role) : '—');
    this.organizationName.set(org?.name ?? null);

    const orgId = org?.id;
    if (!orgId) {
      this.profileName.set(user?.email ?? '—');
      return;
    }

    this.organizationService.loadOrganizationDetails().subscribe({
      next: (details) => {
        if (!details) {
          this.profileName.set(user?.email ?? '—');
          return;
        }
        const member = details.members.find((m) => m.userId === user?.id);
        const name = member ? [member.firstName, member.lastName].filter(Boolean).join(' ') : '';
        this.profileName.set(name || user?.email || '—');
      },
      error: () => this.profileName.set(user?.email ?? '—'),
    });
  }

  loadSessions(): void {
    this.auth.listSessions().subscribe({
      next: (items) => this.sessions.set(items),
      error: () => this.notify.showError('Unable to load sessions'),
    });
  }

  revoke(sessionId: string): void {
    this.auth.revokeSession(sessionId).subscribe({
      next: () => {
        this.notify.showSuccess('Session revoked');
        this.loadSessions();
      },
    });
  }

  logoutAll(): void {
    this.auth.logoutAll().subscribe({
      next: () => {
        this.auth.logoutLocal();
        window.location.href = '/login';
      },
    });
  }
}
