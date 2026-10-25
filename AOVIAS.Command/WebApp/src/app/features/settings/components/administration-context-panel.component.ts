import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs/operators';
import { MatIconModule } from '@angular/material/icon';

import { resolveAdministrationTab } from '../models/settings-nav.model';

/**
 * Context Drawer section for the Enterprise Administration Workspace.
 *
 * Stage 5A scope: what the workspace itself knows — which domain is open, which of its
 * pages are configurable today, and which are not yet. Related policies, workflow
 * configuration, approval status, risk rules, and AI guidance arrive in Stage 5C, when
 * there is configuration to describe. Showing empty placeholders for them now would
 * misrepresent the platform's state.
 */
@Component({
  selector: 'app-administration-context-panel',
  standalone: true,
  imports: [RouterLink, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (tab(); as activeTab) {
      <div class="admin-context">
        <header class="admin-context__header">
          <mat-icon aria-hidden="true">{{ activeTab.icon }}</mat-icon>
          <div>
            <p class="admin-context__eyebrow">Administration domain</p>
            <h3 class="admin-context__title">{{ activeTab.label }}</h3>
          </div>
        </header>

        <p class="admin-context__description">{{ activeTab.description }}</p>

        <section class="admin-context__group">
          <h4>Configurable now</h4>
          @if (available().length) {
            <ul>
              @for (item of available(); track item.route) {
                <li>
                  <a [routerLink]="item.route">{{ item.label }}</a>
                </li>
              }
            </ul>
          } @else {
            <p class="admin-context__note">
              No page in this domain is configurable yet. Its schema lands in Stage 5B.
            </p>
          }
        </section>

        @if (pending().length) {
          <section class="admin-context__group">
            <h4>Not yet configurable</h4>
            <ul class="admin-context__pending">
              @for (item of pending(); track item.route) {
                <li>{{ item.label }}</li>
              }
            </ul>
          </section>
        }
      </div>
    } @else {
      <p class="admin-context__note">Select an administration domain to see its context.</p>
    }
  `,
  styles: [
    `
      .admin-context {
        display: flex;
        flex-direction: column;
        gap: 20px;
        padding: 4px 2px;
      }
      .admin-context__header {
        display: flex;
        align-items: flex-start;
        gap: 12px;
      }
      .admin-context__header mat-icon {
        color: var(--color-primary);
      }
      .admin-context__eyebrow {
        margin: 0;
        font-size: 11px;
        font-weight: 700;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: var(--color-text-secondary, #9ca3af);
      }
      .admin-context__title {
        margin: 2px 0 0;
        font-size: 15px;
        font-weight: 600;
      }
      .admin-context__description {
        margin: 0;
        font-size: 13px;
        line-height: 1.55;
        color: var(--color-text-secondary, #6b7280);
      }
      .admin-context__group h4 {
        margin: 0 0 8px;
        font-size: 11px;
        font-weight: 700;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: var(--color-text-secondary, #9ca3af);
      }
      .admin-context__group ul {
        margin: 0;
        padding: 0;
        list-style: none;
        display: flex;
        flex-direction: column;
        gap: 6px;
      }
      .admin-context__group a {
        font-size: 13px;
        color: var(--color-primary);
        text-decoration: none;
      }
      .admin-context__group a:hover {
        text-decoration: underline;
      }
      .admin-context__pending li {
        font-size: 13px;
        color: var(--color-text-secondary, #9ca3af);
      }
      .admin-context__note {
        margin: 0;
        font-size: 13px;
        line-height: 1.5;
        color: var(--color-text-secondary, #6b7280);
      }
    `,
  ],
})
export class AdministrationContextPanelComponent {
  private readonly router = inject(Router);

  private readonly url = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  readonly tab = computed(() => resolveAdministrationTab(this.url()));
  readonly available = computed(() => this.tab()?.items.filter((item) => !item.comingSoon) ?? []);
  readonly pending = computed(() => this.tab()?.items.filter((item) => item.comingSoon) ?? []);
}
