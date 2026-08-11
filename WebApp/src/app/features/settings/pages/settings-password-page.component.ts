import { ChangeDetectionStrategy, Component } from '@angular/core';

import { RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';

import { ChangePasswordFormComponent } from '../components/change-password-form.component';

import { SettingsPageHeaderComponent } from '../components/settings-page-header.component';

@Component({
  selector: 'app-settings-password-page',

  standalone: true,

  imports: [MatCardModule, ChangePasswordFormComponent, RouterLink, SettingsPageHeaderComponent],

  changeDetection: ChangeDetectionStrategy.OnPush,

  template: `
    <div class="page">
      <app-settings-page-header
        title="Password Management"
        description="Update your account password and credential settings."
      />

      <mat-card class="section">
        <h3>Change Password</h3>

        <app-change-password-form />
      </mat-card>

      <p class="hint">
        For sessions and advanced security options, visit

        <a routerLink="/app/settings/account">Account &amp; Security</a>.
      </p>
    </div>
  `,

  styles: [
    `
      .section {
        padding: 24px;

        margin-bottom: 16px;

        border-radius: 16px;

        border: 1px solid var(--color-border-subtle, #e5e7eb);
      }

      .section h3 {
        margin: 0 0 16px;

        font-size: 16px;

        font-weight: 600;
      }

      .hint {
        font-size: 13px;

        color: var(--color-text-secondary, #6b7280);
      }

      .hint a {
        color: var(--color-brand-primary, var(--color-primary));
      }
    `,
  ],
})
export class SettingsPasswordPageComponent {}
