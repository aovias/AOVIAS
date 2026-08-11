import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../core/ui/services/notification.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-page">
      <h1>Reset password</h1>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <input type="password" formControlName="newPassword" placeholder="New password" />
        <button type="submit" [disabled]="form.invalid || submitting()">Reset password</button>
      </form>
      <a routerLink="/login">Back to sign in</a>
    </div>
  `,
  styles: ['.auth-page { max-width: 400px; margin: 3rem auto; padding: 1rem; } input, button { width: 100%; margin: 0.5rem 0; padding: 0.5rem; }'],
})
export class ResetPasswordComponent {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private auth = inject(AuthService);
  private notify = inject(NotificationService);

  submitting = signal(false);
  private token = this.route.snapshot.queryParamMap.get('token') ?? '';

  form = this.fb.group({
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
  });

  submit(): void {
    if (this.form.invalid || !this.token) return;
    this.submitting.set(true);
    this.auth.resetPassword(this.token, this.form.value.newPassword!).subscribe({
      next: () => {
        this.notify.showSuccess('Password reset successfully');
        this.submitting.set(false);
      },
      error: () => {
        this.notify.showError('Unable to reset password');
        this.submitting.set(false);
      },
    });
  }
}
