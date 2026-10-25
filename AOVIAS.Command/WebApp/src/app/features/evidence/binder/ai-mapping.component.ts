import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import type { EvidenceBinderAiMappingItem } from './models';

@Component({
  selector: 'app-binder-ai-mapping',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <mat-spinner diameter="28" />
    } @else if (!items() || items()!.length === 0) {
      <p class="empty">No AI mapping suggestions yet. Open this tab after selecting an objective element to request suggestions.</p>
    } @else {
      <p class="notice">AI suggestions are advisory. Human approval is required before linking evidence.</p>
      <ul class="list">
        @for (it of items()!; track it.id) {
          <li>
            <div class="title">{{ it.title }}</div>
            @if (it.detail) {
              <div class="detail">{{ it.detail }}</div>
            }
            @if (it.reasoning) {
              <div class="reasoning">Why: {{ it.reasoning }}</div>
            }
            @if (it.sourceReference) {
              <div class="source">Source: {{ it.sourceReference }}</div>
            }
            <div class="meta-row">
              <span class="conf">Confidence: {{ it.confidencePercent }}%</span>
              @if (it.humanApprovalRequired !== false) {
                <span class="approval">Human approval required</span>
              }
            </div>
          </li>
        }
      </ul>
    }
  `,
  styles: [
    `
      .list {
        list-style: none;
        padding: 0;
        margin: 0;
      }
      li {
        padding: 12px;
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        margin-bottom: 10px;
      }
      .title {
        font-weight: 600;
        font-size: 13px;
      }
      .detail {
        font-size: 12px;
        color: #6b7280;
        margin-top: 6px;
      }
      .meta-row {
        display: flex;
        flex-wrap: wrap;
        gap: 10px;
        margin-top: 8px;
        align-items: center;
      }
      .conf {
        font-size: 11px;
        color: #6b7280;
      }
      .approval {
        font-size: 10px;
        font-weight: 600;
        color: #92400e;
        background: #fef3c7;
        padding: 2px 8px;
        border-radius: 4px;
      }
      .reasoning,
      .source {
        font-size: 11px;
        color: #6b7280;
        margin-top: 6px;
      }
      .notice {
        color: #92400e;
        font-size: 12px;
        margin: 0 0 12px;
        line-height: 1.4;
      }
      .empty {
        color: #6b7280;
        font-size: 13px;
        line-height: 1.5;
      }
    `,
  ],
})
export class AiMappingComponent {
  readonly items = input<EvidenceBinderAiMappingItem[] | null>(null);
  readonly loading = input(false);
}
