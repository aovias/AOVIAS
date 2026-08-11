import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { LoadingService } from '../../../core/services/loading.service';
import { UserContextService } from '../../../core/services/user-context.service';
import { HasPermissionDirective } from '../../../shared/authorization/has-permission.directive';
import { NotificationPageStateComponent } from '../../../shared/notifications/components/notification-ui.components';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import {
  Connector,
  ConnectorHealth,
  ConnectorHealthResult,
  IntegrationApiService,
  IntegrationAuditEntry,
  MappingProfile,
  SynchronizationJob,
  WebhookSubscription,
  WebhookDelivery,
  ImportJob,
  ExportJob,
} from '../services/integration-api.service';
import { ConnectorTypePickerComponent } from '../components/connector-type-picker.component';
import { SyncRunStatusChipComponent } from '../components/sync-run-status-chip.component';
import { MappingFieldRowComponent } from '../components/mapping-field-row.component';
import { WebhookDeliveryLogComponent } from '../components/webhook-delivery-log.component';
import { IntegrationHealthChipComponent } from '../components/integration-health-chip.component';
import { IntegrationDiagnosticsSectionComponent } from '../components/integration-diagnostics-section.component';
import { INTEGRATION_ADMIN_BASE } from '../models/integration-admin-nav.model';

function orgIdOrError(userContext: UserContextService, error: { set: (v: string | null) => void }): string | null {
  const orgId = userContext.getOrganization()?.id ?? null;
  if (!orgId) {
    error.set('Organization context is not available.');
  }
  return orgId;
}

function maskConfiguration(json?: string): string {
  if (!json) return '{}';
  try {
    const parsed = JSON.parse(json) as Record<string, unknown>;
    const masked: Record<string, unknown> = {};
    for (const [key, value] of Object.entries(parsed)) {
      if (/secret|password|apikey|api_key|token|credential/i.test(key)) {
        masked[key] = '••••••••';
      } else {
        masked[key] = value;
      }
    }
    return JSON.stringify(masked, null, 2);
  } catch {
    return json;
  }
}

type SortKey = 'name' | 'provider' | 'status' | 'health';

@Component({
  selector: 'app-connectors-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    HasPermissionDirective,
    SettingsPageHeaderComponent,
    NotificationPageStateComponent,
    ConnectorTypePickerComponent,
    IntegrationHealthChipComponent,
  ],
  template: `
    <div class="integration-page">
      <app-settings-page-header
        title="Integration Connectors"
        description="Browse, filter, and configure all external system connectors for your organization."
      />
      @if (loading()) {
        <app-notification-page-state state="loading" message="Loading connectors…" [showRetry]="false" />
      } @else if (error()) {
        <app-notification-page-state state="error" [message]="error()!" (retry)="load()" />
      } @else {
        <div class="toolbar">
          <input class="toolbar__search" type="search" [(ngModel)]="search" placeholder="Search integrations…" aria-label="Search integrations" />
          <select [(ngModel)]="categoryFilter" aria-label="Filter by category">
            <option value="">All categories</option>
            @for (c of categories(); track c) {
              <option [value]="c">{{ c }}</option>
            }
          </select>
          <select [(ngModel)]="statusFilter" aria-label="Filter by status">
            <option value="">All statuses</option>
            @for (s of statuses(); track s) {
              <option [value]="s">{{ s }}</option>
            }
          </select>
          <select [(ngModel)]="healthFilter" aria-label="Filter by health">
            <option value="">All health</option>
            @for (h of healthOptions(); track h) {
              <option [value]="h">{{ h }}</option>
            }
          </select>
          <select [(ngModel)]="sortKey" aria-label="Sort connectors">
            <option value="name">Sort by name</option>
            <option value="provider">Sort by provider</option>
            <option value="status">Sort by status</option>
            <option value="health">Sort by health</option>
          </select>
        </div>

        <div class="toolbar toolbar--secondary" *hasPermission="'Integration.Connector.Manage'">
          <app-connector-type-picker [types]="providers()" (selected)="onType($event)" />
        </div>

        @if (filteredConnectors().length === 0) {
          <app-notification-page-state state="empty" message="No connectors match your filters." [showRetry]="false" />
        } @else {
          <div class="catalog-grid">
            @for (item of pagedConnectors(); track item.connector.id) {
              <article class="catalog-card">
                <div class="catalog-card__header">
                  <strong>{{ item.connector.name }}</strong>
                  <app-integration-health-chip [status]="item.healthStatus" />
                </div>
                <dl class="catalog-card__meta">
                  <div><dt>Category</dt><dd>{{ item.connector.providerType }}</dd></div>
                  <div><dt>Provider</dt><dd>{{ item.connector.providerType }}</dd></div>
                  <div><dt>Status</dt><dd>{{ item.connector.lifecycleState }}</dd></div>
                  <div><dt>Version</dt><dd>v{{ item.connector.version }}</dd></div>
                </dl>
                <div class="catalog-card__actions">
                  <a [routerLink]="[item.connector.id]">View</a>
                  <a [routerLink]="[item.connector.id]">Configure</a>
                  <a [routerLink]="auditRoute">View logs</a>
                </div>
              </article>
            }
          </div>
          <div class="pagination">
            <button mat-stroked-button type="button" [disabled]="page() === 1" (click)="page.set(page() - 1)">Previous</button>
            <span>Page {{ page() }} of {{ totalPages() }}</span>
            <button mat-stroked-button type="button" [disabled]="page() >= totalPages()" (click)="page.set(page() + 1)">Next</button>
          </div>
        }
      }
    </div>
  `,
  styles: [
    `
      .integration-page { display: flex; flex-direction: column; gap: 16px; }
      .toolbar { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
      .toolbar--secondary { margin-top: -4px; }
      .toolbar__search { flex: 1; min-width: 200px; padding: 8px 12px; border-radius: 8px; border: 1px solid var(--color-border-subtle, #e5e7eb); }
      select { padding: 8px 10px; border-radius: 8px; border: 1px solid var(--color-border-subtle, #e5e7eb); }
      .catalog-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 12px; }
      .catalog-card { padding: 16px; border-radius: 16px; border: 1px solid var(--color-border-subtle, #e5e7eb); background: #fff; transition: box-shadow 0.2s ease; }
      .catalog-card:hover { box-shadow: 0 8px 24px color-mix(in srgb, #000 6%, transparent); }
      .catalog-card__header { display: flex; justify-content: space-between; gap: 8px; margin-bottom: 12px; }
      .catalog-card__meta { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; margin: 0 0 12px; font-size: 12px; }
      .catalog-card__meta dt { color: #6b7280; margin: 0; }
      .catalog-card__meta dd { margin: 2px 0 0; font-weight: 500; }
      .catalog-card__actions { display: flex; flex-wrap: wrap; gap: 12px; }
      .catalog-card__actions a { font-size: 13px; color: #2f6bff; text-decoration: none; font-weight: 500; }
      .pagination { display: flex; align-items: center; gap: 12px; justify-content: flex-end; margin-top: 8px; }
    `,
  ],
})
export class ConnectorsListComponent implements OnInit {
  private readonly api = inject(IntegrationApiService);
  private readonly loadingService = inject(LoadingService);
  private readonly userContext = inject(UserContextService);

  readonly loading = signal(true);
  readonly connectors = signal<Connector[]>([]);
  readonly health = signal<ConnectorHealth[]>([]);
  readonly providers = signal<string[]>([]);
  readonly error = signal<string | null>(null);
  readonly page = signal(1);
  readonly pageSize = 9;
  readonly auditRoute = `${INTEGRATION_ADMIN_BASE}/audit`;

  search = '';
  categoryFilter = '';
  statusFilter = '';
  healthFilter = '';
  sortKey: SortKey = 'name';

  readonly categories = computed(() => [...new Set(this.connectors().map((c) => c.providerType))].sort());
  readonly statuses = computed(() => [...new Set(this.connectors().map((c) => c.lifecycleState))].sort());
  readonly healthOptions = computed(() => {
    const fromHealth = this.health().map((h) => h.status);
    const fromConnectors = this.connectors().map((c) => c.healthStatus);
    return [...new Set([...fromHealth, ...fromConnectors])].sort();
  });

  readonly filteredConnectors = computed(() => {
    const healthById = new Map(this.health().map((h) => [h.connectorId, h.status]));
    let items = this.connectors().map((connector) => ({
      connector,
      healthStatus: healthById.get(connector.id) ?? connector.healthStatus ?? 'Unknown',
    }));

    const q = this.search.trim().toLowerCase();
    if (q) {
      items = items.filter(
        (i) =>
          i.connector.name.toLowerCase().includes(q) ||
          i.connector.providerType.toLowerCase().includes(q),
      );
    }
    if (this.categoryFilter) items = items.filter((i) => i.connector.providerType === this.categoryFilter);
    if (this.statusFilter) items = items.filter((i) => i.connector.lifecycleState === this.statusFilter);
    if (this.healthFilter) items = items.filter((i) => i.healthStatus === this.healthFilter);

    const key = this.sortKey;
    items.sort((a, b) => {
      if (key === 'provider') return a.connector.providerType.localeCompare(b.connector.providerType);
      if (key === 'status') return a.connector.lifecycleState.localeCompare(b.connector.lifecycleState);
      if (key === 'health') return a.healthStatus.localeCompare(b.healthStatus);
      return a.connector.name.localeCompare(b.connector.name);
    });
    return items;
  });

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.filteredConnectors().length / this.pageSize)));

  readonly pagedConnectors = computed(() => {
    const start = (this.page() - 1) * this.pageSize;
    return this.filteredConnectors().slice(start, start + this.pageSize);
  });

  ngOnInit(): void {
    this.load();
    this.api.listProviders().pipe(catchError(() => of([] as string[]))).subscribe((d) => this.providers.set(d));
  }

  load(): void {
    const orgId = orgIdOrError(this.userContext, this.error);
    if (!orgId) {
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    forkJoin({
      connectors: this.api.listConnectors(orgId).pipe(catchError(() => of([] as Connector[]))),
      health: this.api.getConnectorHealth(orgId).pipe(catchError(() => of([] as ConnectorHealth[]))),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ connectors, health }) => {
          this.connectors.set(connectors);
          this.health.set(health);
        },
        error: () => this.error.set('Unable to load connectors.'),
      });
  }

  onType(type: string): void {
    const orgId = orgIdOrError(this.userContext, this.error);
    if (!orgId) return;

    this.error.set(null);
    this.loadingService.show();
    this.api
      .createConnector({ organizationId: orgId, name: `New ${type}`, providerType: type, configurationJson: '{}' })
      .pipe(
        catchError(() => {
          this.error.set('Unable to create connector.');
          return of(null);
        }),
        finalize(() => this.loadingService.hide()),
      )
      .subscribe((created) => {
        if (created) this.load();
      });
  }
}

@Component({
  selector: 'app-connector-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    HasPermissionDirective,
    SettingsPageHeaderComponent,
    NotificationPageStateComponent,
    IntegrationHealthChipComponent,
    IntegrationDiagnosticsSectionComponent,
  ],
  template: `
    <div class="integration-page">
      @if (loading()) {
        <app-notification-page-state state="loading" message="Loading connector…" [showRetry]="false" />
      } @else if (error()) {
        <app-notification-page-state state="error" [message]="error()!" (retry)="load()" />
      } @else if (connector()) {
        <app-settings-page-header
          [title]="connector()!.name"
          [description]="'Configure connection settings, monitor health, and test connectivity for ' + connector()!.providerType + ' integration.'"
        />

        <div class="detail-grid">
          <section class="detail-panel">
            <h2>Connection status</h2>
            <dl class="detail-meta">
              <div><dt>Category</dt><dd>{{ connector()!.providerType }}</dd></div>
              <div><dt>Provider</dt><dd>{{ connector()!.providerType }}</dd></div>
              <div><dt>Lifecycle</dt><dd>{{ connector()!.lifecycleState }}</dd></div>
              <div><dt>Health</dt><dd><app-integration-health-chip [status]="healthStatus()" /></dd></div>
              <div><dt>Version</dt><dd>v{{ connector()!.version }}</dd></div>
              @if (healthEntry()) {
                <div><dt>Response time</dt><dd>{{ healthEntry()!.latencyMs }}ms</dd></div>
                <div><dt>Success rate</dt><dd>{{ healthEntry()!.successRate }}%</dd></div>
              }
            </dl>
            <div class="detail-actions">
              <button mat-flat-button color="primary" type="button" *hasPermission="'Integration.Connector.Manage'" (click)="testConnection()" [disabled]="testing()">
                @if (testing()) { <mat-spinner diameter="18" /> } @else { Test connection }
              </button>
              @if (connector()!.lifecycleState === 'Suspended' || connector()!.lifecycleState === 'Disconnected') {
                <button mat-stroked-button type="button" *hasPermission="'Integration.Connector.Manage'" (click)="setEnabled(true)">Enable</button>
              } @else {
                <button mat-stroked-button type="button" *hasPermission="'Integration.Connector.Manage'" (click)="setEnabled(false)">Disable</button>
              }
              <a mat-stroked-button [routerLink]="auditRoute">View logs</a>
            </div>
            @if (testResult()) {
              <div class="test-result" [class.test-result--success]="testSuccess()" [class.test-result--failure]="!testSuccess()">
                <strong>{{ testSuccess() ? 'Connection successful' : 'Connection failed' }}</strong>
                <p>{{ testResult()!.message ?? testResult()!.status }} @if (testResult()!.latencyMs) { ({{ testResult()!.latencyMs }}ms) }</p>
              </div>
            }
          </section>

          <section class="detail-panel">
            <h2>Configuration</h2>
            <p class="detail-hint">Sensitive values are masked. Update via API using existing connector management.</p>
            <pre class="config-block">{{ maskedConfig() }}</pre>
          </section>
        </div>

        <app-integration-diagnostics-section [auditEntries]="auditEntries()" />
      }
    </div>
  `,
  styles: [
    `
      .integration-page { display: flex; flex-direction: column; gap: 20px; }
      .detail-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 16px; }
      .detail-panel { padding: 16px; border-radius: 16px; border: 1px solid #e5e7eb; background: #fff; }
      .detail-panel h2 { margin: 0 0 12px; font-size: var(--font-size-section-title); }
      .detail-meta { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin: 0 0 16px; font-size: 13px; }
      .detail-meta dt { color: #6b7280; margin: 0; }
      .detail-meta dd { margin: 2px 0 0; font-weight: 500; }
      .detail-actions { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
      .detail-hint { font-size: 12px; color: #6b7280; margin: 0 0 8px; }
      .config-block { margin: 0; padding: 12px; border-radius: 8px; background: #f9fafb; font-size: 12px; overflow: auto; }
      .test-result { margin-top: 12px; padding: 12px; border-radius: 8px; font-size: 13px; }
      .test-result--success { background: #dcfce7; color: #166534; }
      .test-result--failure { background: #fee2e2; color: #991b1b; }
      .test-result p { margin: 4px 0 0; }
    `,
  ],
})
export class ConnectorDetailComponent implements OnInit {
  id = input.required<string>();
  private readonly api = inject(IntegrationApiService);
  private readonly userContext = inject(UserContextService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly connector = signal<Connector | null>(null);
  readonly health = signal<ConnectorHealth[]>([]);
  readonly auditEntries = signal<IntegrationAuditEntry[]>([]);
  readonly testing = signal(false);
  readonly testResult = signal<ConnectorHealthResult | null>(null);
  readonly auditRoute = `${INTEGRATION_ADMIN_BASE}/audit`;

  readonly healthEntry = computed(() => this.health().find((h) => h.connectorId === this.id()) ?? null);
  readonly healthStatus = computed(() => this.healthEntry()?.status ?? this.connector()?.healthStatus ?? 'Unknown');
  readonly maskedConfig = computed(() => maskConfiguration(this.connector()?.configurationJson));
  readonly testSuccess = computed(() => {
    const r = this.testResult();
    return r ? /healthy|connected|ok/i.test(r.status) : false;
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const orgId = orgIdOrError(this.userContext, this.error);
    if (!orgId) {
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    forkJoin({
      connector: this.api.getConnector(this.id()).pipe(catchError(() => of(null))),
      health: this.api.getConnectorHealth(orgId).pipe(catchError(() => of([] as ConnectorHealth[]))),
      audit: this.api.listAudit(orgId).pipe(catchError(() => of([] as IntegrationAuditEntry[]))),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ connector, health, audit }) => {
          if (!connector) {
            this.error.set('Connector not found.');
            return;
          }
          this.connector.set(connector);
          this.health.set(health);
          this.auditEntries.set(audit);
        },
        error: () => this.error.set('Unable to load connector details.'),
      });
  }

  testConnection(): void {
    this.testing.set(true);
    this.testResult.set(null);
    this.api
      .validateConnector(this.id())
      .pipe(
        catchError(() => of({ status: 'Failed', latencyMs: 0, message: 'Connection test failed.' } as ConnectorHealthResult)),
        finalize(() => this.testing.set(false)),
      )
      .subscribe((result) => {
        this.testResult.set(result);
        this.load();
      });
  }

  setEnabled(enabled: boolean): void {
    const current = this.connector();
    if (!current) return;

    this.api
      .updateConnector(this.id(), {
        name: current.name,
        configurationJson: current.configurationJson ?? '{}',
        lifecycleState: enabled ? 'Connected' : 'Suspended',
      })
      .pipe(catchError(() => {
        this.error.set('Unable to update connector state.');
        return of(null);
      }))
      .subscribe((updated) => {
        if (updated) this.connector.set(updated);
      });
  }
}

@Component({
  selector: 'app-mappings',
  standalone: true,
  imports: [CommonModule, MappingFieldRowComponent, SettingsPageHeaderComponent, NotificationPageStateComponent],
  template: `
    <div class="integration-page">
      <app-settings-page-header title="Field Mappings" description="Design mapping profiles between external source entities and platform targets." />
      @if (error()) { <app-notification-page-state state="error" [message]="error()!" [showRetry]="false" /> }
      @else if (!mappings().length) { <app-notification-page-state state="empty" message="No mapping profiles configured." [showRetry]="false" /> }
      @else {
        <table class="data-table"><thead><tr><th>Name</th><th>Source</th><th>Target</th><th>Fields</th><th></th></tr></thead>
          <tbody>@for (m of mappings(); track m.id) {
            <tr><td>{{ m.name }}</td><td>{{ m.sourceEntity }}</td><td>{{ m.targetEntity }}</td><td>{{ m.fields.length }}</td>
              <td><button type="button" (click)="selectMapping(m)">Edit fields</button></td></tr>
          }</tbody></table>
        @if (selectedMapping()) {
          <h2>Field mappings — {{ selectedMapping()!.name }}</h2>
          @for (field of editableFields(); track $index) {
            <app-mapping-field-row [sourceField]="field.sourceField" [targetField]="field.targetField"
              (sourceFieldChange)="updateFieldSource($index, $event)" (targetFieldChange)="updateFieldTarget($index, $event)" />
          }
        }
      }
    </div>
  `,
  styles: [`.integration-page { display: flex; flex-direction: column; gap: 16px; } .data-table { width: 100%; border-collapse: collapse; } .data-table th, .data-table td { padding: 10px; border-bottom: 1px solid #e5e7eb; text-align: left; }`],
})
export class MappingsComponent implements OnInit {
  private api = inject(IntegrationApiService);
  private loadingService = inject(LoadingService);
  private userContext = inject(UserContextService);
  mappings = signal<MappingProfile[]>([]);
  selectedMapping = signal<MappingProfile | null>(null);
  editableFields = signal<{ sourceField: string; targetField: string }[]>([]);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const orgId = orgIdOrError(this.userContext, this.error);
    if (!orgId) return;
    this.loadingService.show();
    this.api.listMappings(orgId).pipe(catchError(() => { this.error.set('Unable to load mappings.'); return of([]); }), finalize(() => this.loadingService.hide())).subscribe((d) => this.mappings.set(d));
  }
  selectMapping(mapping: MappingProfile): void { this.selectedMapping.set(mapping); this.editableFields.set(mapping.fields.map((f) => ({ ...f }))); }
  updateFieldSource(index: number, value: string): void { this.editableFields.update((f) => { const c = [...f]; c[index] = { ...c[index], sourceField: value }; return c; }); }
  updateFieldTarget(index: number, value: string): void { this.editableFields.update((f) => { const c = [...f]; c[index] = { ...c[index], targetField: value }; return c; }); }
}

@Component({
  selector: 'app-synchronizations',
  standalone: true,
  imports: [CommonModule, FormsModule, SyncRunStatusChipComponent, SettingsPageHeaderComponent, NotificationPageStateComponent],
  template: `
    <div class="integration-page">
      <app-settings-page-header title="Synchronizations" description="Create scheduled jobs and run on-demand synchronizations." />
      @if (error()) { <app-notification-page-state state="error" [message]="error()!" [showRetry]="false" /> }
      <div class="create-sync">
        <h2>Create scheduled job</h2>
        <label>Job name <input type="text" [(ngModel)]="newJobName" /></label>
        <label>Cron expression <input type="text" [(ngModel)]="cronExpression" aria-label="Cron schedule" /></label>
        <label>Connector <select [(ngModel)]="selectedConnectorId"><option value="">Select connector</option>@for (c of connectors(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }</select></label>
        <label>Mapping profile <select [(ngModel)]="selectedMappingId"><option value="">Select mapping</option>@for (m of mappings(); track m.id) { <option [value]="m.id">{{ m.name }}</option> }</select></label>
        <button type="button" (click)="createJob()">Create sync job</button>
      </div>
      <table class="data-table"><thead><tr><th>Name</th><th>Direction</th><th>Type</th><th>Enabled</th><th>Action</th></tr></thead>
        <tbody>@for (j of jobs(); track j.id) {
          <tr><td>{{ j.name }}</td><td>{{ j.direction }}</td><td>{{ j.syncType }}</td><td>{{ j.isEnabled ? 'Yes' : 'No' }}</td>
            <td><button type="button" (click)="run(j.id)">Run</button> @if (lastRun()) { <app-sync-run-status-chip [status]="lastRun()!.status" /> }</td></tr>
        }</tbody></table>
    </div>
  `,
  styles: [`.integration-page { display: flex; flex-direction: column; gap: 16px; } .create-sync { display: flex; flex-direction: column; gap: 8px; max-width: 28rem; } .create-sync label { display: flex; flex-direction: column; gap: 4px; } .data-table { width: 100%; border-collapse: collapse; } .data-table th, .data-table td { padding: 10px; border-bottom: 1px solid #e5e7eb; }`],
})
export class SynchronizationsComponent implements OnInit {
  private api = inject(IntegrationApiService);
  private loadingService = inject(LoadingService);
  private userContext = inject(UserContextService);
  jobs = signal<SynchronizationJob[]>([]);
  connectors = signal<Connector[]>([]);
  mappings = signal<MappingProfile[]>([]);
  lastRun = signal<{ status: string } | null>(null);
  error = signal<string | null>(null);
  newJobName = 'Scheduled sync';
  cronExpression = '0 0 * * *';
  selectedConnectorId = '';
  selectedMappingId = '';

  ngOnInit(): void {
    const orgId = orgIdOrError(this.userContext, this.error);
    if (!orgId) return;
    this.api.listSyncJobs(orgId).pipe(catchError(() => { this.error.set('Unable to load synchronization jobs.'); return of([]); })).subscribe((d) => this.jobs.set(d));
    this.api.listConnectors(orgId).pipe(catchError(() => of([]))).subscribe((d) => this.connectors.set(d));
    this.api.listMappings(orgId).pipe(catchError(() => of([]))).subscribe((d) => this.mappings.set(d));
  }
  createJob(): void {
    const orgId = orgIdOrError(this.userContext, this.error);
    if (!orgId || !this.selectedConnectorId || !this.selectedMappingId) { this.error.set('Select a connector and mapping profile.'); return; }
    this.api.createSyncJob({ organizationId: orgId, name: this.newJobName.trim() || 'Scheduled sync', sourceConnectorId: this.selectedConnectorId, mappingProfileId: this.selectedMappingId, direction: 'OneWay', syncType: 'Incremental', cronExpression: this.cronExpression.trim() || undefined })
      .pipe(catchError(() => { this.error.set('Unable to create synchronization job.'); return of(null); }))
      .subscribe((job) => { if (job) this.jobs.update((l) => [...l, job]); });
  }
  run(id: string): void {
    this.api.runSync(id).pipe(catchError(() => { this.error.set('Synchronization run failed.'); return of(null); })).subscribe((r) => { if (r) this.lastRun.set(r); });
  }
}

@Component({
  selector: 'app-webhooks',
  standalone: true,
  imports: [CommonModule, WebhookDeliveryLogComponent, SettingsPageHeaderComponent, NotificationPageStateComponent],
  template: `
    <div class="integration-page">
      <app-settings-page-header title="Webhooks" description="Manage inbound and outbound webhook subscriptions and delivery history." />
      @if (error()) { <app-notification-page-state state="error" [message]="error()!" [showRetry]="false" /> }
      @else if (!webhooks().length) { <app-notification-page-state state="empty" message="No webhook subscriptions configured." [showRetry]="false" /> }
      @else {
        <ul class="webhook-list">@for (w of webhooks(); track w.id) {
          <li><button type="button" (click)="selectWebhook(w)">{{ w.name }}</button> — {{ w.direction }} @if (w.targetUrl) { ({{ w.targetUrl }}) }</li>
        }</ul>
        @if (selectedWebhook()) { <h2>Delivery log — {{ selectedWebhook()!.name }}</h2><app-webhook-delivery-log [deliveries]="deliveries()" /> }
      }
    </div>
  `,
  styles: [`.integration-page { display: flex; flex-direction: column; gap: 16px; } .webhook-list { list-style: none; padding: 0; }`],
})
export class WebhooksComponent implements OnInit {
  private api = inject(IntegrationApiService);
  private loadingService = inject(LoadingService);
  private userContext = inject(UserContextService);
  webhooks = signal<WebhookSubscription[]>([]);
  selectedWebhook = signal<WebhookSubscription | null>(null);
  deliveries = signal<WebhookDelivery[]>([]);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const orgId = orgIdOrError(this.userContext, this.error);
    if (!orgId) return;
    this.api.listWebhooks(orgId).pipe(catchError(() => { this.error.set('Unable to load webhooks.'); return of([]); })).subscribe((d) => this.webhooks.set(d));
  }
  selectWebhook(webhook: WebhookSubscription): void {
    this.selectedWebhook.set(webhook);
    this.api.listWebhookDeliveries(webhook.id).pipe(catchError(() => { this.error.set('Unable to load webhook deliveries.'); return of([]); })).subscribe((d) => this.deliveries.set(d));
  }
}

@Component({
  selector: 'app-imports',
  standalone: true,
  imports: [CommonModule, SettingsPageHeaderComponent, NotificationPageStateComponent],
  template: `
    <div class="integration-page">
      <app-settings-page-header title="Import Wizard" description="Import external data using existing mapping profiles and validation." />
      @if (error()) { <app-notification-page-state state="error" [message]="error()!" [showRetry]="false" /> }
      <button type="button" (click)="start()">Start CSV Import</button>
      @if (job()) { <p>Status: {{ job()!.status }}</p><button type="button" (click)="rollback()">Rollback import</button> }
    </div>
  `,
  styles: [`.integration-page { display: flex; flex-direction: column; gap: 16px; }`],
})
export class ImportsComponent {
  private api = inject(IntegrationApiService);
  private loadingService = inject(LoadingService);
  private userContext = inject(UserContextService);
  job = signal<ImportJob | null>(null);
  error = signal<string | null>(null);

  start(): void {
    const orgId = orgIdOrError(this.userContext, this.error);
    if (!orgId) return;
    this.api.createImport({ organizationId: orgId, fileFormat: 'Csv', content: 'ExternalEmployeeNumber,name\nE1,Test' }).pipe(catchError(() => { this.error.set('Unable to start import.'); return of(null); })).subscribe((j) => {
      if (!j) return;
      this.api.previewImport(j.id).pipe(catchError(() => { this.error.set('Unable to preview import.'); return of(null); })).subscribe((p) => { if (p) this.job.set(p); });
    });
  }
  rollback(): void {
    const current = this.job();
    if (!current) return;
    this.api.rollbackImport(current.id).pipe(catchError(() => { this.error.set('Import rollback failed.'); return of(null); })).subscribe((j) => { if (j) this.job.set(j); });
  }
}

@Component({
  selector: 'app-exports',
  standalone: true,
  imports: [CommonModule, SettingsPageHeaderComponent, NotificationPageStateComponent],
  template: `
    <div class="integration-page">
      <app-settings-page-header title="Export Wizard" description="Export platform data to external formats using existing export jobs." />
      @if (error()) { <app-notification-page-state state="error" [message]="error()!" [showRetry]="false" /> }
      <button type="button" (click)="start()">Export Employment CSV</button>
      @if (job()) { <p>Status: {{ job()!.status }}</p> }
    </div>
  `,
  styles: [`.integration-page { display: flex; flex-direction: column; gap: 16px; }`],
})
export class ExportsComponent {
  private api = inject(IntegrationApiService);
  private userContext = inject(UserContextService);
  job = signal<ExportJob | null>(null);
  error = signal<string | null>(null);

  start(): void {
    const orgId = orgIdOrError(this.userContext, this.error);
    if (!orgId) return;
    this.api.createExport({ organizationId: orgId, fileFormat: 'Csv' }).pipe(catchError(() => { this.error.set('Unable to start export.'); return of(null); })).subscribe((j) => {
      if (!j) return;
      this.api.executeExport(j.id).pipe(catchError(() => { this.error.set('Export execution failed.'); return of(null); })).subscribe((e) => { if (e) this.job.set(e); });
    });
  }
}

@Component({
  selector: 'app-integration-audit',
  standalone: true,
  imports: [CommonModule, SettingsPageHeaderComponent, NotificationPageStateComponent],
  template: `
    <div class="integration-page">
      <app-settings-page-header title="Integration Audit" description="Configuration changes, connection tests, credential updates, and integration operations." />
      @if (error()) { <app-notification-page-state state="error" [message]="error()!" [showRetry]="false" /> }
      @else if (!audit().length) { <app-notification-page-state state="empty" message="No audit entries recorded yet." [showRetry]="false" /> }
      @else {
        <table class="data-table"><thead><tr><th>Operation</th><th>Result</th><th>Records</th><th>Duration</th><th>Trace</th><th>When</th></tr></thead>
          <tbody>@for (a of audit(); track a.id) {
            <tr><td>{{ a.operation }}</td><td>{{ a.result }}</td><td>{{ a.recordsProcessed }}</td><td>{{ a.durationMs }}ms</td><td>{{ a.traceId }}</td><td>{{ a.occurredAtUtc | date: 'short' }}</td></tr>
          }</tbody></table>
      }
    </div>
  `,
  styles: [`.integration-page { display: flex; flex-direction: column; gap: 16px; } .data-table { width: 100%; border-collapse: collapse; } .data-table th, .data-table td { padding: 10px; border-bottom: 1px solid #e5e7eb; text-align: left; }`],
})
export class IntegrationAuditComponent implements OnInit {
  private api = inject(IntegrationApiService);
  private userContext = inject(UserContextService);
  audit = signal<IntegrationAuditEntry[]>([]);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const orgId = orgIdOrError(this.userContext, this.error);
    if (!orgId) return;
    this.api.listAudit(orgId).pipe(catchError(() => { this.error.set('Unable to load audit log.'); return of([]); })).subscribe((d) => this.audit.set(d));
  }
}

@Component({
  selector: 'app-monitoring',
  standalone: true,
  imports: [CommonModule, SettingsPageHeaderComponent, NotificationPageStateComponent, IntegrationHealthChipComponent],
  template: `
    <div class="integration-page">
      <app-settings-page-header title="Health Monitoring" description="Monitor connector health, latency, success rates, and connection status across integrations." />
      @if (error()) { <app-notification-page-state state="error" [message]="error()!" [showRetry]="false" /> }
      @else if (!health().length) { <app-notification-page-state state="empty" message="No health data available. Configure connectors to begin monitoring." [showRetry]="false" /> }
      @else {
        <table class="data-table"><thead><tr><th>Connector</th><th>Health</th><th>Latency</th><th>Success %</th><th>Last checked</th></tr></thead>
          <tbody>@for (h of health(); track h.connectorId) {
            <tr><td>{{ h.connectorName }}</td><td><app-integration-health-chip [status]="h.status" /></td><td>{{ h.latencyMs }}ms</td><td>{{ h.successRate }}%</td><td>—</td></tr>
          }</tbody></table>
      }
    </div>
  `,
  styles: [`.integration-page { display: flex; flex-direction: column; gap: 16px; } .data-table { width: 100%; border-collapse: collapse; } .data-table th, .data-table td { padding: 10px; border-bottom: 1px solid #e5e7eb; text-align: left; }`],
})
export class MonitoringComponent implements OnInit {
  private api = inject(IntegrationApiService);
  private userContext = inject(UserContextService);
  health = signal<ConnectorHealth[]>([]);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const orgId = orgIdOrError(this.userContext, this.error);
    if (!orgId) return;
    this.api.getConnectorHealth(orgId).pipe(catchError(() => { this.error.set('Unable to load connector health.'); return of([]); })).subscribe((d) => this.health.set(d));
  }
}
