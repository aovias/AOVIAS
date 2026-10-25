import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatRippleModule } from '@angular/material/core';
import type { NavBookmarkItem } from './favorites-section.component';

@Component({
  selector: 'app-sidebar-recent-section',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, MatTooltipModule, MatRippleModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (recent().length) {
      <section class="menu-section recent-section" aria-labelledby="sidebar-section-recent">
        <button
          type="button"
          class="section-toggle"
          id="sidebar-section-recent"
          (click)="toggleExpand.emit()"
          [attr.aria-expanded]="expanded()"
          aria-controls="sidebar-recent-panel"
        >
          <svg
            class="section-chevron"
            [class.section-chevron--open]="expanded()"
            width="14"
            height="14"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            aria-hidden="true"
          >
            <path
              d="M6 9l6 6 6-6"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
          <span class="section-label">Recent ({{ recent().length }})</span>
        </button>
        <div class="section-divider" aria-hidden="true"></div>
        <div
          id="sidebar-recent-panel"
          class="recent-panel"
          [class.recent-panel--collapsed]="!expanded()"
          role="region"
          [attr.aria-hidden]="!expanded()"
        >
          @for (item of recent(); track item.route) {
            <div class="menu-item-wrapper bookmark-row">
              <a
                [routerLink]="item.route"
                routerLinkActive="active"
                class="menu-item bookmark-link"
                matRipple
                [attr.aria-label]="'Recent: ' + item.label"
                [matTooltip]="item.label"
                matTooltipPosition="right"
                [matTooltipDisabled]="tooltipDisabled()"
                tabindex="{{ expanded() ? 0 : -1 }}"
              >
                <span class="menu-icon" aria-hidden="true">◷</span>
                <span class="menu-label nav-label">{{ item.label }}</span>
              </a>
              <button
                type="button"
                class="bookmark-unpin"
                (click)="
                  toggleFavorite.emit(item); $event.preventDefault(); $event.stopPropagation()
                "
                [attr.aria-label]="
                  (isFavorite(item.route) ? 'Remove favorite ' : 'Add favorite ') + item.label
                "
                [title]="isFavorite(item.route) ? 'Remove favorite' : 'Add favorite'"
                [attr.tabindex]="expanded() ? 0 : -1"
              >
                {{ isFavorite(item.route) ? '★' : '☆' }}
              </button>
            </div>
          }
        </div>
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

    .section-toggle {
      display: flex;
      align-items: center;
      gap: 6px;
      width: 100%;
      margin: 0;
      padding: 0;
      border: none;
      background: transparent;
      cursor: pointer;
      color: var(--color-text-muted);
      min-height: 20px;
      text-align: left;
    }

    .section-toggle:hover {
      color: var(--color-text-primary);
    }

    .section-toggle:focus-visible {
      outline: 2px solid var(--color-brand-primary);
      outline-offset: 2px;
      border-radius: 4px;
    }

    .section-label {
      margin: 0;
      font-size: var(--sidebar-section-header-size, 12px);
      font-weight: 700;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      line-height: 1.2;
    }

    .section-chevron {
      flex-shrink: 0;
      transition: transform var(--sidebar-motion-duration, 180ms) ease;
    }

    .section-chevron--open {
      transform: rotate(0deg);
    }

    .section-chevron:not(.section-chevron--open) {
      transform: rotate(-90deg);
    }

    .section-divider {
      height: 1px;
      margin: 6px 0 8px;
      background: var(--color-border-subtle);
    }

    .recent-panel {
      display: flex;
      flex-direction: column;
      gap: 2px;
      overflow: hidden;
      max-height: 400px;
      opacity: 1;
      transition:
        max-height var(--sidebar-motion-duration, 180ms) ease,
        opacity var(--sidebar-motion-duration, 180ms) ease;
    }

    .recent-panel--collapsed {
      max-height: 0;
      opacity: 0;
      pointer-events: none;
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

    @media (prefers-reduced-motion: reduce) {
      .section-chevron,
      .recent-panel,
      .menu-item {
        transition: none;
      }
    }
  `,
})
export class RecentSectionComponent {
  readonly recent = input.required<NavBookmarkItem[]>();
  readonly expanded = input(true);
  readonly tooltipDisabled = input(true);
  readonly favoriteRoutes = input<readonly string[]>([]);
  readonly toggleExpand = output<void>();
  readonly toggleFavorite = output<NavBookmarkItem>();

  isFavorite(route: string): boolean {
    return this.favoriteRoutes().includes(route);
  }
}
