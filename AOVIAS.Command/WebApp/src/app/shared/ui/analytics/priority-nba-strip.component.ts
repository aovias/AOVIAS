import { ChangeDetectionStrategy, Component, Input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

export interface PriorityNbaItem {
  title: string;
  rationale?: string | null;
  href?: string | null;
  severity?: string | null;
}

/**
 * Next-Best-Action priority strip for role-aware home dashboards.
 */
@Component({
  selector: 'app-priority-nba-strip',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading) {
      <section class="nba-strip nba-strip--state" role="status" aria-live="polite" aria-label="Loading priority actions">
        <p class="nba-strip__empty">Loading priority actions…</p>
      </section>
    } @else if (error) {
      <section class="nba-strip nba-strip--state nba-strip--error" role="alert" aria-label="Priority actions error">
        <p class="nba-strip__empty">{{ error }}</p>
      </section>
    } @else if (!items.length) {
      @if (showEmpty) {
        <section class="nba-strip nba-strip--state" role="status" aria-label="No priority actions">
          <header class="nba-strip__header">
            <h2 class="nba-strip__title">{{ title }}</h2>
          </header>
          <p class="nba-strip__empty">{{ emptyMessage }}</p>
        </section>
      }
    } @else {
      <section class="nba-strip" [attr.aria-label]="ariaLabel || title">
        <header class="nba-strip__header">
          <h2 class="nba-strip__title" id="nba-strip-title">{{ title }}</h2>
          @if (subtitle) {
            <p class="nba-strip__subtitle">{{ subtitle }}</p>
          }
        </header>
        <ul class="nba-strip__list" [attr.aria-labelledby]="'nba-strip-title'">
          @for (item of items; track item.title + (item.href ?? '')) {
            <li class="nba-strip__item" [attr.data-severity]="normalizeSeverity(item.severity)">
              <div class="nba-strip__body">
                <span class="nba-strip__severity" [attr.aria-label]="'Severity ' + normalizeSeverity(item.severity)">
                  {{ normalizeSeverity(item.severity) }}
                </span>
                <strong class="nba-strip__item-title">{{ item.title }}</strong>
                @if (item.rationale) {
                  <p class="nba-strip__rationale">{{ item.rationale }}</p>
                }
              </div>
              @if (item.href) {
                <a
                  class="nba-strip__link"
                  [routerLink]="routePath(item.href)"
                  [queryParams]="routeQuery(item.href)"
                  [attr.aria-label]="'Open priority action: ' + item.title"
                  (click)="itemActivate.emit(item)"
                >
                  Open
                </a>
              }
            </li>
          }
        </ul>
      </section>
    }
  `,
  styles: [
    `
      .nba-strip {
        margin-bottom: 20px;
        padding: 16px 18px;
        border-radius: 12px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        background: linear-gradient(
          135deg,
          color-mix(in srgb, var(--color-primary) 6%, #fff),
          #fff 55%
        );
      }
      .nba-strip__header {
        margin-bottom: 12px;
      }
      .nba-strip__title {
        margin: 0;
        font-size: 15px;
        font-weight: 650;
        color: var(--color-text-primary, #111827);
      }
      .nba-strip__subtitle {
        margin: 4px 0 0;
        font-size: var(--font-size-caption);
        color: var(--color-text-secondary, #6b7280);
      }
      .nba-strip__list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 10px;
      }
      .nba-strip__item {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: 12px;
        padding: 10px 12px;
        border-radius: 8px;
        background: rgba(255, 255, 255, 0.85);
        border: 1px solid var(--color-border-subtle, #e5e7eb);
      }
      .nba-strip__severity {
        display: inline-block;
        font-size: 10px;
        font-weight: 700;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        color: var(--color-text-secondary, #6b7280);
        margin-bottom: 4px;
      }
      .nba-strip__item[data-severity='Critical'] .nba-strip__severity {
        color: var(--color-danger, #b91c1c);
      }
      .nba-strip__item[data-severity='High'] .nba-strip__severity {
        color: var(--color-warning, #b45309);
      }
      .nba-strip__item-title {
        display: block;
        font-size: 13px;
        color: var(--color-text-primary, #111827);
      }
      .nba-strip__rationale {
        margin: 4px 0 0;
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
      .nba-strip__link {
        flex-shrink: 0;
        font-size: 12px;
        font-weight: 600;
        color: var(--color-primary);
        text-decoration: none;
        padding-top: 2px;
      }
      .nba-strip__link:hover {
        text-decoration: underline;
      }
      .nba-strip--state {
        background: var(--color-bg-surface, #fff);
      }
      .nba-strip--error {
        border-color: color-mix(in srgb, var(--color-danger, #b91c1c) 35%, transparent);
      }
      .nba-strip__empty {
        margin: 0;
        font-size: 13px;
        color: var(--color-text-secondary, #6b7280);
      }
      @media (max-width: 1024px) {
        .nba-strip {
          padding: 14px;
        }
        .nba-strip__item {
          flex-direction: column;
          align-items: stretch;
        }
        .nba-strip__link {
          align-self: flex-start;
        }
      }
    `,
  ],
})
export class PriorityNbaStripComponent {
  @Input() title = 'Priority next best actions';
  @Input() subtitle = '';
  @Input() items: PriorityNbaItem[] = [];
  @Input() loading = false;
  @Input() error: string | null = null;
  @Input() showEmpty = true;
  @Input() emptyMessage = 'No priority actions right now.';
  @Input() ariaLabel = '';
  readonly itemActivate = output<PriorityNbaItem>();

  normalizeSeverity(severity?: string | null): string {
    if (!severity) return 'Medium';
    const t = severity.trim();
    return t.charAt(0).toUpperCase() + t.slice(1);
  }

  routePath(href: string): string {
    return href.split('?')[0] || href;
  }

  routeQuery(href: string): Record<string, string> | undefined {
    const q = href.split('?')[1];
    if (!q) return undefined;
    const params: Record<string, string> = {};
    for (const part of q.split('&')) {
      const [k, v] = part.split('=');
      if (k) params[decodeURIComponent(k)] = decodeURIComponent(v ?? '');
    }
    return params;
  }
}
