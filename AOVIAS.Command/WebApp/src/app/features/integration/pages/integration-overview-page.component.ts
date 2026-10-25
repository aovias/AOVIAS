import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { catchError, forkJoin, of } from 'rxjs';
import { SettingsPageHeaderComponent } from '../../settings/components/settings-page-header.component';
import { SettingsComingSoonBadgeComponent } from '../../settings/components/settings-coming-soon-badge.component';
import { NotificationStatCardComponent } from '../../notifications/components/notification-stat-card.component';
import { NotificationPageStateComponent } from '../../../shared/notifications/components/notification-ui.components';
import { UserContextService } from '../../../core/services/user-context.service';
import {
  Connector,
  ConnectorHealth,
  IntegrationApiService,
  IntegrationAuditEntry,
  SynchronizationJob,
} from '../services/integration-api.service';
import { INTEGRATION_ADMIN_BASE } from '../models/integration-admin-nav.model';
import { IntegrationHealthChipComponent } from '../components/integration-health-chip.component';

@Component({
  selector: 'app-integration-overview-page',
  standalone: true,
  imports: [
    RouterLink,
    MatCardModule,
    MatIconModule,
    SettingsPageHeaderComponent,
    SettingsComingSoonBadgeComponent,
    NotificationStatCardComponent,
    NotificationPageStateComponent,
    IntegrationHealthChipComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="overview">
      <app-settings-page-header
        title="Enterprise Integrations"
        description="Configure and monitor external systems, connectivity and platform integration services."
      />
      @if (loading()) {
        <app-notification-page-state state="loading" message="Loading integration overview…" [showRetry]="false" />
      } @else if (error()) {
        <app-notification-page-state state="error" [message]="error()!" (retry)="load()" />
      } @else {
        <section class="overview__stats" aria-label="Integration summary">
          <app-notification-stat-card label="Configured integrations" [value]="stats().configured" />
          <app-notification-stat-card label="Healthy connections" [value]="stats().healthy" />
          <app-notification-stat-card label="Warning connections" [value]="stats().warning" />
          <app-notification-stat-card label="Failed connections" [value]="stats().failed" />
          <app-notification-stat-card label="Disabled integrations" [value]="stats().disabled" />
          <app-notification-stat-card label="Pending synchronizations" [value]="stats().pendingSyncs" />
        </section>

        <section class="overview__section">
          <div class="overview__section-header">
            <h2>Integration catalog</h2>
            <a class="overview__link" [routerLink]="catalogRoute">View all connectors</a>
          </div>
          @if (connectors().length === 0) {
            <app-notification-page-state
              state="empty"
              message="No integrations configured yet. Add a connector to connect external systems."
              [showRetry]="false"
            />
          } @else {
            <div class="catalog-grid">
              @for (item of catalogPreview(); track item.connector.id) {
                <article class="catalog-card">
                  <div class="catalog-card__header">
                    <strong>{{ item.connector.name }}</strong>
                    <app-integration-health-chip [status]="item.healthStatus" />
                  </div>
                  <dl class="catalog-card__meta">
                    <div><dt>Category</dt><dd>{{ item.connector.providerType }}</dd></div>
                    <div><dt>Provider</dt><dd>{{ item.connector.providerType }}</dd></div>
                    <div><dt>Status</dt><dd>{{ item.connector.lifecycleState }}</dd></div>
                    <div><dt>Connection</dt><dd>{{ item.connector.healthStatus }}</dd></div>
                  </dl>
                  <div class="catalog-card__actions">
                    <a mat-button [routerLink]="[connectorsRoute, item.connector.id]">View</a>
                    <a mat-button [routerLink]="[connectorsRoute, item.connector.id]">Configure</a>
                  </div>
                </article>
              }
            </div>
          }
        </section>

        <section class="overview__cards" aria-label="Integration areas">
          @for (card of areaCards; track card.route) {
            <a class="overview-card" [routerLink]="card.route">
              <mat-icon aria-hidden="true">{{ card.icon }}</mat-icon>
              <div>
                <strong>{{ card.label }}</strong>
                <span>{{ card.description }}</span>
              </div>
            </a>
          }
        </section>

        <section class="overview__section">
          <h2>Future capabilities</h2>
          <div class="future-grid">
            @for (item of futurePlaceholders; track item.label) {
              <div class="future-card">
                <mat-icon aria-hidden="true">{{ item.icon }}</mat-icon>
                <div>
                  <strong>{{ item.label }}</strong>
                  <span>{{ item.description }}</span>
                </div>
                <app-settings-coming-soon-badge />
              </div>
            }
          </div>
        </section>
      }
    </div>
  `,
  styles: [
    `
      .overview__stats {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
        gap: 12px;
        margin-bottom: 28px;
      }
      .overview__section {
        margin-bottom: 28px;
      }
      .overview__section-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
        margin-bottom: 12px;
      }
      .overview__section h2 {
        margin: 0;
        font-size: var(--font-size-section-title);
        font-weight: 600;
      }
      .overview__link {
        font-size: 13px;
        color: var(--color-primary);
        text-decoration: none;
      }
      .catalog-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
        gap: 12px;
      }
      .catalog-card {
        padding: 16px;
        border-radius: 16px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        background: var(--color-bg-surface, #fff);
        transition: box-shadow 0.2s ease, transform 0.2s ease;
      }
      .catalog-card:hover {
        box-shadow: 0 8px 24px color-mix(in srgb, #000 6%, transparent);
        transform: translateY(-1px);
      }
      .catalog-card__header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 8px;
        margin-bottom: 12px;
      }
      .catalog-card__meta {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 8px 12px;
        margin: 0 0 12px;
        font-size: 12px;
      }
      .catalog-card__meta dt {
        color: var(--color-text-secondary, #6b7280);
        margin: 0;
      }
      .catalog-card__meta dd {
        margin: 2px 0 0;
        font-weight: 500;
      }
      .catalog-card__actions {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
      }
      .catalog-card__actions a {
        font-size: 13px;
        color: var(--color-primary);
        text-decoration: none;
        font-weight: 500;
      }
      .overview__cards {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
        gap: 12px;
        margin-bottom: 28px;
      }
      .overview-card {
        display: flex;
        gap: 12px;
        padding: 16px;
        border-radius: 16px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        text-decoration: none;
        color: inherit;
        transition: box-shadow 0.2s ease, transform 0.2s ease;
      }
      .overview-card:hover {
        box-shadow: 0 8px 24px color-mix(in srgb, #000 6%, transparent);
        transform: translateY(-1px);
      }
      .overview-card mat-icon {
        color: var(--color-primary);
      }
      .overview-card strong {
        display: block;
        margin-bottom: 4px;
      }
      .overview-card span {
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
      .future-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
        gap: 12px;
      }
      .future-card {
        display: flex;
        flex-direction: column;
        gap: 8px;
        padding: 16px;
        border-radius: 16px;
        border: 1px dashed var(--color-border-subtle, #e5e7eb);
        background: color-mix(in srgb, var(--color-bg-muted, #f9fafb) 80%, transparent);
      }
      .future-card mat-icon {
        color: var(--color-text-secondary, #6b7280);
      }
      .future-card strong {
        display: block;
      }
      .future-card span {
        font-size: 12px;
        color: var(--color-text-secondary, #6b7280);
      }
    `,
  ],
})
export class IntegrationOverviewPageComponent implements OnInit {
  private readonly api = inject(IntegrationApiService);
  private readonly userContext = inject(UserContextService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly connectors = signal<Connector[]>([]);
  readonly health = signal<ConnectorHealth[]>([]);
  readonly syncJobs = signal<SynchronizationJob[]>([]);

  readonly catalogRoute = `${INTEGRATION_ADMIN_BASE}/connectors`;
  readonly connectorsRoute = `${INTEGRATION_ADMIN_BASE}/connectors`;

  readonly stats = computed(() => {
    const connectors = this.connectors();
    const health = this.health();
    const syncJobs = this.syncJobs();
    const healthById = new Map(health.map((h) => [h.connectorId, h.status]));

    let healthy = 0;
    let warning = 0;
    let failed = 0;
    let disabled = 0;

    for (const c of connectors) {
      const status = (healthById.get(c.id) ?? c.healthStatus ?? 'unknown').toLowerCase();
      if (c.lifecycleState === 'Suspended' || c.lifecycleState === 'Disconnected') {
        disabled++;
        continue;
      }
      if (status.includes('healthy') || status.includes('connected')) healthy++;
      else if (status.includes('warning') || status.includes('degraded')) warning++;
      else if (status.includes('fail') || status.includes('offline') || status.includes('unhealthy')) failed++;
    }

    const pendingSyncs = syncJobs.filter((j) => j.isEnabled).length;

    return {
      configured: connectors.length,
      healthy,
      warning,
      failed,
      disabled,
      pendingSyncs,
    };
  });

  readonly catalogPreview = computed(() => {
    const healthById = new Map(this.health().map((h) => [h.connectorId, h]));
    return this.connectors()
      .slice(0, 6)
      .map((connector) => ({
        connector,
        healthStatus: healthById.get(connector.id)?.status ?? connector.healthStatus ?? 'Unknown',
      }));
  });

  readonly areaCards = [
    { label: 'Connectors', description: 'Configure HIS, LIS, LDAP, SMTP, webhooks, and more.', route: `${INTEGRATION_ADMIN_BASE}/connectors`, icon: 'hub' },
    { label: 'Mappings', description: 'Field mapping profiles between external and platform entities.', route: `${INTEGRATION_ADMIN_BASE}/mappings`, icon: 'swap_horiz' },
    { label: 'Synchronizations', description: 'Scheduled and on-demand data synchronization jobs.', route: `${INTEGRATION_ADMIN_BASE}/synchronizations`, icon: 'sync' },
    { label: 'Webhooks', description: 'Inbound and outbound webhook subscriptions and delivery logs.', route: `${INTEGRATION_ADMIN_BASE}/webhooks`, icon: 'webhook' },
    { label: 'Monitoring', description: 'Connector health, latency, and success rates.', route: `${INTEGRATION_ADMIN_BASE}/monitoring`, icon: 'monitor_heart' },
    { label: 'Audit', description: 'Configuration changes, tests, and integration operations.', route: `${INTEGRATION_ADMIN_BASE}/audit`, icon: 'history' },
  ];

  readonly futurePlaceholders = [
    { label: 'Marketplace', description: 'Discover certified integration packages.', icon: 'storefront' },
    { label: 'Integration Templates', description: 'Pre-built connector and mapping templates.', icon: 'library_books' },
    { label: 'Integration Packages', description: 'Versioned bundles for deployment.', icon: 'inventory_2' },
    { label: 'Custom Connectors', description: 'Build and register custom connectors.', icon: 'extension' },
    { label: 'API Explorer', description: 'Browse integration APIs interactively.', icon: 'api' },
  ];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const orgId = this.userContext.getOrganization()?.id;
    if (!orgId) {
      this.loading.set(false);
      this.error.set('Organization context is not available.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      connectors: this.api.listConnectors(orgId).pipe(catchError(() => of([] as Connector[]))),
      health: this.api.getConnectorHealth(orgId).pipe(catchError(() => of([] as ConnectorHealth[]))),
      syncJobs: this.api.listSyncJobs(orgId).pipe(catchError(() => of([] as SynchronizationJob[]))),
    }).subscribe({
      next: ({ connectors, health, syncJobs }) => {
        this.connectors.set(connectors);
        this.health.set(health);
        this.syncJobs.set(syncJobs);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load integration overview.');
        this.loading.set(false);
      },
    });
  }
}
