import { ChangeDetectionStrategy, Component } from '@angular/core';

import { MatCardModule } from '@angular/material/card';

import { MatIconModule } from '@angular/material/icon';

import { SettingsPageHeaderComponent } from '../components/settings-page-header.component';

import { SettingsComingSoonBadgeComponent } from '../components/settings-coming-soon-badge.component';

@Component({
  selector: 'app-settings-theme-page',

  standalone: true,

  imports: [
    MatCardModule,
    MatIconModule,
    SettingsPageHeaderComponent,
    SettingsComingSoonBadgeComponent,
  ],

  changeDetection: ChangeDetectionStrategy.OnPush,

  template: `
    <div class="page">
      <app-settings-page-header
        title="Theme Preferences"
        description="Personalize how AOVIAS Command looks and feels across your workspace."
      />

      <mat-card class="section">
        <app-settings-coming-soon-badge />

        <p class="section__hint">Available in a future release.</p>

        <ul class="options">
          @for (option of options; track option) {
            <li class="options__item">
              <span>{{ option }}</span>

              <span class="options__status">
                <mat-icon aria-hidden="true">schedule</mat-icon>

                Coming soon
              </span>
            </li>
          }
        </ul>
      </mat-card>
    </div>
  `,

  styles: [
    `
      .section {
        padding: 24px;

        border-radius: 16px;

        border: 1px solid var(--color-border-subtle, #e5e7eb);
      }

      .section__hint {
        margin: 12px 0 20px;

        font-size: 14px;

        color: var(--color-text-secondary, #6b7280);
      }

      .options {
        list-style: none;

        margin: 0;

        padding: 0;
      }

      .options__item {
        display: flex;

        justify-content: space-between;

        align-items: center;

        gap: 16px;

        padding: 14px 0;

        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);

        font-size: 14px;
      }

      .options__status {
        display: inline-flex;

        align-items: center;

        gap: 4px;

        font-size: 12px;

        color: var(--color-text-secondary, #9ca3af);

        font-weight: 600;

        text-transform: uppercase;
      }

      .options__status mat-icon {
        font-size: 16px;

        width: 16px;

        height: 16px;
      }
    `,
  ],
})
export class SettingsThemePageComponent {
  readonly options = ['Light', 'Dark', 'Compact Mode', 'Accent Color', 'Navigation Density'];
}
