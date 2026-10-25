import { Routes } from '@angular/router';
import { permissionGuard } from '../../shared/authorization/permission.guard';

export const NOTIFICATION_ADMIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/notification-admin-shell.component').then((m) => m.NotificationAdminShellComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/notification-overview-page.component').then((m) => m.NotificationOverviewPageComponent),
      },
      {
        path: 'templates',
        loadComponent: () =>
          import('./pages/notification-templates-page.component').then((m) => m.NotificationTemplatesPageComponent),
        canActivate: [permissionGuard('Notification.Template.Manage')],
      },
      {
        path: 'channels',
        loadComponent: () =>
          import('./pages/notification-channels-page.component').then((m) => m.NotificationChannelsPageComponent),
        canActivate: [permissionGuard('Notification.View')],
      },
      {
        path: 'providers',
        loadComponent: () =>
          import('./pages/notification-providers-page.component').then((m) => m.NotificationProvidersPageComponent),
        canActivate: [permissionGuard('Notification.Provider.Manage')],
      },
      {
        path: 'queues',
        loadComponent: () =>
          import('./pages/notification-queues-page.component').then((m) => m.NotificationQueuesPageComponent),
        canActivate: [permissionGuard('Notification.Queue.View')],
      },
      {
        path: 'history',
        loadComponent: () =>
          import('./pages/notification-delivery-history-page.component').then(
            (m) => m.NotificationDeliveryHistoryPageComponent,
          ),
        canActivate: [permissionGuard('Notification.History.View')],
      },
      {
        path: 'preferences',
        loadComponent: () =>
          import('./pages/notification-preferences-page.component').then((m) => m.NotificationPreferencesPageComponent),
        canActivate: [permissionGuard('Notification.Preference.Manage')],
      },
      {
        path: 'subscriptions',
        loadComponent: () =>
          import('./pages/notification-subscriptions-page.component').then(
            (m) => m.NotificationSubscriptionsPageComponent,
          ),
        canActivate: [permissionGuard('Notification.View')],
      },
    ],
  },
];

/** @deprecated Use NOTIFICATION_ADMIN_ROUTES */
export const COMMUNICATION_ROUTES = NOTIFICATION_ADMIN_ROUTES;
