import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { sidebarIcon } from './sidebar-icons.registry';
import { isJourneyShellNavPath } from './sidebar-nav.model';
import { TrustHtmlPipe } from './trust-html.pipe';
import { NavigationQueryService } from '../navigation/engine/navigation-query.service';

/**
 * Mobile-only primary shortcuts; routes come from NavigationQueryService.bottomItems().
 */
@Component({
  selector: 'app-bottom-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TrustHtmlPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="app-bottom-nav" role="navigation" [attr.aria-label]="ariaLabel">
      @for (item of items(); track item.route) {
        @if (item.route === '/app/journey') {
          <a
            [routerLink]="item.route"
            [class.app-bottom-nav__link--active]="journeyShellNavActive()"
            class="app-bottom-nav__link"
            [attr.aria-label]="item.label"
          >
            <span class="app-bottom-nav__icon" [innerHTML]="item.icon | trustHtml"></span>
            <span class="app-bottom-nav__label">{{ item.label }}</span>
          </a>
        } @else {
          <a
            [routerLink]="item.route"
            routerLinkActive="app-bottom-nav__link--active"
            class="app-bottom-nav__link"
            [attr.aria-label]="item.label"
          >
            <span class="app-bottom-nav__icon" [innerHTML]="item.icon | trustHtml"></span>
            <span class="app-bottom-nav__label">{{ item.label }}</span>
          </a>
        }
      }
    </nav>
  `,
  styles: [
    `
      :host {
        display: none;
      }

      @media (max-width: 767px) {
        :host {
          display: block;
          position: fixed;
          left: 0;
          right: 0;
          bottom: 48px;
          z-index: var(--z-shell-bottom-nav);
          height: var(--shell-bottom-nav-height);
          min-height: var(--shell-bottom-nav-height);
          padding-bottom: env(safe-area-inset-bottom, 0px);
          box-sizing: border-box;
          background: var(--color-bg-surface);
          border-top: 1px solid var(--color-border-subtle);
          box-shadow: 0 -4px 12px color-mix(in srgb, var(--color-text-primary) 8%, transparent);
        }
      }

      .app-bottom-nav {
        display: flex;
        align-items: stretch;
        justify-content: space-around;
        height: 100%;
        min-height: var(--touch-target-min);
        padding: 0 var(--space-2);
      }

      .app-bottom-nav__link {
        flex: 1 1 0;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 2px;
        min-width: 0;
        min-height: var(--touch-target-min);
        padding: var(--space-1) var(--space-1);
        text-decoration: none;
        color: var(--color-text-secondary);
        font-size: 10px;
        font-weight: 600;
        transition:
          color 0.15s ease,
          background 0.15s ease;
      }

      .app-bottom-nav__link--active {
        color: var(--color-brand-primary);
        background: color-mix(in srgb, var(--color-brand-primary) 10%, transparent);
      }

      .app-bottom-nav__link:focus-visible {
        outline: 2px solid var(--color-brand-primary);
        outline-offset: -2px;
      }

      .app-bottom-nav__icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: var(--icon-size-nav);
        height: var(--icon-size-nav);
        color: inherit;
      }

      :host ::ng-deep .app-bottom-nav__icon svg {
        display: block;
        width: var(--icon-size-nav) !important;
        height: var(--icon-size-nav) !important;
        color: inherit;
      }

      .app-bottom-nav__label {
        line-height: 1.1;
        text-align: center;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        max-width: 100%;
      }
    `,
  ],
})
export class BottomNavComponent {
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly query = inject(NavigationQueryService);

  readonly items = computed(() =>
    this.query.bottomItems().map((i) => ({
      label: i.title,
      route: i.route,
      icon: sidebarIcon(i.icon),
    })),
  );
  readonly ariaLabel = 'Primary navigation shortcuts';

  /** Same rules as sidebar Journey row (`isJourneyShellNavPath`). */
  readonly journeyShellNavActive = signal(false);

  constructor() {
    this.journeyShellNavActive.set(isJourneyShellNavPath(this.router.url));
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.journeyShellNavActive.set(isJourneyShellNavPath(this.router.url)));
  }
}
