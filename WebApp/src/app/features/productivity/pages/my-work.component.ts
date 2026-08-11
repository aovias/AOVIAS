import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageBreadcrumbComponent } from '../../../shared/ui/page-breadcrumb.component';
import { RoleExperienceService } from '../../../core/services/role-experience.service';
import { FavoritesRecentService } from '../../../core/services/favorites-recent.service';
import { SavedFiltersService } from '../../../core/services/saved-filters.service';
import { BreadcrumbProvider } from '../../../core/navigation/surfaces/breadcrumb.provider';
import { KpiCardComponent } from '../../../shared/ui/analytics/kpi-card.component';

interface WorkLink {
  id: string;
  title: string;
  description: string;
  href: string;
  tone?: 'default' | 'warning' | 'critical';
}

@Component({
  selector: 'app-my-work',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PageBreadcrumbComponent, KpiCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="my-work-page role-home-shell">
      <app-page-breadcrumb [items]="breadcrumbItems" />

      <header class="page-header">
        <div>
          <h1 class="page-title">My work</h1>
          <p class="page-description">
            Personal productivity facade — tasks, reviews, CAPAs, audits, and saved filters.
            @if (packId()) {
              <span class="pack-chip">{{ packId() }}</span>
            }
          </p>
        </div>
      </header>

      @if (loading()) {
        <div class="state-banner" role="status" aria-live="polite">Loading your workspace…</div>
      } @else if (error()) {
        <div class="state-banner state-banner--error" role="alert">
          {{ error() }}
          <button type="button" class="link-btn" (click)="reload()">Retry</button>
        </div>
      }

      <section class="kpi-row" aria-label="Attention summary">
        <app-kpi-card
          label="Overdue tasks"
          [value]="overdueTasks()"
          [tone]="overdueTasks() > 0 ? 'critical' : 'default'"
          hint="Open My Tasks"
        />
        <app-kpi-card
          label="Pending reviews"
          [value]="pendingReviews()"
          [tone]="pendingReviews() > 0 ? 'warning' : 'default'"
          hint="Evidence queue"
        />
        <app-kpi-card
          label="Critical CAPAs"
          [value]="criticalCapas()"
          [tone]="criticalCapas() > 0 ? 'critical' : 'default'"
          hint="DCC filter"
        />
        <app-kpi-card
          label="Upcoming audits"
          [value]="upcomingAudits()"
          hint="Auditor sessions"
        />
      </section>

      <section class="work-grid" aria-label="My work modules">
        @for (link of workLinks(); track link.id) {
          <a class="work-card" [routerLink]="link.href" [attr.aria-label]="link.title">
            <h2>{{ link.title }}</h2>
            <p>{{ link.description }}</p>
            <span class="open-label">Open →</span>
          </a>
        }
      </section>

      <section class="split" aria-label="Bookmarks and saved filters">
        <div class="panel">
          <h2>Bookmarks</h2>
          @if (!favorites().length && !recent().length) {
            <p class="empty" role="status">No favorites or recent pages yet. Navigate the app to build this list.</p>
          } @else {
            <ul class="link-list">
              @for (f of favorites(); track f.route) {
                <li>
                  <a [routerLink]="f.route">★ {{ f.label }}</a>
                </li>
              }
              @for (r of recent().slice(0, 5); track r.route) {
                <li>
                  <a [routerLink]="r.route">◷ {{ r.label }}</a>
                </li>
              }
            </ul>
          }
        </div>

        <div class="panel">
          <h2>Saved filters</h2>
          <form class="save-form" (ngSubmit)="saveFilter()">
            <label class="sr-only" for="saved-filter-label">Filter label</label>
            <input
              id="saved-filter-label"
              type="text"
              [(ngModel)]="newFilterLabel"
              name="label"
              placeholder="Label (e.g. My overdue tasks)"
            />
            <label class="sr-only" for="saved-filter-href">Filter link</label>
            <input
              id="saved-filter-href"
              type="text"
              [(ngModel)]="newFilterHref"
              name="href"
              placeholder="/app/tasks"
            />
            <button type="submit" class="link-btn">Save</button>
          </form>
          @if (!savedFilters.items().length) {
            <p class="empty" role="status">No saved filters yet.</p>
          } @else {
            <ul class="link-list">
              @for (s of savedFilters.items(); track s.id) {
                <li class="saved-row">
                  <a [routerLink]="s.href">{{ s.label }}</a>
                  <button type="button" class="unlink" (click)="savedFilters.remove(s.id)" [attr.aria-label]="'Remove ' + s.label">
                    Remove
                  </button>
                </li>
              }
            </ul>
          }
        </div>
      </section>
    </div>
  `,
  styles: [
    `
      .my-work-page {
        padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
        background: var(--color-bg-page, var(--bg-primary));
        min-height: calc(100vh - 64px);
      }
      .page-header {
        margin-bottom: 20px;
      }
      .page-title {
        margin: 0 0 6px;
        font-size: var(--font-size-page-title);
        font-weight: 600;
        color: var(--color-text-primary, var(--text-primary));
      }
      .page-description {
        margin: 0;
        font-size: var(--font-size-subtitle);
        color: var(--color-text-secondary, var(--text-secondary));
      }
      .pack-chip {
        display: inline-block;
        margin-left: 6px;
        padding: 2px 8px;
        border-radius: 999px;
        background: color-mix(in srgb, var(--color-primary) 12%, transparent);
        color: var(--color-primary);
        font-size: 12px;
        font-weight: 600;
      }
      .state-banner {
        margin-bottom: 16px;
        padding: 12px 14px;
        border-radius: 10px;
        border: 1px solid var(--color-border-subtle, var(--border-color));
        background: var(--color-bg-surface, var(--card-bg));
        display: flex;
        gap: 12px;
        align-items: center;
      }
      .state-banner--error {
        color: var(--color-danger, #b91c1c);
      }
      .kpi-row {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
        gap: 12px;
        margin-bottom: 20px;
      }
      .work-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
        gap: 14px;
        margin-bottom: 24px;
      }
      .work-card {
        display: block;
        padding: 16px;
        border-radius: 12px;
        border: 1px solid var(--color-border-subtle, var(--border-color));
        background: var(--color-bg-surface, var(--card-bg));
        text-decoration: none;
        color: inherit;
        min-height: 120px;
      }
      .work-card h2 {
        margin: 0 0 8px;
        font-size: var(--font-size-section-title);
        color: var(--color-text-primary, var(--text-primary));
      }
      .work-card p {
        margin: 0;
        font-size: 13px;
        color: var(--color-text-secondary, var(--text-secondary));
        line-height: 1.45;
      }
      .open-label {
        display: inline-block;
        margin-top: 12px;
        font-size: 12px;
        font-weight: 650;
        color: var(--color-primary);
      }
      .split {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
        gap: 16px;
      }
      .panel {
        padding: 16px;
        border-radius: 12px;
        border: 1px solid var(--color-border-subtle, var(--border-color));
        background: var(--color-bg-surface, var(--card-bg));
      }
      .panel h2 {
        margin: 0 0 12px;
        font-size: var(--font-size-section-title);
      }
      .empty {
        margin: 0;
        font-size: 13px;
        color: var(--color-text-secondary, var(--text-secondary));
      }
      .link-list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 8px;
      }
      .link-list a {
        color: var(--color-primary);
        text-decoration: none;
        font-size: 13px;
      }
      .saved-row {
        display: flex;
        justify-content: space-between;
        gap: 8px;
        align-items: center;
      }
      .save-form {
        display: grid;
        gap: 8px;
        margin-bottom: 12px;
      }
      .save-form input {
        height: 2.1rem;
        border-radius: 8px;
        border: 1px solid var(--color-border-subtle, var(--border-color));
        padding: 0 0.65rem;
        background: var(--color-bg-page, #fff);
        color: var(--color-text-primary, #111827);
      }
      .link-btn,
      .unlink {
        border: 1px solid var(--color-border-subtle, var(--border-color));
        background: var(--color-bg-page, #fff);
        border-radius: 8px;
        padding: 6px 10px;
        font-size: 12px;
        cursor: pointer;
        color: var(--color-text-primary, #111827);
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
      @media (max-width: 1024px) {
        .my-work-page {
          padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
        }
        .kpi-row {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
      }
      @media (max-width: 640px) {
        .kpi-row,
        .work-grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class MyWorkComponent implements OnInit {
  private readonly roleExperience = inject(RoleExperienceService);
  private readonly favoritesRecent = inject(FavoritesRecentService);
  private readonly breadcrumbs = inject(BreadcrumbProvider);
  readonly savedFilters = inject(SavedFiltersService);

  readonly breadcrumbItems = (() => {
    const trail = this.breadcrumbs.forUrl('/app/me');
    return trail.length
      ? trail
      : [
          { label: 'Home', route: '/app/dashboard' },
          { label: 'My work' },
        ];
  })();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly packId = signal('quality-manager');
  readonly overdueTasks = signal(0);
  readonly pendingReviews = signal(0);
  readonly criticalCapas = signal(0);
  readonly upcomingAudits = signal(0);
  readonly showAudits = signal(false);

  readonly favorites = computed(() => this.favoritesRecent.favorites());
  readonly recent = computed(() => this.favoritesRecent.recent());

  newFilterLabel = '';
  newFilterHref = '/app/tasks';

  readonly workLinks = computed<WorkLink[]>(() => {
    const links: WorkLink[] = [
      {
        id: 'tasks',
        title: 'My Tasks',
        description: 'Open the task workspace for your assigned compliance work.',
        href: '/app/tasks',
      },
      {
        id: 'reviews',
        title: 'My Reviews',
        description: 'Evidence review queue awaiting verification.',
        href: '/app/evidence/review-queue',
        tone: 'warning',
      },
      {
        id: 'capas',
        title: 'My CAPAs',
        description: 'Deficiency Command Center — CAPA / issues filter.',
        href: '/app/audit/capa',
        tone: 'critical',
      },
    ];
    if (this.showAudits()) {
      links.push({
        id: 'audits',
        title: 'My Audits',
        description: 'Auditor sessions assigned to your role.',
        href: '/app/auditor/sessions',
      });
    }
    links.push({
      id: 'dashboard',
      title: 'Role home',
      description: 'Return to the role-aware dashboard.',
      href: '/app/dashboard',
    });
    return links;
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.roleExperience.getHome().subscribe({
      next: (home) => {
        this.applyHome(home.packId, home.attentionCounts);
        this.loading.set(false);
      },
      error: () => {
        const local = this.roleExperience.resolveLocalPack();
        this.applyHome(local.packId, local.attentionCounts);
        this.loading.set(false);
      },
    });
  }

  saveFilter(): void {
    const label = this.newFilterLabel.trim();
    const href = this.newFilterHref.trim() || '/app/tasks';
    if (!label) return;
    this.savedFilters.add({
      label,
      module: 'other',
      href: href.startsWith('/') ? href : `/${href}`,
    });
    this.newFilterLabel = '';
  }

  private applyHome(
    packId: string,
    counts: {
      overdueTasks: number;
      pendingReviews: number;
      criticalCapas: number;
      upcomingAudits: number;
    },
  ): void {
    this.packId.set(packId);
    this.overdueTasks.set(counts.overdueTasks ?? 0);
    this.pendingReviews.set(counts.pendingReviews ?? 0);
    this.criticalCapas.set(counts.criticalCapas ?? 0);
    this.upcomingAudits.set(counts.upcomingAudits ?? 0);
    this.showAudits.set(packId === 'auditor' || (counts.upcomingAudits ?? 0) > 0);
  }
}
