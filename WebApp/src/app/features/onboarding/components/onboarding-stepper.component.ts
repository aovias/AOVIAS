import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

interface Step {
  number: number;
  title: string;
  icon: string;
}

@Component({
  selector: 'app-onboarding-stepper',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="stepper-container">
      <div class="progress-meta" *ngIf="progressPercent !== null">
        <span>{{ progressPercent }}% complete</span>
        <span class="hint">Click a completed step to go back</span>
      </div>
      <div class="stepper">
        <div
          *ngFor="let step of steps; let i = index"
          class="step"
          [class.active]="currentStep === step.number"
          [class.completed]="currentStep > step.number"
          [class.clickable]="currentStep > step.number"
          (click)="onStepClick(step.number)"
          (keydown.enter)="onStepClick(step.number)"
          [attr.tabindex]="currentStep > step.number ? 0 : -1"
          [attr.role]="currentStep > step.number ? 'button' : null">

          <div class="step-circle">
            <span *ngIf="currentStep <= step.number" class="step-icon">{{ step.icon }}</span>
            <svg *ngIf="currentStep > step.number" width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path d="M13.5 4L6 11.5L2.5 8" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </div>

          <div class="step-label">
            <span class="step-number">Step {{ step.number }}{{ step.number === 5 ? ' (optional)' : '' }}</span>
            <span class="step-title">{{ step.title }}</span>
          </div>

          <div *ngIf="i < steps.length - 1" class="step-connector"></div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .stepper-container {
      padding: 24px 32px;
      background: var(--color-surface, #ffffff);
      border-bottom: 1px solid var(--color-border, #e5e7eb);
    }

    .progress-meta {
      display: flex;
      justify-content: space-between;
      max-width: 1000px;
      margin: 0 auto 12px;
      font-size: 13px;
      color: var(--color-text-secondary, #6b7280);
    }

    .step.clickable { cursor: pointer; }
    .step.clickable:hover .step-circle {
      border-color: var(--color-primary, #0b5fff);
    }

    [data-theme="dark"] .stepper-container {
      background: var(--color-surface, #1f2937);
      border-bottom-color: var(--color-border, #374151);
    }

    .stepper {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      max-width: 1000px;
      margin: 0 auto;
    }

    .step {
      position: relative;
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
    }

    .step-circle {
      width: 52px;
      height: 52px;
      border-radius: 50%;
      background: var(--card-bg, #ffffff);
      border: 3px solid var(--border-color, #e5e7eb);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 22px;
      transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
      position: relative;
      z-index: 2;
      box-shadow: var(--shadow-sm, 0 1px 2px rgba(0, 0, 0, 0.05));
    }

    [data-theme="dark"] .step-circle {
      background: var(--bg-tertiary, #1e293b);
      border-color: var(--border-color, rgba(255, 255, 255, 0.1));
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
    }

    .step.active .step-circle {
      background: var(--primary-color, var(--color-primary));
      border-color: var(--primary-color, var(--color-primary));
      box-shadow: 0 0 0 5px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 15%, transparent)), 0 4px 12px color-mix(in srgb, var(--color-primary) 30%, transparent);
      animation: pulse 2.5s ease-in-out infinite;
      transform: scale(1.05);
    }

    [data-theme="dark"] .step.active .step-circle {
      background: var(--primary-color, var(--color-primary));
      border-color: var(--primary-color, var(--color-primary));
      box-shadow: 0 0 0 5px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 15%, transparent)), 0 4px 12px color-mix(in srgb, var(--color-primary) 30%, transparent);
    }

    .step.completed .step-circle {
      background: var(--success-color, #10b981);
      border-color: var(--success-color, #10b981);
      box-shadow: 0 2px 8px rgba(16, 185, 129, 0.3);
    }

    [data-theme="dark"] .step.completed .step-circle {
      background: var(--success-color, #34d399);
      border-color: var(--success-color, #34d399);
      box-shadow: 0 2px 8px rgba(52, 211, 153, 0.3);
    }

    .step.active .step-icon,
    .step.completed .step-icon {
      filter: brightness(0) invert(1);
    }

    @keyframes pulse {
      0%, 100% {
        box-shadow: 0 0 0 5px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 15%, transparent)), 0 4px 12px color-mix(in srgb, var(--color-primary) 30%, transparent);
      }
      50% {
        box-shadow: 0 0 0 8px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 8%, transparent)), 0 6px 16px color-mix(in srgb, var(--color-primary) 20%, transparent);
      }
    }

    [data-theme="dark"] .step.active .step-circle {
      animation: pulseDark 2.5s ease-in-out infinite;
    }

    @keyframes pulseDark {
      0%, 100% {
        box-shadow: 0 0 0 5px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 15%, transparent)), 0 4px 12px color-mix(in srgb, var(--color-primary) 30%, transparent);
      }
      50% {
        box-shadow: 0 0 0 8px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 8%, transparent)), 0 6px 16px color-mix(in srgb, var(--color-primary) 20%, transparent);
      }
    }

    .step-label {
      margin-top: 12px;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .step-number {
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: var(--color-text-secondary, #9ca3af);
    }

    .step.active .step-number {
      color: var(--color-primary);
    }

    .step-title {
      font-size: 13px;
      font-weight: 500;
      color: var(--color-text-secondary, #6b7280);
    }

    [data-theme="dark"] .step-title {
      color: var(--color-text-secondary, #9ca3af);
    }

    .step.active .step-title {
      color: var(--color-text-primary, #111827);
      font-weight: 600;
    }

    [data-theme="dark"] .step.active .step-title {
      color: var(--color-text-primary, #f9fafb);
    }

    .step-connector {
      position: absolute;
      top: 26px;
      left: calc(50% + 26px);
      right: calc(-50% + 26px);
      height: 3px;
      background: var(--border-color, #e5e7eb);
      z-index: 1;
      border-radius: 2px;
    }

    [data-theme="dark"] .step-connector {
      background: var(--border-color, rgba(255, 255, 255, 0.08));
    }

    .step.completed .step-connector {
      background: var(--success-color, #10b981);
      box-shadow: 0 0 8px rgba(16, 185, 129, 0.3);
    }

    [data-theme="dark"] .step.completed .step-connector {
      background: var(--success-color, #34d399);
      box-shadow: 0 0 8px rgba(52, 211, 153, 0.3);
    }

    @media (max-width: 1024px) {
      .step-title {
        display: none;
      }

      .step-number {
        font-size: 10px;
      }

      .step-circle {
        width: 40px;
        height: 40px;
        font-size: 16px;
      }
    }

    @media (max-width: 640px) {
      .stepper {
        overflow-x: auto;
        justify-content: flex-start;
        padding-bottom: 8px;
      }

      .step {
        flex: 0 0 80px;
      }

      .step-connector {
        left: calc(50% + 20px);
        right: calc(-50% + 20px);
      }
    }
  `]
})
export class OnboardingStepperComponent {
  @Input() currentStep = 1;
  @Input() progressPercent: number | null = null;
  @Output() stepSelect = new EventEmitter<number>();

  steps: Step[] = [
    { number: 1, title: 'Welcome', icon: '👋' },
    { number: 2, title: 'Hospital Profile', icon: '🏥' },
    { number: 3, title: 'Framework Selection', icon: '📋' },
    { number: 4, title: 'AI Assessment', icon: '🤖' },
    { number: 5, title: 'Team Invitation', icon: '👥' },
    { number: 6, title: 'Completion', icon: '✓' }
  ];

  onStepClick(stepNumber: number): void {
    if (stepNumber < this.currentStep) {
      this.stepSelect.emit(stepNumber);
    }
  }
}
