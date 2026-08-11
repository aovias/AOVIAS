import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { SecurityApiService } from '../services/security-api.service';
import { NotificationService } from '../../../core/ui/services/notification.service';

@Component({
  selector: 'app-security-policies',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <section class="page">
      <h1><a routerLink="..">Security</a> / Platform policies</h1>
      <form [formGroup]="form" (ngSubmit)="save()">
        <label>Device trust (days) <input type="number" formControlName="deviceTrustDurationDays" /></label>
        <label>Risk threshold medium <input type="number" formControlName="riskThresholdMedium" /></label>
        <label>Risk threshold high <input type="number" formControlName="riskThresholdHigh" /></label>
        <label>Require MFA on unknown device <input type="checkbox" formControlName="requireMfaOnUnknownDevice" /></label>
        <label>Max concurrent sessions <input type="number" formControlName="maxConcurrentSessions" /></label>
        <button type="submit">Save policy</button>
      </form>
    </section>
  `,
  styles: [`.page { padding: 1.5rem; } label { display: block; margin: 0.75rem 0; } input { margin-left: 0.5rem; }`],
})
export class SecurityPoliciesComponent {
  private api = inject(SecurityApiService);
  private fb = inject(FormBuilder);
  private notify = inject(NotificationService);

  form = this.fb.group({
    deviceTrustDurationDays: [30],
    riskThresholdMedium: [40],
    riskThresholdHigh: [70],
    requireMfaOnUnknownDevice: [false],
    maxConcurrentSessions: [10],
  });

  save(): void {
    this.api.updatePolicy(this.form.value as Record<string, unknown>).subscribe({
      next: () => this.notify.showSuccess('Policy updated'),
      error: () => this.notify.showError('Policy update failed'),
    });
  }
}
