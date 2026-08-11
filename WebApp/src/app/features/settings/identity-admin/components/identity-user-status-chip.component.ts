import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-identity-user-status-chip',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: ` <span class="chip" [class]="statusClass">{{ label }}</span> `,
  styles: [
    `
      .chip {
        display: inline-flex;
        align-items: center;
        padding: 2px 10px;
        border-radius: 999px;
        font-size: 12px;
        font-weight: 600;
        text-transform: capitalize;
        border: 1px solid transparent;
      }
      .chip--active {
        background: #dcfce7;
        color: #166534;
        border-color: #bbf7d0;
      }
      .chip--suspended,
      .chip--disabled {
        background: #fef3c7;
        color: #92400e;
        border-color: #fde68a;
      }
      .chip--deleted,
      .chip--archived {
        background: #fee2e2;
        color: #991b1b;
        border-color: #fecaca;
      }
      .chip--unknown {
        background: #f3f4f6;
        color: #374151;
        border-color: #e5e7eb;
      }
    `,
  ],
})
export class IdentityUserStatusChipComponent {
  @Input({ required: true }) status = '';

  get label(): string {
    return this.status || 'Unknown';
  }

  get statusClass(): string {
    const s = this.status.toLowerCase();
    if (s === 'active') return 'chip chip--active';
    if (s === 'suspended' || s === 'disabled') return 'chip chip--suspended';
    if (s === 'deleted' || s === 'archived') return 'chip chip--deleted';
    return 'chip chip--unknown';
  }
}
