import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  OnInit,
  OnDestroy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink, RouterLinkActive } from '@angular/router';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { EMPTY, Subject, catchError, combineLatest, debounceTime, distinctUntilChanged, finalize, of, takeUntil } from 'rxjs';
import {
  EvidenceApiService,
  EvidenceReviewQueueItem,
} from '../services/evidence-api.service';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { EvidenceStatusBadgeComponent } from '../../../shared/ui/evidence-status-badge/evidence-status-badge.component';
import {
  canReviewEvidenceInQueue,
  evidenceStatusLabel,
} from '../../../shared/models/evidence-status.model';

@Component({
  selector: 'app-evidence-review-queue',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    RouterLinkActive,
    MatSnackBarModule,
    EvidenceStatusBadgeComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="evidence-page">
      <nav class="module-tabs">
        <a routerLink="/app/evidence" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Repository</a>
        <a routerLink="/app/evidence" fragment="coverage">Coverage</a>
        <a routerLink="/app/evidence/review-queue" routerLinkActive="active">Review Queue</a>
      </nav>

      <header class="page-header">
        <div>
          <h1>Evidence Review Queue</h1>
          <p>Authoritative verification surface — approve, reject, or return evidence for revision.</p>
        </div>
      </header>

      @if (error()) {
        <div class="banner error">{{ error() }}</div>
      }

      <div class="filters">
        <select [(ngModel)]="filterStatus" (ngModelChange)="reload()">
          <option value="">Awaiting Review</option>
          <option value="pendingReview">Pending Review</option>
          <option value="submitted">Submitted</option>
          <option value="approved">Approved</option>
          <option value="rejected">Rejected</option>
          <option value="needsMoreEvidence">Needs More Evidence</option>
        </select>
        <select [(ngModel)]="filterStage" (ngModelChange)="reload()">
          <option value="">All Stages</option>
          <option value="Core">Core</option>
          <option value="Commitment">Commitment</option>
          <option value="Achievement">Achievement</option>
          <option value="Excellence">Excellence</option>
        </select>
        <input type="search" placeholder="Search..." [(ngModel)]="search" (ngModelChange)="onSearchChange($event)" />
      </div>

      @if (loading()) {
        <p class="muted">Loading review queue…</p>
      } @else if (items().length === 0) {
        <p class="muted">No evidence items match the current filters.</p>
      } @else {
        <table class="queue-table">
          <thead>
            <tr>
              <th>File</th>
              <th>Objective Element</th>
              <th>Stage</th>
              <th>Department</th>
              <th>Status</th>
              <th>Uploaded</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            @for (row of items(); track row.linkId) {
              <tr [class.row-highlight]="highlightEvidenceId() === row.evidenceId">
                <td>{{ row.fileName }}</td>
                <td>{{ row.objectiveElementCode }} — {{ row.objectiveElementName }}</td>
                <td>{{ row.maturityStage ?? '—' }}</td>
                <td>{{ row.department || '—' }}</td>
                <td><app-evidence-status-badge [status]="row.status" /></td>
                <td>{{ row.uploadDate | date: 'mediumDate' }}</td>
                <td class="actions">
                  <button
                    type="button"
                    class="view"
                    (click)="viewEvidence(row)"
                    [disabled]="viewLoading() && viewRow()?.evidenceId === row.evidenceId"
                  >
                    View
                  </button>
                  @if (canAct(row)) {
                    <button type="button" (click)="review(row, 'approve')" [disabled]="acting()">Approve</button>
                    <button type="button" class="danger" (click)="review(row, 'reject')" [disabled]="acting()">Reject</button>
                    <button type="button" class="warn" (click)="review(row, 'requestMoreEvidence')" [disabled]="acting()">
                      Return For Revision
                    </button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
        <div class="pagination">
          <button type="button" [disabled]="page() <= 1 || loading()" (click)="setPage(page() - 1)">Previous</button>
          <span>Page {{ page() }} · {{ totalCount() }} items</span>
          <button type="button" [disabled]="page() * pageSize() >= totalCount() || loading()" (click)="setPage(page() + 1)">Next</button>
        </div>
      }
    </div>

    @if (viewRow(); as row) {
      <div class="preview-backdrop" (click)="closeView()" role="presentation"></div>
      <div class="preview-dialog" role="dialog" aria-modal="true" [attr.aria-label]="'Preview ' + row.fileName">
        <header class="preview-header">
          <div class="preview-title">
            <h2>{{ row.fileName }}</h2>
            <p>{{ row.objectiveElementCode }} — {{ row.objectiveElementName }}</p>
          </div>
          <div class="preview-header-actions">
            <button type="button" (click)="downloadViewed(row)" [disabled]="viewDownloadInFlight()">Download</button>
            <button type="button" class="preview-close" (click)="closeView()" aria-label="Close preview">Close</button>
          </div>
        </header>
        <div class="preview-body">
          @if (viewLoading()) {
            <p class="muted">Loading preview…</p>
          } @else if (viewError()) {
            <p class="preview-error">{{ viewError() }}</p>
          } @else {
            @switch (previewMode()) {
              @case ('pdf') {
                @if (previewSafeUrl()) {
                  <iframe class="preview-frame" title="Evidence preview" [src]="previewSafeUrl()"></iframe>
                }
              }
              @case ('image') {
                @if (previewSafeUrl()) {
                  <img class="preview-image" [src]="previewSafeUrl()" [alt]="row.fileName" />
                }
              }
              @case ('text') {
                <pre class="preview-text">{{ previewTextContent() }}</pre>
              }
              @default {
                <div class="preview-unsupported">
                  <p>Preview is not available for this file type.</p>
                  <p class="muted">Use Download to open the file locally.</p>
                </div>
              }
            }
          }
        </div>
      </div>
    }
  `,
  styles: [
    `
      .evidence-page {
        min-height: 100vh;
        background: var(--bg-primary);
        padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
        color: var(--text-primary);
      }

      .module-tabs {
        display: flex;
        gap: 0.5rem;
        margin-bottom: 1.25rem;
        border-bottom: 1px solid var(--border-color);
        padding-bottom: 0.5rem;
      }

      .module-tabs a {
        padding: 0.5rem 1rem;
        text-decoration: none;
        color: var(--text-secondary);
        border-radius: 6px 6px 0 0;
      }

      .module-tabs a:hover {
        color: var(--text-primary);
      }

      .module-tabs a.active {
        color: var(--primary-color);
        font-weight: 600;
        border-bottom: 2px solid var(--primary-color);
      }

      .page-header h1 {
        margin: 0 0 0.25rem;
        font-size: var(--font-size-page-title);
        color: var(--text-primary);
      }

      .page-header p {
        margin: 0;
        color: var(--text-secondary);
      }

      .filters {
        display: flex;
        gap: 0.75rem;
        margin: 1rem 0;
        flex-wrap: wrap;
      }

      .filters select,
      .filters input {
        padding: 0.5rem 0.75rem;
        border: 1px solid var(--border-color);
        border-radius: 6px;
        min-width: 160px;
        background: var(--card-bg);
        color: var(--text-primary);
      }

      .filters select option {
        background: var(--card-bg);
        color: var(--text-primary);
      }

      :host-context([data-theme='dark']) .filters select {
        color-scheme: dark;
      }

      :host-context([data-theme='light']) .filters select {
        color-scheme: light;
      }

      .queue-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 0.875rem;
      }

      .queue-table th,
      .queue-table td {
        padding: var(--table-cell-padding-block) var(--table-cell-padding-inline);
        border-bottom: 1px solid var(--border-color);
        text-align: left;
        vertical-align: middle;
      }

      .queue-table th {
        background: var(--color-bg-surface-alt);
        color: var(--text-secondary);
        font-weight: 600;
      }

      .queue-table td {
        color: var(--text-primary);
      }

      .queue-table th:last-child,
      .queue-table td.actions {
        min-width: 26rem;
      }

      .row-highlight {
        background: color-mix(in srgb, var(--primary-color) 10%, var(--card-bg));
      }

      .actions {
        display: flex;
        gap: 0.35rem;
        flex-wrap: nowrap;
        align-items: center;
      }

      .actions button {
        padding: 0.35rem 0.6rem;
        border: 1px solid var(--border-color);
        border-radius: 4px;
        background: var(--card-bg);
        color: var(--text-primary);
        cursor: pointer;
        font-size: 0.75rem;
        white-space: nowrap;
        flex-shrink: 0;
        transition: background 0.2s ease, border-color 0.2s ease, color 0.2s ease;
      }

      .actions button:hover:not(:disabled) {
        background: var(--hover-bg);
        border-color: color-mix(in srgb, var(--success-color) 45%, var(--border-color));
        color: var(--success-color);
      }

      .actions button.danger {
        background: color-mix(in srgb, var(--danger-color) 10%, var(--card-bg));
        border-color: color-mix(in srgb, var(--danger-color) 40%, var(--border-color));
        color: var(--danger-color);
      }

      .actions button.danger:hover:not(:disabled) {
        background: color-mix(in srgb, var(--danger-color) 18%, var(--card-bg));
        border-color: var(--danger-color);
        color: var(--danger-color);
      }

      .actions button.warn {
        background: color-mix(in srgb, var(--warning-color) 10%, var(--card-bg));
        border-color: color-mix(in srgb, var(--warning-color) 40%, var(--border-color));
        color: var(--warning-color);
      }

      .actions button.warn:hover:not(:disabled) {
        background: color-mix(in srgb, var(--warning-color) 18%, var(--card-bg));
        border-color: var(--warning-color);
        color: var(--warning-color);
      }

      .actions button.view {
        background: color-mix(in srgb, var(--primary-color) 10%, var(--card-bg));
        border-color: color-mix(in srgb, var(--primary-color) 40%, var(--border-color));
        color: var(--primary-color);
      }

      .actions button.view:hover:not(:disabled) {
        background: color-mix(in srgb, var(--primary-color) 18%, var(--card-bg));
        border-color: var(--primary-color);
        color: var(--primary-color);
      }

      .actions button:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      .banner.error {
        background: color-mix(in srgb, var(--danger-color) 12%, var(--card-bg));
        color: var(--danger-color);
        border: 1px solid color-mix(in srgb, var(--danger-color) 28%, var(--border-color));
        padding: 0.75rem 1rem;
        border-radius: 6px;
        margin-bottom: 1rem;
      }

      .muted {
        color: var(--text-secondary);
      }

      .pagination {
        display: flex;
        align-items: center;
        gap: 1rem;
        margin-top: 1rem;
        color: var(--text-secondary);
      }

      .pagination button {
        padding: 0.4rem 0.75rem;
        border: 1px solid var(--border-color);
        border-radius: 6px;
        background: var(--card-bg);
        color: var(--text-primary);
        cursor: pointer;
      }

      .pagination button:hover:not(:disabled) {
        background: var(--hover-bg);
      }

      .pagination button:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      @media (max-width: 1100px) {
        .actions {
          flex-wrap: wrap;
        }
      }

      .preview-backdrop {
        position: fixed;
        inset: 0;
        background: var(--overlay-backdrop);
        z-index: var(--z-modal, 1000);
      }

      .preview-dialog {
        position: fixed;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        z-index: calc(var(--z-modal, 1000) + 1);
        width: min(960px, calc(100vw - 2rem));
        max-height: min(85vh, 900px);
        display: flex;
        flex-direction: column;
        background: var(--card-bg);
        border: 1px solid var(--border-color);
        border-radius: 12px;
        box-shadow: var(--shadow-lg);
        overflow: hidden;
      }

      .preview-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 1rem;
        padding: 1rem 1.25rem;
        border-bottom: 1px solid var(--border-color);
      }

      .preview-title h2 {
        margin: 0;
        font-size: var(--font-size-section-title);
        color: var(--text-primary);
      }

      .preview-title p {
        margin: 0.25rem 0 0;
        font-size: 0.85rem;
        color: var(--text-secondary);
      }

      .preview-header-actions {
        display: flex;
        gap: 0.5rem;
        flex-shrink: 0;
      }

      .preview-header-actions button,
      .preview-close {
        padding: 0.4rem 0.75rem;
        border: 1px solid var(--border-color);
        border-radius: 6px;
        background: var(--card-bg);
        color: var(--text-primary);
        cursor: pointer;
        font-size: 0.85rem;
      }

      .preview-header-actions button:hover:not(:disabled),
      .preview-close:hover {
        background: var(--hover-bg);
      }

      .preview-header-actions button:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      .preview-body {
        flex: 1;
        min-height: 320px;
        overflow: auto;
        padding: 1rem 1.25rem;
        background: var(--bg-primary);
      }

      .preview-frame {
        width: 100%;
        min-height: 480px;
        border: 1px solid var(--border-color);
        border-radius: 8px;
        background: var(--card-bg);
      }

      .preview-image {
        max-width: 100%;
        max-height: 70vh;
        display: block;
        margin: 0 auto;
        border-radius: 8px;
        border: 1px solid var(--border-color);
      }

      .preview-text {
        margin: 0;
        padding: 1rem;
        border-radius: 8px;
        border: 1px solid var(--border-color);
        background: var(--card-bg);
        color: var(--text-primary);
        white-space: pre-wrap;
        word-break: break-word;
        font-size: 0.85rem;
        max-height: 70vh;
        overflow: auto;
      }

      .preview-unsupported,
      .preview-error {
        color: var(--text-primary);
      }

      .preview-error {
        color: var(--danger-color);
      }
    `,
  ],
})
export class EvidenceReviewQueueComponent implements OnInit, OnDestroy {
  private readonly api = inject(EvidenceApiService);
  private readonly context = inject(ComplianceContextService);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroy$ = new Subject<void>();
  private searchDebounce?: ReturnType<typeof setTimeout>;
  private previewObjectUrl: string | null = null;

  readonly items = signal<EvidenceReviewQueueItem[]>([]);
  readonly loading = signal(false);
  readonly acting = signal(false);
  readonly error = signal<string | null>(null);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly totalCount = signal(0);
  readonly highlightEvidenceId = signal<string | null>(null);
  readonly viewRow = signal<EvidenceReviewQueueItem | null>(null);
  readonly viewLoading = signal(false);
  readonly viewError = signal<string | null>(null);
  readonly viewDownloadInFlight = signal(false);
  readonly previewMode = signal<'idle' | 'pdf' | 'image' | 'text' | 'unsupported'>('idle');
  readonly previewSafeUrl = signal<SafeResourceUrl | null>(null);
  readonly previewTextContent = signal('');

  filterStatus = '';
  filterStage = '';
  search = '';

  ngOnInit(): void {
    this.applyRouteQuery();
    this.reload();

    combineLatest([this.context.hospitalId$, this.context.frameworkId$])
      .pipe(
        debounceTime(50),
        distinctUntilChanged((prev, curr) => prev[0] === curr[0] && prev[1] === curr[1]),
        takeUntil(this.destroy$),
      )
      .subscribe(([hospitalId, frameworkId]) => {
        if (hospitalId && frameworkId) {
          this.reload();
        }
      });
  }

  ngOnDestroy(): void {
    this.revokePreviewBlob();
    this.destroy$.next();
    this.destroy$.complete();
    if (this.searchDebounce) clearTimeout(this.searchDebounce);
  }

  private applyRouteQuery(): void {
    const qp = this.route.snapshot.queryParamMap;
    const evidenceId = (qp.get('evidenceId') ?? '').trim();
    const oeId = (qp.get('objectiveElementId') ?? '').trim();
    const search = (qp.get('search') ?? '').trim();
    const status = (qp.get('status') ?? '').trim();
    if (evidenceId) this.highlightEvidenceId.set(evidenceId);
    if (search) this.search = search;
    if (status) this.filterStatus = status;
    if (oeId && !search) this.search = oeId;
  }

  reload(): void {
    const frameworkId = this.context.getFrameworkId();
    if (!frameworkId) {
      this.error.set('Select a compliance framework to view the review queue.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.api
      .getReviewQueue({
        frameworkId,
        status: this.filterStatus || undefined,
        stage: this.filterStage || undefined,
        search: this.search.trim() || undefined,
        page: this.page(),
        pageSize: this.pageSize(),
      })
      .pipe(
        takeUntil(this.destroy$),
        catchError(() => {
          this.error.set('Failed to load review queue.');
          return of({ items: [], totalCount: 0, page: 1, pageSize: 20 });
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((res) => {
        this.items.set(res.items ?? []);
        this.totalCount.set(res.totalCount ?? 0);
      });
  }

  onSearchChange(_value: string): void {
    if (this.searchDebounce) clearTimeout(this.searchDebounce);
    this.searchDebounce = setTimeout(() => {
      this.page.set(1);
      this.reload();
    }, 350);
  }

  setPage(next: number): void {
    this.page.set(next);
    this.reload();
  }

  canAct(row: EvidenceReviewQueueItem): boolean {
    return canReviewEvidenceInQueue(row.status);
  }

  formatStatus(status: string): string {
    return evidenceStatusLabel(status, 'evidence');
  }

  viewEvidence(row: EvidenceReviewQueueItem): void {
    this.viewRow.set(row);
    this.viewError.set(null);
    this.viewLoading.set(true);
    this.revokePreviewBlob();
    this.previewMode.set('idle');

    this.api
      .downloadLibraryEvidenceFile(row.evidenceId)
      .pipe(
        takeUntil(this.destroy$),
        catchError(() => {
          this.viewError.set('Unable to load evidence preview.');
          this.previewMode.set('unsupported');
          return EMPTY;
        }),
        finalize(() => this.viewLoading.set(false)),
      )
      .subscribe((res) => {
        if (res) {
          this.applyPreviewBlob(row.fileName, res.blob, res.contentType);
        }
      });
  }

  closeView(): void {
    this.revokePreviewBlob();
    this.viewRow.set(null);
    this.viewError.set(null);
    this.previewMode.set('idle');
  }

  downloadViewed(row: EvidenceReviewQueueItem): void {
    this.viewDownloadInFlight.set(true);
    this.api
      .downloadLibraryEvidenceFile(row.evidenceId)
      .pipe(
        takeUntil(this.destroy$),
        catchError(() => {
          this.snackBar.open('Download failed.', 'Dismiss', { duration: 4000 });
          return EMPTY;
        }),
        finalize(() => this.viewDownloadInFlight.set(false)),
      )
      .subscribe(({ blob }) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = row.fileName || 'evidence';
        anchor.click();
        URL.revokeObjectURL(url);
      });
  }

  private revokePreviewBlob(): void {
    if (this.previewObjectUrl) {
      URL.revokeObjectURL(this.previewObjectUrl);
      this.previewObjectUrl = null;
    }
    this.previewSafeUrl.set(null);
    this.previewTextContent.set('');
  }

  private applyPreviewBlob(fileName: string, blob: Blob, contentType: string | null): void {
    const ct = (contentType || blob.type || '').toLowerCase();
    const ext = fileName.includes('.') ? fileName.split('.').pop()!.toLowerCase() : '';

    if (ct.includes('pdf') || ext === 'pdf') {
      const raw = URL.createObjectURL(blob);
      this.previewObjectUrl = raw;
      this.previewSafeUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(raw));
      this.previewMode.set('pdf');
      return;
    }

    if (ct.startsWith('image/') || ['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg', 'bmp'].includes(ext)) {
      const raw = URL.createObjectURL(blob);
      this.previewObjectUrl = raw;
      this.previewSafeUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(raw));
      this.previewMode.set('image');
      return;
    }

    const textLike =
      ct.startsWith('text/') || ['txt', 'md', 'csv', 'json', 'xml', 'log'].includes(ext);
    if (textLike && blob.size <= 512 * 1024) {
      blob
        .text()
        .then((text) => {
          this.previewTextContent.set(text);
          this.previewMode.set('text');
        })
        .catch(() => this.previewMode.set('unsupported'));
      return;
    }

    this.previewMode.set('unsupported');
  }

  review(row: EvidenceReviewQueueItem, action: 'approve' | 'reject' | 'requestMoreEvidence'): void {
    let remark: string | null = null;
    if (action !== 'approve') {
      remark = window.prompt(
        action === 'reject' ? 'Rejection comment (required):' : 'Describe what revision is needed:',
      );
      if (!remark?.trim()) {
        this.snackBar.open('A comment is required for this action.', 'Dismiss', { duration: 4000 });
        return;
      }
    }

    this.acting.set(true);
    this.api
      .approveOrReject(row.evidenceId, {
        action,
        remark,
        rowVersion: row.rowVersion ?? null,
      })
      .pipe(
        takeUntil(this.destroy$),
        catchError(() => {
          this.snackBar.open('Review action failed.', 'Dismiss', { duration: 4000 });
          return of(null);
        }),
        finalize(() => this.acting.set(false)),
      )
      .subscribe((result) => {
        if (result) {
          this.snackBar.open(`Evidence ${this.formatStatus(result.status)}.`, 'Dismiss', { duration: 3000 });
          this.reload();
        }
      });
  }
}
