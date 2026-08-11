import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FrameworkCatalogItem,
  FrameworksApiService,
  FrameworkStructureNode,
} from '../services/frameworks-api.service';

@Component({
  selector: 'app-framework-navigator',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="fw-page">
      <header class="fw-header">
        <div>
          <h1>Framework catalog</h1>
          <p>Publish, activate, retire, or archive framework versions. Drill into structure for chapters → standards → OEs.</p>
        </div>
        <label class="fw-toggle">
          <input type="checkbox" [checked]="includeInactive()" (change)="toggleInactive($event)" />
          Include inactive
        </label>
      </header>

      @if (error()) {
        <div class="fw-banner fw-banner--error" role="alert">{{ error() }}</div>
      }
      @if (message()) {
        <div class="fw-banner fw-banner--ok" role="status">{{ message() }}</div>
      }

      @if (loading()) {
        <p class="fw-muted">Loading catalog…</p>
      } @else if (!rows().length) {
        <p class="fw-muted">No frameworks returned.</p>
      } @else {
        <div class="fw-table-wrap">
          <table class="fw-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Version</th>
                <th>Status</th>
                <th>Code</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (row of rows(); track trackId(row)) {
                <tr [class.fw-row--selected]="selectedId() === trackId(row)">
                  <td>
                    <button type="button" class="fw-link" (click)="select(row)">{{ row.name }}</button>
                  </td>
                  <td>{{ row.version || '—' }}</td>
                  <td><span class="fw-status" [attr.data-status]="(row.status || '').toLowerCase()">{{ row.status || '—' }}</span></td>
                  <td>{{ row.code || '—' }}</td>
                  <td class="fw-actions">
                    <button type="button" (click)="run('publish', row)" [disabled]="busy()">Publish</button>
                    <button type="button" (click)="run('activate', row)" [disabled]="busy()">Activate</button>
                    <button type="button" (click)="run('retire', row)" [disabled]="busy()">Retire</button>
                    <button type="button" (click)="run('archive', row)" [disabled]="busy()">Archive</button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }

      @if (selectedId()) {
        <aside class="fw-structure">
          <h2>Structure</h2>
          @if (structureLoading()) {
            <p class="fw-muted">Loading structure…</p>
          } @else if (structureError()) {
            <p class="fw-banner fw-banner--error">{{ structureError() }}</p>
          } @else if (structure()) {
            <pre class="fw-pre">{{ structurePreview() }}</pre>
          }
        </aside>
      }
    </section>
  `,
  styles: [
    `
      .fw-page { padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end); display: grid; gap: 20px; }
      .fw-header { display: flex; justify-content: space-between; gap: 16px; align-items: flex-start; }
      h1 { margin: 0 0 6px; font-size: var(--font-size-page-title); }
      .fw-header p { margin: 0; color: var(--text-secondary, #64748b); max-width: 56ch; }
      .fw-toggle { display: flex; align-items: center; gap: 8px; font-size: 0.9rem; }
      .fw-table-wrap { overflow: auto; border: 1px solid var(--border-color, #e2e8f0); border-radius: 10px; }
      .fw-table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
      .fw-table th, .fw-table td { padding: 10px 12px; text-align: left; border-bottom: 1px solid var(--border-color, #e2e8f0); }
      .fw-table th { background: var(--surface-2, #f8fafc); font-weight: 600; }
      .fw-row--selected { background: color-mix(in srgb, var(--color-primary) 6%, transparent); }
      .fw-link { background: none; border: none; color: var(--primary-color, var(--color-primary)); cursor: pointer; font: inherit; padding: 0; }
      .fw-actions { display: flex; flex-wrap: wrap; gap: 6px; }
      .fw-actions button { border: 1px solid var(--border-color, #cbd5e1); background: #fff; border-radius: 6px; padding: 4px 8px; cursor: pointer; font-size: 0.8rem; }
      .fw-actions button:disabled { opacity: 0.5; cursor: not-allowed; }
      .fw-status { display: inline-block; padding: 2px 8px; border-radius: 999px; background: #e2e8f0; font-size: 0.75rem; font-weight: 600; }
      .fw-status[data-status='active'] { background: #dcfce7; color: #166534; }
      .fw-status[data-status='archived'] { background: #f1f5f9; color: #475569; }
      .fw-status[data-status='retired'] { background: #ffedd5; color: #9a3412; }
      .fw-status[data-status='published'] { background: var(--color-primary-100); color: var(--color-primary-active); }
      .fw-banner { padding: 10px 12px; border-radius: 8px; }
      .fw-banner--error { background: #fef2f2; color: #991b1b; }
      .fw-banner--ok { background: #f0fdf4; color: #166534; }
      .fw-muted { color: var(--text-secondary, #64748b); }
      .fw-structure { border-top: 1px solid var(--border-color, #e2e8f0); padding-top: 12px; }
      .fw-pre { margin: 0; white-space: pre-wrap; font-size: 0.8rem; background: var(--surface-2, #f8fafc); padding: 12px; border-radius: 8px; max-height: 360px; overflow: auto; }
    `,
  ],
})
export class FrameworkNavigatorComponent {
  private readonly api = inject(FrameworksApiService);

  readonly includeInactive = signal(true);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly rows = signal<FrameworkCatalogItem[]>([]);
  readonly selectedId = signal<string | null>(null);
  readonly structure = signal<FrameworkStructureNode | null>(null);
  readonly structureLoading = signal(false);
  readonly structureError = signal<string | null>(null);

  readonly structurePreview = computed(() => {
    const s = this.structure();
    return s ? JSON.stringify(s, null, 2) : '';
  });

  constructor() {
    this.reload();
  }

  trackId(row: FrameworkCatalogItem): string {
    return row.frameworkId || row.id || '';
  }

  toggleInactive(ev: Event): void {
    const checked = (ev.target as HTMLInputElement).checked;
    this.includeInactive.set(checked);
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listCatalog(this.includeInactive()).subscribe({
      next: (rows) => {
        this.rows.set(Array.isArray(rows) ? rows : []);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.error?.message || err?.message || 'Failed to load catalog');
      },
    });
  }

  select(row: FrameworkCatalogItem): void {
    const id = this.trackId(row);
    if (!id) return;
    this.selectedId.set(id);
    this.structure.set(null);
    this.structureError.set(null);
    this.structureLoading.set(true);
    this.api.getStructure(id).subscribe({
      next: (s) => {
        this.structure.set(s);
        this.structureLoading.set(false);
      },
      error: (err) => {
        this.structureLoading.set(false);
        this.structureError.set(err?.error?.error?.message || err?.message || 'Failed to load structure');
      },
    });
  }

  run(action: 'publish' | 'activate' | 'retire' | 'archive', row: FrameworkCatalogItem): void {
    const id = this.trackId(row);
    if (!id) return;
    this.busy.set(true);
    this.message.set(null);
    this.error.set(null);
    const call =
      action === 'publish'
        ? this.api.publish(id)
        : action === 'activate'
          ? this.api.activate(id)
          : action === 'retire'
            ? this.api.retire(id)
            : this.api.archive(id);

    call.subscribe({
      next: () => {
        this.busy.set(false);
        this.message.set(`${action} succeeded for ${row.name} ${row.version || ''}`.trim());
        this.reload();
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err?.error?.error?.message || err?.message || `${action} failed`);
      },
    });
  }
}
