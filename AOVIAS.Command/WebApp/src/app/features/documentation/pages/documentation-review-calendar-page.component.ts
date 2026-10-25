import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { catchError, finalize, of } from 'rxjs';
import { DocumentationApiService } from '../services/documentation-api.service';
import { DOCUMENT_STATUS_LABELS, type ControlledDocumentStatus } from '../models/documentation.enums';
import type { ReviewCalendarItem } from '../models/documentation.model';

@Component({
  selector: 'app-documentation-review-calendar-page',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/documentation-workspace.layout.scss',
  template: `
    <div class="qdms-workspace">
      <header class="page-header">
        <div>
          <h1>Review Calendar</h1>
          <p class="subtitle">Effective dates, next review, frequency, and reminder tiers (90 / 60 / 30 / overdue)</p>
        </div>
      </header>

      @if (error()) { <div class="banner error">{{ error() }}</div> }
      @if (actionMessage()) { <div class="banner success">{{ actionMessage() }}</div> }
      @if (loading()) { <p class="muted">Loading review calendar…</p> }
      @else {
        <section class="panel">
          <table class="data-table">
            <thead>
              <tr>
                <th>Document</th>
                <th>Title</th>
                <th>Next Review</th>
                <th>Effective</th>
                <th>Frequency</th>
                <th>Review Owner</th>
                <th>Status</th>
                <th>Reminder</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (r of items(); track r.documentId) {
                <tr>
                  <td><strong>{{ r.documentNumber }}</strong></td>
                  <td>{{ r.title }}</td>
                  <td>{{ r.nextReviewDate | date: 'mediumDate' }}</td>
                  <td>{{ r.effectiveDate | date: 'mediumDate' }}</td>
                  <td>{{ r.reviewFrequencyMonths }} mo</td>
                  <td>{{ r.reviewOwnerName ?? '—' }}</td>
                  <td>{{ statusLabel(r.status) }}</td>
                  <td><span class="badge" [class.badge--review]="r.reminderTier !== 'Scheduled'">{{ r.reminderTier }}</span></td>
                  <td>
                    @if (canCompleteReview(r.status)) {
                      <button
                        type="button"
                        class="btn-primary"
                        [disabled]="completingId() === r.documentId"
                        (click)="completeReview(r)">
                        {{ completingId() === r.documentId ? 'Saving…' : 'Complete review' }}
                      </button>
                    }
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="9" class="muted">No scheduled reviews.</td></tr>
              }
            </tbody>
          </table>
        </section>
      }
    </div>
  `,
})
export class DocumentationReviewCalendarPageComponent implements OnInit {
  private readonly api = inject(DocumentationApiService);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionMessage = signal<string | null>(null);
  readonly items = signal<ReviewCalendarItem[]>([]);
  readonly completingId = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  statusLabel(s: ControlledDocumentStatus): string {
    return DOCUMENT_STATUS_LABELS[s] ?? s;
  }

  canCompleteReview(status: ControlledDocumentStatus): boolean {
    return status === 'reviewDue' || status === 'effective';
  }

  completeReview(item: ReviewCalendarItem): void {
    this.completingId.set(item.documentId);
    this.error.set(null);
    this.actionMessage.set(null);
    this.api.completeReview(item.documentId, {
      noChangesRequired: true,
      outcome: 'NoChanges',
    }).pipe(
      catchError(() => {
        this.error.set(`Unable to complete review for ${item.documentNumber}.`);
        return of(null);
      }),
      finalize(() => this.completingId.set(null)),
    ).subscribe((detail) => {
      if (!detail) return;
      this.actionMessage.set(`Review completed for ${detail.documentNumber}.`);
      this.load();
    });
  }

  private load(): void {
    this.loading.set(true);
    this.api.getReviewCalendar().pipe(
      catchError(() => { this.error.set('Unable to load review calendar.'); return of([]); }),
      finalize(() => this.loading.set(false)),
    ).subscribe((list) => this.items.set(list));
  }
}
