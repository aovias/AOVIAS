import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

export interface PageBreadcrumbItem {
  label: string;
  route?: string | null;
}

/**
 * Lightweight breadcrumb trail for feature landings (Phase 3 Sprint 6).
 * Prefer this over ad-hoc admin-nav crumbs for operational modules.
 */
@Component({
  selector: 'app-page-breadcrumb',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="page-breadcrumb" [attr.aria-label]="ariaLabel">
      <ol>
        @for (crumb of items; track crumb.label + (crumb.route ?? ''); let last = $last) {
          <li>
            @if (!last && crumb.route) {
              <a [routerLink]="crumb.route">{{ crumb.label }}</a>
            } @else {
              <span [attr.aria-current]="last ? 'page' : null">{{ crumb.label }}</span>
            }
          </li>
        }
      </ol>
    </nav>
  `,
  styles: [
    `
      .page-breadcrumb {
        margin-bottom: 12px;
      }
      ol {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem;
        align-items: center;
        font-size: 12px;
        color: var(--color-text-secondary, var(--text-secondary, #6b7280));
      }
      li {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
      }
      li:not(:last-child)::after {
        content: '/';
        color: var(--color-text-muted, #9ca3af);
        margin-left: 0.15rem;
      }
      a {
        color: var(--color-primary);
        text-decoration: none;
      }
      a:hover {
        text-decoration: underline;
      }
      span[aria-current='page'] {
        color: var(--color-text-primary, var(--text-primary, #111827));
        font-weight: 600;
      }
    `,
  ],
})
export class PageBreadcrumbComponent {
  @Input({ required: true }) items: PageBreadcrumbItem[] = [];
  @Input() ariaLabel = 'Breadcrumb';
}
