import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-sidebar-collapse-toggle',
  standalone: true,
  imports: [MatTooltipModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="collapse-toggle"
      (click)="toggle.emit()"
      [attr.aria-pressed]="collapsed()"
      [attr.aria-label]="collapsed() ? 'Expand sidebar' : 'Collapse sidebar'"
      [matTooltip]="collapsed() ? 'Expand sidebar' : 'Collapse sidebar'"
      matTooltipPosition="right"
      [matTooltipDisabled]="!collapsed()"
    >
      <svg
        width="16"
        height="16"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        aria-hidden="true"
      >
        @if (collapsed()) {
          <path d="M9 18l6-6-6-6" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
        } @else {
          <path
            d="M15 18l-6-6 6-6"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round"
          />
        }
      </svg>
    </button>
  `,
  styles: `
    .collapse-toggle {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      margin: 0;
      padding: 0;
      border: 1px solid var(--color-border-subtle);
      border-radius: 6px;
      background: transparent;
      color: var(--color-text-secondary);
      cursor: pointer;
      transition:
        background var(--sidebar-motion-duration, 180ms) ease,
        color var(--sidebar-motion-duration, 180ms) ease,
        border-color var(--sidebar-motion-duration, 180ms) ease;
    }

    .collapse-toggle:hover {
      background: var(--color-bg-hover);
      color: var(--color-text-primary);
    }

    .collapse-toggle:focus-visible {
      outline: 2px solid var(--color-brand-primary);
      outline-offset: 2px;
    }
  `,
})
export class CollapseToggleComponent {
  readonly collapsed = input(false);
  readonly toggle = output<void>();
}
