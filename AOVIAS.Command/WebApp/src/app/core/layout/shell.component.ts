import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  viewChild,
  ElementRef,
  afterNextRender,
  DestroyRef,
  PLATFORM_ID,
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeftRight } from '@ng-icons/lucide';
import { HeaderComponent } from './header.component';
import { SidebarComponent } from '../navigation/sidebar/sidebar.component';
import { BottomNavComponent } from './bottom-nav.component';
import { ShellSidebarService } from './shell-sidebar.service';
import { CopilotNavigationBridgeService } from '../../features/copilot/services/copilot-navigation-bridge.service';
import { PerformanceTimelineService } from '../performance/performance-timeline.service';
import { OrganizationBrandingThemeService } from '../services/organization-branding-theme.service';
import { CommandPaletteComponent } from '../navigation/search/command-palette.component';
import { KeyboardShortcutsService } from '../navigation/shortcuts/keyboard-shortcuts.service';
import { WorkspacePreferencesService } from '../navigation/personalization/workspace-preferences.service';
import { TrialExperienceBannerComponent } from '../../shared/components/trial-experience-banner.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterOutlet,
    HeaderComponent,
    SidebarComponent,
    BottomNavComponent,
    CommandPaletteComponent,
    MatTooltipModule,
    NgIcon,
    TrialExperienceBannerComponent,
  ],
  viewProviders: [provideIcons({ lucideArrowLeftRight })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="shell-container" [style.--app-shell-header-height.px]="measuredHeaderHeightPx()">
      <app-header #headerBlock></app-header>
      <app-trial-experience-banner></app-trial-experience-banner>
      <div
        class="shell-content"
        [style.padding-inline-start.px]="shellSidebar.shellContentInlineStartPaddingPx()"
        [class.shell-content--sidebar-overlay-blocking]="
          shellSidebar.isOverlayMode() && shellSidebar.isOpen()
        "
      >
        <app-sidebar></app-sidebar>
        <main class="main-content">
          <router-outlet></router-outlet>
        </main>
      </div>
      <app-bottom-nav></app-bottom-nav>
      <app-command-palette />
      <footer class="footer">
        <div class="footer-content">
          <div class="footer-left">
            @if (!shellSidebar.isOverlayMode()) {
              <button
                type="button"
                class="footer-rail-toggle"
                (click)="shellSidebar.toggleSidebarRailCollapsed()"
                [attr.aria-label]="
                  shellSidebar.isSidebarRailCollapsed() ? 'Expand sidebar' : 'Collapse sidebar'
                "
                [matTooltip]="
                  shellSidebar.isSidebarRailCollapsed() ? 'Expand sidebar' : 'Collapse sidebar'
                "
                matTooltipPosition="above"
              >
                <ng-icon name="lucideArrowLeftRight" size="18" aria-hidden="true" />
              </button>
            }
            <span class="footer-brand">AOVIAS Healthcare Compliance Platform</span>
            <span class="footer-version">Version 1.0</span>
          </div>
          <div class="footer-right">
            <span class="footer-copyright">© 2026 AOVIAS</span>
          </div>
        </div>
      </footer>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-height: 100vh;
        min-height: 100dvh;
      }

      .shell-content--sidebar-overlay-blocking .main-content {
        pointer-events: none;
        user-select: none;
      }

      .shell-container {
        display: flex;
        flex-direction: column;
        /* Cap to one viewport so tall page content scrolls inside main, not the document (keeps footer visible on tablet/desktop). */
        height: 100vh;
        height: 100dvh;
        max-height: 100vh;
        max-height: 100dvh;
        overflow: hidden;
        background: var(--bg-primary);
      }

      app-header {
        flex-shrink: 0;
      }

      .shell-content {
        display: flex;
        flex: 1;
        min-height: 0;
        overflow: hidden;
        align-items: stretch;
      }

      /*
       * UI-2 — the scroll region only. It must stay a plain fluid block:
       * routed components are created by router-outlet, so their host elements
       * do NOT carry this component's scoped attribute, and a child selector
       * such as .main-content > * silently fails to match them. Page geometry
       * therefore belongs to the page container itself (--page-padding and the
       * global .aov-page class), never to a child selector written here.
       */
      .main-content {
        flex: 1 1 auto;
        min-width: 0;
        min-height: 0;
        overflow-y: auto;
        background: var(--bg-secondary);
      }

      @media (max-width: 767px) {
        .shell-content {
          min-width: 0;
        }

        .main-content {
          flex: 1 1 auto;
          width: 100%;
          max-width: 100%;
          min-width: 0;
          padding-bottom: calc(var(--shell-bottom-nav-height) + env(safe-area-inset-bottom, 0px));
          box-sizing: border-box;
        }
      }

      .footer {
        flex-shrink: 0;
        height: 48px;
        background: var(--card-bg);
        border-top: 1px solid var(--border-color);
        display: flex;
        align-items: center;
        padding: 0 24px;
      }

      .footer-content {
        display: flex;
        align-items: center;
        justify-content: space-between;
        width: 100%;
      }

      .footer-left {
        display: flex;
        align-items: center;
        gap: 16px;
      }

      .footer-rail-toggle {
        flex-shrink: 0;
        width: 32px;
        height: 32px;
        margin: 0;
        padding: 0;
        border: none;
        border-radius: 6px;
        background: transparent;
        color: var(--text-secondary);
        display: inline-flex;
        align-items: center;
        justify-content: center;
        cursor: pointer;
        user-select: none;
        transition:
          color 0.2s ease,
          background 0.2s ease;
      }

      .footer-rail-toggle:hover {
        color: var(--text-primary);
        background: var(--hover-bg);
      }

      .footer-rail-toggle:focus-visible {
        outline: 2px solid var(--color-brand-primary);
        outline-offset: 2px;
      }

      :host ::ng-deep .footer-rail-toggle ng-icon {
        display: flex;
        color: inherit;
      }

      :host ::ng-deep .footer-rail-toggle ng-icon svg {
        display: block;
      }

      .footer-brand {
        font-size: 13px;
        font-weight: 600;
        color: var(--text-primary);
      }

      .footer-version {
        font-size: 12px;
        color: var(--text-secondary);
        padding: 4px 8px;
        background: var(--hover-bg);
        border-radius: 4px;
      }

      .footer-copyright {
        font-size: 12px;
        color: var(--text-secondary);
      }

      @media (max-width: 768px) {
        .footer-left {
          flex-direction: column;
          align-items: flex-start;
          gap: 4px;
        }
        .footer-right {
          display: none;
        }
      }
    `,
  ],
})
export class ShellComponent {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly destroyRef = inject(DestroyRef);
  readonly shellSidebar = inject(ShellSidebarService);

  /**
   * Live height of the shell header (all rows) so the fixed tablet left rail can start below it
   * and never sit under the menu bar. Used as --app-shell-header-height.
   */
  readonly measuredHeaderHeightPx = signal(64);
  private readonly headerBlock = viewChild.required('headerBlock', { read: ElementRef });

  constructor() {
    inject(CopilotNavigationBridgeService);
    const perf = inject(PerformanceTimelineService);
    const brandingTheme = inject(OrganizationBrandingThemeService);
    const shortcuts = inject(KeyboardShortcutsService);
    const navPrefs = inject(WorkspacePreferencesService);
    perf.mark('shell_activate');
    afterNextRender(() => {
      perf.mark('shell_render_complete');
      if (!isPlatformBrowser(this.platformId)) {
        return;
      }
      shortcuts.start();
      this.destroyRef.onDestroy(() => shortcuts.stop());
      if (navPrefs.prefs().defaultCollapsedSidebar && !this.shellSidebar.isSidebarRailCollapsed()) {
        this.shellSidebar.toggleSidebarRailCollapsed();
      }
      brandingTheme.loadAndApply();
      const host = this.headerBlock().nativeElement;
      const target = (host.querySelector('.header') as HTMLElement | null) ?? host;
      const apply = () => {
        const h = Math.round(target.offsetHeight);
        if (h > 0) {
          this.measuredHeaderHeightPx.set(h);
        }
      };
      apply();
      const ro = new ResizeObserver(apply);
      ro.observe(target);
      this.destroyRef.onDestroy(() => ro.disconnect());
    });
  }
}
