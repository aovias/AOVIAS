import { Component, Output, EventEmitter, OnInit, Input, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OnboardingJourneyDto } from '../models/onboarding-journey.model';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-welcome-step',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="welcome-container">
      <div class="welcome-card">
        <div class="coach-tip" *ngIf="showCoachTip()">
          <span>Tip: Your progress saves automatically — you can resume anytime.</span>
          <button type="button" class="dismiss" (click)="dismissCoach()">Got it</button>
        </div>
        <div class="welcome-icon">
          <img
            [src]="themeService.theme() === 'light' ? '/assets/logo_white_background.png' : '/assets/logo_dark_background.png'"
            alt="AOVIAS Command"
            class="welcome-logo-image"
          />
        </div>
        <h1>Welcome to AOVIAS Command</h1>
        <p class="org-name" *ngIf="journey?.organizationName">{{ journey?.organizationName }}</p>
        <p class="welcome-subtitle">{{ subtitle }}</p>
        <div class="meta-grid" *ngIf="journey">
          <div class="meta-item" *ngIf="journey.administratorName || journey.administratorEmail">
            <strong>Administrator</strong>
            <span>{{ journey.administratorName || journey.administratorEmail }}</span>
          </div>
          <div class="meta-item">
            <strong>Progress</strong>
            <span>{{ journey.progressPercent }}% · ~{{ journey.estimatedMinutesRemaining }} min left</span>
          </div>
          <div class="meta-item" *ngIf="journey.nextAction">
            <strong>Next step</strong>
            <span>{{ journey.nextAction }}</span>
          </div>
          <div class="meta-item" *ngIf="journey.trial as t">
            <strong>Trial</strong>
            <span>{{ t.licenseStatus }} · {{ t.daysRemaining ?? '—' }} days · {{ t.usedSeats }}/{{ t.maxSeats }} seats</span>
          </div>
        </div>
        <div class="links" *ngIf="supportEmail || docsUrl">
          <a *ngIf="docsUrl" [href]="docsUrl" target="_blank" rel="noopener">Documentation</a>
          <a *ngIf="supportEmail" [href]="'mailto:' + supportEmail">Support</a>
        </div>
        <div class="welcome-actions">
          <button class="btn-primary" (click)="onStart.emit()">{{ resumeLabel }}</button>
          <button class="btn-secondary" (click)="onSkip.emit()">Resume later</button>
        </div>
        <p class="skip-note" *ngIf="skipMessage">{{ skipMessage }}</p>
        <div class="welcome-footer"><span>Estimated completion: {{ etaLabel }}</span></div>
      </div>
    </div>
  `,
  styles: [`
    .welcome-container { display:flex; align-items:center; justify-content:center; min-height:500px; padding:48px 24px; }
    .welcome-card { max-width:640px; width:100%; text-align:center; }
    .coach-tip { display:flex; justify-content:space-between; gap:12px; text-align:left; margin-bottom:20px; padding:10px 12px; border-left:3px solid var(--color-primary,#0b5fff); background:color-mix(in srgb, var(--color-primary,#0b5fff) 8%, transparent); font-size:13px; }
    .dismiss { border:none; background:transparent; color:var(--color-primary,#0b5fff); cursor:pointer; font-weight:600; }
    .welcome-logo-image { width:auto; max-width:220px; height:72px; object-fit:contain; }
    h1 { margin:12px 0 4px; font-size:var(--font-size-page-title); color:var(--color-text-primary,#111827); }
    .org-name { margin:0 0 8px; font-weight:600; }
    .welcome-subtitle { margin:0 0 24px; color:var(--color-text-secondary,#6b7280); line-height:1.5; }
    .meta-grid { display:grid; grid-template-columns:1fr 1fr; gap:12px; text-align:left; margin-bottom:20px; }
    .meta-item { padding:12px; border:1px solid var(--color-border,#e5e7eb); border-radius:8px; background:var(--color-background,#f9fafb); }
    .meta-item strong { display:block; font-size:12px; margin-bottom:4px; }
    .meta-item span { font-size:13px; color:var(--color-text-secondary,#6b7280); }
    .links { display:flex; gap:16px; justify-content:center; margin-bottom:24px; font-size:13px; }
    .welcome-actions { display:flex; flex-direction:column; gap:12px; margin-bottom:12px; }
    .btn-primary,.btn-secondary { padding:14px 24px; border-radius:8px; font-size:15px; font-weight:600; border:none; cursor:pointer; }
    .btn-primary { background:var(--color-primary); color:#fff; }
    .btn-secondary { background:transparent; color:var(--color-text-secondary,#6b7280); border:1px solid var(--color-border,#e5e7eb); }
    .skip-note { font-size:13px; color:var(--color-text-secondary,#6b7280); }
    .welcome-footer { font-size:13px; color:var(--color-text-secondary,#9ca3af); }
    @media (max-width:640px) { .meta-grid { grid-template-columns:1fr; } }
  `]
})
export class WelcomeStepComponent implements OnInit {
  readonly themeService = inject(ThemeService);
  @Input() journey: OnboardingJourneyDto | null = null;
  @Input() subtitle = 'Set up your hospital compliance system with guided steps';
  @Input() supportEmail: string | null = null;
  @Input() docsUrl: string | null = null;
  @Input() skipMessage: string | null = null;
  @Output() onStart = new EventEmitter<void>();
  @Output() onSkip = new EventEmitter<void>();
  showCoachTip = signal(false);
  ngOnInit(): void {
    try { this.showCoachTip.set(localStorage.getItem('onboarding.tour.dismissed') !== '1'); }
    catch { this.showCoachTip.set(true); }
  }
  dismissCoach(): void {
    this.showCoachTip.set(false);
    try { localStorage.setItem('onboarding.tour.dismissed', '1'); } catch { /* */ }
  }
  get resumeLabel(): string {
    return (this.journey?.progressPercent ?? 0) > 15 ? 'Continue setup' : 'Start setup';
  }
  get etaLabel(): string {
    const m = this.journey?.estimatedMinutesRemaining;
    return m == null ? 'about 20-30 minutes' : `about ${m} minutes remaining`;
  }
}
