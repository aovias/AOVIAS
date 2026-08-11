import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { SettingsComingSoonBadgeComponent } from '../../settings/components/settings-coming-soon-badge.component';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';

@Component({
  selector: 'app-prompt-version-history',
  standalone: true,
  imports: [MatCardModule, SettingsPageHeaderComponent, SettingsComingSoonBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ai-page">
      <app-settings-page-header
        title="Prompt Version History"
        description="Review published prompt versions and rollback controls for governed layers."
      />
      <mat-card class="ai-page__card">
        <mat-card-content>
          <div class="ai-page__placeholder">
            <app-settings-coming-soon-badge />
            <p>Version history and rollback controls connect to published prompt versions.</p>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [
    `
      .ai-page__card {
        border-radius: 16px;
      }
      .ai-page__placeholder {
        display: flex;
        flex-direction: column;
        gap: 12px;
        padding: 24px 8px;
        color: var(--color-text-secondary, #6b7280);
      }
      .ai-page__placeholder p {
        margin: 0;
        font-size: 14px;
        line-height: 1.5;
      }
    `,
  ],
})
export class PromptVersionHistoryComponent {}
