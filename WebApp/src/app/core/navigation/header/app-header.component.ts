import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  input,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter, take } from 'rxjs';
import { ThemeService } from '../../services/theme.service';
import { ShellSidebarService } from '../../layout/shell-sidebar.service';
import { OrganizationService } from '../../services/organization.service';
import { SessionService } from '../../auth/session.service';
import { WorkspaceContextComponent } from './workspace-context.component';
import { HeaderActionsComponent } from './header-actions.component';
import { HeaderLayoutService } from './header-layout.service';
import { CommandPaletteService } from '../search/command-palette.service';
import { SidebarComplianceProgressComponent } from '../../layout/sidebar-compliance-progress.component';

/**
 * Enterprise shell header — context only (NAV-2A).
 * Hospital/org brand, compliance progress, search, notifications, theme, refresh, user.
 * Module navigation lives in the left sidebar.
 */
@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    WorkspaceContextComponent,
    HeaderActionsComponent,
    SidebarComplianceProgressComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header
      class="app-header"
      [class.app-header--rail-collapsed]="
        !administrationMode() && shellSidebar.isSidebarRailCollapsed()
      "
      [class.app-header--administration]="administrationMode()"
      [class.app-header--band-laptop]="layout.band() === 'laptop'"
      [class.app-header--band-tablet]="layout.band() === 'tablet'"
      [class.app-header--band-mobile]="layout.band() === 'mobile'"
      [style.--header-rail-px.px]="
        administrationMode() ? null : shellSidebar.shellSidebarColumnPx()
      "
    >
      <div class="app-header__brand">
        @if (!administrationMode()) {
          <button
            type="button"
            class="app-header__nav-toggle"
            (click)="onNavToggle()"
            [attr.aria-expanded]="shellSidebar.isOpen()"
            aria-controls="app-sidebar-nav"
            aria-label="Toggle navigation menu"
          >
            <svg
              width="20"
              height="20"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              aria-hidden="true"
            >
              <path
                d="M4 6h16M4 12h16M4 18h16"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
            </svg>
          </button>
        }

        <a class="app-header__logo" routerLink="/app/dashboard" aria-label="AOVIAS home">
          @if (!administrationMode() && shellSidebar.isSidebarRailCollapsed()) {
            <img
              src="/assets/praman_logo.png"
              alt=""
              class="app-header__logo-img app-header__logo-img--mark"
              width="36"
              height="36"
            />
          } @else if (themeService.theme() === 'light') {
            <img
              src="/assets/logo_white_background.png"
              alt="AOVIAS"
              class="app-header__logo-img"
            />
          } @else {
            <img src="/assets/logo_dark_background.png" alt="AOVIAS" class="app-header__logo-img" />
          }
        </a>
      </div>

      <div class="app-header__main">
        @if (!administrationMode()) {
          <div class="app-header__context" aria-label="Workspace context">
            <app-workspace-context />
            <app-sidebar-compliance-progress class="app-header__compliance" />
          </div>
        }

        <div class="app-header__spacer"></div>

        <app-header-actions [userName]="userName()" (searchActivate)="onSearchActivate()" />
      </div>
    </header>
  `,
  styles: `
    :host {
      display: block;
    }

    .app-header {
      display: flex;
      align-items: center;
      gap: 5px;
      min-height: var(--header-height, 64px);
      height: var(--header-height, 64px);
      padding: 0 16px 0 0;
      background: var(--header-bg);
      border-bottom: 1px solid var(--border-color);
      position: sticky;
      top: 0;
      z-index: var(--z-dropdown);
      backdrop-filter: blur(8px);
      flex-wrap: nowrap;
      overflow: visible;
      box-sizing: border-box;
    }

    .app-header__brand {
      display: flex;
      align-items: center;
      gap: 10px;
      flex: 0 0 auto;
      min-width: 0;
      width: var(--header-rail-px, auto);
      max-width: var(--header-rail-px, none);
      padding-left: 12px;
      box-sizing: border-box;
    }

    .app-header--administration .app-header__brand {
      width: auto;
      max-width: none;
    }

    .app-header__nav-toggle {
      display: none;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      border: none;
      border-radius: 8px;
      background: transparent;
      color: var(--color-text-secondary, var(--text-secondary));
      cursor: pointer;
    }

    .app-header__nav-toggle:focus-visible {
      outline: 2px solid var(--color-brand-primary, var(--primary-color));
      outline-offset: 2px;
    }

    .app-header__logo {
      display: inline-flex;
      align-items: center;
      cursor: pointer;
      text-decoration: none;
      min-width: 0;
    }

    .app-header__logo-img {
      height: 32px;
      width: auto;
      max-width: 140px;
      object-fit: contain;
      display: block;
    }

    .app-header__logo-img--mark {
      width: 36px;
      height: 36px;
    }

    .app-header__main {
      flex: 1 1 auto;
      display: flex;
      align-items: center;
      gap: 12px;
      min-width: 0;
      flex-wrap: nowrap;
      overflow: visible;
    }

    .app-header__context {
      display: flex;
      align-items: center;
      gap: 12px;
      min-width: 0;
      flex: 0 1 auto;
      overflow: hidden;
    }

    .app-header__compliance {
      flex-shrink: 0;
    }

    .app-header__spacer {
      flex: 1 1 auto;
      min-width: 8px;
    }

    .app-header__ops-search {
      position: absolute;
      width: 1px;
      height: 1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      opacity: 0;
      pointer-events: none;
    }

    .app-header__ops-search--visible {
      position: relative;
      width: auto;
      height: auto;
      overflow: visible;
      clip: auto;
      opacity: 1;
      pointer-events: auto;
      flex: 0 1 280px;
      min-width: 160px;
      max-width: 320px;
    }

    .app-header--band-laptop {
      gap: 5px;
    }

    .app-header--band-tablet .app-header__brand {
      width: auto;
      max-width: none;
    }

    .app-header--band-mobile .app-header__nav-toggle,
    .app-header--band-tablet .app-header__nav-toggle {
      display: inline-flex;
    }

    .app-header--band-mobile .app-header__logo-img:not(.app-header__logo-img--mark) {
      max-width: 96px;
    }

    .app-header--rail-collapsed .app-header__logo-img:not(.app-header__logo-img--mark) {
      max-width: 52px;
    }

    @media (max-width: 767px) {
      .app-header {
        padding-right: 10px;
        gap: 5px;
      }

      .app-header__brand {
        width: auto;
        max-width: none;
        padding-left: 8px;
      }

      .app-header__nav-toggle {
        display: inline-flex;
      }
    }

    @media (prefers-reduced-motion: reduce) {
      .app-header {
        transition: none;
      }
    }
  `,
})
export class HeaderComponent implements OnInit {
  /** When true, hides operational navigation and uses a simplified administration layout. */
  readonly administrationMode = input(false);

  readonly themeService = inject(ThemeService);
  readonly shellSidebar = inject(ShellSidebarService);
  readonly layout = inject(HeaderLayoutService);
  private readonly organizationService = inject(OrganizationService);
  private readonly sessionService = inject(SessionService);
  private readonly destroyRef = inject(DestroyRef);

  readonly userName = signal('');
  private readonly commandPalette = inject(CommandPaletteService);

  ngOnInit(): void {
    this.resolveUserName();
  }

  onNavToggle(): void {
    this.shellSidebar.toggle();
  }

  onSearchActivate(): void {
    this.commandPalette.open();
  }

  private resolveUserName(): void {
    const userId = this.sessionService.getUserId();
    const displayName = this.organizationService.getMemberDisplayName(userId);
    if (displayName) {
      this.userName.set(displayName);
      return;
    }
    this.organizationService.organization$
      .pipe(
        filter((org) => org !== null),
        take(1),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        const name = this.organizationService.getMemberDisplayName(userId);
        this.userName.set(name || '');
      });
  }
}
