import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-settings-coming-soon-badge',
  standalone: true,
  imports: [MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="coming-soon" role="status">
      <mat-icon aria-hidden="true">schedule</mat-icon>
      <span>{{ label }}</span>
    </span>
  `,
  styles: [
    `
      .coming-soon {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        padding: 6px 12px;
        border-radius: 999px;
        background: color-mix(in srgb, var(--color-text-secondary, #6b7280) 10%, transparent);
        color: var(--color-text-secondary, #6b7280);
        font-size: 12px;
        font-weight: 600;
        letter-spacing: 0.02em;
      }
      .coming-soon mat-icon {
        font-size: 16px;
        width: 16px;
        height: 16px;
      }
    `,
  ],
})
export class SettingsComingSoonBadgeComponent {
  @Input() label = 'Coming Soon';
}
