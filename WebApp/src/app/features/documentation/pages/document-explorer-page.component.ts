import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule, NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, finalize, of, Subject, switchMap } from 'rxjs';
import { IntelligenceApiService } from '../services/intelligence-api.service';
import type { DocumentExplorerNode, DocumentSearchResult } from '../models/intelligence.models';
import { DOCUMENT_STATUS_LABELS, DOCUMENT_TYPE_LABELS } from '../models/documentation.enums';
import type { ControlledDocumentListItem } from '../models/documentation.model';

@Component({
  selector: 'app-document-explorer-page',
  standalone: true,
  imports: [CommonModule, NgTemplateOutlet, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/documentation-workspace.layout.scss',
  template: `
    <div class="qdms-workspace explorer">
      <header class="page-header">
        <div>
          <h1>Document Explorer</h1>
          <p class="subtitle">Hierarchical navigation across controlled documentation</p>
        </div>
        <input
          type="search"
          class="search-input"
          placeholder="Enterprise search — title, sections, mappings…"
          [value]="searchQuery()"
          (input)="onSearchInput($any($event.target).value)"
        />
      </header>

      @if (error()) {
        <div class="banner error">{{ error() }}</div>
      }

      <div class="explorer-layout">
        <aside class="explorer-tree panel">
          <h2>Browse</h2>
          @if (loading()) {
            <p class="muted">Loading tree…</p>
          } @else {
            @for (node of tree(); track node.id) {
              <ng-container *ngTemplateOutlet="treeNode; context: { $implicit: node, depth: 0 }" />
            }
          }
        </aside>

        <main class="explorer-main">
          @if (searchQuery().trim().length >= 2) {
            <section class="panel">
              <h2>Search Results</h2>
              @if (searchLoading()) {
                <p class="muted">Searching…</p>
              } @else if (!searchResults().length) {
                <p class="muted">No matches for "{{ searchQuery() }}".</p>
              } @else {
                @for (r of searchResults(); track r.documentId + r.matchLocation) {
                  <a [routerLink]="r.route ? routeParts(r.route) : ['/app/documentation/authoring', r.documentId]" class="search-hit">
                    <strong>{{ r.title }}</strong>
                    <span class="muted">{{ r.documentNumber }} · {{ r.matchLocation }}</span>
                    <p>{{ r.matchContext }}</p>
                  </a>
                }
              }
            </section>
          }

          <section class="panel">
            <h2>Recent</h2>
            @if (recent().length) {
              <div class="doc-list">
                @for (doc of recent(); track doc.id) {
                  <a [routerLink]="['/app/documentation/authoring', doc.id]" class="doc-card">
                    <strong>{{ doc.title }}</strong>
                    <span>{{ typeLabel(doc.documentType) }} · {{ statusLabel(doc.status) }}</span>
                  </a>
                }
              </div>
            } @else {
              <p class="muted">No recent documents.</p>
            }
          </section>

          <section class="panel">
            <h2>Pinned / Effective</h2>
            @if (pinned().length) {
              <div class="doc-list">
                @for (doc of pinned(); track doc.id) {
                  <a [routerLink]="['/app/documentation/authoring', doc.id]" class="doc-card">
                    <strong>{{ doc.title }}</strong>
                    <span>{{ doc.department ?? 'General' }}</span>
                  </a>
                }
              </div>
            } @else {
              <p class="muted">No pinned documents.</p>
            }
          </section>
        </main>
      </div>
    </div>

    <ng-template #treeNode let-node let-depth="depth">
      @if (node.nodeType === 'document' && node.documentId) {
        <a
          [routerLink]="node.route ? routeParts(node.route) : ['/app/documentation/authoring', node.documentId]"
          class="tree-doc"
          [style.padding-left.px]="depth * 16 + 8"
        >
          {{ node.label }}
        </a>
      } @else {
        <details class="tree-folder" [style.margin-left.px]="depth * 12" open>
          <summary>{{ node.label }}</summary>
          @for (child of node.children; track child.id) {
            <ng-container *ngTemplateOutlet="treeNode; context: { $implicit: child, depth: depth + 1 }" />
          }
        </details>
      }
    </ng-template>
  `,
  styles: [`
    .search-input { padding: 8px 12px; border-radius: 6px; border: 1px solid var(--color-border-subtle, #e5e7eb); min-width: min(480px, 100%); }
    .explorer-layout { display: grid; grid-template-columns: 280px 1fr; gap: 16px; align-items: start; }
    @media (max-width: 900px) { .explorer-layout { grid-template-columns: 1fr; } }
    .explorer-tree { max-height: calc(100vh - 220px); overflow: auto; }
    .tree-folder { margin: 4px 0; font-size: 13px; }
    .tree-folder summary { cursor: pointer; font-weight: 600; padding: 4px 0; }
    .tree-doc { display: block; padding: 6px 8px; font-size: 12px; color: inherit; text-decoration: none; border-radius: 4px; }
    .tree-doc:hover { background: color-mix(in srgb, var(--color-primary) 8%, transparent); color: var(--color-primary); }
    .doc-list { display: grid; gap: 8px; }
    .doc-card { display: block; padding: 10px 12px; border: 1px solid var(--color-border-subtle, #e5e7eb); border-radius: 6px; text-decoration: none; color: inherit; font-size: 13px; }
    .doc-card span { display: block; font-size: 11px; opacity: 0.7; margin-top: 2px; }
    .search-hit { display: block; padding: 10px; border-bottom: 1px solid var(--color-border-subtle, #e5e7eb); text-decoration: none; color: inherit; }
    .search-hit p { margin: 4px 0 0; font-size: 12px; opacity: 0.8; }
    .search-hit .muted { font-size: 11px; margin-left: 8px; }
  `],
})
export class DocumentExplorerPageComponent implements OnInit {
  private readonly api = inject(IntelligenceApiService);
  private readonly search$ = new Subject<string>();

  readonly loading = signal(true);
  readonly searchLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly tree = signal<DocumentExplorerNode[]>([]);
  readonly recent = signal<ControlledDocumentListItem[]>([]);
  readonly pinned = signal<ControlledDocumentListItem[]>([]);
  readonly searchQuery = signal('');
  readonly searchResults = signal<DocumentSearchResult[]>([]);

  ngOnInit(): void {
    this.api.getExplorer().pipe(
      catchError(() => {
        this.error.set('Unable to load document explorer.');
        return of(null);
      }),
      finalize(() => this.loading.set(false)),
    ).subscribe((data) => {
      if (!data) return;
      this.tree.set(data.tree);
      this.recent.set(data.recent);
      this.pinned.set(data.pinned);
    });

    this.search$.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap((q) => {
        if (q.trim().length < 2) {
          this.searchResults.set([]);
          return of([] as DocumentSearchResult[]);
        }
        this.searchLoading.set(true);
        return this.api.search(q).pipe(
          catchError(() => of([] as DocumentSearchResult[])),
          finalize(() => this.searchLoading.set(false)),
        );
      }),
    ).subscribe((results) => this.searchResults.set(results));
  }

  onSearchInput(value: string): void {
    this.searchQuery.set(value);
    this.search$.next(value);
  }

  typeLabel(t: ControlledDocumentListItem['documentType']): string {
    return DOCUMENT_TYPE_LABELS[t] ?? t;
  }

  statusLabel(s: ControlledDocumentListItem['status']): string {
    return DOCUMENT_STATUS_LABELS[s] ?? s;
  }

  routeParts(route: string): string[] {
    return route.startsWith('/') ? route.split('/').filter(Boolean) : [route];
  }
}
