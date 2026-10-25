import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ObjectiveElement, oeRequiresActionPlan } from '../../models/objective-element.model';
import { coreOeBelowThreshold } from '../../utils/journey-validators';

@Component({
  selector: 'app-oe-card',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article
      class="oe-card"
      [class.na]="oe().isNa"
      [class.core-low]="coreHighlight()"
      [attr.aria-label]="'Objective Element ' + oe().code"
    >
      <header class="oe-head">
        <div class="codes">
          <span class="oe-code">{{ oe().code }}</span>
          <span class="class-badge" [attr.data-class]="oe().classification">{{ classLabel(oe().classification) }}</span>
        </div>
        @if (oe().isNa) {
          <span class="na-pill">N/A</span>
        }
      </header>
      <p class="desc">{{ oe().description }}</p>
      <div class="score-row">
        <div class="score-main">
          <span class="score-num" [attr.data-score]="oe().score">{{ oe().score }}</span>
          <span class="score-label">{{ oe().scoreLabel }}</span>
        </div>
        <div class="score-bar" [attr.data-score]="oe().score">
          <div class="fill" [style.width.%]="scorePercent()"></div>
        </div>
      </div>
      <div class="status-grid">
        <div>
          <span class="lbl">Evidence</span>
          <span class="pill ev" [attr.data-ev]="oe().evidenceStatus">{{ evidenceLabel(oe().evidenceStatus) }}</span>
        </div>
        <div>
          <span class="lbl">Audit</span>
          <span class="pill au" [attr.data-au]="oe().auditStatus">{{ auditLabel(oe().auditStatus) }}</span>
        </div>
      </div>
      @if (needsAction()) {
        <div class="action-banner" role="alert">Action plan required</div>
      }
      <div class="owner-row">
        <span>{{ oe().departmentLabel ?? '—' }}</span>
        <span>{{ oe().assigneeDisplayName ?? '—' }}</span>
      </div>
      <div class="time-row">
        <span>Updated {{ oe().lastUpdatedAt ?? '—' }}</span>
        <span>Next review {{ oe().nextReviewAt ?? '—' }}</span>
      </div>
      <div class="actions">
        <button type="button" class="btn" (click)="viewTasks.emit()" [disabled]="readOnly()">View tasks</button>
        <button type="button" class="btn" (click)="uploadEvidence.emit()" [disabled]="readOnly()">Upload evidence</button>
        <button type="button" class="btn primary" (click)="openDetails.emit()">Details</button>
      </div>
    </article>
  `,
  styles: `
    :host {
      display: block;
    }
    .oe-card {
      border: var(--border-width-hairline) solid var(--border-color);
      border-radius: var(--radius-card);
      padding: 0.875rem;
      background: var(--card-bg);
      box-shadow: var(--shadow-sm);
    }
    .oe-card.na {
      opacity: 0.85;
      background: var(--bg-tertiary);
    }
    .oe-card.core-low {
      box-shadow: 0 0 0 2px color-mix(in srgb, var(--danger-color) 25%, transparent);
    }
    .oe-head {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: var(--space-2);
    }
    .codes {
      display: flex;
      flex-wrap: wrap;
      gap: var(--space-2);
      align-items: center;
    }
    .oe-code {
      font-weight: var(--font-weight-bold);
      font-size: var(--font-size-body);
      color: var(--text-primary);
    }
    .class-badge {
      font-size: 0.625rem;
      font-weight: var(--font-weight-bold);
      padding: 2px var(--space-2);
      border-radius: 999px;
      text-transform: uppercase;
    }
    .class-badge[data-class='core'] {
      background: color-mix(in srgb, var(--danger-color) 12%, transparent);
      color: var(--danger-color);
    }
    .class-badge[data-class='commitment'] {
      background: color-mix(in srgb, var(--warning-color) 15%, transparent);
      color: var(--warning-color);
    }
    .class-badge[data-class='achievement'] {
      background: var(--primary-bg-light);
      color: var(--primary-color);
    }
    .class-badge[data-class='excellence'] {
      background: color-mix(in srgb, var(--success-color) 12%, transparent);
      color: var(--success-color);
    }
    .na-pill {
      font-size: 0.6875rem;
      font-weight: var(--font-weight-bold);
      padding: 2px var(--space-2);
      border-radius: var(--radius-sm);
      background: var(--border-color);
      color: var(--text-secondary);
    }
    .desc {
      margin: var(--space-2) 0;
      font-size: var(--font-size-caption);
      line-height: 1.45;
      color: var(--text-secondary);
      display: -webkit-box;
      -webkit-line-clamp: 3;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }
    .score-row {
      margin: 0.625rem 0;
    }
    .score-main {
      display: flex;
      align-items: baseline;
      gap: var(--space-2);
    }
    .score-num {
      font-size: 1.375rem;
      font-weight: var(--font-weight-bold);
    }
    .score-num[data-score='1'],
    .score-num[data-score='2'] {
      color: var(--danger-color);
    }
    .score-num[data-score='3'] {
      color: var(--warning-color);
    }
    .score-num[data-score='4'],
    .score-num[data-score='5'] {
      color: var(--success-color);
    }
    .score-label {
      font-size: var(--font-size-caption);
      color: var(--text-secondary);
    }
    .score-bar {
      height: 0.375rem;
      border-radius: var(--radius-xs);
      background: var(--border-color);
      margin-top: 0.375rem;
      overflow: hidden;
    }
    .score-bar .fill {
      height: 100%;
      border-radius: var(--radius-xs);
      background: var(--primary-color);
    }
    .score-bar[data-score='1'] .fill,
    .score-bar[data-score='2'] .fill {
      background: var(--danger-color);
    }
    .score-bar[data-score='3'] .fill {
      background: var(--warning-color);
    }
    .score-bar[data-score='4'] .fill,
    .score-bar[data-score='5'] .fill {
      background: var(--success-color);
    }
    .status-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: var(--space-2);
      font-size: var(--font-size-caption);
      margin: 0.625rem 0;
    }
    .lbl {
      display: block;
      font-size: 0.625rem;
      text-transform: uppercase;
      color: var(--text-muted);
      margin-bottom: var(--space-1);
    }
    .pill {
      font-weight: var(--font-weight-semibold);
    }
    .action-banner {
      background: color-mix(in srgb, var(--warning-color) 15%, transparent);
      color: var(--warning-color);
      font-size: var(--font-size-caption);
      font-weight: var(--font-weight-bold);
      padding: var(--space-2) 0.625rem;
      border-radius: var(--radius-md);
      margin-bottom: var(--space-2);
    }
    .owner-row,
    .time-row {
      display: flex;
      justify-content: space-between;
      font-size: 0.6875rem;
      color: var(--text-secondary);
      margin-top: var(--space-1);
    }
    .actions {
      display: flex;
      flex-wrap: wrap;
      gap: var(--space-2);
      margin-top: var(--space-3);
    }
    .btn {
      font-size: var(--font-size-caption);
      padding: 0.375rem 0.625rem;
      border-radius: var(--radius-md);
      border: var(--border-width-hairline) solid var(--border-color);
      background: var(--card-bg);
      cursor: pointer;
      font-family: inherit;
      min-height: var(--button-height);
    }
    .btn.primary {
      border-color: var(--primary-color);
      color: var(--primary-color);
      font-weight: var(--font-weight-semibold);
    }
    .btn:disabled {
      opacity: 0.45;
      cursor: not-allowed;
    }
  `,
})
export class OeCardComponent {
  readonly oe = input.required<ObjectiveElement>();
  readonly readOnly = input(false);

  readonly viewTasks = output<void>();
  readonly uploadEvidence = output<void>();
  readonly openDetails = output<void>();

  needsAction(): boolean {
    return oeRequiresActionPlan(this.oe());
  }

  coreHighlight(): boolean {
    return coreOeBelowThreshold(this.oe(), 4);
  }

  scorePercent(): number {
    return (this.oe().score / 5) * 100;
  }

  classLabel(c: ObjectiveElement['classification']): string {
    const m: Record<ObjectiveElement['classification'], string> = {
      core: 'Core',
      commitment: 'Commitment',
      achievement: 'Achievement',
      excellence: 'Excellence',
    };
    return m[c];
  }

  evidenceLabel(s: ObjectiveElement['evidenceStatus']): string {
    const m = {
      complete: 'Complete',
      missing: 'Missing',
      pending_verification: 'Pending verification',
    };
    return m[s];
  }

  auditLabel(s: ObjectiveElement['auditStatus']): string {
    const m = {
      compliant: 'Compliant',
      non_compliant: 'Non-compliant',
      not_applicable: 'N/A',
    };
    return m[s];
  }
}
