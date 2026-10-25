import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OperationalMode } from '../../models/nabh.enums';
import { JourneyWorkspaceStore } from '../../state/journey-workspace.store';

const MODES: { id: OperationalMode; label: string }[] = [
  { id: OperationalMode.SelfAssessment, label: 'Self Assessment' },
  { id: OperationalMode.InternalAudit, label: 'Internal Audit' },
  { id: OperationalMode.FinalAudit, label: 'Final Audit' },
  { id: OperationalMode.Surveillance, label: 'Surveillance' },
];

/**
 * Final / Surveillance modes may restrict visible OEs (Core+Commitment, Core-only) via store filters.
 * Edit vs read-only: `store.readOnly()` — wire forms in child components.
 */
@Component({
  selector: 'app-lifecycle-mode-switch',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="lifecycle" role="radiogroup" aria-label="Audit lifecycle mode">
      @for (m of modes; track m.id) {
        <button
          type="button"
          class="mode-btn"
          [class.active]="store.lifecycleMode() === m.id"
          [attr.aria-checked]="store.lifecycleMode() === m.id"
          role="radio"
          (click)="store.setLifecycleMode(m.id)"
        >
          {{ m.label }}
        </button>
      }
    </div>
    @if (store.readOnly()) {
      <p class="hint">Read-only mode for this lifecycle stage.</p>
    }
  `,
  styles: `
    :host {
      display: block;
    }
    .lifecycle {
      display: flex;
      flex-wrap: wrap;
      gap: var(--space-2);
    }
    .mode-btn {
      border: var(--border-width-hairline) solid var(--border-color);
      background: var(--card-bg);
      color: var(--text-secondary);
      padding: var(--space-2) 0.875rem;
      border-radius: var(--radius-md);
      font-size: var(--font-size-caption);
      font-weight: var(--font-weight-semibold);
      cursor: pointer;
      font-family: inherit;
      min-height: var(--button-height);
    }
    .mode-btn.active {
      border-color: var(--primary-color);
      color: var(--primary-color);
      box-shadow: var(--shadow-focus-ring-primary);
    }
    .hint {
      margin: var(--space-2) 0 0;
      font-size: var(--font-size-caption);
      color: var(--text-secondary);
    }
  `,
})
export class LifecycleModeSwitchComponent {
  readonly store = inject(JourneyWorkspaceStore);
  readonly modes = MODES;
}
