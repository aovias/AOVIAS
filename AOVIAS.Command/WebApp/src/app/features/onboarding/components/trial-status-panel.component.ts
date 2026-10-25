import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OnboardingTrialSummaryDto } from '../models/onboarding-journey.model';

@Component({
  selector: 'app-trial-status-panel',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="trial-strip" *ngIf="trial">
      <div class="trial-main">
        <strong>{{ trial.licenseType || 'Trial' }}</strong>
        <span class="status" [class.ok]="trial.isOperational">{{ trial.licenseStatus || 'Unknown' }}</span>
        <span *ngIf="trial.daysRemaining != null">{{ trial.daysRemaining }} day{{ trial.daysRemaining === 1 ? '' : 's' }} left</span>
        <span *ngIf="trial.expiresAtUtc as exp">· expires {{ exp | date: 'mediumDate' }}</span>
        <span>· seats {{ trial.usedSeats }}/{{ trial.maxSeats }}</span>
      </div>
      <div class="trial-ents" *ngIf="trial.entitlements?.length">
        <span class="chip" *ngFor="let e of trial.entitlements.slice(0, 4)">{{ e }}</span>
      </div>
      <button type="button" class="upgrade" disabled title="Available in a later commercial phase">Upgrade (coming soon)</button>
    </div>
  `,
  styles: [`
    .trial-strip {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 12px 20px;
      padding: 10px 24px;
      border-bottom: 1px solid var(--color-border, #e5e7eb);
      background: color-mix(in srgb, var(--color-primary, #0b5fff) 6%, transparent);
      font-size: 13px;
      color: var(--color-text-secondary, #4b5563);
    }
    .trial-main { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
    .trial-main strong { color: var(--color-text-primary, #111827); }
    .status { padding: 2px 8px; border-radius: 4px; background: #fee2e2; color: #991b1b; }
    .status.ok { background: #dcfce7; color: #166534; }
    .chip {
      display: inline-block;
      margin-right: 6px;
      padding: 2px 8px;
      border-radius: 4px;
      background: var(--color-surface, #fff);
      border: 1px solid var(--color-border, #e5e7eb);
      font-size: 11px;
    }
    .upgrade {
      margin-left: auto;
      border: 1px solid var(--color-border, #d1d5db);
      background: transparent;
      padding: 6px 12px;
      border-radius: 6px;
      font-size: 12px;
      opacity: 0.7;
      cursor: not-allowed;
    }
  `],
})
export class TrialStatusPanelComponent {
  @Input() trial: OnboardingTrialSummaryDto | null = null;
}
