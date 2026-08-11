import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatSnackBar } from '@angular/material/snack-bar';

import {
  AdminFilterBarComponent,
  AdminPageStateComponent,
  AdminStatusChipComponent,
  AdminToolbarComponent,
} from '../../../shared/ui/administration';
import type {
  AdminFilterChange,
  AdminFilterDefinition,
  AdminFilterSelection,
  AdminStatusTone,
} from '../../../shared/ui/administration';
import { WorkspaceSearchComponent } from '../../../shared/ui/workspace/workspace-search.component';
import {
  ConfirmationDialogComponent,
  type ConfirmationDialogData,
} from '../../../shared/ui/confirmation-dialog/confirmation-dialog.component';

import {
  FrameworksApiService,
  type FrameworkCatalogItem,
} from '../../frameworks/services/frameworks-api.service';

type LifecycleAction = 'publish' | 'activate' | 'retire' | 'archive';

const ACTION_LABELS: Record<LifecycleAction, string> = {
  publish: 'Publish',
  activate: 'Activate',
  retire: 'Retire',
  archive: 'Archive',
};

/**
 * Framework Management — Administration Workspace, Frameworks tab.
 *
 * Bound to the existing `FrameworksApiService`, which already exposes the catalog and the
 * publish / activate / retire / archive lifecycle. No parallel client is introduced.
 *
 * Chapters, standards, and objective elements are reachable through the framework
 * structure endpoint; browsing them stays in the operational Framework Navigator
 * (`/app/frameworks`). Administration governs lifecycle, not day-to-day browsing.
 */
@Component({
  selector: 'app-frameworks-admin-page',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    AdminToolbarComponent,
    AdminFilterBarComponent,
    AdminPageStateComponent,
    AdminStatusChipComponent,
    WorkspaceSearchComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-admin-toolbar
      title="Framework Management"
      subtitle="Accreditation frameworks available to this organization, with their version and lifecycle state."
      icon="account_balance"
    >
      <app-workspace-search
        toolbarSearch
        [(value)]="query"
        placeholder="Search frameworks, codes, or authorities…"
      />
      <app-admin-filter-bar
        toolbarFilters
        [filters]="filters"
        [selection]="selection()"
        (filterChange)="onFilterChange($event)"
      />
      <button
        toolbarActions
        mat-stroked-button
        type="button"
        (click)="reload()"
        [disabled]="loading()"
      >
        <mat-icon aria-hidden="true">refresh</mat-icon>
        Refresh
      </button>
    </app-admin-toolbar>

    @if (loading()) {
      <app-admin-page-state state="loading" message="Loading framework catalog…" />
    } @else if (error()) {
      <app-admin-page-state state="error" [message]="error()!" (retry)="reload()" />
    } @else if (!frameworks().length) {
      <app-admin-page-state
        state="empty"
        icon="account_balance"
        message="No frameworks are available to this organization yet."
      />
    } @else if (!visible().length) {
      <app-admin-page-state state="no-results" />
    } @else {
      <div class="frameworks-admin__table-wrap praman-scrollbar">
        <table class="frameworks-admin__table">
          <caption class="frameworks-admin__caption">
            {{
              visible().length
            }}
            of
            {{
              frameworks().length
            }}
            frameworks
          </caption>
          <thead>
            <tr>
              <th scope="col">Framework</th>
              <th scope="col">Authority</th>
              <th scope="col">Version</th>
              <th scope="col">Status</th>
              <th scope="col"><span class="frameworks-admin__sr">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            @for (framework of visible(); track frameworkId(framework)) {
              <tr>
                <td>
                  <span class="frameworks-admin__name">{{ framework.name }}</span>
                  @if (framework.code) {
                    <span class="frameworks-admin__code">{{ framework.code }}</span>
                  }
                </td>
                <td>{{ framework.authority || '—' }}</td>
                <td>{{ framework.version || '—' }}</td>
                <td>
                  <app-admin-status-chip
                    [label]="statusLabel(framework)"
                    [tone]="statusTone(framework)"
                  />
                </td>
                <td class="frameworks-admin__actions">
                  <button
                    mat-icon-button
                    type="button"
                    [matMenuTriggerFor]="menu"
                    [disabled]="!frameworkId(framework) || busyId() === frameworkId(framework)"
                    [attr.aria-label]="'Lifecycle actions for ' + framework.name"
                  >
                    <mat-icon aria-hidden="true">more_vert</mat-icon>
                  </button>
                  <mat-menu #menu="matMenu">
                    @for (action of actions; track action) {
                      <button
                        mat-menu-item
                        type="button"
                        (click)="confirmAction(framework, action)"
                      >
                        {{ actionLabels[action] }}
                      </button>
                    }
                  </mat-menu>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .frameworks-admin__table-wrap {
        margin-top: 20px;
        overflow-x: auto;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        border-radius: 16px;
        background: var(--color-bg-surface, #fff);
      }
      .frameworks-admin__table {
        width: 100%;
        border-collapse: collapse;
        font-size: 14px;
      }
      .frameworks-admin__caption {
        padding: 12px 16px;
        text-align: left;
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);
      }
      .frameworks-admin__table th {
        text-align: left;
        padding: 10px 16px;
        font-size: 11px;
        font-weight: 700;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: var(--color-text-secondary, #9ca3af);
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);
        white-space: nowrap;
      }
      .frameworks-admin__table td {
        padding: 12px 16px;
        border-bottom: 1px solid var(--color-border-subtle, #f1f5f9);
        vertical-align: middle;
      }
      .frameworks-admin__table tbody tr:last-child td {
        border-bottom: none;
      }
      .frameworks-admin__name {
        display: block;
        font-weight: 600;
      }
      .frameworks-admin__code {
        display: block;
        margin-top: 2px;
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
      .frameworks-admin__actions {
        width: 56px;
        text-align: right;
      }
      .frameworks-admin__sr {
        position: absolute;
        width: 1px;
        height: 1px;
        overflow: hidden;
        clip: rect(0 0 0 0);
        white-space: nowrap;
      }
    `,
  ],
})
export class FrameworksAdminPageComponent {
  private readonly api = inject(FrameworksApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly actions: LifecycleAction[] = ['publish', 'activate', 'retire', 'archive'];
  readonly actionLabels = ACTION_LABELS;

  readonly frameworks = signal<FrameworkCatalogItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly busyId = signal<string | null>(null);

  readonly query = signal('');
  readonly selection = signal<AdminFilterSelection>({});

  readonly filters: AdminFilterDefinition[] = [
    {
      key: 'status',
      label: 'Status',
      icon: 'filter_list',
      allLabel: 'All statuses',
      options: [
        { value: 'active', label: 'Active' },
        { value: 'published', label: 'Published' },
        { value: 'retired', label: 'Retired' },
        { value: 'archived', label: 'Archived' },
        { value: 'draft', label: 'Draft' },
      ],
    },
  ];

  readonly visible = computed(() => {
    const term = this.query().trim().toLowerCase();
    const status = this.selection()['status'];

    return this.frameworks().filter((framework) => {
      if (status && this.statusKey(framework) !== status) return false;
      if (!term) return true;

      return [framework.name, framework.code, framework.authority, framework.version]
        .filter((field): field is string => !!field)
        .some((field) => field.toLowerCase().includes(term));
    });
  });

  constructor() {
    this.reload();
  }

  frameworkId(framework: FrameworkCatalogItem): string | undefined {
    return framework.id ?? framework.frameworkId;
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.listCatalog(true).subscribe({
      next: (items) => {
        this.frameworks.set(items ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load the framework catalog.');
        this.loading.set(false);
      },
    });
  }

  /**
   * Normalised status key. The catalog reports either an explicit `status` string or only
   * `isActive`, so both are handled rather than assuming one shape.
   */
  private statusKey(framework: FrameworkCatalogItem): string {
    const explicit = framework.status?.trim().toLowerCase();
    if (explicit) return explicit;
    if (framework.archivedAt) return 'archived';
    if (framework.retiredAt) return 'retired';
    if (framework.isActive) return 'active';
    if (framework.publishedAt) return 'published';
    return 'draft';
  }

  statusLabel(framework: FrameworkCatalogItem): string {
    const key = this.statusKey(framework);
    return key.charAt(0).toUpperCase() + key.slice(1);
  }

  statusTone(framework: FrameworkCatalogItem): AdminStatusTone {
    switch (this.statusKey(framework)) {
      case 'active':
        return 'success';
      case 'published':
        return 'info';
      case 'retired':
        return 'warning';
      case 'archived':
        return 'danger';
      default:
        return 'neutral';
    }
  }

  onFilterChange(change: AdminFilterChange): void {
    this.selection.set(change.selection);
  }

  confirmAction(framework: FrameworkCatalogItem, action: LifecycleAction): void {
    const id = this.frameworkId(framework);
    if (!id) return;

    const data: ConfirmationDialogData = {
      title: `${ACTION_LABELS[action]} framework`,
      message: `${ACTION_LABELS[action]} “${framework.name}”? This changes the framework's lifecycle state for the whole organization.`,
      confirmText: ACTION_LABELS[action],
    };

    this.dialog
      .open(ConfirmationDialogComponent, { data, width: '440px' })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed) this.runAction(id, framework.name, action);
      });
  }

  private runAction(id: string, name: string, action: LifecycleAction): void {
    this.busyId.set(id);

    const request$ =
      action === 'publish'
        ? this.api.publish(id)
        : action === 'activate'
          ? this.api.activate(id)
          : action === 'retire'
            ? this.api.retire(id)
            : this.api.archive(id);

    request$.subscribe({
      next: () => {
        this.busyId.set(null);
        this.snackBar.open(`${name} — ${ACTION_LABELS[action].toLowerCase()}d.`, 'Dismiss', {
          duration: 4000,
        });
        this.reload();
      },
      error: () => {
        this.busyId.set(null);
        this.snackBar.open(`Could not ${ACTION_LABELS[action].toLowerCase()} ${name}.`, 'Dismiss', {
          duration: 5000,
        });
      },
    });
  }
}
