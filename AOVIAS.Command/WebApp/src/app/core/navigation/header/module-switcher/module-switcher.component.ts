import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OverlayModule } from '@angular/cdk/overlay';
import { A11yModule } from '@angular/cdk/a11y';
import { TrustHtmlPipe } from '../../../layout/trust-html.pipe';
import { RouterLink } from '@angular/router';
import { FavoritesProvider } from '../../surfaces/favorites.provider';
import { PinnedService } from '../../favorites/pinned.service';
import { NavigationHistoryService } from '../../engine/navigation-history.service';
import { ModuleSwitcherService } from './module-switcher.service';
import type { ModuleEntry } from './module-switcher.models';

@Component({
  selector: 'app-module-switcher',
  standalone: true,
  imports: [CommonModule, FormsModule, OverlayModule, A11yModule, TrustHtmlPipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="module-switcher__trigger"
      cdkOverlayOrigin
      #triggerOrigin="cdkOverlayOrigin"
      (click)="toggle()"
      [attr.aria-expanded]="open()"
      aria-haspopup="listbox"
      aria-controls="module-switcher-panel"
      [attr.aria-label]="'Module: ' + (active()?.title || 'Select module')"
    >
      @if (active()?.icon; as icon) {
        <span
          class="module-switcher__icon"
          [innerHTML]="icon | trustHtml"
          aria-hidden="true"
        ></span>
      }
      <span class="module-switcher__label">{{ active()?.title || 'Modules' }}</span>
      <svg
        class="module-switcher__chevron"
        [class.module-switcher__chevron--open]="open()"
        width="14"
        height="14"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        aria-hidden="true"
      >
        <path d="M6 9l6 6 6-6" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
    </button>

    <ng-template
      cdkConnectedOverlay
      [cdkConnectedOverlayOrigin]="triggerOrigin"
      [cdkConnectedOverlayOpen]="open()"
      [cdkConnectedOverlayHasBackdrop]="true"
      cdkConnectedOverlayBackdropClass="module-switcher-backdrop"
      [cdkConnectedOverlayPositions]="positions"
      (backdropClick)="close()"
      (detach)="close()"
    >
      <div
        id="module-switcher-panel"
        class="module-switcher__panel"
        role="dialog"
        aria-label="Switch module"
        cdkTrapFocus
        [cdkTrapFocusAutoCapture]="true"
      >
        <label class="sr-only" for="module-switcher-search">Search modules</label>
        <input
          #searchInput
          id="module-switcher-search"
          class="module-switcher__search"
          type="search"
          placeholder="Search modules…"
          [ngModel]="query()"
          (ngModelChange)="onQuery($event)"
          (keydown)="onSearchKeydown($event)"
          autocomplete="off"
        />

        <div
          class="module-switcher__list"
          role="listbox"
          [attr.aria-activedescendant]="activeOptionId()"
        >
          @if (!filtered().length) {
            <p class="module-switcher__empty">No modules match</p>
          } @else {
            @for (mod of filtered(); track mod.id; let i = $index) {
              <button
                type="button"
                class="module-switcher__option"
                role="option"
                [id]="'module-opt-' + mod.id"
                [class.module-switcher__option--active]="i === highlight()"
                [class.module-switcher__option--current]="mod.id === active()?.id"
                [attr.aria-selected]="i === highlight()"
                [disabled]="mod.disabled"
                (click)="select(mod)"
                (mouseenter)="highlight.set(i)"
              >
                <span
                  class="module-switcher__option-icon"
                  [innerHTML]="mod.icon | trustHtml"
                  aria-hidden="true"
                ></span>
                <span class="module-switcher__option-text">
                  <span class="module-switcher__option-title">{{ mod.title }}</span>
                  <span class="module-switcher__option-meta">{{ mod.items.length }} pages</span>
                </span>
              </button>
            }
          }
        </div>

        <div class="module-switcher__footer">
          @if (pinned.pinned().length) {
            <div class="module-switcher__chips">
              <span class="module-switcher__chips-label">Pinned</span>
              @for (p of pinned.pinned().slice(0, 4); track p.route) {
                <a class="module-switcher__chip" [routerLink]="p.route" (click)="close()">{{
                  p.label
                }}</a>
              }
            </div>
          }
          @if (favorites.favorites().length) {
            <div class="module-switcher__chips">
              <span class="module-switcher__chips-label">Favorites</span>
              @for (f of favorites.favorites().slice(0, 4); track f.route) {
                <a class="module-switcher__chip" [routerLink]="f.route" (click)="close()">{{
                  f.label
                }}</a>
              }
            </div>
          }
        </div>
      </div>
    </ng-template>
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      min-width: 0;
    }

    .module-switcher__trigger {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      height: var(--header-control-height, 36px);
      padding: 0 10px;
      border: 1px solid var(--color-border-subtle, var(--border-color));
      border-radius: 8px;
      background: transparent;
      color: var(--color-text-primary, var(--text-primary));
      font-size: 13px;
      font-weight: 600;
      cursor: pointer;
      max-width: 220px;
      min-width: 0;
      transition:
        background var(--sidebar-motion-duration, 180ms) ease,
        border-color var(--sidebar-motion-duration, 180ms) ease;
    }

    .module-switcher__trigger:hover {
      background: var(--color-bg-hover, var(--hover-bg));
    }

    .module-switcher__trigger:focus-visible {
      outline: 2px solid var(--color-brand-primary, var(--primary-color));
      outline-offset: 2px;
    }

    .module-switcher__icon {
      width: var(--header-icon-size, 18px);
      height: var(--header-icon-size, 18px);
      flex-shrink: 0;
      display: inline-flex;
      color: var(--color-brand-primary, var(--primary-color));
    }

    :host ::ng-deep .module-switcher__icon svg {
      width: var(--header-icon-size, 18px) !important;
      height: var(--header-icon-size, 18px) !important;
      display: block;
    }

    .module-switcher__label {
      min-width: 0;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .module-switcher__chevron {
      flex-shrink: 0;
      opacity: 0.7;
      transition: transform var(--sidebar-motion-duration, 180ms) ease;
    }

    .module-switcher__chevron--open {
      transform: rotate(180deg);
    }

    .module-switcher__panel {
      width: min(360px, calc(100vw - 24px));
      max-height: min(420px, 70vh);
      display: flex;
      flex-direction: column;
      background: var(--card-bg, var(--color-bg-sidebar, #fff));
      border: 1px solid var(--color-border-subtle, var(--border-color));
      border-radius: 12px;
      box-shadow: 0 12px 40px rgba(0, 0, 0, 0.16);
      overflow: hidden;
      z-index: var(--z-dropdown, 200);
    }

    .module-switcher__search {
      margin: 10px 10px 6px;
      height: var(--header-control-height, 36px);
      padding: 0 12px;
      border: 1px solid var(--color-border-subtle, var(--border-color));
      border-radius: 8px;
      background: var(--color-bg-page, var(--bg-secondary, #f7f9fc));
      color: inherit;
      font-size: 13px;
    }

    .module-switcher__search:focus {
      outline: 2px solid var(--color-brand-primary, var(--primary-color));
      outline-offset: 1px;
    }

    .module-switcher__list {
      flex: 1;
      overflow-y: auto;
      padding: 4px 6px 10px;
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .module-switcher__empty {
      margin: 12px;
      font-size: 13px;
      color: var(--color-text-muted, var(--text-secondary));
    }

    .module-switcher__option {
      display: flex;
      align-items: center;
      gap: 10px;
      width: 100%;
      min-height: 40px;
      padding: 6px 10px;
      border: none;
      border-radius: 8px;
      background: transparent;
      color: inherit;
      text-align: left;
      cursor: pointer;
    }

    .module-switcher__option:hover,
    .module-switcher__option--active {
      background: var(--color-bg-hover, var(--hover-bg));
    }

    .module-switcher__option--current {
      color: var(--color-brand-primary, var(--primary-color));
      font-weight: 600;
    }

    .module-switcher__option:disabled {
      opacity: 0.45;
      cursor: not-allowed;
    }

    .module-switcher__option:focus-visible {
      outline: 2px solid var(--color-brand-primary, var(--primary-color));
      outline-offset: -2px;
    }

    .module-switcher__option-icon {
      width: 18px;
      height: 18px;
      flex-shrink: 0;
      display: inline-flex;
    }

    :host ::ng-deep .module-switcher__option-icon svg {
      width: 18px !important;
      height: 18px !important;
      display: block;
    }

    .module-switcher__option-text {
      display: flex;
      flex-direction: column;
      min-width: 0;
      gap: 1px;
    }

    .module-switcher__option-title {
      font-size: 13px;
      font-weight: 600;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .module-switcher__option-meta {
      font-size: 11px;
      color: var(--color-text-muted, var(--text-secondary));
    }

    .module-switcher__footer {
      min-height: 0;
      padding: 8px 10px 10px;
      border-top: 1px solid var(--color-border-subtle, var(--border-color));
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .module-switcher__chips {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      align-items: center;
    }

    .module-switcher__chips-label {
      font-size: 10px;
      font-weight: 700;
      letter-spacing: 0.05em;
      text-transform: uppercase;
      color: var(--color-text-muted, var(--text-secondary));
      width: 100%;
    }

    .module-switcher__chip {
      font-size: 11px;
      font-weight: 600;
      padding: 3px 8px;
      border-radius: 999px;
      border: 1px solid var(--color-border-subtle, var(--border-color));
      color: var(--color-text-primary, var(--text-primary));
      text-decoration: none;
    }

    .module-switcher__chip:hover {
      background: var(--color-bg-hover, var(--hover-bg));
    }

    .sr-only {
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      border: 0;
    }

    @media (prefers-reduced-motion: reduce) {
      .module-switcher__chevron,
      .module-switcher__trigger {
        transition: none;
      }
    }
  `,
})
export class ModuleSwitcherComponent {
  readonly moduleService = inject(ModuleSwitcherService);
  readonly favorites = inject(FavoritesProvider);
  readonly pinned = inject(PinnedService);
  private readonly history = inject(NavigationHistoryService);

  readonly open = signal(false);
  readonly query = signal('');
  readonly highlight = signal(0);
  readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  readonly active = this.moduleService.activeModule;
  readonly filtered = computed(() =>
    this.moduleService.filterModules(this.moduleService.modules(), this.query()),
  );

  readonly activeOptionId = computed(() => {
    const list = this.filtered();
    const i = this.highlight();
    const mod = list[i];
    return mod ? `module-opt-${mod.id}` : null;
  });

  readonly positions = [
    {
      originX: 'start' as const,
      originY: 'bottom' as const,
      overlayX: 'start' as const,
      overlayY: 'top' as const,
      offsetY: 6,
    },
    {
      originX: 'start' as const,
      originY: 'top' as const,
      overlayX: 'start' as const,
      overlayY: 'bottom' as const,
      offsetY: -6,
    },
  ];

  toggle(): void {
    if (this.open()) this.close();
    else this.openPanel();
  }

  openPanel(): void {
    this.open.set(true);
    this.query.set('');
    const list = this.filtered();
    const idx = list.findIndex((m) => m.id === this.active()?.id);
    this.highlight.set(idx >= 0 ? idx : 0);
    queueMicrotask(() => this.searchInput()?.nativeElement.focus());
  }

  close(): void {
    this.open.set(false);
  }

  onQuery(value: string): void {
    this.query.set(value);
    this.highlight.set(0);
  }

  select(mod: ModuleEntry): void {
    this.history.trackModuleVisit(mod.id, mod.title, mod.route);
    this.moduleService.navigateToModule(mod);
    this.close();
  }

  onSearchKeydown(event: KeyboardEvent): void {
    const list = this.filtered();
    if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
      return;
    }
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (!list.length) return;
      this.highlight.update((i) => (i + 1) % list.length);
      return;
    }
    if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (!list.length) return;
      this.highlight.update((i) => (i - 1 + list.length) % list.length);
      return;
    }
    if (event.key === 'Enter') {
      event.preventDefault();
      const mod = list[this.highlight()];
      if (mod) this.select(mod);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open()) this.close();
  }
}
