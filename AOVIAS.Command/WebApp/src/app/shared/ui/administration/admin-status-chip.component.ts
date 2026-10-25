import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

export type AdminStatusTone = 'neutral' | 'success' | 'warning' | 'danger' | 'info';

/**
 * Shared Administration status / label chip.
 */
@Component({
  selector: 'app-admin-status-chip',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="admin-status-chip" [attr.data-tone]="tone">{{ label }}</span>
  `,
  styles: [
    `
      .admin-status-chip {
        display: inline-block;
        padding: 2px 10px;
        border-radius: 999px;
        font-size: 12px;
        font-weight: 600;
        line-height: 1.4;
        background: #f3f4f6;
        color: #374151;
      }
      .admin-status-chip[data-tone='success'] {
        background: #ecfdf5;
        color: #047857;
      }
      .admin-status-chip[data-tone='warning'] {
        background: #fffbeb;
        color: #b45309;
      }
      .admin-status-chip[data-tone='danger'] {
        background: #fef2f2;
        color: #b91c1c;
      }
      .admin-status-chip[data-tone='info'] {
        background: var(--color-primary-50, #eef3ff);
        color: var(--color-primary-700, #1e4bc7);
      }
    `,
  ],
})
export class AdminStatusChipComponent {
  @Input({ required: true }) label!: string;
  @Input() tone: AdminStatusTone = 'neutral';
}
