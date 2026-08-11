import { ChangeDetectionStrategy, Component } from '@angular/core';

import { MatCardModule } from '@angular/material/card';

import { SettingsPageHeaderComponent } from '../components/settings-page-header.component';

@Component({
  selector: 'app-settings-about-page',

  standalone: true,

  imports: [MatCardModule, SettingsPageHeaderComponent],

  changeDetection: ChangeDetectionStrategy.OnPush,

  template: `
    <div class="page">
      <app-settings-page-header
        title="About AOVIAS Command"
        description="Healthcare compliance platform information and release details."
      />

      <mat-card class="section">
        <dl>
          <dt>Product</dt>

          <dd>AOVIAS Command — Healthcare Compliance Platform</dd>

          <dt>Version</dt>

          <dd>1.0</dd>

          <dt>Publisher</dt>

          <dd>AOVIAS</dd>
        </dl>

        <p class="section__copy">© 2026 AOVIAS. All rights reserved.</p>
      </mat-card>
    </div>
  `,

  styles: [
    `
      .section {
        padding: 24px;

        max-width: 560px;

        border-radius: 16px;

        border: 1px solid var(--color-border-subtle, #e5e7eb);
      }

      dl {
        margin: 0;

        display: grid;

        grid-template-columns: 120px 1fr;

        gap: 12px 16px;
      }

      dt {
        font-weight: 600;

        color: var(--color-text-secondary, #6b7280);
      }

      dd {
        margin: 0;
      }

      .section__copy {
        margin: 20px 0 0;

        font-size: 13px;

        color: var(--color-text-secondary, #6b7280);
      }
    `,
  ],
})
export class SettingsAboutPageComponent {}
