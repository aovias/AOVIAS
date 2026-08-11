import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatRippleModule } from '@angular/material/core';

export interface NavBookmarkItem {
  route: string;
  label: string;
}

@Component({
  selector: 'app-sidebar-favorites-section',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, MatTooltipModule, MatRippleModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (favorites().length) {
      <section class="menu-section favorites-section" aria-labelledby="sidebar-section-favorites">
        <div class="section-header">
          <h2 class="section-label" id="sidebar-section-favorites">★ Favorites</h2>
        </div>
        <div class="section-divider" aria-hidden="true"></div>
        @for (fav of favorites(); track fav.route; let i = $index) {
          <div class="menu-item-wrapper bookmark-row">
            <a
              [routerLink]="fav.route"
              routerLinkActive="active"
              class="menu-item bookmark-link"
              matRipple
              [attr.aria-label]="'Favorite: ' + fav.label"
              [matTooltip]="fav.label"
              matTooltipPosition="right"
              [matTooltipDisabled]="tooltipDisabled()"
            >
              <span class="menu-icon" aria-hidden="true">★</span>
              <span class="menu-label nav-label">{{ fav.label }}</span>
            </a>
            <button
              type="button"
              class="bookmark-unpin"
              (click)="moveUp.emit(fav); $event.preventDefault(); $event.stopPropagation()"
              [attr.aria-label]="'Move up ' + fav.label"
              title="Move up"
              [disabled]="i === 0"
            >
              ↑
            </button>
            <button
              type="button"
              class="bookmark-unpin"
              (click)="moveDown.emit(fav); $event.preventDefault(); $event.stopPropagation()"
              [attr.aria-label]="'Move down ' + fav.label"
              title="Move down"
              [disabled]="i === favorites().length - 1"
            >
              ↓
            </button>
            <button
              type="button"
              class="bookmark-unpin"
              (click)="unpin.emit(fav); $event.preventDefault(); $event.stopPropagation()"
              [attr.aria-label]="'Remove favorite ' + fav.label"
              title="Remove favorite"
            >
              ×
            </button>
          </div>
        }
      </section>
    }
  `,
  styles: `
    :host {
      display: block;
    }

    .menu-section {
      display: flex;
      flex-direction: column;
      gap: 2px;
      margin-bottom: var(--sidebar-section-gap, 16px);
      padding: 0 var(--sidebar-item-pad-x, 12px);
    }

    .section-header {
      display: flex;
      align-items: center;
      min-height: 20px;
    }

    .section-label {
      margin: 0;
      padding: 0;
      font-size: var(--sidebar-section-header-size, 12px);
      font-weight: 700;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      color: var(--color-text-muted);
      line-height: 1.2;
    }

    .section-divider {
      height: 1px;
      margin: 6px 0 8px;
      background: var(--color-border-subtle);
    }

    .bookmark-row {
      display: flex;
      align-items: center;
      gap: 2px;
    }

    .bookmark-link {
      flex: 1;
      min-width: 0;
    }

    .bookmark-unpin {
      flex-shrink: 0;
      width: 24px;
      height: 24px;
      border: none;
      border-radius: 4px;
      background: transparent;
      color: var(--color-text-muted);
      cursor: pointer;
      font-size: 14px;
      line-height: 1;
    }

    .bookmark-unpin:hover {
      background: var(--color-bg-hover);
      color: var(--color-text-primary);
    }

    .bookmark-unpin:focus-visible {
      outline: 2px solid var(--color-brand-primary);
      outline-offset: 1px;
    }

    .menu-item {
      display: flex;
      align-items: center;
      gap: 10px;
      min-height: var(--sidebar-item-height, 38px);
      height: var(--sidebar-item-height, 38px);
      padding: 0 var(--sidebar-item-pad-x, 12px);
      border-radius: 8px;
      color: var(--color-text-primary);
      text-decoration: none;
      position: relative;
      overflow: hidden;
      transition:
        background var(--sidebar-motion-duration, 180ms) ease,
        color var(--sidebar-motion-duration, 180ms) ease;
    }

    .menu-item:hover {
      background: var(--color-bg-hover);
    }

    .menu-item:focus-visible {
      outline: 2px solid var(--color-brand-primary);
      outline-offset: 2px;
    }

    .menu-item.active {
      background: color-mix(in srgb, var(--color-brand-primary) 12%, transparent);
      color: var(--color-brand-primary);
    }

    .menu-item.active::before {
      content: '';
      position: absolute;
      left: 0;
      top: 6px;
      bottom: 6px;
      width: 3px;
      border-radius: 0 2px 2px 0;
      background: var(--color-brand-primary);
    }

    .menu-icon {
      width: var(--sidebar-icon-size, 18px);
      height: var(--sidebar-icon-size, 18px);
      flex-shrink: 0;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      font-size: 14px;
    }

    .menu-label {
      flex: 1;
      min-width: 0;
      font-size: 13px;
      font-weight: 500;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
  `,
})
export class FavoritesSectionComponent {
  readonly favorites = input.required<NavBookmarkItem[]>();
  readonly tooltipDisabled = input(true);
  readonly unpin = output<NavBookmarkItem>();
  readonly moveUp = output<NavBookmarkItem>();
  readonly moveDown = output<NavBookmarkItem>();
}
