import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  SupportingEvidenceDto,
  TechnicalMetadataDto,
} from '../../../features/control-center/services/ai-assistant-api.service';

/**
 * Collapsed-by-default panel holding evidence references and technical analysis details.
 * Technical metrics (confidence, answer mode) are only revealed here for advanced users.
 */
@Component({
  selector: 'app-supporting-evidence-panel',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (hasContent()) {
      <section class="evidence-panel">
        <button
          type="button"
          class="evidence-toggle"
          [attr.aria-expanded]="expanded()"
          (click)="toggle()"
        >
          <span class="evidence-toggle-label">
            Supporting Evidence
            @if (evidence().length) {
              <span class="evidence-count">{{ evidence().length }}</span>
            }
          </span>
          <svg
            class="evidence-chevron"
            [class.evidence-chevron--open]="expanded()"
            width="14"
            height="14"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round"
            aria-hidden="true"
          >
            <polyline points="6 9 12 15 18 9" />
          </svg>
        </button>

        @if (expanded()) {
          <div class="evidence-body">
            @if (evidence().length) {
              <h6 class="evidence-subhead">Evidence reviewed</h6>
              <ul class="evidence-list">
                @for (item of evidence(); track item.label) {
                  <li class="evidence-item">
                    <span class="evidence-item-type">{{ item.sourceType }}</span>
                    <span class="evidence-item-label">{{ item.label }}</span>
                  </li>
                }
              </ul>
            }

            <h6 class="evidence-subhead">Analysis details</h6>
            <dl class="evidence-meta">
              <div class="evidence-meta-row">
                <dt>Assessment certainty</dt>
                <dd>{{ certaintyLabel() }}</dd>
              </div>
              @if (technical()?.answerMode) {
                <div class="evidence-meta-row">
                  <dt>Analysis basis</dt>
                  <dd>{{ analysisBasis() }}</dd>
                </div>
              }
            </dl>
          </div>
        }
      </section>
    }
  `,
  styles: [
    `
      .evidence-panel {
        border: 1px solid var(--border-color);
        border-radius: 10px;
        background: color-mix(in srgb, var(--bg-tertiary) 88%, transparent);
        overflow: hidden;
      }

      .evidence-toggle {
        width: 100%;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 8px;
        padding: 8px 10px;
        background: transparent;
        border: none;
        cursor: pointer;
        color: var(--text-secondary);
        font-size: 11.5px;
        font-weight: 600;
      }

      .evidence-toggle:hover {
        color: var(--text-primary);
      }

      .evidence-toggle-label {
        display: inline-flex;
        align-items: center;
        gap: 6px;
      }

      .evidence-count {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        min-width: 18px;
        height: 18px;
        padding: 0 5px;
        border-radius: 999px;
        font-size: 10px;
        font-weight: 700;
        color: var(--text-secondary);
        background: color-mix(in srgb, var(--border-color) 60%, transparent);
      }

      .evidence-chevron {
        transition: transform 0.18s ease;
        flex-shrink: 0;
      }

      .evidence-chevron--open {
        transform: rotate(180deg);
      }

      .evidence-body {
        padding: 2px 10px 10px;
        border-top: 1px solid var(--border-color);
      }

      .evidence-subhead {
        margin: 10px 0 5px;
        font-size: 10px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: var(--text-muted);
      }

      .evidence-list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 5px;
      }

      .evidence-item {
        display: flex;
        flex-direction: column;
        gap: 1px;
        padding: 6px 8px;
        border-radius: 8px;
        background: color-mix(in srgb, var(--card-bg) 70%, transparent);
        border: 1px solid var(--border-color);
      }

      .evidence-item-type {
        font-size: 9.5px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--primary-color);
      }

      .evidence-item-label {
        font-size: 11.5px;
        color: var(--text-secondary);
        line-height: 1.35;
      }

      .evidence-meta {
        margin: 0;
      }

      .evidence-meta-row {
        display: flex;
        justify-content: space-between;
        gap: 10px;
        padding: 3px 0;
      }

      .evidence-meta-row dt {
        font-size: 11px;
        color: var(--text-muted);
      }

      .evidence-meta-row dd {
        margin: 0;
        font-size: 11px;
        font-weight: 600;
        color: var(--text-secondary);
        text-align: right;
      }
    `,
  ],
})
export class SupportingEvidencePanelComponent {
  readonly evidence = input<SupportingEvidenceDto[]>([]);
  readonly technical = input<TechnicalMetadataDto | null>(null);

  private readonly expandedState = signal(false);
  readonly expanded = this.expandedState.asReadonly();

  readonly hasContent = computed(() => this.evidence().length > 0 || this.technical() != null);

  readonly certaintyLabel = computed(() => {
    const confidence = this.technical()?.confidence ?? 0;
    if (confidence >= 80) {
      return 'High';
    }
    if (confidence >= 55) {
      return 'Moderate';
    }
    if (confidence > 0) {
      return 'Further evidence would improve accuracy';
    }
    return 'Further evidence would improve accuracy';
  });

  readonly analysisBasis = computed(() => {
    switch (this.technical()?.answerMode) {
      case 'OrganizationSpecific':
        return 'Your workspace data';
      case 'Hybrid':
        return 'Workspace data and expert guidance';
      case 'GeneralKnowledge':
      case 'ComplianceKnowledge':
        return 'Expert compliance guidance';
      default:
        return 'Compliance analysis';
    }
  });

  toggle(): void {
    this.expandedState.update((open) => !open);
  }
}
