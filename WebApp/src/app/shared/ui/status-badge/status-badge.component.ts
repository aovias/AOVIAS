import { Component, ChangeDetectionStrategy, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatChipsModule } from '@angular/material/chips';

export type StatusType = 'success' | 'warning' | 'error' | 'info' | 'neutral';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule, MatChipsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-chip [class]="'status-' + status">
      {{ label }}
    </mat-chip>
  `,
  styles: [
    `
      .status-success {
        background-color: #28a745;
        color: white;
      }
      .status-warning {
        background-color: #ffc107;
        color: #212529;
      }
      .status-error {
        background-color: #dc3545;
        color: white;
      }
      .status-info {
        background-color: #17a2b8;
        color: white;
      }
      .status-neutral {
        background-color: #6c757d;
        color: white;
      }
    `,
  ],
})
export class StatusBadgeComponent {
  @Input() label = '';
  @Input() status: StatusType = 'neutral';
}
