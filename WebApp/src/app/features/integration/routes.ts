import { Routes } from '@angular/router';
import { permissionGuard } from '../../shared/authorization/permission.guard';

export const INTEGRATION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/integration-admin-shell.component').then((m) => m.IntegrationAdminShellComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/integration-overview-page.component').then((m) => m.IntegrationOverviewPageComponent),
      },
      {
        path: 'connectors',
        loadComponent: () => import('./pages/integration-pages.component').then((m) => m.ConnectorsListComponent),
        canActivate: [permissionGuard('Integration.Connector.Read')],
      },
      {
        path: 'connectors/:id',
        loadComponent: () => import('./pages/integration-pages.component').then((m) => m.ConnectorDetailComponent),
        canActivate: [permissionGuard('Integration.Connector.Read')],
      },
      {
        path: 'mappings',
        loadComponent: () => import('./pages/integration-pages.component').then((m) => m.MappingsComponent),
        canActivate: [permissionGuard('Integration.Mapping.Read')],
      },
      {
        path: 'synchronizations',
        loadComponent: () => import('./pages/integration-pages.component').then((m) => m.SynchronizationsComponent),
        canActivate: [permissionGuard('Integration.Sync.Read')],
      },
      {
        path: 'webhooks',
        loadComponent: () => import('./pages/integration-pages.component').then((m) => m.WebhooksComponent),
        canActivate: [permissionGuard('Integration.Webhook.Read')],
      },
      {
        path: 'imports',
        loadComponent: () => import('./pages/integration-pages.component').then((m) => m.ImportsComponent),
        canActivate: [permissionGuard('Integration.Import.Execute')],
      },
      {
        path: 'exports',
        loadComponent: () => import('./pages/integration-pages.component').then((m) => m.ExportsComponent),
        canActivate: [permissionGuard('Integration.Export.Execute')],
      },
      {
        path: 'audit',
        loadComponent: () => import('./pages/integration-pages.component').then((m) => m.IntegrationAuditComponent),
        canActivate: [permissionGuard('Integration.Audit.Read')],
      },
      {
        path: 'monitoring',
        loadComponent: () => import('./pages/integration-pages.component').then((m) => m.MonitoringComponent),
        canActivate: [permissionGuard('Integration.Monitor.Read')],
      },
    ],
  },
];
