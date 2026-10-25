import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  model,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

/**
 * Standard workspace search — Stage 1.3, Step 4.
 *
 * One search experience for every data surface: same placement, width,
 * affordances and keyboard behaviour, so the control behaves identically on
 * Findings, CAPA and Planning.
 *
 * Debouncing stays with the caller. Pages already own their own RxJS pipelines
 * with page-appropriate delays, and duplicating that here would mean two timers
 * racing on every keystroke.
 */
@Component({
  selector: 'app-workspace-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="search">
      <span class="search__icon" aria-hidden="true">⌕</span>
      <input
        #field
        type="search"
        class="search__input"
        [placeholder]="placeholder()"
        [attr.aria-label]="placeholder()"
        [ngModel]="value()"
        (ngModelChange)="value.set($event)"
        (keydown.escape)="clear()"
      />
      @if (value()) {
        <button type="button" class="search__clear" (click)="clear()" aria-label="Clear search">
          <span aria-hidden="true">✕</span>
        </button>
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-inline-size: 0;
      }

      .search {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        inline-size: 100%;
        max-inline-size: 22rem;
        block-size: var(--control-height-md);
        padding-inline: var(--space-3);
        border: var(--border-width-hairline) solid var(--border-color);
        border-radius: var(--radius-control);
        background: var(--card-bg);
      }

      .search:focus-within {
        border-color: var(--primary-color);
        outline: var(--focus-ring-width) solid var(--primary-color);
        outline-offset: var(--focus-ring-offset);
      }

      .search__icon {
        color: var(--text-muted);
        font-size: var(--font-size-body-sm);
        flex-shrink: 0;
      }

      .search__input {
        flex: 1;
        min-inline-size: 0;
        border: none;
        background: none;
        color: var(--text-primary);
        font-size: var(--font-size-caption);
        outline: none;
      }

      /* The native clear affordance would sit alongside ours. */
      .search__input::-webkit-search-cancel-button {
        display: none;
      }

      .search__clear {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        inline-size: 1.25rem;
        block-size: 1.25rem;
        flex-shrink: 0;
        border: none;
        border-radius: var(--radius-pill);
        background: none;
        color: var(--text-muted);
        font-size: var(--font-size-micro);
        cursor: pointer;
      }

      .search__clear:hover {
        background: var(--bg-tertiary);
        color: var(--text-primary);
      }

      .search__clear:focus-visible {
        outline: var(--focus-ring-width) solid var(--primary-color);
        outline-offset: var(--focus-ring-offset);
      }
    `,
  ],
})
export class WorkspaceSearchComponent {
  readonly value = model<string>('');
  readonly placeholder = input('Search…');

  private readonly field = viewChild<ElementRef<HTMLInputElement>>('field');

  /** Lets a host bind a keyboard shortcut without reaching into the DOM. */
  focus(): void {
    this.field()?.nativeElement.focus();
  }

  protected clear(): void {
    this.value.set('');
    this.focus();
  }
}
