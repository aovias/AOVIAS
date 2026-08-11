import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../core/ui/services/notification.service';

function passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return password && confirm && password !== confirm ? { passwordMismatch: true } : null;
}

@Component({
  selector: 'app-register',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="register-page">
      <h1>Create account</h1>
      <p class="lede">Register with your email to access AOVIAS. For a new hospital organization, use organization signup.</p>

      @if (completed()) {
        <div class="success-panel">
          <h2>Check your email</h2>
          <p>{{ completedMessage() }}</p>
          <button type="button" class="linkish" (click)="resend()" [disabled]="submitting()">Resend verification email</button>
          <a routerLink="/login">Continue to sign in</a>
        </div>
      } @else {
        <form [formGroup]="form" (ngSubmit)="submit()">
          <label for="email">Email</label>
          <input id="email" type="email" formControlName="email" autocomplete="email" />
          @if (form.controls.email.touched && form.controls.email.invalid) {
            <span class="error">Enter a valid email address.</span>
          }

          <label for="password">Password</label>
          <input id="password" type="password" formControlName="password" autocomplete="new-password" />
          <p class="hint">At least 8 characters with upper, lower, number, and symbol.</p>

          <label for="confirmPassword">Confirm password</label>
          <input id="confirmPassword" type="password" formControlName="confirmPassword" autocomplete="new-password" />
          @if (form.hasError('passwordMismatch') && form.controls.confirmPassword.touched) {
            <span class="error">Passwords do not match.</span>
          }

          @if (errorMessage()) {
            <div class="error-banner">{{ errorMessage() }}</div>
          }

          <button type="submit" [disabled]="form.invalid || submitting()">
            {{ submitting() ? 'Creating account…' : 'Create account' }}
          </button>
        </form>
      }

      <p class="footer-links">
        <a routerLink="/login">Sign in</a>
        ·
        <a routerLink="/signup">Organization signup</a>
      </p>
    </div>
  `,
  styles: [
    `
      .register-page {
        max-width: 420px;
        margin: 3rem auto;
        padding: 1.5rem;
      }
      h1 {
        margin: 0 0 0.5rem;
        font-size: 1.75rem;
      }
      .lede {
        color: #555;
        margin-bottom: 1.5rem;
      }
      label {
        display: block;
        margin-top: 0.75rem;
        font-weight: 600;
      }
      input {
        width: 100%;
        padding: 0.6rem 0.75rem;
        margin-top: 0.25rem;
        box-sizing: border-box;
      }
      button[type='submit'] {
        width: 100%;
        margin-top: 1.25rem;
        padding: 0.75rem;
        cursor: pointer;
      }
      .hint {
        font-size: 0.8rem;
        color: #666;
        margin: 0.25rem 0 0;
      }
      .error {
        color: #b00020;
        font-size: 0.85rem;
      }
      .error-banner {
        margin-top: 0.75rem;
        padding: 0.75rem;
        background: #fdecea;
        color: #b00020;
        border-radius: 4px;
      }
      .success-panel {
        padding: 1rem 0;
      }
      .footer-links {
        margin-top: 1.5rem;
        text-align: center;
      }
      .linkish {
        display: block;
        margin: 0.75rem 0;
        background: none;
        border: none;
        color: #0b5fff;
        cursor: pointer;
        padding: 0;
        text-align: left;
      }
    `,
  ],
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);

  submitting = signal(false);
  completed = signal(false);
  completedMessage = signal('');
  errorMessage = signal<string | null>(null);
  private registeredEmail = '';

  form = this.fb.nonNullable.group(
    {
      email: ['', [Validators.required, Validators.email]],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$/),
        ],
      ],
      confirmPassword: ['', Validators.required],
    },
    { validators: passwordMatchValidator },
  );

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.errorMessage.set(null);
    const email = this.form.controls.email.value.trim().toLowerCase();
    const password = this.form.controls.password.value;
    this.auth.register({ email, password }).subscribe({
      next: (res) => {
        this.submitting.set(false);
        this.registeredEmail = email;
        if (res.requiresEmailVerification) {
          this.completed.set(true);
          this.completedMessage.set(
            res.message || 'Account created. Please verify your email before continuing.',
          );
        } else {
          this.notify.showSuccess(res.message || 'Account created successfully.');
          void this.router.navigateByUrl('/login');
        }
      },
      error: (err) => {
        this.submitting.set(false);
        const msg =
          err?.error?.message ||
          err?.error?.error ||
          err?.error?.errors?.[0]?.message ||
          'Unable to create account. Try a different email or check password requirements.';
        this.errorMessage.set(typeof msg === 'string' ? msg : 'Unable to create account.');
      },
    });
  }

  resend(): void {
    if (!this.registeredEmail) return;
    this.submitting.set(true);
    this.auth.resendVerification(this.registeredEmail).subscribe({
      next: (res) => {
        this.submitting.set(false);
        this.notify.showSuccess(res.message || 'Verification email sent.');
      },
      error: () => {
        this.submitting.set(false);
        this.notify.showError('Unable to resend verification email.');
      },
    });
  }
}
