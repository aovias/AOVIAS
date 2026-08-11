import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';

export interface AdminFilterOption {
  /** Stable value emitted on selection. */
  value: string;
  label: string;
  /** Optional count badge, e.g. number of matching records. */
  count?: number;
}

export interface AdminFilterDefinition {
  /** Stable key identifying the filter within a tab. */
  key: string;
  label: string;
  options: AdminFilterOption[];
  /** Label used for the "no filter applied" option. */
  allLabel?: string;
  icon?: string;
}

/** Current selection, keyed by `AdminFilterDefinition.key`. `null` means unfiltered. */
export type AdminFilterSelection = Record<string, string | null>;

export interface AdminFilterChange {
  key: string;
  value: string | null;
  selection: AdminFilterSelection;
}

/**
 * Shared Administration filter bar.
 *
 * Declarative: a tab supplies `filters` and the current `selection`, and receives a typed
 * `filterChange` carrying both the individual change and the resulting full selection.
 * Keeping selection state with the caller means filters survive a tab's own reloads.
 */
@Component({
  selector: 'app-admin-filter-bar',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatMenuModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="admin-filter-bar" role="group" [attr.aria-label]="ariaLabel">
      @for (filter of filters; track filter.key) {
        <button
          type="button"
          mat-stroked-button
          class="admin-filter-bar__trigger"
          [class.admin-filter-bar__trigger--active]="isActive(filter.key)"
          [matMenuTriggerFor]="menu"
          [attr.aria-label]="filter.label + ': ' + activeLabel(filter)"
        >
          @if (filter.icon) {
            <mat-icon aria-hidden="true">{{ filter.icon }}</mat-icon>
          }
          <span class="admin-filter-bar__label">{{ filter.label }}</span>
          <span class="admin-filter-bar__value">{{ activeLabel(filter) }}</span>
          <mat-icon class="admin-filter-bar__chevron" aria-hidden="true">expand_more</mat-icon>
        </button>

        <mat-menu #menu="matMenu">
          <button type="button" mat-menu-item (click)="select(filter.key, null)">
            {{ filter.allLabel || 'All' }}
          </button>
          @for (option of filter.options; track option.value) {
            <button type="button" mat-menu-item (click)="select(filter.key, option.value)">
              <span>{{ option.label }}</span>
              @if (option.count !== undefined) {
                <span class="admin-filter-bar__count">{{ option.count }}</span>
              }
            </button>
          }
        </mat-menu>
      }

      @if (showClear && hasActiveFilter()) {
        <button type="button" mat-button class="admin-filter-bar__clear" (click)="clear()">
          <mat-icon aria-hidden="true">close</mat-icon>
          Clear filters
        </button>
      }
    </div>
  `,
  styles: [
    `
      .admin-filter-bar {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 8px;
      }
      .admin-filter-bar__trigger {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        font-size: 13px;
      }
      .admin-filter-bar__trigger--active {
        border-color: var(--color-primary);
        color: var(--color-primary);
      }
      .admin-filter-bar__label {
        color: var(--color-text-secondary, #6b7280);
      }
      .admin-filter-bar__trigger--active .admin-filter-bar__label {
        color: inherit;
      }
      .admin-filter-bar__value {
        font-weight: 600;
      }
      .admin-filter-bar__chevron {
        font-size: 18px;
        width: 18px;
        height: 18px;
      }
      .admin-filter-bar__count {
        margin-left: 8px;
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
      .admin-filter-bar__clear {
        font-size: 13px;
        color: var(--color-text-secondary, #6b7280);
      }
    `,
  ],
})
export class AdminFilterBarComponent {
  @Input({ required: true }) filters: AdminFilterDefinition[] = [];
  @Input() selection: AdminFilterSelection = {};
  @Input() showClear = true;
  @Input() ariaLabel = 'Filters';
  @Output() filterChange = new EventEmitter<AdminFilterChange>();

  isActive(key: string): boolean {
    return this.selection[key] != null;
  }

  hasActiveFilter(): boolean {
    return this.filters.some((filter) => this.isActive(filter.key));
  }

  activeLabel(filter: AdminFilterDefinition): string {
    const value = this.selection[filter.key];
    if (value == null) {
      return filter.allLabel || 'All';
    }
    return filter.options.find((option) => option.value === value)?.label ?? value;
  }

  select(key: string, value: string | null): void {
    const selection: AdminFilterSelection = { ...this.selection, [key]: value };
    this.filterChange.emit({ key, value, selection });
  }

  clear(): void {
    const selection: AdminFilterSelection = {};
    for (const filter of this.filters) {
      selection[filter.key] = null;
    }
    this.filterChange.emit({ key: '', value: null, selection });
  }
}
