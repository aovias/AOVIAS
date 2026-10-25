import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../core/ui/services/notification.service';

@Component({
  selector: 'app-change-password-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form class="change-password" [formGroup]="form" (ngSubmit)="submit()">
      <label>
        Current password
        <input type="password" formControlName="currentPassword" autocomplete="current-password" />
      </label>
      <label>
        New password
        <input type="password" formControlName="newPassword" autocomplete="new-password" />
      </label>
      <button type="submit" [disabled]="form.invalid">Update password</button>
    </form>
  `,
  styles: [
    `
      .change-password {
        display: grid;
        gap: 12px;
        max-width: 360px;
      }
      label {
        display: grid;
        gap: 6px;
        font-size: 14px;
        font-weight: 500;
      }
      input {
        padding: 10px 12px;
        border: 1px solid var(--color-border-subtle, #d1d5db);
        border-radius: 8px;
      }
      button {
        justify-self: start;
        padding: 10px 16px;
        border: none;
        border-radius: 8px;
        background: var(--color-brand-primary, var(--color-primary));
        color: #fff;
        font-weight: 600;
        cursor: pointer;
      }
      button:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    `,
  ],
})
export class ChangePasswordFormComponent {
  private readonly auth = inject(AuthService);
  private readonly notify = inject(NotificationService);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
  });

  submit(): void {
    if (this.form.invalid) return;
    const { currentPassword, newPassword } = this.form.value;
    this.auth.changePassword(currentPassword!, newPassword!).subscribe({
      next: () => {
        this.notify.showSuccess('Password updated');
        this.form.reset();
      },
      error: () => this.notify.showError('Password update failed'),
    });
  }
}
