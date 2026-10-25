import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { PageBreadcrumbComponent } from '../../../shared/ui/page-breadcrumb.component';
import { RoleExperienceService } from '../../../core/services/role-experience.service';
import { BreadcrumbProvider } from '../../../core/navigation/surfaces/breadcrumb.provider';
import {
  RoleReportCatalogItem,
  filterReportCatalogByPack,
  normalizePackId,
} from '../models/role-report-catalog';

@Component({
  selector: 'app-reports-landing',
  standalone: true,
  imports: [CommonModule, RouterLink, PageBreadcrumbComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="reports-landing-page role-home-shell">
      <app-page-breadcrumb [items]="breadcrumbItems" />

      <div class="page-header">
        <div class="header-content">
          <h1 class="page-title">Reports</h1>
          <p class="page-description">
            Role-filtered catalog for your experience pack
            @if (packId()) {
              <span class="pack-chip" [attr.aria-label]="'Role pack ' + packId()">{{ packId() }}</span>
            }
            — compliance, audit, tasks, evidence, and readiness.
          </p>
        </div>
        <div class="header-actions" role="toolbar" aria-label="Report export actions">
          <button type="button" class="action-btn" (click)="exportCatalogCsv()" aria-label="Export catalog as CSV">
            Export CSV
          </button>
          <button type="button" class="action-btn action-btn--ghost" (click)="printCatalog()" aria-label="Print report catalog">
            Print
          </button>
        </div>
      </div>

      @if (loading()) {
        <div class="state-banner" role="status" aria-live="polite">Loading report catalog…</div>
      } @else if (error()) {
        <div class="state-banner state-banner--error" role="alert">
          {{ error() }}
          <button type="button" class="action-btn" (click)="reload()">Retry</button>
        </div>
      } @else if (!categories().length) {
        <div class="state-banner" role="status">No reports available for this role pack.</div>
      } @else {
        <div class="report-categories" aria-label="Report categories">
          @for (category of categories(); track category.id) {
            <a
              class="category-card"
              [style.border-left-color]="category.color"
              [routerLink]="category.route"
              [attr.aria-label]="category.title + ', ' + category.reportCount + ' reports'"
            >
              <div class="card-icon" [style.color]="category.color" aria-hidden="true">
                <div [innerHTML]="category.icon"></div>
              </div>
              <div class="card-content">
                <h3 class="card-title">{{ category.title }}</h3>
                <p class="card-description">{{ category.description }}</p>
                <div class="card-footer">
                  <span class="report-count">{{ category.reportCount }} reports</span>
                  <svg class="arrow-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true">
                    <path d="M5 12h14M12 5l7 7-7 7" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  </svg>
                </div>
              </div>
            </a>
          }
        </div>
      }
    </div>
  `,
  styles: [
    `
      .reports-landing-page {
        padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
        background: var(--color-bg-page, var(--bg-primary));
        min-height: calc(100vh - 64px);
      }
      .page-header {
        display: flex;
        flex-wrap: wrap;
        gap: 16px;
        justify-content: space-between;
        align-items: flex-start;
        margin-bottom: 28px;
      }
      .header-content {
        max-width: 720px;
      }
      .page-title {
        font-size: var(--font-size-page-title);
        font-weight: 600;
        color: var(--color-text-primary, var(--text-primary));
        margin: 0 0 8px;
      }
      .page-description {
        font-size: var(--font-size-subtitle);
        color: var(--color-text-secondary, var(--text-secondary));
        margin: 0;
        line-height: 1.6;
      }
      .pack-chip {
        display: inline-block;
        margin: 0 4px;
        padding: 2px 8px;
        border-radius: 999px;
        background: color-mix(in srgb, var(--color-primary) 12%, transparent);
        color: var(--color-primary);
        font-size: 12px;
        font-weight: 600;
      }
      .header-actions {
        display: flex;
        gap: 8px;
      }
      .action-btn {
        padding: 8px 14px;
        border-radius: 8px;
        border: 1px solid var(--color-primary);
        background: var(--color-primary);
        color: #fff;
        font-size: 13px;
        font-weight: 600;
        cursor: pointer;
      }
      .action-btn--ghost {
        background: transparent;
        color: var(--color-primary);
      }
      .state-banner {
        padding: 16px 18px;
        border-radius: 10px;
        border: 1px solid var(--color-border-subtle, var(--border-color));
        background: var(--color-bg-surface, var(--card-bg));
        color: var(--color-text-secondary, var(--text-secondary));
        display: flex;
        gap: 12px;
        align-items: center;
      }
      .state-banner--error {
        border-color: color-mix(in srgb, var(--color-danger, #b91c1c) 40%, transparent);
        color: var(--color-danger, #b91c1c);
      }
      .report-categories {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
        gap: 20px;
      }
      .category-card {
        background: var(--color-bg-surface, var(--card-bg));
        border: 1px solid var(--color-border-subtle, var(--border-color));
        border-left: 4px solid;
        border-radius: 10px;
        padding: 20px;
        display: flex;
        gap: 16px;
        text-decoration: none;
        color: inherit;
        transition: transform 0.15s ease, box-shadow 0.15s ease;
      }
      .category-card:hover {
        transform: translateY(-2px);
        box-shadow: var(--shadow-md, 0 8px 20px rgba(16, 24, 40, 0.08));
      }
      .card-icon {
        width: 52px;
        height: 52px;
        display: flex;
        align-items: center;
        justify-content: center;
        background: var(--color-bg-page, var(--bg-primary));
        border-radius: 12px;
        flex-shrink: 0;
      }
      .card-content {
        flex: 1;
        display: flex;
        flex-direction: column;
        gap: 8px;
      }
      .card-title {
        font-size: 17px;
        font-weight: 600;
        margin: 0;
        color: var(--color-text-primary, var(--text-primary));
      }
      .card-description {
        font-size: 13px;
        color: var(--color-text-secondary, var(--text-secondary));
        margin: 0;
        line-height: 1.5;
        flex: 1;
      }
      .card-footer {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding-top: 8px;
        border-top: 1px solid var(--color-border-subtle, var(--border-color));
      }
      .report-count {
        font-size: 11px;
        font-weight: 650;
        color: var(--color-text-muted, var(--text-secondary));
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }
      .arrow-icon {
        color: var(--color-text-muted, var(--text-secondary));
      }
      @media (max-width: 1024px) {
        .reports-landing-page {
          padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
        }
        .report-categories {
          grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
        }
      }
      @media (max-width: 768px) {
        .report-categories {
          grid-template-columns: 1fr;
        }
        .page-header {
          flex-direction: column;
        }
      }
      @media print {
        .header-actions,
        app-page-breadcrumb {
          display: none !important;
        }
        .category-card {
          break-inside: avoid;
          box-shadow: none;
        }
      }
    `,
  ],
})
export class ReportsLandingComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly roleExperience = inject(RoleExperienceService);
  private readonly breadcrumbs = inject(BreadcrumbProvider);

  readonly breadcrumbItems = (() => {
    const trail = this.breadcrumbs.forUrl('/app/reports/compliance');
    if (trail.length >= 1) {
      return [
        { label: 'Home', route: '/app/dashboard' },
        { label: trail[0].label, route: trail[0].route },
      ];
    }
    return [
      { label: 'Home', route: '/app/dashboard' },
      { label: 'Reports' },
    ];
  })();

  readonly categories = signal<RoleReportCatalogItem[]>([]);
  readonly packId = signal<string>('quality-manager');
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.roleExperience.getHome().subscribe({
      next: (home) => {
        const pack = normalizePackId(home.packId);
        this.packId.set(pack);
        this.categories.set(filterReportCatalogByPack(pack));
        this.loading.set(false);
      },
      error: () => {
        const pack = normalizePackId(this.roleExperience.resolveLocalPack().packId);
        this.packId.set(pack);
        this.categories.set(filterReportCatalogByPack(pack));
        this.loading.set(false);
        this.error.set(null);
      },
    });
  }

  exportCatalogCsv(): void {
    const rows = [
      ['id', 'title', 'route', 'reportCount', 'packId'].join(','),
      ...this.categories().map((c) =>
        [c.id, csvEscape(c.title), c.route, String(c.reportCount), this.packId()].join(','),
      ),
    ];
    const blob = new Blob([rows.join('\n')], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `report-catalog-${this.packId()}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  printCatalog(): void {
    window.print();
  }

  navigateToCategory(route: string): void {
    void this.router.navigateByUrl(route);
  }
}

function csvEscape(value: string): string {
  if (value.includes(',') || value.includes('"')) {
    return `"${value.replace(/"/g, '""')}"`;
  }
  return value;
}
