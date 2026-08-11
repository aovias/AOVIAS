import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { ActivatedRoute, RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';

import { MatButtonModule } from '@angular/material/button';

import { MatIconModule } from '@angular/material/icon';

import { SettingsPageHeaderComponent } from '../components/settings-page-header.component';

import { SettingsComingSoonBadgeComponent } from '../components/settings-coming-soon-badge.component';

@Component({
  selector: 'app-settings-placeholder-page',

  standalone: true,

  imports: [
    MatCardModule,

    MatButtonModule,

    MatIconModule,

    RouterLink,

    SettingsPageHeaderComponent,

    SettingsComingSoonBadgeComponent,
  ],

  changeDetection: ChangeDetectionStrategy.OnPush,

  template: `
    <div class="placeholder-page">
      <app-settings-page-header [title]="title" [description]="description" />

      <mat-card class="placeholder">
        @if (comingSoon) {
          <app-settings-coming-soon-badge label="Coming Soon" />

          <p class="placeholder__soon-copy">Available in a future release.</p>
        }

        @if (actionRoute) {
          <a mat-stroked-button [routerLink]="actionRoute">{{ actionLabel }}</a>
        }

        @if (futureItems.length) {
          <ul class="placeholder__list">
            @for (item of futureItems; track item) {
              <li>
                <mat-icon aria-hidden="true">schedule</mat-icon>

                <span>{{ item }}</span>
              </li>
            }
          </ul>
        }
      </mat-card>
    </div>
  `,

  styles: [
    `
      .placeholder {
        padding: 28px;

        border-radius: 16px;

        border: 1px solid var(--color-border-subtle, #e5e7eb);

        max-width: 720px;
      }

      .placeholder__soon-copy {
        margin: 12px 0 0;

        font-size: 14px;

        color: var(--color-text-secondary, #6b7280);
      }

      .placeholder__list {
        margin: 20px 0 0;

        padding: 0;

        list-style: none;
      }

      .placeholder__list li {
        display: flex;

        align-items: center;

        gap: 10px;

        padding: 10px 0;

        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);

        font-size: 14px;

        color: var(--color-text-secondary, #6b7280);
      }

      .placeholder__list mat-icon {
        font-size: 18px;

        width: 18px;

        height: 18px;
      }
    `,
  ],
})
export class SettingsPlaceholderPageComponent {
  private readonly route = inject(ActivatedRoute);

  readonly title = (this.route.snapshot.data['title'] as string) ?? 'Settings';

  readonly description =
    (this.route.snapshot.data['description'] as string) ?? 'This section is not available yet.';

  readonly comingSoon = !!this.route.snapshot.data['comingSoon'];

  readonly actionRoute = this.route.snapshot.data['actionRoute'] as string | undefined;

  readonly actionLabel = (this.route.snapshot.data['actionLabel'] as string) ?? 'Open';

  readonly futureItems = (this.route.snapshot.data['futureItems'] as string[]) ?? [];
}
