import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ThemeService } from '../../services/theme.service';
import { FavoritesRecentService } from '../../services/favorites-recent.service';
import { NotificationBellComponent } from '../../../shared/notifications/components/notification-bell.component';
import { UserProfileMenuComponent } from '../../layout/user-profile-menu.component';
import { HeaderLayoutService } from './header-layout.service';
import { HeaderRefreshService } from './header-refresh.service';
import { SearchTriggerComponent } from './search-trigger.component';
import { OverflowMenuComponent } from './overflow-menu.component';
import type { HeaderActionId } from './header-layout.service';

@Component({
  selector: 'app-header-actions',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    NotificationBellComponent,
    UserProfileMenuComponent,
    SearchTriggerComponent,
    OverflowMenuComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="header-actions" role="toolbar" aria-label="Header actions">
      @if (layout.isInline('search')) {
        <app-search-trigger
          [iconOnly]="layout.searchIconOnly()"
          (activate)="searchActivate.emit()"
        />
      }

      @if (layout.isInline('refresh')) {
        <button
          type="button"
          class="icon-button"
          (click)="refresh.requestRefresh()"
          title="Refresh"
          aria-label="Refresh"
        >
          <svg
            width="18"
            height="18"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            aria-hidden="true"
          >
            <path
              d="M21 12a9 9 0 11-2.64-6.36M21 3v6h-6"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
        </button>
      }

      @if (layout.isInline('theme')) {
        <button
          type="button"
          class="icon-button"
          (click)="theme.toggleTheme()"
          title="Toggle theme"
          aria-label="Toggle theme"
        >
          @if (theme.theme() === 'light') {
            <svg
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              aria-hidden="true"
            >
              <path
                d="M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
            </svg>
          } @else {
            <svg
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              aria-hidden="true"
            >
              <circle cx="12" cy="12" r="5" stroke-width="2" />
              <path
                d="M12 1v2m0 18v2M4.22 4.22l1.42 1.42m12.72 12.72l1.42 1.42M1 12h2m18 0h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"
                stroke-width="2"
                stroke-linecap="round"
              />
            </svg>
          }
        </button>
      }

      @if (layout.isInline('bookmarks')) {
        <div class="bookmarks-wrap">
          <button
            type="button"
            class="icon-button"
            (click)="toggleBookmarks()"
            [attr.aria-expanded]="bookmarksOpen()"
            aria-controls="header-bookmarks-panel"
            aria-label="Favorites and recent pages"
            title="Favorites & recent"
          >
            <svg
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              aria-hidden="true"
            >
              <path
                d="M12 2l2.4 7.4H22l-6 4.6 2.3 7L12 17.8 5.7 21l2.3-7-6-4.6h7.6L12 2z"
                stroke-width="1.8"
                stroke-linejoin="round"
              />
            </svg>
          </button>
          @if (bookmarksOpen()) {
            <div id="header-bookmarks-panel" class="bookmarks-panel" role="menu">
              <div class="bookmarks-section">
                <p class="bookmarks-heading">Favorites</p>
                @if (!favorites.favorites().length) {
                  <p class="bookmarks-empty">No favorites yet</p>
                } @else {
                  @for (f of favorites.favorites(); track f.route) {
                    <a role="menuitem" [routerLink]="f.route" (click)="bookmarksOpen.set(false)">{{
                      f.label
                    }}</a>
                  }
                }
              </div>
              <div class="bookmarks-section">
                <p class="bookmarks-heading">Recent</p>
                @if (!favorites.recent().length) {
                  <p class="bookmarks-empty">No recent pages</p>
                } @else {
                  @for (r of favorites.recent().slice(0, 8); track r.route) {
                    <a role="menuitem" [routerLink]="r.route" (click)="bookmarksOpen.set(false)">{{
                      r.label
                    }}</a>
                  }
                }
              </div>
              <a class="bookmarks-footer" routerLink="/app/me" (click)="bookmarksOpen.set(false)"
                >Open My work</a
              >
            </div>
          }
        </div>
      }

      @if (layout.overflowActionIds().length) {
        <app-header-overflow-menu
          [items]="layout.overflowActionIds()"
          (action)="onOverflow($event)"
        />
      }

      <app-notification-bell #notificationBell class="notification-wrapper" />

      <div class="user-menu">
        <button
          type="button"
          class="user-button"
          data-testid="profile-menu-trigger"
          (click)="toggleUserMenu()"
          [attr.title]="userName() || 'Account menu'"
          [attr.aria-expanded]="userMenuOpen()"
        >
          <div class="user-avatar">
            @if (userName()) {
              <span class="avatar-initials">{{ userName().charAt(0) }}</span>
            } @else {
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                aria-hidden="true"
              >
                <path
                  d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2M12 11a4 4 0 100-8 4 4 0 000 8z"
                  stroke-width="2"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />
              </svg>
            }
          </div>
          @if (userName() && !layout.searchIconOnly()) {
            <span class="user-display-name">{{ userName() }}</span>
          }
          <svg
            width="14"
            height="14"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            [class.rotate]="userMenuOpen()"
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
        @if (userMenuOpen()) {
          <div class="user-dropdown">
            <app-user-profile-menu (closed)="userMenuOpen.set(false)" />
          </div>
        }
      </div>
    </div>
  `,
  styles: `
    :host {
      display: contents;
    }

    .header-actions {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      flex-shrink: 0;
      min-width: 0;
    }

    .icon-button {
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

    .icon-button:hover {
      background: var(--color-bg-hover, var(--hover-bg));
      color: var(--color-text-primary, var(--text-primary));
    }

    .icon-button:focus-visible {
      outline: 2px solid var(--color-brand-primary, var(--primary-color));
      outline-offset: 2px;
    }

    .bookmarks-wrap {
      position: relative;
    }

    .bookmarks-panel {
      position: absolute;
      top: calc(100% + 6px);
      right: 0;
      width: 260px;
      background: var(--card-bg, #fff);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
      z-index: var(--z-modal);
      padding: 10px 0 0;
    }

    .bookmarks-section {
      padding: 4px 12px 10px;
    }

    .bookmarks-heading {
      margin: 0 0 6px;
      font-size: 11px;
      font-weight: 700;
      letter-spacing: 0.04em;
      text-transform: uppercase;
      color: var(--text-secondary);
    }

    .bookmarks-empty {
      margin: 0;
      font-size: 12px;
      color: var(--text-muted, var(--text-secondary));
    }

    .bookmarks-panel a {
      display: block;
      padding: 6px 4px;
      font-size: 13px;
      color: var(--text-primary);
      text-decoration: none;
      border-radius: 6px;
    }

    .bookmarks-panel a:hover {
      background: var(--hover-bg);
    }

    .bookmarks-footer {
      display: block;
      margin-top: 4px;
      padding: 10px 16px !important;
      border-top: 1px solid var(--border-color);
      font-weight: 600;
      color: var(--primary-color) !important;
    }

    .user-menu {
      position: relative;
    }

    .user-button {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      height: var(--header-control-height, 36px);
      padding: 0 8px;
      border: none;
      border-radius: 8px;
      background: transparent;
      color: inherit;
      cursor: pointer;
    }

    .user-button:hover {
      background: var(--color-bg-hover, var(--hover-bg));
    }

    .user-button:focus-visible {
      outline: 2px solid var(--color-brand-primary, var(--primary-color));
      outline-offset: 2px;
    }

    .user-avatar {
      width: 28px;
      height: 28px;
      border-radius: 50%;
      background: color-mix(
        in srgb,
        var(--color-brand-primary, var(--primary-color)) 18%,
        transparent
      );
      display: inline-flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .avatar-initials {
      font-size: 12px;
      font-weight: 700;
      color: var(--color-brand-primary, var(--primary-color));
      text-transform: uppercase;
    }

    .user-display-name {
      font-size: 13px;
      font-weight: 600;
      max-width: 120px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .user-button svg.rotate {
      transform: rotate(180deg);
    }

    .user-dropdown {
      position: absolute;
      top: calc(100% + 6px);
      right: 0;
      z-index: var(--z-modal, 1000);
      min-width: 280px;
    }

    .notification-wrapper {
      display: inline-flex;
    }
  `,
})
export class HeaderActionsComponent {
  readonly userName = input('');
  readonly searchActivate = output<void>();

  readonly layout = inject(HeaderLayoutService);
  readonly theme = inject(ThemeService);
  readonly refresh = inject(HeaderRefreshService);
  readonly favorites = inject(FavoritesRecentService);
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly userMenuOpen = signal(false);
  readonly bookmarksOpen = signal(false);
  private readonly notificationBell = viewChild<NotificationBellComponent>('notificationBell');

  toggleUserMenu(): void {
    this.bookmarksOpen.set(false);
    this.notificationBell()?.close();
    this.userMenuOpen.update((v) => !v);
  }

  toggleBookmarks(): void {
    this.userMenuOpen.set(false);
    this.notificationBell()?.close();
    this.bookmarksOpen.update((v) => !v);
  }

  onOverflow(id: HeaderActionId | 'settings'): void {
    if (id === 'refresh') this.refresh.requestRefresh();
    if (id === 'theme') this.theme.toggleTheme();
    if (id === 'bookmarks') this.toggleBookmarks();
    if (id === 'search') this.searchActivate.emit();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    if (!(event.target instanceof Node)) return;
    const root = this.host.nativeElement;
    const notif = root.querySelector('.notification-wrapper');
    if (notif && !notif.contains(event.target)) this.notificationBell()?.close();
    const user = root.querySelector('.user-menu');
    if (user && !user.contains(event.target)) this.userMenuOpen.set(false);
    const bookmarks = root.querySelector('.bookmarks-wrap');
    if (bookmarks && !bookmarks.contains(event.target)) this.bookmarksOpen.set(false);
  }
}
