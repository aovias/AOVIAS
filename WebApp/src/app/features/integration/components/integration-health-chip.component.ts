import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'app-integration-health-chip',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="health-chip" [attr.data-status]="normalized">{{ status }}</span>`,
  styles: [
    `
      .health-chip {
        display: inline-flex;
        padding: 2px 10px;
        border-radius: 999px;
        font-size: 12px;
        font-weight: 600;
        text-transform: capitalize;
        background: #f3f4f6;
        color: #6b7280;
      }
      .health-chip[data-status='healthy'],
      .health-chip[data-status='active'],
      .health-chip[data-status='connected'] {
        background: #dcfce7;
        color: #166534;
      }
      .health-chip[data-status='warning'],
      .health-chip[data-status='degraded'] {
        background: #fef3c7;
        color: #92400e;
      }
      .health-chip[data-status='failed'],
      .health-chip[data-status='offline'],
      .health-chip[data-status='error'] {
        background: #fee2e2;
        color: #991b1b;
      }
      .health-chip[data-status='disabled'],
      .health-chip[data-status='inactive'] {
        background: #e5e7eb;
        color: #4b5563;
      }
    `,
  ],
})
export class IntegrationHealthChipComponent {
  @Input({ required: true }) status!: string;

  get normalized(): string {
    return (this.status ?? 'unknown').toLowerCase();
  }
}
