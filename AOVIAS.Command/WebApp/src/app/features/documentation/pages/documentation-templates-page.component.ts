import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { catchError, finalize, of } from 'rxjs';
import { DocumentationApiService } from '../services/documentation-api.service';
import { DOCUMENT_TYPE_LABELS, type ControlledDocumentType } from '../models/documentation.enums';
import type { DocumentTemplate } from '../models/documentation.model';

@Component({
  selector: 'app-documentation-templates-page',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/documentation-workspace.layout.scss',
  template: `
    <div class="qdms-workspace">
      <header class="page-header">
        <div>
          <h1>Document Templates</h1>
          <p class="subtitle">Built-in healthcare templates — customizable per hospital</p>
        </div>
      </header>

      @if (error()) { <div class="banner error">{{ error() }}</div> }
      @if (loading()) { <p class="muted">Loading templates…</p> }
      @else {
        <div class="kpi-grid" style="grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));">
          @for (t of templates(); track t.id) {
            <article class="panel">
              <h2 style="font-size:15px;margin-bottom:4px;">{{ t.name }}</h2>
              <p class="muted">{{ t.category }} · {{ typeLabel(t.documentType) }}</p>
              @if (t.description) { <p class="muted">{{ t.description }}</p> }
              @if (t.isBuiltIn) { <span class="badge">Built-in</span> }
            </article>
          } @empty {
            <p class="muted">No templates available.</p>
          }
        </div>
      }
    </div>
  `,
})
export class DocumentationTemplatesPageComponent implements OnInit {
  private readonly api = inject(DocumentationApiService);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly templates = signal<DocumentTemplate[]>([]);

  ngOnInit(): void {
    this.api.getTemplates().pipe(
      catchError(() => { this.error.set('Unable to load templates.'); return of([]); }),
      finalize(() => this.loading.set(false)),
    ).subscribe((list) => this.templates.set(list));
  }

  typeLabel(t: ControlledDocumentType): string {
    return DOCUMENT_TYPE_LABELS[t] ?? t;
  }
}
