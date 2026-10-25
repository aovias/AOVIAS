import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-search-trigger',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="search-trigger"
      [class.search-trigger--icon]="iconOnly()"
      (click)="activate.emit()"
      aria-label="Search"
      title="Search (Ctrl/⌘K)"
    >
      <svg
        width="18"
        height="18"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        aria-hidden="true"
      >
        <circle cx="11" cy="11" r="7" stroke-width="2" />
        <path d="M20 20l-3.5-3.5" stroke-width="2" stroke-linecap="round" />
      </svg>
      @if (!iconOnly()) {
        <span class="search-trigger__label">Search</span>
        <kbd class="search-trigger__kbd">{{ shortcutLabel }}</kbd>
      }
    </button>
  `,
  styles: `
    :host {
      display: inline-flex;
    }

    .search-trigger {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      height: var(--header-control-height, 36px);
      padding: 0 12px;
      border: 1px solid var(--color-border-subtle, var(--border-color));
      border-radius: 8px;
      background: var(--color-bg-page, transparent);
      color: var(--color-text-secondary, var(--text-secondary));
      cursor: pointer;
      font-size: 13px;
    }

    .search-trigger--icon {
      width: var(--header-control-height, 36px);
      padding: 0;
      justify-content: center;
    }

    .search-trigger:hover {
      background: var(--color-bg-hover, var(--hover-bg));
      color: var(--color-text-primary, var(--text-primary));
    }

    .search-trigger:focus-visible {
      outline: 2px solid var(--color-brand-primary, var(--primary-color));
      outline-offset: 2px;
    }

    .search-trigger__label {
      font-weight: 500;
    }

    .search-trigger__kbd {
      font-family: inherit;
      font-size: 11px;
      font-weight: 600;
      padding: 2px 6px;
      border-radius: 4px;
      border: 1px solid var(--color-border-subtle, var(--border-color));
      background: var(--card-bg, #fff);
      color: var(--color-text-muted, var(--text-secondary));
    }
  `,
})
export class SearchTriggerComponent {
  readonly iconOnly = input(false);
  readonly activate = output<void>();

  readonly shortcutLabel =
    typeof navigator !== 'undefined' && /Mac|iPhone|iPad/i.test(navigator.platform)
      ? '⌘K'
      : 'Ctrl K';
}
