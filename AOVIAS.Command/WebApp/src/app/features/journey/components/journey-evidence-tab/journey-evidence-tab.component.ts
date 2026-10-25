import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { OeEvidenceItem } from '../../models/objective-element.model';
import { EvidenceStatusBadgeComponent } from '../../../../shared/ui/evidence-status-badge/evidence-status-badge.component';
import { canReviewEvidenceInQueue } from '../../../../shared/models/evidence-status.model';
import {
  EVIDENCE_REVIEW_QUEUE_PATH,
  evidenceReviewQueueQuery,
} from '../../../../shared/utils/evidence-review-navigation.util';

@Component({
  selector: 'app-journey-evidence-tab',
  standalone: true,
  imports: [CommonModule, RouterLink, EvidenceStatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ev-head">
      <h4>Evidence</h4>
      <button type="button" class="upload" (click)="uploadRequested.emit()" [disabled]="readOnly()">
        Upload evidence
      </button>
    </div>
    @if (!items().length) {
      <p class="empty">No evidence linked to this objective element.</p>
    } @else {
      <table class="ev-table">
        <thead>
          <tr>
            <th>Document</th>
            <th>Verification</th>
            <th>Uploaded</th>
            <th>Expiry</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          @for (e of items(); track e.id) {
            <tr>
              <td>{{ e.name }}</td>
              <td><app-evidence-status-badge [status]="e.verificationStatus" /></td>
              <td>{{ e.uploadedAt }}</td>
              <td>{{ e.expiryAt ?? '—' }}</td>
              <td>
                @if (canReview(e)) {
                  <a
                    class="review-link"
                    [routerLink]="reviewQueuePath"
                    [queryParams]="reviewParams(e)"
                  >
                    Review
                  </a>
                }
              </td>
            </tr>
          }
        </tbody>
      </table>
    }
  `,
  styles: `
    .ev-head {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: var(--space-3);
    }
    h4 {
      margin: 0;
      font-size: var(--font-size-body);
      color: var(--text-primary);
    }
    .upload {
      font-size: var(--font-size-caption);
      padding: var(--space-2) var(--space-3);
      border-radius: var(--radius-md);
      border: var(--border-width-hairline) solid var(--border-color);
      background: var(--card-bg);
      color: var(--text-primary);
      cursor: pointer;
      font-family: inherit;
      min-height: var(--button-height);
      font-weight: var(--font-weight-semibold);
    }
    .upload:not(:disabled):hover {
      border-color: var(--primary-color);
      color: var(--primary-color);
    }
    .upload:disabled {
      opacity: 0.45;
      cursor: not-allowed;
    }
    .empty {
      font-size: var(--font-size-caption);
      color: var(--text-secondary);
    }
    .ev-table {
      width: 100%;
      border-collapse: collapse;
      font-size: var(--font-size-caption);
    }
    th,
    td {
      padding: var(--space-2);
      border-bottom: var(--border-width-hairline) solid var(--border-color);
      text-align: left;
    }
    .review-link {
      color: var(--primary-color);
      font-weight: var(--font-weight-semibold);
      text-decoration: none;
    }
    .review-link:hover {
      text-decoration: underline;
    }
  `,
})
export class JourneyEvidenceTabComponent {
  readonly items = input<OeEvidenceItem[]>([]);
  readonly readOnly = input(false);
  readonly uploadRequested = output<void>();

  readonly reviewQueuePath = EVIDENCE_REVIEW_QUEUE_PATH;

  canReview(e: OeEvidenceItem): boolean {
    return canReviewEvidenceInQueue(e.verificationStatus);
  }

  reviewParams(e: OeEvidenceItem): Record<string, string> {
    return evidenceReviewQueueQuery({ evidenceId: e.id });
  }
}
