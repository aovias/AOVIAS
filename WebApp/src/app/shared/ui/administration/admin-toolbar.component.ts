import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

/**
 * Shared Administration workspace toolbar.
 *
 * Every administration tab renders its header through this component so spacing,
 * type scale, and the search / filter / action ordering stay identical across tabs.
 *
 * Projection slots — prefixed so they cannot collide with an input of the projected
 * component (a bare `filters` attribute would bind `AdminFilterBarComponent.filters`):
 *   [toolbarSearch]  — search control (usually `app-workspace-search`)
 *   [toolbarFilters] — filter controls (usually `app-admin-filter-bar`)
 *   [toolbarActions] — primary and secondary actions, right aligned
 *   default          — optional supplementary row beneath the toolbar
 */
@Component({
  selector: 'app-admin-toolbar',
  standalone: true,
  imports: [MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="admin-toolbar" [class.admin-toolbar--compact]="compact">
      <div class="admin-toolbar__primary">
        <div class="admin-toolbar__identity">
          @if (icon) {
            <span class="admin-toolbar__icon" aria-hidden="true">
              <mat-icon>{{ icon }}</mat-icon>
            </span>
          }
          <div class="admin-toolbar__headings">
            <h1 class="admin-toolbar__title">{{ title }}</h1>
            @if (subtitle) {
              <p class="admin-toolbar__subtitle">{{ subtitle }}</p>
            }
          </div>
        </div>
        <div class="admin-toolbar__actions">
          <ng-content select="[toolbarActions]" />
        </div>
      </div>

      <div class="admin-toolbar__controls">
        <div class="admin-toolbar__search">
          <ng-content select="[toolbarSearch]" />
        </div>
        <div class="admin-toolbar__filters">
          <ng-content select="[toolbarFilters]" />
        </div>
      </div>

      <ng-content />
    </div>
  `,
  styles: [
    `
      .admin-toolbar {
        display: flex;
        flex-direction: column;
        gap: 16px;
        padding: 0 0 16px;
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);
      }
      .admin-toolbar--compact {
        gap: 12px;
        padding-bottom: 12px;
      }
      .admin-toolbar__primary {
        display: flex;
        flex-wrap: wrap;
        align-items: flex-start;
        justify-content: space-between;
        gap: 16px;
      }
      .admin-toolbar__identity {
        display: flex;
        align-items: flex-start;
        gap: 12px;
        min-width: 0;
      }
      .admin-toolbar__icon {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 36px;
        height: 36px;
        flex: none;
        border-radius: 12px;
        background: var(--color-bg-subtle, #f3f4f6);
        color: var(--color-primary);
      }
      .admin-toolbar__headings {
        min-width: 0;
      }
      .admin-toolbar__title {
        margin: 0;
        font-size: 18px;
        font-weight: 600;
        line-height: 1.3;
        color: var(--color-text-primary, inherit);
      }
      .admin-toolbar__subtitle {
        margin: 4px 0 0;
        font-size: 13px;
        line-height: 1.5;
        max-width: 72ch;
        color: var(--color-text-secondary, #6b7280);
      }
      .admin-toolbar__actions {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 8px;
      }
      .admin-toolbar__controls {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 12px;
      }
      .admin-toolbar__controls:empty {
        display: none;
      }
      .admin-toolbar__search {
        flex: 1 1 260px;
        min-width: 0;
      }
      .admin-toolbar__search:empty,
      .admin-toolbar__filters:empty {
        display: none;
      }
      .admin-toolbar__filters {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 8px;
      }

      @media (max-width: 640px) {
        .admin-toolbar__actions {
          width: 100%;
        }
        .admin-toolbar__search {
          flex-basis: 100%;
        }
      }
    `,
  ],
})
export class AdminToolbarComponent {
  @Input({ required: true }) title!: string;
  @Input() subtitle = '';
  @Input() icon = '';
  @Input() compact = false;
}
