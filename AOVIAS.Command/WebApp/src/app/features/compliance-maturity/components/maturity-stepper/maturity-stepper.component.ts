import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MaturityStageModel, MaturityStageStatus } from '../../models/compliance-maturity.model';

@Component({
  selector: 'app-maturity-stepper',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="maturity-stepper">
      <div class="maturity-stepper__track">
        @for (stage of stages(); track stage.stage; let i = $index; let last = $last) {
          <div class="maturity-stepper__step" [class]="'maturity-stepper__step--' + statusClass(stage.status)">
            <div class="maturity-stepper__node-wrapper">
              @if (stage.status === 'locked') {
                <div class="maturity-stepper__node maturity-stepper__node--locked" [attr.aria-label]="stage.stage + ' locked'">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                    <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
                    <path d="M7 11V7a5 5 0 0110 0v4"/>
                  </svg>
                </div>
              } @else if (stage.status === 'completed') {
                <div class="maturity-stepper__node maturity-stepper__node--completed" [attr.aria-label]="stage.stage + ' completed'">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                    <polyline points="20 6 9 17 4 12"/>
                  </svg>
                </div>
              } @else {
                <div class="maturity-stepper__node maturity-stepper__node--active" [attr.aria-label]="stage.stage + ' in progress'">
                  <div class="maturity-stepper__node-pulse"></div>
                </div>
              }
            </div>
            @if (!last) {
              <div class="maturity-stepper__connector" [class.maturity-stepper__connector--filled]="stage.status === 'completed'"></div>
            }
            <div class="maturity-stepper__label">
              <span class="maturity-stepper__stage-name">{{ stage.label }}</span>
              <span class="maturity-stepper__score">{{ (stage.progressPercent ?? stage.score) | number: '1.0-0' }}%</span>
              <span class="maturity-stepper__status-tag maturity-stepper__status-tag--{{ statusClass(stage.status) }}">
                {{ statusLabel(stage.status) }}
              </span>
            </div>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .maturity-stepper {
      background: var(--bg-primary);
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-lg, 12px);
      padding: 1.5rem 2rem;
      overflow-x: auto;
    }

    .maturity-stepper__track {
      display: flex;
      align-items: flex-start;
      min-width: 480px;
    }

    .maturity-stepper__step {
      display: flex;
      flex-direction: column;
      align-items: center;
      position: relative;
      flex: 1;
    }

    .maturity-stepper__node-wrapper {
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1;
    }

    .maturity-stepper__connector {
      position: absolute;
      top: 18px;
      left: 50%;
      right: -50%;
      height: 2px;
      background: var(--border-subtle);
      z-index: 0;
    }

    .maturity-stepper__connector--filled {
      background: var(--color-success, #16a34a);
    }

    .maturity-stepper__node {
      width: 36px;
      height: 36px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      position: relative;
    }

    .maturity-stepper__node--active {
      background: var(--color-primary);
      color: white;
      box-shadow: 0 0 0 4px rgba(91,106,249,0.2);
    }

    .maturity-stepper__node-pulse {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: white;
    }

    .maturity-stepper__node--completed {
      background: var(--color-success, #16a34a);
      color: white;
    }

    .maturity-stepper__node--locked {
      background: var(--bg-secondary);
      border: 2px solid var(--border-subtle);
      color: var(--color-text-tertiary);
    }

    .maturity-stepper__label {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.25rem;
      margin-top: 0.75rem;
      text-align: center;
    }

    .maturity-stepper__stage-name {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--color-text-primary);
    }

    .maturity-stepper__score {
      font-size: 1.25rem;
      font-weight: 700;
      color: var(--color-text-primary);
    }

    .maturity-stepper__step--locked .maturity-stepper__score {
      color: var(--color-text-tertiary);
    }

    .maturity-stepper__status-tag {
      font-size: 0.6875rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      border-radius: 999px;
      padding: 0.125rem 0.5rem;
    }

    .maturity-stepper__status-tag--completed {
      background: var(--color-success-subtle, #f0fdf4);
      color: var(--color-success, #16a34a);
    }

    .maturity-stepper__status-tag--in-progress {
      background: var(--color-primary-background);
      color: var(--color-primary);
    }

    .maturity-stepper__status-tag--available {
      background: rgba(14, 165, 233, 0.12);
      color: #0284c7;
    }

    .maturity-stepper__status-tag--locked {
      background: var(--bg-secondary);
      color: var(--color-text-tertiary);
    }
  `],
})
export class MaturityStepperComponent {
  stages = input.required<MaturityStageModel[]>();

  statusClass(status: MaturityStageStatus): string {
    switch (status) {
      case 'completed': return 'completed';
      case 'inProgress': return 'in-progress';
      case 'available': return 'available';
      case 'locked': return 'locked';
      default: return 'locked';
    }
  }

  statusLabel(status: MaturityStageStatus): string {
    switch (status) {
      case 'completed': return 'Completed';
      case 'inProgress': return 'In Progress';
      case 'available': return 'Available';
      case 'locked': return 'Locked';
      default: return status;
    }
  }
}
