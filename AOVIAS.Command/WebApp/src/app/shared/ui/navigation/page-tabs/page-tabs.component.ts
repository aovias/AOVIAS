import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NgIcon } from '@ng-icons/core';

/**
 * A single entry in a {@link PageTabsComponent} strip.
 *
 * Provide `route` for navigation tabs (the component renders anchors and lets
 * the router own the active state), or omit it for selection tabs (the
 * component renders an ARIA tablist and emits `tabSelected`).
 */
export interface PageTabItem {
  /** Stable identity — used for tracking, `activeId`, and emitted selection. */
  id: string;
  label: string;
  /** Router target. Presence of this switches the strip into navigation mode. */
  route?: string | unknown[];
  queryParams?: Record<string, unknown> | null;
  /**
   * Lucide icon name. The *host* component must register it via `provideIcons`
   * in its `viewProviders`; this component resolves icons from the injector.
   */
  icon?: string;
  /** Small count/status pill rendered after the label. */
  badge?: string | number | null;
  disabled?: boolean;
  /** Overrides the accessible name when `label` alone is not descriptive. */
  ariaLabel?: string;
}

export type PageTabsVariant = 'primary' | 'secondary' | 'section';

/**
 * UI-4 — the single page-navigation pattern for AOVIAS COMMAND.
 *
 * Replaces per-module tab implementations so height, active indicator, hover
 * animation, spacing, and typography are identical everywhere. Three variants
 * cover the cases that previously had bespoke CSS:
 *
 * - `primary`   — top-level module tabs (underline indicator on a divider rule)
 * - `secondary` — segmented control for sub-views inside a page or panel
 * - `section`   — quiet inline navigation within a card or side panel
 *
 * Two modes, chosen by the items themselves:
 *
 * - **Navigation** (items have `route`) — renders `<nav><a routerLink>`, so
 *   deep links, middle-click, and browser history all work.
 * - **Selection** (no `route`) — renders `role="tablist"` with roving tabindex,
 *   arrow/Home/End key support, and `aria-selected`; emits `tabSelected`.
 */
@Component({
  selector: 'app-page-tabs',
  standalone: true,
  imports: [NgTemplateOutlet, RouterLink, RouterLinkActive, NgIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isNavigation()) {
      <nav
        class="page-tabs"
        [class]="variantClass()"
        [class.page-tabs--inset]="inset()"
        [attr.aria-label]="ariaLabel()"
      >
        <div class="page-tabs__strip praman-scrollbar praman-scrollbar--timeline">
          @for (tab of tabs(); track tab.id) {
            <a
              class="page-tabs__tab"
              [routerLink]="tab.disabled ? null : tab.route"
              [queryParams]="tab.queryParams ?? null"
              routerLinkActive="page-tabs__tab--active"
              [routerLinkActiveOptions]="{ exact: exactMatch() }"
              [attr.aria-disabled]="tab.disabled ? 'true' : null"
              [attr.tabindex]="tab.disabled ? -1 : null"
              [attr.aria-label]="tab.ariaLabel ?? null"
              (keydown)="onKeydown($event)"
            >
              <ng-container
                [ngTemplateOutlet]="tabBody"
                [ngTemplateOutletContext]="{ $implicit: tab }"
              />
            </a>
          }
        </div>
      </nav>
    } @else {
      <div
        class="page-tabs"
        [class]="variantClass()"
        [class.page-tabs--inset]="inset()"
        role="tablist"
        [attr.aria-label]="ariaLabel()"
      >
        <div class="page-tabs__strip praman-scrollbar praman-scrollbar--timeline">
          @for (tab of tabs(); track tab.id; let i = $index) {
            <button
              type="button"
              class="page-tabs__tab"
              [class.page-tabs__tab--active]="tab.id === resolvedActiveId()"
              role="tab"
              [id]="tabElementId(tab)"
              [attr.aria-selected]="tab.id === resolvedActiveId()"
              [attr.aria-controls]="panelId() ? panelId() : null"
              [attr.aria-label]="tab.ariaLabel ?? null"
              [disabled]="tab.disabled === true"
              [tabindex]="tab.id === resolvedActiveId() ? 0 : -1"
              (click)="select(tab)"
              (keydown)="onKeydown($event, i)"
            >
              <ng-container
                [ngTemplateOutlet]="tabBody"
                [ngTemplateOutletContext]="{ $implicit: tab }"
              />
            </button>
          }
        </div>
      </div>
    }

    <ng-template #tabBody let-tab>
      @if (tab.icon) {
        <ng-icon class="page-tabs__icon" [name]="tab.icon" aria-hidden="true" />
      }
      <span class="page-tabs__label">{{ tab.label }}</span>
      @if (tab.badge !== null && tab.badge !== undefined && tab.badge !== '') {
        <span class="page-tabs__badge">{{ tab.badge }}</span>
      }
    </ng-template>
  `,
  styles: [
    `
      :host {
        display: block;
        min-inline-size: 0;
      }

      .page-tabs {
        display: flex;
        inline-size: 100%;
        min-inline-size: 0;
      }

      /*
       * Module bars span the full page width, so the divider runs edge to edge
       * while the tabs themselves start at the page padding — lining the first
       * tab up with the content below.
       */
      .page-tabs--inset .page-tabs__strip {
        padding-inline: var(--page-padding);
      }

      /*
       * The strip scrolls horizontally instead of wrapping, so tab height and
       * the baseline of the active indicator never change between pages.
       */
      .page-tabs__strip {
        display: flex;
        align-items: stretch;
        min-inline-size: 0;
        overflow-x: auto;
        overflow-y: hidden;
        scrollbar-width: thin;
      }

      .page-tabs__tab {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: var(--icon-gap);
        flex-shrink: 0;
        block-size: var(--control-height-lg);
        padding-inline: var(--space-4);
        margin: 0;
        border: 0;
        background: transparent;
        color: var(--text-secondary);
        font-family: inherit;
        font-size: var(--font-size-body-sm);
        font-weight: var(--font-weight-medium);
        line-height: 1;
        white-space: nowrap;
        text-decoration: none;
        cursor: pointer;
        transition: var(--transition-control);
      }

      .page-tabs__tab:hover:not([aria-disabled='true']):not(:disabled) {
        color: var(--text-primary);
      }

      .page-tabs__tab:focus-visible {
        outline: var(--focus-ring-width) solid var(--state-focus-ring-color);
        outline-offset: calc(var(--focus-ring-offset) * -1);
        border-radius: var(--radius-control);
      }

      .page-tabs__tab[aria-disabled='true'],
      .page-tabs__tab:disabled {
        color: var(--state-disabled-fg);
        cursor: not-allowed;
        pointer-events: none;
      }

      .page-tabs__icon {
        font-size: var(--icon-size-md);
        flex-shrink: 0;
      }

      .page-tabs__badge {
        display: inline-flex;
        align-items: center;
        padding: 0 var(--space-2);
        min-inline-size: var(--space-5);
        block-size: var(--space-5);
        border-radius: var(--radius-chip);
        background: var(--bg-tertiary);
        color: var(--text-secondary);
        font-size: var(--font-size-micro);
        font-weight: var(--font-weight-semibold);
        font-variant-numeric: var(--font-variant-numeric-tabular);
      }

      .page-tabs__tab--active .page-tabs__badge {
        background: var(--state-selected-bg);
        color: var(--state-selected-fg);
      }

      /* ---- primary: module-level tabs on a divider rule ---- */

      .page-tabs--primary {
        border-block-end: var(--border-width-hairline) solid var(--border-color);
      }

      .page-tabs--primary .page-tabs__tab {
        position: relative;
      }

      .page-tabs--primary .page-tabs__tab::after {
        content: '';
        position: absolute;
        inset-inline: var(--space-2);
        inset-block-end: 0;
        block-size: 2px;
        border-radius: var(--radius-pill) var(--radius-pill) 0 0;
        background: transparent;
        transition: background-color var(--motion-duration-base) var(--motion-easing-standard);
      }

      .page-tabs--primary .page-tabs__tab--active {
        color: var(--primary-color);
        font-weight: var(--font-weight-semibold);
      }

      .page-tabs--primary .page-tabs__tab--active::after {
        background: var(--primary-color);
      }

      .page-tabs--primary
        .page-tabs__tab:hover:not(.page-tabs__tab--active):not([aria-disabled='true']):not(
          :disabled
        )::after {
        background: var(--border-color);
      }

      /* ---- secondary: segmented control ---- */

      .page-tabs--secondary .page-tabs__strip {
        gap: var(--space-1);
        padding: var(--space-1);
        background: var(--bg-tertiary);
        border-radius: var(--radius-control);
      }

      .page-tabs--secondary .page-tabs__tab {
        block-size: var(--control-height-md);
        padding-inline: var(--space-3);
        border-radius: calc(var(--radius-control) - 2px);
      }

      .page-tabs--secondary
        .page-tabs__tab:hover:not(.page-tabs__tab--active):not([aria-disabled='true']):not(
          :disabled
        ) {
        background: var(--state-hover-bg);
      }

      .page-tabs--secondary .page-tabs__tab--active {
        background: var(--card-bg);
        color: var(--text-primary);
        font-weight: var(--font-weight-semibold);
        box-shadow: var(--elevation-card);
      }

      /* ---- section: quiet inline nav inside cards and panels ---- */

      .page-tabs--section .page-tabs__strip {
        gap: var(--space-1);
      }

      .page-tabs--section .page-tabs__tab {
        block-size: var(--control-height-sm);
        padding-inline: var(--space-3);
        border-radius: var(--radius-chip);
        font-size: var(--font-size-caption);
      }

      .page-tabs--section
        .page-tabs__tab:hover:not(.page-tabs__tab--active):not([aria-disabled='true']):not(
          :disabled
        ) {
        background: var(--state-hover-bg);
      }

      .page-tabs--section .page-tabs__tab--active {
        background: var(--state-selected-bg);
        color: var(--state-selected-fg);
        font-weight: var(--font-weight-semibold);
      }

      /* ---- responsive + accessibility ---- */

      @media (pointer: coarse) {
        .page-tabs__tab {
          min-block-size: var(--touch-target-min);
        }
      }

      @media (max-width: 767px) {
        .page-tabs__tab {
          padding-inline: var(--space-3);
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .page-tabs__tab,
        .page-tabs--primary .page-tabs__tab::after {
          transition-duration: 0.01ms;
        }
      }
    `,
  ],
})
export class PageTabsComponent {
  readonly tabs = input.required<readonly PageTabItem[]>();
  readonly variant = input<PageTabsVariant>('primary');
  readonly ariaLabel = input<string>('Page sections');

  /** Selection mode only — the currently active tab id. */
  readonly activeId = input<string | null>(null);
  /** Selection mode only — id of the element this tablist controls. */
  readonly panelId = input<string | null>(null);

  /** Navigation mode only — require an exact URL match for the active state. */
  readonly exactMatch = input<boolean>(false);

  /**
   * Pads the tab strip by the page padding while the strip itself still spans
   * the full width. Use inside `.aov-module-nav` so the divider runs edge to
   * edge but the first tab aligns with the page content beneath it.
   */
  readonly inset = input<boolean>(false);

  readonly tabSelected = output<PageTabItem>();

  protected readonly isNavigation = computed(() => this.tabs().some((tab) => !!tab.route));

  protected readonly variantClass = computed(() => `page-tabs--${this.variant()}`);

  /** Falls back to the first enabled tab so a tablist always has one tab stop. */
  protected readonly resolvedActiveId = computed(() => {
    const current = this.activeId();
    const items = this.tabs();
    if (current && items.some((tab) => tab.id === current)) {
      return current;
    }
    return items.find((tab) => !tab.disabled)?.id ?? null;
  });

  protected tabElementId(tab: PageTabItem): string {
    return `page-tab-${tab.id}`;
  }

  protected select(tab: PageTabItem): void {
    if (tab.disabled) {
      return;
    }
    this.tabSelected.emit(tab);
  }

  /**
   * Arrow keys move between tabs in both modes; in selection mode the moved-to
   * tab also becomes active, matching the ARIA automatic-activation pattern.
   */
  protected onKeydown(event: KeyboardEvent, index?: number): void {
    const horizontalKeys = ['ArrowRight', 'ArrowLeft', 'Home', 'End'];
    if (!horizontalKeys.includes(event.key)) {
      return;
    }

    const items = this.tabs();
    const enabled = items.filter((tab) => !tab.disabled);
    if (enabled.length === 0) {
      return;
    }

    const current =
      index !== undefined ? items[index] : items.find((tab) => tab.id === this.resolvedActiveId());
    const position = current ? enabled.findIndex((tab) => tab.id === current.id) : 0;

    let next: PageTabItem;
    switch (event.key) {
      case 'ArrowRight':
        next = enabled[(position + 1) % enabled.length];
        break;
      case 'ArrowLeft':
        next = enabled[(position - 1 + enabled.length) % enabled.length];
        break;
      case 'Home':
        next = enabled[0];
        break;
      default:
        next = enabled[enabled.length - 1];
        break;
    }

    event.preventDefault();

    const host = event.currentTarget as HTMLElement | null;
    const strip = host?.closest('.page-tabs__strip');
    const target =
      strip?.querySelectorAll<HTMLElement>('.page-tabs__tab')[
        items.findIndex((tab) => tab.id === next.id)
      ];
    target?.focus();

    if (!this.isNavigation()) {
      this.select(next);
    }
  }
}
