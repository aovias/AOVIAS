import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OeActionPlan } from '../../models/objective-element.model';

@Component({
  selector: 'app-action-plan-list',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!plans().length) {
      <p class="empty">No action plans</p>
    } @else {
      <table class="ap-table">
        <thead>
          <tr>
            <th>Action</th>
            <th>Owner</th>
            <th>Deadline</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          @for (p of plans(); track p.id) {
            <tr>
              <td>{{ p.title }}</td>
              <td>{{ p.ownerName }}</td>
              <td>{{ p.deadline }}</td>
              <td>
                <span class="st" [attr.data-st]="p.status">{{ statusLabel(p.status) }}</span>
              </td>
            </tr>
          }
        </tbody>
      </table>
    }
  `,
  styles: `
    .empty {
      font-size: var(--font-size-caption);
      color: var(--text-secondary);
    }
    .ap-table {
      width: 100%;
      border-collapse: collapse;
      font-size: var(--font-size-caption);
    }
    th,
    td {
      padding: var(--space-2) 0.625rem;
      border-bottom: var(--border-width-hairline) solid var(--border-color);
      text-align: left;
    }
    .st[data-st='open'] {
      color: var(--warning-color);
    }
    .st[data-st='in_progress'] {
      color: var(--primary-color);
    }
    .st[data-st='closed'] {
      color: var(--success-color);
    }
  `,
})
export class ActionPlanListComponent {
  readonly plans = input<OeActionPlan[]>([]);

  statusLabel(s: OeActionPlan['status']): string {
    return s === 'open' ? 'Open' : s === 'in_progress' ? 'In progress' : 'Closed';
  }
}
