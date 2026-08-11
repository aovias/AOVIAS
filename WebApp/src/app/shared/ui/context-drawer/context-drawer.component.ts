import { NgComponentOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Type,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { ContextDrawerService } from './context-drawer.service';

/**
 * Enterprise Context Drawer — Stage 1.2.
 *
 * The platform's standard contextual surface. It replaces the permanent right
 * column that reserved 18–22% of the workspace whether or not anything was in
 * it; here the workspace keeps its full width and context arrives on request.
 *
 * Generic by construction. It renders whatever sections the host workspace
 * registered with {@link ContextDrawerService} and knows nothing about Audit,
 * Risk or any other module (Step 12).
 *
 * Two presentation modes, both from one component:
 *  - **pinned** — participates in layout and pushes the workspace, for wide
 *    screens where the user has explicitly opted in.
 *  - **overlay** — floats above the workspace with a scrim, the default
 *    everywhere else. Overlay is what keeps the laptop case honest: the
 *    workspace never loses width just because context exists.
 *
 * Section components are resolved by dynamic import the first time they are
 * shown and cached after, so an unopened drawer costs nothing at page load.
 */
@Component({
  selector: 'app-context-drawer',
  standalone: true,
  imports: [NgComponentOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (service.hasSections()) {
      <!--
        The scrim only exists in overlay mode. A pinned drawer sits in the
        layout, so dimming the workspace behind it would be wrong.
      -->
      @if (isOpen() && !pinned()) {
        <div class="scrim" (click)="service.close()" aria-hidden="true"></div>
      }

      <aside
        #panel
        class="drawer"
        [class.is-open]="isOpen()"
        [class.is-pinned]="pinned()"
        role="complementary"
        [attr.aria-label]="'Context: ' + (active()?.label ?? 'workspace')"
        [attr.aria-hidden]="isOpen() ? null : 'true'"
        [attr.inert]="isOpen() ? null : ''"
        (keydown)="onKeydown($event)"
      >
        <header class="drawer-header">
          <h2 class="drawer-title">{{ active()?.label }}</h2>
          <div class="header-actions">
            <button
              type="button"
              class="icon-btn"
              [class.is-active]="pinned()"
              (click)="service.togglePinned()"
              [attr.aria-pressed]="pinned()"
              [title]="pinned() ? 'Unpin context' : 'Pin context open'"
            >
              <span aria-hidden="true">{{ pinned() ? '📌' : '📍' }}</span>
              <span class="sr-only">{{ pinned() ? 'Unpin context' : 'Pin context open' }}</span>
            </button>
            <button type="button" class="icon-btn" (click)="close()" title="Close context">
              <span aria-hidden="true">✕</span>
              <span class="sr-only">Close context</span>
            </button>
          </div>
        </header>

        @if (service.sections().length > 1) {
          <div class="section-rail" role="tablist" [attr.aria-label]="'Context sections'">
            @for (section of service.sections(); track section.id) {
              <button
                type="button"
                role="tab"
                class="rail-tab"
                [class.is-active]="section.id === active()?.id"
                [attr.aria-selected]="section.id === active()?.id"
                (click)="service.select(section.id)"
              >
                <span class="rail-glyph" aria-hidden="true">{{ section.glyph }}</span>
                <span class="rail-label">{{ section.label }}</span>
                @if (section.badge) {
                  <span class="rail-badge">{{ section.badge }}</span>
                }
              </button>
            }
          </div>
        }

        <div class="drawer-body" role="tabpanel">
          @if (loadedComponent(); as cmp) {
            <ng-container *ngComponentOutlet="cmp" />
          } @else if (loading()) {
            <p class="muted">Loading…</p>
          } @else {
            <p class="muted">Nothing to show for this view.</p>
          }
        </div>
      </aside>
    }
  `,
  styles: [
    `
      :host {
        display: contents;
      }

      .scrim {
        position: fixed;
        inset: 0;
        background: rgb(0 0 0 / 32%);
        z-index: 40;
      }

      .drawer {
        display: flex;
        flex-direction: column;
        inline-size: min(23rem, 100vw);
        background: var(--card-bg);
        border-inline-start: var(--border-width-hairline) solid var(--border-color);
        overflow: hidden;
      }

      /*
       * Overlay is the default. Fixed to the viewport's inline end so it never
       * participates in the workspace's width calculation.
       */
      .drawer:not(.is-pinned) {
        position: fixed;
        inset-block: 0;
        inset-inline-end: 0;
        z-index: 41;
        box-shadow: var(--elevation-card);
        transform: translateX(100%);
        visibility: hidden;
        transition:
          transform var(--motion-duration-base) var(--motion-easing-standard),
          visibility var(--motion-duration-base) var(--motion-easing-standard);
      }

      .drawer:not(.is-pinned).is-open {
        transform: translateX(0);
        visibility: visible;
      }

      /* Pinned takes part in the flex row and pushes the workspace instead. */
      .drawer.is-pinned {
        position: relative;
        flex-shrink: 0;
      }

      .drawer-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-2);
        padding: var(--space-2) var(--space-3);
        border-block-end: var(--border-width-hairline) solid var(--border-color);
        min-block-size: var(--control-height-lg);
        flex-shrink: 0;
      }

      .drawer-title {
        margin: 0;
        font-size: var(--font-size-body-sm);
        font-weight: var(--font-weight-semibold);
        color: var(--text-primary);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .header-actions {
        display: flex;
        gap: var(--space-1);
        flex-shrink: 0;
      }

      .icon-btn {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        inline-size: var(--control-height-sm);
        block-size: var(--control-height-sm);
        border: none;
        border-radius: var(--radius-control);
        background: none;
        color: var(--text-secondary);
        cursor: pointer;
        font-size: var(--font-size-caption);
      }

      .icon-btn:hover {
        background: var(--bg-tertiary);
        color: var(--text-primary);
      }

      .icon-btn.is-active {
        background: var(--bg-tertiary);
        color: var(--primary-color);
      }

      .icon-btn:focus-visible,
      .rail-tab:focus-visible {
        outline: var(--focus-ring-width) solid var(--primary-color);
        outline-offset: var(--focus-ring-offset);
      }

      .section-rail {
        display: flex;
        gap: var(--space-1);
        padding: var(--space-2) var(--space-2) 0;
        overflow-x: auto;
        flex-shrink: 0;
      }

      .rail-tab {
        display: inline-flex;
        align-items: center;
        gap: var(--space-1);
        /* Slightly roomier than micro text: these are primary navigation for the drawer. */
        padding: var(--space-1) var(--space-3);
        border: none;
        border-radius: var(--radius-chip);
        background: none;
        color: var(--text-muted);
        font-size: var(--font-size-body-sm);
        font-weight: var(--font-weight-medium);
        white-space: nowrap;
        cursor: pointer;
      }

      .rail-tab:hover {
        background: var(--bg-tertiary);
        color: var(--text-primary);
      }

      .rail-tab.is-active {
        background: var(--bg-tertiary);
        color: var(--text-primary);
        font-weight: var(--font-weight-semibold);
      }

      .rail-glyph {
        font-size: var(--icon-size-md);
        line-height: 1;
      }

      .rail-badge {
        font-size: var(--font-size-caption);
        font-variant-numeric: var(--font-variant-numeric-tabular);
        color: var(--text-muted);
      }

      .drawer-body {
        flex: 1;
        min-block-size: 0;
        overflow-y: auto;
      }

      .muted {
        margin: 0;
        padding: var(--space-3);
        color: var(--text-muted);
        font-size: var(--font-size-caption);
      }

      .sr-only {
        position: absolute;
        inline-size: 1px;
        block-size: 1px;
        overflow: hidden;
        clip-path: inset(50%);
        white-space: nowrap;
      }

      /* Tablet and below: the drawer takes the full viewport width. */
      @media (max-width: 900px) {
        .drawer,
        .drawer.is-pinned {
          position: fixed;
          inset: 0;
          inline-size: 100vw;
          z-index: 41;
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .drawer:not(.is-pinned) {
          transition: none;
        }
      }
    `,
  ],
})
export class ContextDrawerComponent {
  protected readonly service = inject(ContextDrawerService);

  private readonly panelRef = viewChild<ElementRef<HTMLElement>>('panel');

  protected readonly isOpen = this.service.isOpen;
  protected readonly pinned = this.service.pinned;
  protected readonly active = this.service.activeSection;

  protected readonly loading = signal(false);

  /** Resolved section components, keyed by section id, so each loads once. */
  private readonly cache = new Map<string, Type<unknown>>();
  private readonly resolved = signal<ReadonlyMap<string, Type<unknown>>>(new Map());

  protected readonly loadedComponent = computed(() => {
    const section = this.active();
    if (!section) return null;
    return this.resolved().get(section.id) ?? null;
  });

  /** Element focused before opening, restored on close. */
  private previouslyFocused: HTMLElement | null = null;

  constructor() {
    // Fetch the active section's component the first time it is needed.
    effect(() => {
      const section = this.active();
      if (!section || !this.isOpen()) return;
      if (this.cache.has(section.id)) return;

      this.loading.set(true);
      void section
        .load()
        .then((cmp) => {
          this.cache.set(section.id, cmp);
          this.resolved.set(new Map(this.cache));
        })
        .finally(() => this.loading.set(false));
    });

    // Move focus into the drawer on open and hand it back on close, so keyboard
    // users are not dropped at the top of the document.
    effect(() => {
      const open = this.isOpen();
      if (open) {
        this.previouslyFocused = document.activeElement as HTMLElement | null;
        queueMicrotask(() => this.focusFirst());
      } else if (this.previouslyFocused) {
        this.previouslyFocused.focus?.();
        this.previouslyFocused = null;
      }
    });
  }

  protected close(): void {
    // Closing from inside a pinned drawer means "stop pinning" — otherwise the
    // close button would appear to do nothing.
    if (this.pinned()) this.service.setPinned(false);
    this.service.close();
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.stopPropagation();
      this.close();
      return;
    }

    // Trap Tab inside the overlay drawer. A pinned drawer is part of the page,
    // so tabbing out of it into the workspace is correct there.
    if (event.key !== 'Tab' || this.pinned()) return;

    const focusable = this.focusableElements();
    if (focusable.length === 0) return;

    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    const activeEl = document.activeElement;

    if (event.shiftKey && activeEl === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && activeEl === last) {
      event.preventDefault();
      first.focus();
    }
  }

  private focusFirst(): void {
    this.focusableElements()[0]?.focus();
  }

  private focusableElements(): HTMLElement[] {
    const root = this.panelRef()?.nativeElement;
    if (!root) return [];
    return Array.from(
      root.querySelectorAll<HTMLElement>(
        'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
      ),
    ).filter((el) => el.offsetParent !== null || el === document.activeElement);
  }
}
