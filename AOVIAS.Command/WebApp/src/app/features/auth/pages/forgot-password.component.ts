import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../core/ui/services/notification.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-page">
      <h1>Forgot password</h1>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>Email</label>
        <input type="email" formControlName="email" />
        <button type="submit" [disabled]="form.invalid || submitting()">Send reset link</button>
      </form>
      <a routerLink="/login">Back to sign in</a>
    </div>
  `,
  styles: ['.auth-page { max-width: 400px; margin: 3rem auto; padding: 1rem; } input, button { width: 100%; margin: 0.5rem 0; padding: 0.5rem; }'],
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private notify = inject(NotificationService);

  submitting = signal(false);
  form = this.fb.group({ email: ['', [Validators.required, Validators.email]] });

  submit(): void {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.auth.forgotPassword(this.form.value.email!).subscribe({
      next: (res) => {
        this.notify.showSuccess(res.message);
        this.submitting.set(false);
      },
      error: () => {
        this.notify.showError('Unable to send reset link');
        this.submitting.set(false);
      },
    });
  }
}
