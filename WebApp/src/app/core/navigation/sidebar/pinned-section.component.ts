import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatRippleModule } from '@angular/material/core';
import type { NavBookmarkItem } from './favorites-section.component';

@Component({
  selector: 'app-sidebar-pinned-section',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, MatTooltipModule, MatRippleModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (pinned().length) {
      <section class="menu-section pinned-section" aria-labelledby="sidebar-section-pinned">
        <div class="section-header">
          <h2 class="section-label" id="sidebar-section-pinned">Pinned</h2>
        </div>
        <div class="section-divider" aria-hidden="true"></div>
        @for (item of pinned(); track item.route) {
          <div class="menu-item-wrapper bookmark-row">
            <a
              [routerLink]="item.route"
              routerLinkActive="active"
              class="menu-item bookmark-link"
              matRipple
              [attr.aria-label]="'Pinned: ' + item.label"
              [matTooltip]="item.label"
              matTooltipPosition="right"
              [matTooltipDisabled]="tooltipDisabled()"
            >
              <span class="menu-icon" aria-hidden="true">◈</span>
              <span class="menu-label nav-label">{{ item.label }}</span>
            </a>
            <button
              type="button"
              class="bookmark-unpin"
              (click)="unpin.emit(item); $event.preventDefault(); $event.stopPropagation()"
              [attr.aria-label]="'Unpin ' + item.label"
              title="Unpin"
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
    }
    .bookmark-unpin:hover {
      background: var(--color-bg-hover);
      color: var(--color-text-primary);
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
    }
    .menu-item:hover {
      background: var(--color-bg-hover);
    }
    .menu-item.active {
      background: color-mix(in srgb, var(--color-brand-primary) 12%, transparent);
      color: var(--color-brand-primary);
    }
    .menu-icon {
      width: var(--sidebar-icon-size, 18px);
      height: var(--sidebar-icon-size, 18px);
      flex-shrink: 0;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      font-size: 12px;
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
export class PinnedSectionComponent {
  readonly pinned = input.required<NavBookmarkItem[]>();
  readonly tooltipDisabled = input(true);
  readonly unpin = output<NavBookmarkItem>();
}
