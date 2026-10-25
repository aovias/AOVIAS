import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { EvidenceStatusBadgeComponent } from '../../../shared/ui/evidence-status-badge/evidence-status-badge.component';
import { canReviewEvidenceInQueue } from '../../../shared/models/evidence-status.model';
import {
  EVIDENCE_REVIEW_QUEUE_PATH,
  evidenceReviewQueueQuery,
} from '../../../shared/utils/evidence-review-navigation.util';
import type { EvidenceBinderEvidenceItem } from './models';

@Component({
  selector: 'app-binder-evidence-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatButtonModule,
    MatProgressSpinnerModule,
    EvidenceStatusBadgeComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="list">
      @if (loading()) {
        <mat-spinner diameter="28" />
      } @else if (items().length === 0) {
        <p class="empty">No evidence files yet.</p>
      } @else {
        @for (ev of items(); track ev.id) {
          <div class="item">
            <div class="main">
              <div class="name">{{ ev.fileName }}</div>
              <div class="meta">{{ ev.uploadedBy }} · {{ ev.uploadedAt | date: 'mediumDate' }}</div>
              <app-evidence-status-badge [status]="ev.status" />
            </div>
            @if (canReview(ev)) {
              <a
                class="review-link"
                [routerLink]="reviewQueuePath"
                [queryParams]="reviewQueueParams(ev)"
              >
                Open in Review Queue
              </a>
            }
          </div>
        }
      }
    </div>
    @if (totalCount() > pageSize()) {
      <div class="pager">
        <button mat-button type="button" [disabled]="page() <= 1" (click)="pageChange.emit(page() - 1)">Previous</button>
        <span>Page {{ page() }} · {{ totalCount() }} total</span>
        <button
          mat-button
          type="button"
          [disabled]="page() * pageSize() >= totalCount()"
          (click)="pageChange.emit(page() + 1)"
        >
          Next
        </button>
      </div>
    }
  `,
  styles: [
    `
      .list {
        display: flex;
        flex-direction: column;
        gap: 10px;
      }
      .item {
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        padding: 12px;
        display: flex;
        flex-wrap: wrap;
        justify-content: space-between;
        gap: 8px;
        align-items: center;
      }
      .name {
        font-weight: 500;
        font-size: 13px;
      }
      .meta {
        font-size: 11px;
        color: #6b7280;
      }
      .review-link {
        font-size: 12px;
        font-weight: 600;
        color: var(--primary-color, var(--color-primary));
        text-decoration: none;
      }
      .review-link:hover {
        text-decoration: underline;
      }
      .empty {
        color: #6b7280;
        font-size: 13px;
      }
      .pager {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 12px;
        margin-top: 12px;
        font-size: 12px;
      }
    `,
  ],
})
export class EvidenceListComponent {
  readonly items = input<EvidenceBinderEvidenceItem[]>([]);
  readonly loading = input(false);
  readonly page = input(1);
  readonly pageSize = input(20);
  readonly totalCount = input(0);
  readonly objectiveElementId = input<string | null>(null);
  readonly objectiveElementCode = input<string | null>(null);
  readonly pageChange = output<number>();

  readonly reviewQueuePath = EVIDENCE_REVIEW_QUEUE_PATH;

  canReview(ev: EvidenceBinderEvidenceItem): boolean {
    return canReviewEvidenceInQueue(ev.status);
  }

  reviewQueueParams(ev: EvidenceBinderEvidenceItem): Record<string, string> {
    return evidenceReviewQueueQuery({
      evidenceId: ev.evidenceId,
      objectiveElementId: this.objectiveElementId(),
      objectiveElementCode: this.objectiveElementCode(),
    });
  }
}
