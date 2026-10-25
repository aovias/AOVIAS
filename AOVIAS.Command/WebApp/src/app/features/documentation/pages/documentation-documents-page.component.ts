import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { catchError, finalize, of, switchMap } from 'rxjs';
import { DocumentationApiService } from '../services/documentation-api.service';
import {
  DOCUMENT_STATUS_LABELS,
  DOCUMENT_TYPE_LABELS,
  TYPE_FILTER_MAP,
  type ControlledDocumentStatus,
  type ControlledDocumentType,
} from '../models/documentation.enums';
import type { ControlledDocumentListItem } from '../models/documentation.model';

@Component({
  selector: 'app-documentation-documents-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/documentation-workspace.layout.scss',
  template: `
    <div class="qdms-workspace">
      <header class="page-header">
        <div>
          <h1>{{ pageTitle() }}</h1>
          <p class="subtitle">Enterprise controlled document register — search, filter, export</p>
        </div>
      </header>

      <div class="toolbar">
        <input type="search" placeholder="Search documents…" [value]="search()" (input)="onSearch($event)" />
        <select (change)="onStatusFilter($event)">
          <option value="">All statuses</option>
          @for (s of statusOptions; track s) {
            <option [value]="s">{{ statusLabel(s) }}</option>
          }
        </select>
        <button type="button" class="btn-secondary" (click)="exportCsv()">Export</button>
      </div>

      @if (error()) {
        <div class="banner error">{{ error() }}</div>
      }
      @if (actionMessage()) {
        <div class="banner success">{{ actionMessage() }}</div>
      }

      @if (loading()) {
        <p class="muted">Loading documents…</p>
      } @else {
        <section class="panel">
          @if (!documents().length) {
            <p class="muted">No documents match your filters.</p>
          } @else {
            <table class="data-table">
              <thead>
                <tr>
                  <th>Document Number</th>
                  <th>Title</th>
                  <th>Type</th>
                  <th>Department</th>
                  <th>Owner</th>
                  <th>Version</th>
                  <th>Status</th>
                  <th>Effective</th>
                  <th>Review</th>
                  <th>Framework</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (doc of documents(); track doc.id) {
                  <tr>
                    <td><strong>{{ doc.documentNumber }}</strong></td>
                    <td>{{ doc.title }}</td>
                    <td>{{ typeLabel(doc.documentType) }}</td>
                    <td>{{ doc.department ?? '—' }}</td>
                    <td>{{ doc.ownerName ?? '—' }}</td>
                    <td>{{ doc.currentVersion ?? '—' }}</td>
                    <td><span class="badge">{{ statusLabel(doc.status) }}</span></td>
                    <td>{{ doc.effectiveDate | date: 'mediumDate' }}</td>
                    <td>{{ doc.reviewDate | date: 'mediumDate' }}</td>
                    <td>{{ doc.mappedFramework ?? '—' }}</td>
                    <td class="actions-cell">
                      <a [routerLink]="['/app/documentation/authoring', doc.id]">Studio</a>
                      @if (doc.status === 'draft') {
                        <button type="button" class="btn-secondary" [disabled]="busyId() === doc.id"
                          (click)="submitForReview(doc)">Submit</button>
                      }
                      @if (doc.status === 'underReview' && doc.currentVersionId) {
                        <button type="button" class="btn-secondary" [disabled]="busyId() === doc.id"
                          (click)="approve(doc)">Approve</button>
                      }
                      @if (doc.status === 'approved') {
                        <button type="button" class="btn-primary" [disabled]="busyId() === doc.id"
                          (click)="publish(doc)">Publish</button>
                      }
                      @if (canCreateVersion(doc.status)) {
                        <button type="button" class="btn-secondary" [disabled]="busyId() === doc.id"
                          (click)="createVersion(doc)">New version</button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          }
        </section>
      }
    </div>
  `,
})
export class DocumentationDocumentsPageComponent implements OnInit {
  private readonly api = inject(DocumentationApiService);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionMessage = signal<string | null>(null);
  readonly documents = signal<ControlledDocumentListItem[]>([]);
  readonly search = signal('');
  readonly pageTitle = signal('Controlled Documents');
  readonly busyId = signal<string | null>(null);
  readonly statusOptions: ControlledDocumentStatus[] = [
    'draft', 'underReview', 'approved', 'published', 'effective', 'reviewDue', 'superseded', 'archived',
  ];

  private typeFilter?: ControlledDocumentType;
  private statusFilter?: ControlledDocumentStatus;

  ngOnInit(): void {
    const data = this.route.snapshot.data;
    const section = data['section'] as string | undefined;
    this.pageTitle.set(
      section === 'policies' ? 'Policies'
        : section === 'sops' ? 'SOPs'
          : section === 'manuals' ? 'Manuals'
            : section === 'forms' ? 'Forms & Templates'
              : 'Controlled Documents',
    );
    const tf = data['typeFilter'] as string | undefined;
    if (tf) this.typeFilter = TYPE_FILTER_MAP[section ?? ''] ?? (tf as ControlledDocumentType);
    this.load();
  }

  onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
    this.load();
  }

  onStatusFilter(event: Event): void {
    const v = (event.target as HTMLSelectElement).value as ControlledDocumentStatus | '';
    this.statusFilter = v || undefined;
    this.load();
  }

  canCreateVersion(status: ControlledDocumentStatus): boolean {
    return status !== 'archived' && status !== 'superseded';
  }

  submitForReview(doc: ControlledDocumentListItem): void {
    this.runAction(doc.id, `Submitted ${doc.documentNumber} for review.`, () =>
      this.api.transition(doc.id, { targetStatus: 'underReview' }));
  }

  approve(doc: ControlledDocumentListItem): void {
    if (!doc.currentVersionId) return;
    this.runAction(doc.id, `Approved ${doc.documentNumber}.`, () =>
      this.api.approve({ documentVersionId: doc.currentVersionId!, approved: true }));
  }

  publish(doc: ControlledDocumentListItem): void {
    this.runAction(doc.id, `Published ${doc.documentNumber}.`, () =>
      this.api.publish(doc.id, { requireReAcknowledge: true }));
  }

  createVersion(doc: ControlledDocumentListItem): void {
    const summary = window.prompt('Change summary for new major version', `Revision of ${doc.currentVersion ?? 'current'}`);
    if (summary === null) return;

    this.busyId.set(doc.id);
    this.error.set(null);
    this.actionMessage.set(null);
    this.api.getDocument(doc.id).pipe(
      switchMap((detail) =>
        this.api.createVersion(doc.id, {
          content: detail.content ?? '',
          changeSummary: summary.trim() || undefined,
        })),
      catchError(() => {
        this.error.set(`Unable to create version for ${doc.documentNumber}.`);
        return of(null);
      }),
      finalize(() => this.busyId.set(null)),
    ).subscribe((result) => {
      if (!result) return;
      this.actionMessage.set(`Created version ${result.currentVersion} for ${result.documentNumber}.`);
      this.load();
    });
  }

  exportCsv(): void {
    const rows = this.documents();
    if (!rows.length) return;
    const header = ['DocumentNumber', 'Title', 'Type', 'Department', 'Status', 'Version'];
    const lines = rows.map((d) =>
      [d.documentNumber, d.title, d.documentType, d.department ?? '', d.status, d.currentVersion ?? '']
        .map((c) => `"${String(c).replace(/"/g, '""')}"`)
        .join(','),
    );
    const blob = new Blob([[header.join(','), ...lines].join('\n')], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'controlled-documents.csv';
    a.click();
    URL.revokeObjectURL(url);
  }

  typeLabel(t: ControlledDocumentType): string {
    return DOCUMENT_TYPE_LABELS[t] ?? t;
  }

  statusLabel(s: ControlledDocumentStatus): string {
    return DOCUMENT_STATUS_LABELS[s] ?? s;
  }

  private runAction(
    id: string,
    successMessage: string,
    call: () => ReturnType<DocumentationApiService['publish']>,
  ): void {
    this.busyId.set(id);
    this.error.set(null);
    this.actionMessage.set(null);
    call().pipe(
      catchError(() => {
        this.error.set('Action failed. Check document status and permissions.');
        return of(null);
      }),
      finalize(() => this.busyId.set(null)),
    ).subscribe((result) => {
      if (!result) return;
      this.actionMessage.set(successMessage);
      this.load();
    });
  }

  private load(): void {
    this.loading.set(true);
    this.api
      .getDocuments({
        type: this.typeFilter,
        status: this.statusFilter,
        search: this.search() || undefined,
      })
      .pipe(
        catchError(() => {
          this.error.set('Unable to load controlled documents.');
          return of([] as ControlledDocumentListItem[]);
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((list) => this.documents.set(list));
  }
}
