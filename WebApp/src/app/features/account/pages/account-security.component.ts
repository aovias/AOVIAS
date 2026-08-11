import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/auth/auth.service';
import { UserSessionDto } from '../../../core/auth/models/login-response.model';
import { NotificationService } from '../../../core/ui/services/notification.service';

@Component({
  selector: 'app-account-security',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="page">
      <h1>Account security</h1>
      <section>
        <h2>Change password</h2>
        <form [formGroup]="passwordForm" (ngSubmit)="changePassword()">
          <input type="password" formControlName="currentPassword" placeholder="Current password" />
          <input type="password" formControlName="newPassword" placeholder="New password" />
          <button type="submit" [disabled]="passwordForm.invalid">Update password</button>
        </form>
      </section>
      <section>
        <h2>Active sessions</h2>
        @for (s of sessions(); track s.sessionId) {
          <div class="session-row">
            <span>{{ s.userAgent || 'Unknown device' }} {{ s.isCurrent ? '(current)' : '' }}</span>
            @if (!s.isCurrent) {
              <button type="button" (click)="revoke(s.sessionId)">Revoke</button>
            }
          </div>
        }
        <button type="button" (click)="logoutAll()">Sign out everywhere</button>
      </section>
    </div>
  `,
  styles: [
    `
      .page { padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end); max-width: 720px; }
      section { margin-bottom: 2rem; }
      input, button { display: block; margin: 0.5rem 0; padding: 0.5rem; max-width: 360px; }
      .session-row { display: flex; justify-content: space-between; align-items: center; padding: 0.5rem 0; border-bottom: 1px solid #eee; }
    `,
  ],
})
export class AccountSecurityComponent implements OnInit {
  private auth = inject(AuthService);
  private fb = inject(FormBuilder);
  private notify = inject(NotificationService);

  sessions = signal<UserSessionDto[]>([]);
  passwordForm = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
  });

  ngOnInit(): void {
    this.loadSessions();
  }

  loadSessions(): void {
    this.auth.listSessions().subscribe({
      next: (items) => this.sessions.set(items),
      error: () => this.notify.showError('Unable to load sessions'),
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) return;
    const { currentPassword, newPassword } = this.passwordForm.value;
    this.auth.changePassword(currentPassword!, newPassword!).subscribe({
      next: () => this.notify.showSuccess('Password updated'),
      error: () => this.notify.showError('Password update failed'),
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
