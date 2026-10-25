import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideShieldCheck } from '@ng-icons/lucide';
import { ContextFrameworkDisplayService } from '../../../../core/services/context-framework-display.service';

@Component({
  selector: 'app-journey-framework-context',
  standalone: true,
  imports: [CommonModule, NgIcon],
  viewProviders: [provideIcons({ lucideShieldCheck })],
  template: `
    <div class="journey-framework-context" aria-label="Selected framework">
      <div class="journey-framework-context__value">
        <div class="journey-framework-context__badge">
          <ng-icon name="lucideShieldCheck" size="14"></ng-icon>
        </div>
        <div class="journey-framework-context__content">
          <span class="journey-framework-context__name">{{ framework.frameworkName() }}</span>
          <span class="journey-framework-context__code">{{ framework.frameworkCode() }}</span>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        width: 100%;
        min-width: 0;
      }

      .journey-framework-context {
        width: 100%;
        max-width: 22rem;
      }

      .journey-framework-context__value {
        display: flex;
        align-items: center;
        gap: 10px;
        min-width: 0;
        flex-wrap: wrap;
      }

      .journey-framework-context__badge {
        width: 28px;
        height: 28px;
        border-radius: 50%;
        background: var(--context-framework-badge-bg);
        color: var(--context-framework-badge-color);
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
      }

      .journey-framework-context__content {
        flex: 1;
        display: flex;
        flex-direction: column;
        gap: 1px;
        min-width: 0;
      }

      .journey-framework-context__name {
        font-size: 13px;
        font-weight: 600;
        color: var(--hospital-card-name-color);
        overflow-wrap: anywhere;
        word-break: break-word;
      }

      @media (min-width: 40rem) {
        .journey-framework-context__name {
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }
      }

      .journey-framework-context__code {
        font-size: 11px;
        color: var(--hospital-card-org-color);
        overflow-wrap: anywhere;
        word-break: break-word;
      }

      @media (min-width: 40rem) {
        .journey-framework-context__code {
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JourneyFrameworkContextComponent {
  protected readonly framework = inject(ContextFrameworkDisplayService);
}
