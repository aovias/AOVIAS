import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { OrganizationMember } from '../../../../core/services/organization.service';

@Component({
  selector: 'app-assignee-chips-field',
  standalone: true,
  imports: [CommonModule, MatFormFieldModule, MatSelectModule, MatTooltipModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-form-field
      appearance="fill"
      subscriptSizing="dynamic"
      class="assignee-chips-field task-drawer__ff w-full"
      [attr.aria-labelledby]="ariaLabelledBy() ?? null"
    >
      @if (ariaLabelledBy()) {
        <mat-label class="sr-only">Assign to</mat-label>
      } @else {
        <mat-label matTooltip="Required for creating a compliance task." matTooltipShowDelay="300">Assign to</mat-label>
      }
      <mat-select
        [value]="selectedUserId()"
        (selectionChange)="selectedUserIdChange.emit($event.value ?? '')"
        [disabled]="disabled()"
        panelClass="task-drawer-assignee-panel"
      >
        <mat-select-trigger>
          @if (!selectedUserId()) {
            <span class="assignee-chips-field__placeholder">Select a person…</span>
          } @else {
            @if (memberById(selectedUserId()); as m) {
              <span class="assignee-chips-field__chip-list">
                <span
                  class="assignee-chips-field__chip"
                  [matTooltip]="displayName(m)"
                  matTooltipShowDelay="200"
                >
                  <span class="assignee-chips-field__avatar" aria-hidden="true">{{ initials(m) }}</span>
                  <span class="assignee-chips-field__name">{{ shortName(m) }}</span>
                </span>
              </span>
            }
          }
        </mat-select-trigger>
        @for (m of members(); track m.userId) {
          <mat-option [value]="m.userId">{{ displayName(m) }}</mat-option>
        }
      </mat-select>
    </mat-form-field>
  `,
  styles: [
    `
      .assignee-chips-field {
        width: 100%;
      }
      .assignee-chips-field__placeholder {
        color: var(--color-text-muted);
        font-size: var(--font-size-sm, 0.8125rem);
      }
      .assignee-chips-field__chip-list {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 6px;
        padding: 2px 0;
        width: 100%;
      }
      .assignee-chips-field__chip {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        max-width: 100%;
        padding: 1px 6px 1px 3px;
        border-radius: var(--radius-pill, 999px);
        background: color-mix(in srgb, var(--color-brand-primary) 14%, transparent);
        border: 1px solid color-mix(in srgb, var(--color-brand-primary) 22%, transparent);
        font-size: var(--font-size-xs, 0.75rem);
        color: var(--color-text-primary);
      }
      .assignee-chips-field__avatar {
        flex-shrink: 0;
        width: 20px;
        height: 20px;
        border-radius: 999px;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        font-size: 10px;
        font-weight: 700;
        background: color-mix(in srgb, var(--color-brand-primary) 28%, transparent);
        color: var(--color-brand-primary);
      }
      .assignee-chips-field__name {
        min-width: 0;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        max-width: 9rem;
      }
    `,
  ],
})
export class AssigneeChipsFieldComponent {
  members = input<OrganizationMember[]>([]);
  selectedUserId = input<string>('');
  disabled = input(false);
  /** When set, visible label is provided by parent; keep sr-only mat-label for a11y. */
  ariaLabelledBy = input<string | null>(null);
  selectedUserIdChange = output<string>();

  memberById(userId: string): OrganizationMember | undefined {
    return this.members().find((m) => m.userId === userId);
  }

  displayName(m: OrganizationMember): string {
    const parts = [m.firstName, m.lastName].filter(Boolean);
    if (parts.length) return parts.join(' ');
    return m.email ?? m.userId;
  }

  shortName(m: OrganizationMember): string {
    const first = (m.firstName ?? '').trim();
    const last = (m.lastName ?? '').trim();
    if (first && last) return `${first} ${last.charAt(0)}.`;
    if (first) return first;
    return (m.email ?? m.userId).split('@')[0];
  }

  initials(m: OrganizationMember): string {
    const a = (m.firstName ?? '').charAt(0);
    const b = (m.lastName ?? '').charAt(0);
    if (a || b) return `${a}${b}`.toUpperCase();
    return (m.email ?? '?').charAt(0).toUpperCase();
  }
}
