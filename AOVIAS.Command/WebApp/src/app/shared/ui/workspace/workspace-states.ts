import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Workspace state primitives — Stage 1.3.
 *
 * Audit shipped twelve different loading strings and fourteen different empty
 * messages, every one of them bare text with no icon, no explanation of what
 * to do next, and no skeleton. These three components are the standard
 * replacements.
 *
 * Platform-level: loading, empty and error are universal states, and every
 * future workspace should express them identically (Step 15).
 */

/**
 * Layout-stable loading placeholder.
 *
 * Renders the *shape* of the content that is coming rather than a spinner, so
 * the page does not reflow when data lands — Step 8's "loading should preserve
 * layout stability". A spinner tells the user to wait; a skeleton tells them
 * what they are waiting for.
 */
@Component({
  selector: 'app-workspace-skeleton',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="skeleton" [attr.aria-busy]="true" [attr.aria-label]="label()" role="status">
      @if (variant() === 'table') {
        <div class="sk-row sk-head"></div>
        @for (row of rows(); track row) {
          <div class="sk-row"></div>
        }
      } @else if (variant() === 'cards') {
        <div class="sk-cards">
          @for (row of rows(); track row) {
            <div class="sk-card"></div>
          }
        </div>
      } @else {
        @for (row of rows(); track row) {
          <div class="sk-line" [style.inline-size.%]="row === 1 ? 45 : 100"></div>
        }
      }
      <span class="sr-only">{{ label() }}</span>
    </div>
  `,
  styleUrl: './workspace-skeleton.component.scss',
})
export class WorkspaceSkeletonComponent {
  readonly variant = input<'table' | 'cards' | 'text'>('table');
  readonly count = input(4);
  /** Announced to screen readers, e.g. "Loading findings". */
  readonly label = input('Loading');

  protected rows(): number[] {
    return Array.from({ length: this.count() }, (_, i) => i + 1);
  }
}

/**
 * Standard empty state.
 *
 * Every empty state carries an icon, an explanation and — where one exists — the
 * action that resolves it. An empty table with no guidance leaves the user
 * unsure whether the view is broken, filtered, or genuinely empty, which is the
 * exact ambiguity Step 9 targets.
 */
@Component({
  selector: 'app-workspace-empty-state',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="empty">
      <span class="empty__icon" aria-hidden="true">{{ icon() }}</span>
      <p class="empty__title">{{ title() }}</p>
      @if (description()) {
        <p class="empty__desc">{{ description() }}</p>
      }
      @if (actionLabel()) {
        <button type="button" class="empty__action" (click)="action.emit()">
          {{ actionLabel() }}
        </button>
      }
    </div>
  `,
  styleUrl: './workspace-empty-state.component.scss',
})
export class WorkspaceEmptyStateComponent {
  readonly title = input.required<string>();
  readonly description = input<string | null>(null);
  readonly icon = input('◔');
  readonly actionLabel = input<string | null>(null);
  readonly action = output<void>();
}

/**
 * Standard error state with recovery.
 *
 * Always pairs the message with a retry, and never surfaces a raw exception —
 * Step 10. Pages pass a sentence the user can act on; diagnostics belong in the
 * console, not the interface.
 */
@Component({
  selector: 'app-workspace-error-state',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="err" role="alert">
      <span class="err__icon" aria-hidden="true">!</span>
      <div class="err__body">
        <p class="err__msg">{{ message() }}</p>
        @if (hint()) {
          <p class="err__hint">{{ hint() }}</p>
        }
      </div>
      @if (retryable()) {
        <button type="button" class="err__retry" (click)="retry.emit()">Try again</button>
      }
    </div>
  `,
  styleUrl: './workspace-error-state.component.scss',
})
export class WorkspaceErrorStateComponent {
  readonly message = input.required<string>();
  /** Optional recovery guidance — what the user can do about it. */
  readonly hint = input<string | null>(null);
  readonly retryable = input(true);
  readonly retry = output<void>();
}
