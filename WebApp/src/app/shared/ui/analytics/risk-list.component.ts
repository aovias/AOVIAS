import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

export interface RiskListItem {
  label: string;
  detail?: string | null;
  severity?: 'low' | 'medium' | 'high' | 'critical' | string;
  href?: string | null;
}

/**
 * Compact risk / attention list for role-experience surfaces.
 */
@Component({
  selector: 'app-risk-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (items.length) {
      <section class="risk-list" [attr.aria-label]="title || 'Risk list'">
        @if (title) {
          <h3 class="risk-list__title">{{ title }}</h3>
        }
        <ul class="risk-list__ul">
          @for (item of items; track item.label) {
            <li class="risk-list__item" [attr.data-severity]="item.severity || 'medium'">
              <div>
                <strong>{{ item.label }}</strong>
                @if (item.detail) {
                  <p>{{ item.detail }}</p>
                }
              </div>
              @if (item.href) {
                <a [routerLink]="item.href">View</a>
              }
            </li>
          }
        </ul>
      </section>
    }
  `,
  styles: [
    `
      .risk-list__title {
        margin: 0 0 8px;
        font-size: 13px;
        font-weight: 650;
      }
      .risk-list__ul {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 8px;
      }
      .risk-list__item {
        display: flex;
        justify-content: space-between;
        gap: 12px;
        padding: 10px 12px;
        border-radius: 8px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        background: var(--color-bg-surface, #fff);
      }
      .risk-list__item p {
        margin: 4px 0 0;
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
      .risk-list__item a {
        font-size: 12px;
        font-weight: 600;
        color: var(--color-primary);
        text-decoration: none;
      }
      .risk-list__item[data-severity='critical'],
      .risk-list__item[data-severity='high'] {
        border-color: color-mix(in srgb, var(--color-danger, #b91c1c) 35%, #e5e7eb);
      }
    `,
  ],
})
export class RiskListComponent {
  @Input() title = '';
  @Input() items: RiskListItem[] = [];
}
