import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCompass } from '@ng-icons/lucide';

/**
 * Placeholder for a capability tab that has no page yet (Phase 1: Intelligence,
 * Administration).
 *
 * Deliberately states what the capability WILL hold rather than showing an
 * empty screen, so the tab is honest about being unbuilt instead of looking
 * broken. No data is fetched and nothing is faked.
 */
@Component({
  selector: 'app-audit-capability-placeholder',
  standalone: true,
  imports: [NgIcon],
  viewProviders: [provideIcons({ lucideCompass })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="aov-page">
      <section class="aov-card placeholder">
        <div class="aov-empty">
          <ng-icon name="lucideCompass" class="aov-empty__icon" aria-hidden="true" />
          <h2 class="aov-empty__title">{{ capability() }}</h2>
          <p class="aov-empty__message">{{ description() }}</p>

          @if (planned().length) {
            <ul class="placeholder__list">
              @for (item of planned(); track item) {
                <li class="placeholder__item">{{ item }}</li>
              }
            </ul>
          }

          <p class="placeholder__note aov-text-caption">
            This capability is not built yet. Existing audit functionality is unaffected.
          </p>
        </div>
      </section>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-inline-size: 0;
      }

      .placeholder {
        padding: 0;
      }

      .placeholder__list {
        display: flex;
        flex-wrap: wrap;
        justify-content: center;
        gap: var(--space-2);
        list-style: none;
        margin: var(--space-3) 0 0;
        padding: 0;
        max-inline-size: 44rem;
      }

      .placeholder__item {
        padding: var(--space-1) var(--space-3);
        border: var(--border-width-hairline) solid var(--border-color);
        border-radius: var(--radius-chip);
        background: var(--bg-tertiary);
        color: var(--text-secondary);
        font-size: var(--font-size-caption);
      }

      .placeholder__note {
        margin: var(--space-4) 0 0;
      }
    `,
  ],
})
export class AuditCapabilityPlaceholderComponent {
  readonly capability = input.required<string>();
  readonly description = input.required<string>();
  readonly planned = input<readonly string[]>([]);
}
