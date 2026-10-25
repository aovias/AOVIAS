import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../core/ui/services/notification.service';
import { SessionService } from '../../../core/auth/session.service';
import { RoleExperienceService } from '../../../core/services/role-experience.service';

@Component({
  selector: 'app-mfa-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="auth-page">
      <h1>Two-factor authentication</h1>
      <p>Enter the code from your authenticator app for {{ email() }}.</p>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <input formControlName="code" placeholder="6-digit code" maxlength="8" />
        <button type="submit" [disabled]="form.invalid || submitting()">Verify</button>
      </form>
    </div>
  `,
  styles: [
    `
      .auth-page {
        max-width: 400px;
        margin: 3rem auto;
        padding: 1rem;
      }
      input,
      button {
        display: block;
        width: 100%;
        margin-top: 0.75rem;
        padding: 0.5rem;
      }
    `,
  ],
})
export class MfaLoginComponent {
  private readonly roleExperience = inject(RoleExperienceService);
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private notify = inject(NotificationService);
  private session = inject(SessionService);

  protected email = signal('');
  protected submitting = signal(false);
  private challengeToken = '';

  form = this.fb.group({
    code: ['', [Validators.required, Validators.minLength(6)]],
  });

  constructor() {
    const state = history.state as { mfaChallengeToken?: string; email?: string };
    this.challengeToken = state?.mfaChallengeToken ?? '';
    this.email.set(state?.email ?? '');
    if (!this.challengeToken) {
      void this.router.navigate(['/login']);
    }
  }

  submit(): void {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.auth
      .completeMfaLogin({
        mfaChallengeToken: this.challengeToken,
        code: this.form.value.code!,
        deviceId: this.session.getOrCreateDeviceId(),
      })
      .subscribe({
        next: () => {
          this.notify.showSuccess('Signed in successfully');
          this.roleExperience.resolveDefaultHomeRoute().subscribe((route) => {
            const [path, query] = route.split('?');
            const queryParams: Record<string, string> = {};
            if (query) {
              for (const part of query.split('&')) {
                const [k, v] = part.split('=');
                if (k) queryParams[decodeURIComponent(k)] = decodeURIComponent(v ?? '');
              }
            }
            void this.router.navigate([path || '/app/dashboard'], {
              queryParams: Object.keys(queryParams).length ? queryParams : undefined,
            });
          });
        },
        error: () => {
          this.submitting.set(false);
          this.notify.showError('Invalid MFA code');
        },
      });
  }
}
