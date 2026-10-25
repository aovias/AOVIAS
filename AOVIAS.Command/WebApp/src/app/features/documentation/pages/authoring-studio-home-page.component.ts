import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import { AuthoringApiService } from '../services/authoring-api.service';
import { DocumentationApiService } from '../services/documentation-api.service';
import type { ControlledDocumentListItem, DocumentTemplate } from '../models/documentation.model';
import type { ControlledDocumentType } from '../models/documentation.enums';

@Component({
  selector: 'app-authoring-studio-home-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/documentation-workspace.layout.scss',
  template: `
    <div class="qdms-workspace">
      <header class="page-header">
        <div>
          <h1>Authoring Studio</h1>
          <p class="subtitle">Create and maintain controlled healthcare documentation — structured, compliant, audit-ready</p>
        </div>
      </header>

      <section class="panel" style="margin-bottom:16px;">
        <h2>Start New Document</h2>
        <div class="toolbar">
          <input type="text" placeholder="Document title" [value]="newTitle()" (input)="newTitle.set(($any($event.target)).value)" />
          <select (change)="onTypeChange($event)">
            <option value="sop">SOP</option>
            <option value="policy">Policy</option>
            <option value="manual">Manual</option>
            <option value="form">Form</option>
            <option value="protocol">Protocol</option>
          </select>
          <button type="button" class="btn-primary" [disabled]="creating() || !newTitle().trim()" (click)="createNew()">
            {{ creating() ? 'Creating…' : 'Open in Studio' }}
          </button>
        </div>
        @if (createError()) { <p class="banner error">{{ createError() }}</p> }
      </section>

      <section class="panel" style="margin-bottom:16px;">
        <h2>Templates</h2>
        <div class="kpi-grid" style="grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));">
          @for (t of templates(); track t.id) {
            <button type="button" class="panel" style="text-align:left;cursor:pointer;" (click)="createFromTemplate(t)">
              <strong>{{ t.name }}</strong>
              <p class="muted">{{ t.category }}</p>
            </button>
          }
        </div>
      </section>

      <section class="panel">
        <h2>Continue Editing</h2>
        @if (loading()) { <p class="muted">Loading documents…</p> }
        @else if (!documents().length) { <p class="muted">No draft documents yet.</p> }
        @else {
          <table class="data-table">
            <thead><tr><th>Number</th><th>Title</th><th>Type</th><th>Status</th><th></th></tr></thead>
            <tbody>
              @for (d of documents(); track d.id) {
                <tr>
                  <td>{{ d.documentNumber }}</td>
                  <td>{{ d.title }}</td>
                  <td>{{ d.documentType }}</td>
                  <td><span class="badge">{{ d.status }}</span></td>
                  <td><a [routerLink]="['/app/documentation/authoring', d.id]">Open Studio</a></td>
                </tr>
              }
            </tbody>
          </table>
        }
      </section>
    </div>
  `,
})
export class AuthoringStudioHomePageComponent implements OnInit {
  private readonly authoringApi = inject(AuthoringApiService);
  private readonly docsApi = inject(DocumentationApiService);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly creating = signal(false);
  readonly createError = signal<string | null>(null);
  readonly newTitle = signal('');
  readonly newType = signal<ControlledDocumentType>('sop');
  readonly templates = signal<DocumentTemplate[]>([]);
  readonly documents = signal<ControlledDocumentListItem[]>([]);

  ngOnInit(): void {
    this.docsApi.getDocuments({ status: 'draft' }).pipe(
      catchError(() => of([])),
      finalize(() => this.loading.set(false)),
    ).subscribe((list) => this.documents.set(list));

    this.authoringApi.getTemplates().pipe(catchError(() => of([]))).subscribe((t) => this.templates.set(t));
  }

  onTypeChange(event: Event): void {
    this.newType.set((event.target as HTMLSelectElement).value as ControlledDocumentType);
  }

  createNew(): void {
    this.creating.set(true);
    this.createError.set(null);
    this.authoringApi.createAuthoring({
      title: this.newTitle().trim(),
      documentType: this.newType(),
    }).pipe(
      catchError(() => { this.createError.set('Unable to create document.'); return of(null); }),
      finalize(() => this.creating.set(false)),
    ).subscribe((session) => {
      if (session) void this.router.navigate(['/app/documentation/authoring', session.documentId]);
    });
  }

  createFromTemplate(template: DocumentTemplate): void {
    this.creating.set(true);
    this.authoringApi.createAuthoring({
      title: template.name,
      documentType: template.documentType,
      templateId: template.id,
    }).pipe(
      catchError(() => { this.createError.set('Unable to create from template.'); return of(null); }),
      finalize(() => this.creating.set(false)),
    ).subscribe((session) => {
      if (session) void this.router.navigate(['/app/documentation/authoring', session.documentId]);
    });
  }
}
