import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  OnDestroy,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { A11yModule } from '@angular/cdk/a11y';
import { Subject, catchError, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { ComplianceContextService } from '../../services/compliance-context.service';
import {
  ComplianceSearchApiService,
  type ComplianceSearchHit,
} from '../../services/compliance-search-api.service';
import { TrustHtmlPipe } from '../../layout/trust-html.pipe';
import { NavigationHistoryService } from '../engine/navigation-history.service';
import { NAV_BADGE_RESOLVER } from '../engine/providers/navigation.tokens';
import { FavoritesService } from '../favorites/favorites.service';
import { PinnedService } from '../favorites/pinned.service';
import { PersonalizationService } from '../personalization/personalization.service';
import { WorkspacePreferencesService } from '../personalization/workspace-preferences.service';
import { CommandPaletteService } from './command-palette.service';
import { SearchIndexService, type SearchGroupedResults } from './search-index.service';
import type { SearchIndexEntry } from './search-sources';

@Component({
  selector: 'app-command-palette',
  standalone: true,
  imports: [CommonModule, FormsModule, A11yModule, TrustHtmlPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (palette.isOpen()) {
      <div class="cp-root" role="presentation">
        <div class="cp-backdrop" (click)="palette.close()" aria-hidden="true"></div>
        <div
          class="cp-dialog"
          role="dialog"
          aria-modal="true"
          aria-label="Command palette"
          cdkTrapFocus
          [cdkTrapFocusAutoCapture]="true"
        >
          <label class="sr-only" for="command-palette-input">Search anything</label>
          <input
            #searchInput
            id="command-palette-input"
            class="cp-input"
            type="search"
            placeholder="Search anything…"
            [ngModel]="query()"
            (ngModelChange)="onQuery($event)"
            (keydown)="onKeydown($event)"
            autocomplete="off"
            aria-controls="command-palette-results"
            [attr.aria-activedescendant]="activeDescendant()"
          />

          <div id="command-palette-results" class="cp-body" role="listbox">
            @if (!query().trim()) {
              @if (history.recentSearches().length) {
                <section class="cp-section">
                  <h3 class="cp-section__title">Recent Searches</h3>
                  @for (s of history.recentSearches().slice(0, 6); track s) {
                    <button type="button" class="cp-row" (click)="onQuery(s)">
                      <span class="cp-row__title">{{ s }}</span>
                    </button>
                  }
                </section>
              }
              @if (suggested().length) {
                <section class="cp-section">
                  <h3 class="cp-section__title">Suggested Pages</h3>
                  @for (item of suggested(); track item.id; let i = $index) {
                    <button
                      type="button"
                      class="cp-row"
                      [class.cp-row--active]="flatIndex(i, 'suggested') === highlight()"
                      role="option"
                      [id]="'cp-opt-sug-' + i"
                      (click)="activate(item)"
                      (mouseenter)="highlight.set(flatIndex(i, 'suggested'))"
                    >
                      @if (item.icon) {
                        <span class="cp-row__icon" [innerHTML]="item.icon | trustHtml"></span>
                      }
                      <span class="cp-row__title">{{ item.title }}</span>
                    </button>
                  }
                </section>
              }
            }

            @for (group of groups(); track group.category) {
              <section class="cp-section">
                <h3 class="cp-section__title">{{ group.label }}</h3>
                @for (item of group.items; track item.id; let i = $index) {
                  <div
                    class="cp-row"
                    [class.cp-row--active]="isHighlighted(group, i)"
                    role="option"
                    [id]="optionId(group.category, i)"
                    (click)="activate(item)"
                    (mouseenter)="setHighlight(group, i)"
                    (keydown.enter)="activate(item)"
                    tabindex="-1"
                  >
                    @if (item.icon) {
                      <span class="cp-row__icon" [innerHTML]="item.icon | trustHtml"></span>
                    }
                    <span class="cp-row__text">
                      <span class="cp-row__title">{{ item.title }}</span>
                      @if (item.subtitle) {
                        <span class="cp-row__sub">{{ item.subtitle }}</span>
                      }
                    </span>
                    @if (prefs.showBadges() && item.badgeKey; as key) {
                      @if (badgeCount(key); as count) {
                        <span class="cp-row__badge">{{ count }}</span>
                      }
                    }
                    @if (item.route) {
                      <button
                        type="button"
                        class="cp-row__star"
                        [attr.aria-label]="
                          favorites.isFavorite(item.route) ? 'Unfavorite' : 'Favorite'
                        "
                        (click)="toggleFavorite(item, $event)"
                      >
                        {{ favorites.isFavorite(item.route) ? '★' : '☆' }}
                      </button>
                    }
                  </div>
                }
              </section>
            }

            @if (opsLoading()) {
              <p class="cp-hint">Searching ops…</p>
            } @else if (opsHits().length) {
              <section class="cp-section">
                <h3 class="cp-section__title">Ops / Entities</h3>
                @for (hit of opsHits(); track hit.id + hit.type; let i = $index) {
                  <button
                    type="button"
                    class="cp-row"
                    [class.cp-row--active]="opsFlatIndex(i) === highlight()"
                    (click)="activateOps(hit)"
                    (mouseenter)="highlight.set(opsFlatIndex(i))"
                  >
                    <span class="cp-row__type">{{ hit.type }}</span>
                    <span class="cp-row__title">{{ hit.title }}</span>
                  </button>
                }
              </section>
            }

            @if (query().trim() && !groups().length && !opsHits().length && !opsLoading()) {
              <p class="cp-hint">No results</p>
            }
          </div>

          <footer class="cp-footer">
            <span><kbd>↑</kbd><kbd>↓</kbd> navigate</span>
            <span><kbd>Enter</kbd> open</span>
            <span><kbd>Esc</kbd> close</span>
            @if (continueItem(); as cont) {
              <button type="button" class="cp-continue" (click)="go(cont.route)">
                Continue: {{ cont.label }}
              </button>
            }
          </footer>
        </div>
      </div>
    }
  `,
  styles: `
    .cp-root {
      position: fixed;
      inset: 0;
      z-index: 1200;
      display: flex;
      justify-content: center;
      align-items: flex-start;
      padding-top: 12vh;
    }
    .cp-backdrop {
      position: absolute;
      inset: 0;
      background: color-mix(in srgb, #0f172a 45%, transparent);
    }
    .cp-dialog {
      position: relative;
      width: min(640px, calc(100vw - 32px));
      max-height: min(72vh, 560px);
      display: flex;
      flex-direction: column;
      background: var(--color-bg-surface, var(--card-bg, #fff));
      border: 1px solid var(--color-border-subtle, var(--border-color, #e5e7eb));
      border-radius: 12px;
      box-shadow: 0 24px 48px rgba(15, 23, 42, 0.22);
      overflow: hidden;
    }
    .cp-input {
      width: 100%;
      border: 0;
      border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);
      padding: 16px 18px;
      font-size: 16px;
      background: transparent;
      color: var(--color-text-primary, #111);
      outline: none;
    }
    .cp-body {
      overflow: auto;
      flex: 1 1 auto;
      padding: 8px 0;
    }
    .cp-section {
      padding: 4px 8px 8px;
    }
    .cp-section__title {
      margin: 0;
      padding: 6px 10px;
      font-size: 11px;
      font-weight: 700;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      color: var(--color-text-muted, #6b7280);
    }
    .cp-row {
      display: flex;
      align-items: center;
      gap: 10px;
      width: 100%;
      border: 0;
      background: transparent;
      text-align: left;
      padding: 8px 10px;
      border-radius: 8px;
      cursor: pointer;
      color: inherit;
      font: inherit;
    }
    .cp-row--active,
    .cp-row:hover {
      background: color-mix(in srgb, var(--color-brand-primary) 10%, transparent);
    }
    .cp-row__icon {
      width: 18px;
      height: 18px;
      flex: 0 0 auto;
      color: var(--color-text-secondary, #667085);
    }
    :host ::ng-deep .cp-row__icon svg {
      width: 18px;
      height: 18px;
      display: block;
    }
    .cp-row__text {
      flex: 1 1 auto;
      min-width: 0;
      display: flex;
      flex-direction: column;
      gap: 2px;
    }
    .cp-row__title {
      font-size: 14px;
      font-weight: 500;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .cp-row__sub,
    .cp-row__type {
      font-size: 11px;
      color: var(--color-text-muted, #6b7280);
      text-transform: uppercase;
    }
    .cp-row__badge {
      font-size: 11px;
      font-weight: 700;
      padding: 2px 6px;
      border-radius: 999px;
      background: color-mix(in srgb, var(--color-brand-primary) 16%, transparent);
    }
    .cp-row__star {
      border: 0;
      background: transparent;
      cursor: pointer;
      color: var(--color-text-muted, #6b7280);
      font-size: 14px;
      padding: 2px 4px;
    }
    .cp-hint {
      margin: 12px;
      color: var(--color-text-muted, #6b7280);
      font-size: 13px;
    }
    .cp-footer {
      display: flex;
      flex-wrap: wrap;
      gap: 12px;
      align-items: center;
      padding: 8px 14px;
      border-top: 1px solid var(--color-border-subtle, #e5e7eb);
      font-size: 11px;
      color: var(--color-text-muted, #6b7280);
    }
    .cp-footer kbd {
      font-family: inherit;
      border: 1px solid var(--color-border-subtle, #e5e7eb);
      border-radius: 4px;
      padding: 1px 5px;
      margin-right: 2px;
    }
    .cp-continue {
      margin-left: auto;
      border: 0;
      background: transparent;
      color: var(--color-brand-primary);
      cursor: pointer;
      font-size: 12px;
      font-weight: 600;
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
})
export class CommandPaletteComponent implements OnDestroy {
  readonly palette = inject(CommandPaletteService);
  readonly history = inject(NavigationHistoryService);
  readonly favorites = inject(FavoritesService);
  readonly prefs = inject(WorkspacePreferencesService);
  private readonly index = inject(SearchIndexService);
  private readonly personalization = inject(PersonalizationService);
  private readonly pinned = inject(PinnedService);
  private readonly router = inject(Router);
  private readonly badges = inject(NAV_BADGE_RESOLVER);
  private readonly api = inject(ComplianceSearchApiService);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');
  private readonly opsQuery$ = new Subject<string>();

  readonly query = signal('');
  readonly highlight = signal(0);
  readonly groups = signal<SearchGroupedResults[]>([]);
  readonly opsHits = signal<ComplianceSearchHit[]>([]);
  readonly opsLoading = signal(false);
  readonly suggested = this.personalization.suggested;
  readonly continueItem = signal<{ label: string; route: string } | null>(null);

  private flat: SearchIndexEntry[] = [];

  constructor() {
    effect(() => {
      if (!this.palette.isOpen()) return;
      this.query.set('');
      this.refreshGroups('');
      this.continueItem.set(this.personalization.continueWhereLeftOff(this.router.url));
      queueMicrotask(() => this.searchInput()?.nativeElement?.focus());
    });

    this.opsQuery$
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap((q) => {
          const trimmed = q.trim();
          if (trimmed.length < 2) {
            this.opsLoading.set(false);
            this.opsHits.set([]);
            return of({ results: [] as ComplianceSearchHit[] });
          }
          this.opsLoading.set(true);
          return this.api
            .search({ q: trimmed, hospitalId: this.complianceContext.getHospitalId() })
            .pipe(catchError(() => of({ results: [] as ComplianceSearchHit[] })));
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((res) => {
        this.opsHits.set(res.results ?? []);
        this.opsLoading.set(false);
        this.rebuildFlat();
      });
  }

  ngOnDestroy(): void {
    this.opsQuery$.complete();
  }

  onQuery(value: string): void {
    this.query.set(value);
    this.highlight.set(0);
    this.refreshGroups(value);
    this.opsQuery$.next(value);
  }

  onKeydown(event: KeyboardEvent): void {
    const total = this.flat.length + this.opsHits().length;
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (total) this.highlight.update((h) => (h + 1) % total);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (total) this.highlight.update((h) => (h - 1 + total) % total);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      this.activateHighlighted();
    } else if (event.key === 'Escape') {
      event.preventDefault();
      this.palette.close();
    }
  }

  activate(item: SearchIndexEntry): void {
    if (this.query().trim().length >= 2) {
      this.history.trackSearch(this.query());
    }
    if (item.route) {
      this.go(item.route);
      if (item.id)
        this.personalization.trackUsage({
          id: item.id,
          route: item.route,
          moduleId: item.moduleId,
        });
    }
    this.palette.close();
  }

  activateOps(hit: ComplianceSearchHit): void {
    const route = hit.href && hit.href.startsWith('/') ? hit.href : '/app/dashboard';
    this.history.trackSearch(this.query());
    this.go(route);
    this.palette.close();
  }

  go(route: string): void {
    void this.router.navigateByUrl(route);
  }

  toggleFavorite(item: SearchIndexEntry, event: Event): void {
    event.stopPropagation();
    if (!item.route) return;
    this.favorites.toggle(item.route, item.title, item.id);
  }

  badgeCount(key: string): number | null {
    const snap = this.badges.resolve(key as 'findings' | 'issues' | 'alerts');
    return snap && snap.count > 0 ? snap.count : null;
  }

  flatIndex(i: number, _group: string): number {
    return i;
  }

  optionId(category: string, i: number): string {
    return `cp-opt-${category}-${i}`;
  }

  activeDescendant(): string | null {
    const h = this.highlight();
    if (h < this.flat.length) {
      const item = this.flat[h];
      const group = this.groups().find((g) => g.items.includes(item));
      if (!group) return null;
      return this.optionId(group.category, group.items.indexOf(item));
    }
    return null;
  }

  isHighlighted(group: SearchGroupedResults, i: number): boolean {
    let offset = 0;
    for (const g of this.groups()) {
      if (g.category === group.category) return this.highlight() === offset + i;
      offset += g.items.length;
    }
    return false;
  }

  setHighlight(group: SearchGroupedResults, i: number): void {
    let offset = 0;
    for (const g of this.groups()) {
      if (g.category === group.category) {
        this.highlight.set(offset + i);
        return;
      }
      offset += g.items.length;
    }
  }

  opsFlatIndex(i: number): number {
    return this.flat.length + i;
  }

  private activateHighlighted(): void {
    const h = this.highlight();
    if (h < this.flat.length) {
      this.activate(this.flat[h]);
      return;
    }
    const ops = this.opsHits()[h - this.flat.length];
    if (ops) this.activateOps(ops);
  }

  private refreshGroups(q: string): void {
    const favExtras: SearchIndexEntry[] = this.favorites.favorites().map((f) => ({
      id: f.id ?? f.route,
      title: f.label,
      route: f.route,
      category: 'favorites' as const,
      keywords: [f.label, f.route],
    }));
    const pinnedExtras: SearchIndexEntry[] = this.pinned.pinned().map((p) => ({
      id: `pin-${p.id}`,
      title: p.label,
      route: p.route,
      category: 'favorites' as const,
      keywords: [p.label],
    }));
    const suggested = !q.trim() ? [] : this.suggested();
    const extras = [...favExtras, ...pinnedExtras, ...suggested];
    this.groups.set(this.index.search(q, extras));
    this.rebuildFlat();
  }

  private rebuildFlat(): void {
    this.flat = this.groups().flatMap((g) => [...g.items]);
  }
}
