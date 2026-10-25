import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged, switchMap, of, catchError } from 'rxjs';
import { ComplianceContextService } from '../services/compliance-context.service';
import {
  ComplianceSearchApiService,
  ComplianceSearchHit,
} from '../services/compliance-search-api.service';

const SAVED_KEY = 'praman.ops.savedSearches';

@Component({
  selector: 'app-global-ops-search',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ops-search" [class.open]="open()">
      <label class="sr-only" for="ops-search-input">Operational search (Ctrl or Cmd + K)</label>
      <input
        #opsSearchInput
        id="ops-search-input"
        type="search"
        class="ops-search-input"
        placeholder="Search tasks, audits, CAPA… (Ctrl/⌘K)"
        [ngModel]="query()"
        (ngModelChange)="onQuery($event)"
        (focus)="open.set(true)"
        autocomplete="off"
        aria-keyshortcuts="Control+K Meta+K"
      />
      @if (open()) {
        <div class="ops-search-panel" role="listbox">
          @if (saved().length) {
            <div class="saved-row">
              <span class="label">Saved</span>
              @for (s of saved(); track s) {
                <button type="button" class="chip" (click)="applySaved(s)">{{ s }}</button>
              }
            </div>
          }
          @if (loading()) {
            <p class="hint">Searching…</p>
          } @else if (query().trim().length < 2) {
            <p class="hint">Type at least 2 characters. Enter to save the query.</p>
          } @else if (!hits().length) {
            <p class="hint">No results.</p>
          } @else {
            <ul>
              @for (hit of hits(); track hit.id + hit.type) {
                <li>
                  <a [routerLink]="routeFor(hit)" (click)="open.set(false)">
                    <span class="type">{{ hit.type }}</span>
                    <span class="title">{{ hit.title }}</span>
                    @if (hit.status) {
                      <span class="status">{{ hit.status }}</span>
                    }
                  </a>
                </li>
              }
            </ul>
          }
          @if (query().trim().length >= 2) {
            <button type="button" class="save-btn" (click)="saveCurrent()">Save search</button>
          }
        </div>
      }
    </div>
  `,
  styles: [
    `
      .ops-search {
        position: relative;
        flex: 1 1 16rem;
        max-width: 28rem;
        min-width: 12rem;
        margin: 0 0.75rem;
      }
      .ops-search-input {
        width: 100%;
        height: 2.25rem;
        border-radius: 0.5rem;
        border: 1px solid var(--border-color, #d0d5dd);
        background: var(--card-bg, #fff);
        color: var(--text-primary, #101828);
        padding: 0 0.75rem;
        font-size: 0.9rem;
      }
      .ops-search-panel {
        position: absolute;
        top: calc(100% + 0.35rem);
        left: 0;
        right: 0;
        z-index: 40;
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #d0d5dd);
        border-radius: 0.6rem;
        box-shadow: 0 8px 24px rgba(16, 24, 40, 0.12);
        padding: 0.5rem;
        max-height: 22rem;
        overflow: auto;
      }
      ul {
        list-style: none;
        margin: 0;
        padding: 0;
      }
      li a {
        display: grid;
        grid-template-columns: 5.5rem 1fr auto;
        gap: 0.5rem;
        padding: 0.45rem 0.5rem;
        text-decoration: none;
        color: inherit;
        border-radius: 0.35rem;
        font-size: 0.85rem;
      }
      li a:hover {
        background: var(--hover-bg, #f2f4f7);
      }
      .type {
        color: var(--text-secondary, #667085);
        text-transform: uppercase;
        font-size: 0.7rem;
        align-self: center;
      }
      .title {
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
      .status {
        color: var(--text-secondary, #667085);
        font-size: 0.75rem;
      }
      .hint,
      .label {
        color: var(--text-secondary, #667085);
        font-size: 0.8rem;
        margin: 0.35rem 0.25rem;
      }
      .saved-row {
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem;
        align-items: center;
        margin-bottom: 0.35rem;
      }
      .chip,
      .save-btn {
        border: 1px solid var(--border-color, #d0d5dd);
        background: var(--bg-primary, #f9fafb);
        border-radius: 999px;
        padding: 0.2rem 0.6rem;
        font-size: 0.75rem;
        cursor: pointer;
      }
      .save-btn {
        margin-top: 0.35rem;
        border-radius: 0.4rem;
        width: 100%;
      }
      .sr-only {
        position: absolute;
        width: 1px;
        height: 1px;
        padding: 0;
        margin: -1px;
        overflow: hidden;
        clip: rect(0, 0, 0, 0);
        border: 0;
      }
    `,
  ],
})
export class GlobalOpsSearchComponent {
  private readonly api = inject(ComplianceSearchApiService);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly query$ = new Subject<string>();

  readonly query = signal('');
  readonly hits = signal<ComplianceSearchHit[]>([]);
  readonly loading = signal(false);
  readonly open = signal(false);
  readonly saved = signal<string[]>(this.readSaved());

  /** Focus target for Ctrl/Cmd+K (Phase 3 Sprint 6). */
  private readonly opsSearchInput = viewChild<ElementRef<HTMLInputElement>>('opsSearchInput');

  constructor() {
    this.query$
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap((q) => {
          const trimmed = q.trim();
          if (trimmed.length < 2) {
            this.loading.set(false);
            this.hits.set([]);
            return of({ results: [] as ComplianceSearchHit[] });
          }
          this.loading.set(true);
          const hospitalId = this.complianceContext.getHospitalId();
          return this.api
            .search({ q: trimmed, hospitalId })
            .pipe(catchError(() => of({ results: [] as ComplianceSearchHit[] })));
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((res) => {
        this.hits.set(res.results ?? []);
        this.loading.set(false);
      });
  }

  onQuery(value: string): void {
    this.query.set(value);
    this.open.set(true);
    this.query$.next(value);
  }

  applySaved(value: string): void {
    this.onQuery(value);
  }

  saveCurrent(): void {
    const q = this.query().trim();
    if (q.length < 2) return;
    const next = [q, ...this.saved().filter((s) => s !== q)].slice(0, 8);
    this.saved.set(next);
    try {
      localStorage.setItem(SAVED_KEY, JSON.stringify(next));
    } catch {
      /* ignore quota */
    }
  }

  routeFor(hit: ComplianceSearchHit): string {
    if (hit.href && hit.href.startsWith('/')) {
      return hit.href;
    }
    return '/app/dashboard';
  }

  /** Ctrl/Cmd+K is owned by CommandPaletteService (NAV-4). Ops search is a palette section. */

  @HostListener('document:keydown.enter', ['$event'])
  onEnter(event: Event): void {
    if (!this.open() || !this.host.nativeElement.contains(document.activeElement)) {
      return;
    }
    event.preventDefault();
    this.saveCurrent();
  }

  @HostListener('document:click', ['$event'])
  onDocClick(event: MouseEvent): void {
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
    }
  }

  focusSearch(): void {
    const el = this.opsSearchInput()?.nativeElement;
    if (!el) return;
    this.open.set(true);
    el.focus();
    el.select();
  }

  private readSaved(): string[] {
    try {
      const raw = localStorage.getItem(SAVED_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed.filter((x) => typeof x === 'string').slice(0, 8) : [];
    } catch {
      return [];
    }
  }
}
