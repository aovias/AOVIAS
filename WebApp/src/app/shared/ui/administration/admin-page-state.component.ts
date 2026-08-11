import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

export type AdminPageStateKind =
  | 'loading'
  | 'empty'
  | 'error'
  | 'permission-denied'
  | 'no-results';

/**
 * Shared Administration empty / loading / error / permission state.
 * Use across Settings, AI, Workforce, Notifications, Integrations, and Audit.
 */
@Component({
  selector: 'app-admin-page-state',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="admin-page-state"
      [attr.data-state]="state"
      role="status"
      aria-live="polite"
      [attr.aria-busy]="state === 'loading' ? true : null"
    >
      @switch (state) {
        @case ('loading') {
          <mat-spinner diameter="36" />
          <span>{{ message || 'Loading…' }}</span>
        }
        @case ('empty') {
          <mat-icon aria-hidden="true">{{ icon || 'inbox' }}</mat-icon>
          <p>{{ message || 'Nothing here yet.' }}</p>
        }
        @case ('error') {
          <mat-icon class="admin-page-state__error" aria-hidden="true">error_outline</mat-icon>
          <p>{{ message || 'Something went wrong.' }}</p>
          @if (showRetry) {
            <button mat-stroked-button type="button" (click)="retry.emit()">Retry</button>
          }
        }
        @case ('permission-denied') {
          <mat-icon aria-hidden="true">lock</mat-icon>
          <p>{{ message || 'You do not have permission to view this content.' }}</p>
        }
        @case ('no-results') {
          <mat-icon aria-hidden="true">search_off</mat-icon>
          <p>{{ message || 'No results match your filters.' }}</p>
        }
      }
    </div>
  `,
  styles: [
    `
      .admin-page-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 12px;
        padding: 40px 16px;
        color: var(--color-text-secondary, #6b7280);
        text-align: center;
      }
      .admin-page-state p {
        margin: 0;
        font-size: 14px;
        line-height: 1.5;
        max-width: 42ch;
      }
      .admin-page-state__error {
        color: #b91c1c;
      }
      .admin-page-state mat-icon:not(.admin-page-state__error) {
        font-size: 32px;
        width: 32px;
        height: 32px;
        opacity: 0.7;
      }
    `,
  ],
})
export class AdminPageStateComponent {
  @Input({ required: true }) state!: AdminPageStateKind;
  @Input() message = '';
  @Input() icon = '';
  @Input() showRetry = true;
  @Output() retry = new EventEmitter<void>();
}
