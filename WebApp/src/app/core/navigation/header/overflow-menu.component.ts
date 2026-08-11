import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  input,
  output,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { OverlayModule } from '@angular/cdk/overlay';
import { A11yModule } from '@angular/cdk/a11y';
import type { HeaderActionId } from './header-layout.service';

@Component({
  selector: 'app-header-overflow-menu',
  standalone: true,
  imports: [CommonModule, RouterLink, OverlayModule, A11yModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="overflow-menu__trigger"
      cdkOverlayOrigin
      #origin="cdkOverlayOrigin"
      (click)="toggle()"
      [attr.aria-expanded]="open()"
      aria-haspopup="menu"
      aria-controls="header-overflow-panel"
      aria-label="More actions"
      title="More"
    >
      <svg
        width="18"
        height="18"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        aria-hidden="true"
      >
        <circle cx="5" cy="12" r="1.6" fill="currentColor" stroke="none" />
        <circle cx="12" cy="12" r="1.6" fill="currentColor" stroke="none" />
        <circle cx="19" cy="12" r="1.6" fill="currentColor" stroke="none" />
      </svg>
    </button>

    <ng-template
      cdkConnectedOverlay
      [cdkConnectedOverlayOrigin]="origin"
      [cdkConnectedOverlayOpen]="open()"
      [cdkConnectedOverlayHasBackdrop]="true"
      cdkConnectedOverlayBackdropClass="header-overflow-backdrop"
      [cdkConnectedOverlayPositions]="positions"
      (backdropClick)="close()"
      (detach)="close()"
    >
      <div
        id="header-overflow-panel"
        class="overflow-menu__panel"
        role="menu"
        aria-label="More header actions"
        cdkTrapFocus
      >
        @if (items().includes('refresh')) {
          <button
            type="button"
            role="menuitem"
            class="overflow-menu__item"
            (click)="emitAndClose('refresh')"
          >
            Refresh
          </button>
        }
        @if (items().includes('theme')) {
          <button
            type="button"
            role="menuitem"
            class="overflow-menu__item"
            (click)="emitAndClose('theme')"
          >
            Toggle theme
          </button>
        }
        @if (items().includes('bookmarks')) {
          <button
            type="button"
            role="menuitem"
            class="overflow-menu__item"
            (click)="emitAndClose('bookmarks')"
          >
            Favorites &amp; recent
          </button>
        }
        @if (items().includes('search')) {
          <button
            type="button"
            role="menuitem"
            class="overflow-menu__item"
            (click)="emitAndClose('search')"
          >
            Search
          </button>
        }
        <a
          role="menuitem"
          class="overflow-menu__item"
          routerLink="/app/settings/account"
          (click)="close()"
        >
          Settings
        </a>
        <a
          role="menuitem"
          class="overflow-menu__item"
          routerLink="/app/settings/account"
          fragment="navigation-preferences"
          (click)="close()"
        >
          Navigation preferences
        </a>
      </div>
    </ng-template>
  `,
  styles: `
    :host {
      display: inline-flex;
    }

    .overflow-menu__trigger {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: var(--header-control-height, 36px);
      height: var(--header-control-height, 36px);
      border: none;
      border-radius: 8px;
      background: transparent;
      color: var(--color-text-secondary, var(--text-secondary));
      cursor: pointer;
    }

    .overflow-menu__trigger:hover {
      background: var(--color-bg-hover, var(--hover-bg));
      color: var(--color-text-primary, var(--text-primary));
    }

    .overflow-menu__trigger:focus-visible {
      outline: 2px solid var(--color-brand-primary, var(--primary-color));
      outline-offset: 2px;
    }

    .overflow-menu__panel {
      min-width: 180px;
      padding: 6px;
      background: var(--card-bg, #fff);
      border: 1px solid var(--color-border-subtle, var(--border-color));
      border-radius: 10px;
      box-shadow: 0 10px 28px rgba(0, 0, 0, 0.14);
    }

    .overflow-menu__item {
      display: block;
      width: 100%;
      text-align: left;
      border: none;
      background: transparent;
      border-radius: 6px;
      padding: 8px 10px;
      font-size: 13px;
      color: inherit;
      text-decoration: none;
      cursor: pointer;
    }

    .overflow-menu__item:hover {
      background: var(--color-bg-hover, var(--hover-bg));
    }

    .overflow-menu__item:focus-visible {
      outline: 2px solid var(--color-brand-primary, var(--primary-color));
      outline-offset: -2px;
    }
  `,
})
export class OverflowMenuComponent {
  readonly items = input<HeaderActionId[]>([]);
  readonly action = output<HeaderActionId | 'settings'>();

  readonly open = signal(false);
  readonly positions = [
    {
      originX: 'end' as const,
      originY: 'bottom' as const,
      overlayX: 'end' as const,
      overlayY: 'top' as const,
      offsetY: 6,
    },
  ];

  toggle(): void {
    this.open.update((v) => !v);
  }

  close(): void {
    this.open.set(false);
  }

  emitAndClose(id: HeaderActionId): void {
    this.action.emit(id);
    this.close();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open()) this.close();
  }
}
