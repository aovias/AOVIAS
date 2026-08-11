import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgClass } from '@angular/common';
import { IsActiveMatchOptions, RouterLink, RouterLinkActive } from '@angular/router';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatRippleModule } from '@angular/material/core';
import { TrustHtmlPipe } from '../../layout/trust-html.pipe';
import type { BadgeVariant, SidebarMenuItem } from './sidebar.models';

@Component({
  selector: 'app-sidebar-item',
  standalone: true,
  imports: [
    NgClass,
    RouterLink,
    RouterLinkActive,
    MatTooltipModule,
    MatRippleModule,
    TrustHtmlPipe,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="menu-item-wrapper" [class.menu-item-wrapper--nested]="nested()">
      @if (item().children?.length) {
        <button
          type="button"
          class="menu-item"
          matRipple
          [matRippleDisabled]="disabled()"
          [class.expanded]="expanded()"
          [class.menu-item--primary]="item().primary"
          (click)="toggleExpand.emit()"
          [attr.aria-label]="item().ariaLabel || item().label"
          [attr.aria-expanded]="expanded()"
          [matTooltip]="item().tooltip || item().label"
          matTooltipPosition="right"
          [matTooltipDisabled]="tooltipDisabled()"
        >
          <span class="menu-icon" [innerHTML]="item().icon | trustHtml"></span>
          <span class="menu-label nav-label">{{ item().label }}</span>
          <svg
            class="expand-icon"
            [class.rotate]="expanded()"
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
        </button>
        @if (expanded()) {
          <div class="submenu" role="group">
            @for (child of item().children; track child.id ?? child.route) {
              @if (childDisabled(child)) {
                <span
                  class="submenu-item submenu-item--disabled"
                  tabindex="-1"
                  role="link"
                  aria-disabled="true"
                  [attr.title]="childDisabledReason(child)"
                >
                  <span class="submenu-label">{{ child.label }}</span>
                </span>
              } @else {
                <a
                  [routerLink]="child.route"
                  routerLinkActive="active"
                  class="submenu-item"
                  matRipple
                  [attr.aria-label]="child.ariaLabel || child.label"
                  [matTooltip]="child.tooltip || child.label"
                  matTooltipPosition="right"
                  [matTooltipDisabled]="tooltipDisabled()"
                >
                  <span class="submenu-label">{{ child.label }}</span>
                </a>
              }
            }
          </div>
        }
      } @else if (disabled()) {
        <span
          class="menu-item menu-item--disabled"
          tabindex="-1"
          role="link"
          aria-disabled="true"
          [attr.title]="disabledReason()"
          [attr.aria-description]="disabledReason()"
        >
          <span class="menu-icon" [innerHTML]="item().icon | trustHtml"></span>
          <span class="menu-label nav-label">{{ item().label }}</span>
          @if (badge(); as ab) {
            <span class="nav-badge" [ngClass]="'nav-badge--' + ab.variant">{{
              formatBadge(ab.count)
            }}</span>
          }
        </span>
      } @else if (item().route === '/app/journey') {
        <a
          [routerLink]="item().route"
          [class.active]="journeyActive()"
          class="menu-item"
          matRipple
          [class.menu-item--primary]="item().primary && journeyActive()"
          [attr.aria-label]="item().ariaLabel || item().label"
          [matTooltip]="item().tooltip || item().label"
          matTooltipPosition="right"
          [matTooltipDisabled]="tooltipDisabled()"
        >
          <span class="menu-icon" [innerHTML]="item().icon | trustHtml"></span>
          <span class="menu-label nav-label">{{ item().label }}</span>
          @if (badge(); as ab) {
            <span class="nav-badge" [ngClass]="'nav-badge--' + ab.variant">{{
              formatBadge(ab.count)
            }}</span>
          }
        </a>
      } @else {
        <a
          [routerLink]="item().route"
          routerLinkActive="active"
          [routerLinkActiveOptions]="
            item().subsetActive ? subsetActiveLinkOptions() : exactActiveLinkOptions()
          "
          class="menu-item"
          matRipple
          [class.menu-item--primary]="item().primary"
          [attr.aria-label]="item().ariaLabel || item().label"
          [matTooltip]="item().tooltip || item().label"
          matTooltipPosition="right"
          [matTooltipDisabled]="tooltipDisabled()"
        >
          <span class="menu-icon" [innerHTML]="item().icon | trustHtml"></span>
          <span class="menu-label nav-label">{{ item().label }}</span>
          @if (badge(); as ab) {
            <span class="nav-badge" [ngClass]="'nav-badge--' + ab.variant">{{
              formatBadge(ab.count)
            }}</span>
          }
        </a>
      }
    </div>
  `,
  styles: `
    :host {
      display: block;
    }

    .menu-item-wrapper--nested {
      padding-left: 16px;
    }

    .menu-item {
      display: flex;
      align-items: center;
      gap: 10px;
      width: 100%;
      box-sizing: border-box;
      min-height: var(--sidebar-item-height, 36px);
      height: var(--sidebar-item-height, 36px);
      padding: 0 var(--sidebar-item-pad-x, 12px);
      border: none;
      border-radius: 8px;
      background: transparent;
      color: var(--color-text-primary);
      text-decoration: none;
      font: inherit;
      text-align: left;
      cursor: pointer;
      position: relative;
      overflow: hidden;
      transition:
        background var(--sidebar-motion-duration, 180ms) ease,
        color var(--sidebar-motion-duration, 180ms) ease;
    }

    .menu-item:hover:not(.menu-item--disabled) {
      background: var(--color-bg-hover);
    }

    .menu-item:focus-visible {
      outline: 2px solid var(--color-brand-primary);
      outline-offset: 2px;
    }

    .menu-item.active {
      background: color-mix(in srgb, var(--color-brand-primary) 14%, transparent);
      color: var(--color-text-primary);
      font-weight: 650;
    }

    .menu-item.active .menu-icon {
      color: var(--color-brand-primary);
    }

    .menu-item.active .menu-label {
      color: var(--color-text-primary);
      font-weight: 650;
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

    .menu-item--primary:not(.menu-item--disabled) {
      border: 1px solid color-mix(in srgb, var(--color-brand-primary) 35%, transparent);
      background: color-mix(in srgb, var(--color-brand-primary) 8%, transparent);
      font-weight: 600;
      color: var(--color-brand-primary);
    }

    .menu-item--disabled {
      opacity: 0.45;
      cursor: not-allowed;
      pointer-events: none;
    }

    .menu-icon {
      width: var(--sidebar-icon-size, 18px);
      height: var(--sidebar-icon-size, 18px);
      flex-shrink: 0;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      color: inherit;
    }

    :host ::ng-deep .menu-icon svg {
      display: block;
      width: var(--sidebar-icon-size, 18px) !important;
      height: var(--sidebar-icon-size, 18px) !important;
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

    .expand-icon {
      flex-shrink: 0;
      transition: transform var(--sidebar-motion-duration, 180ms) ease;
    }

    .expand-icon.rotate {
      transform: rotate(180deg);
    }

    .submenu {
      display: flex;
      flex-direction: column;
      gap: 2px;
      margin-top: 2px;
      padding-left: 18px;
      animation: navSlideDown var(--sidebar-motion-duration, 180ms) ease;
    }

    .submenu-item {
      display: flex;
      align-items: center;
      min-height: 34px;
      padding: 0 12px;
      border-radius: 6px;
      color: var(--color-text-secondary);
      text-decoration: none;
      font-size: 13px;
      position: relative;
      transition: background var(--sidebar-motion-duration, 180ms) ease;
    }

    .submenu-item:hover {
      background: var(--color-bg-hover);
      color: var(--color-text-primary);
    }

    .submenu-item:focus-visible {
      outline: 2px solid var(--color-brand-primary);
      outline-offset: 2px;
    }

    .submenu-item.active {
      color: var(--color-brand-primary);
      background: color-mix(in srgb, var(--color-brand-primary) 10%, transparent);
    }

    .submenu-item--disabled {
      opacity: 0.45;
      pointer-events: none;
    }

    .nav-badge {
      flex-shrink: 0;
      margin-left: auto;
      min-width: 18px;
      height: 18px;
      padding: 0 5px;
      border-radius: 9px;
      font-size: 10px;
      font-weight: 700;
      display: inline-flex;
      align-items: center;
      justify-content: center;
    }

    .nav-badge--neutral {
      background: color-mix(in srgb, var(--color-text-muted) 22%, transparent);
      color: var(--color-text-primary);
    }

    .nav-badge--warning {
      background: color-mix(in srgb, var(--color-status-warning) 28%, transparent);
      color: var(--color-status-warning);
    }

    .nav-badge--critical {
      background: color-mix(in srgb, var(--color-status-danger) 26%, transparent);
      color: var(--color-status-danger);
    }

    @keyframes navSlideDown {
      from {
        opacity: 0;
        transform: translateY(-4px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    @media (prefers-reduced-motion: reduce) {
      .menu-item,
      .expand-icon,
      .submenu {
        transition: none;
        animation: none;
      }
    }
  `,
})
export class SidebarItemComponent {
  readonly item = input.required<SidebarMenuItem>();
  readonly nested = input(false);
  readonly expanded = input(false);
  readonly disabled = input(false);
  readonly disabledReason = input('');
  readonly tooltipDisabled = input(true);
  readonly journeyActive = input(false);
  readonly badge = input<{ count: number; variant: BadgeVariant } | null>(null);
  readonly exactActiveLinkOptions = input.required<IsActiveMatchOptions>();
  readonly subsetActiveLinkOptions = input.required<IsActiveMatchOptions>();
  readonly isChildDisabled = input<(child: SidebarMenuItem) => boolean>();
  readonly childReason = input<(child: SidebarMenuItem) => string>();

  readonly toggleExpand = output<void>();

  formatBadge(count: number): string {
    return count > 99 ? '99+' : String(count);
  }

  childDisabled(child: SidebarMenuItem): boolean {
    return this.isChildDisabled()?.(child) ?? !!child.disabled;
  }

  childDisabledReason(child: SidebarMenuItem): string {
    return this.childReason()?.(child) ?? child.disabledReason ?? '';
  }
}
