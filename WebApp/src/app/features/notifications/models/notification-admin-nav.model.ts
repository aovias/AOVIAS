export const NOTIFICATION_ADMIN_BASE = '/app/settings/notifications';

export interface NotificationAdminNavItem {
  label: string;
  route: string;
  segment: string;
  icon: string;
  permission?: string;
}

export function getNotificationAdminNavItems(): NotificationAdminNavItem[] {
  return [
    { label: 'Overview', route: NOTIFICATION_ADMIN_BASE, segment: '', icon: 'dashboard' },
    { label: 'Templates', route: `${NOTIFICATION_ADMIN_BASE}/templates`, segment: 'templates', icon: 'description', permission: 'Notification.Template.Manage' },
    { label: 'Channels', route: `${NOTIFICATION_ADMIN_BASE}/channels`, segment: 'channels', icon: 'hub' },
    { label: 'Delivery Providers', route: `${NOTIFICATION_ADMIN_BASE}/providers`, segment: 'providers', icon: 'settings_input_component', permission: 'Notification.Provider.Manage' },
    { label: 'Queues', route: `${NOTIFICATION_ADMIN_BASE}/queues`, segment: 'queues', icon: 'queue', permission: 'Notification.Queue.View' },
    { label: 'Delivery History', route: `${NOTIFICATION_ADMIN_BASE}/history`, segment: 'history', icon: 'history', permission: 'Notification.History.View' },
    { label: 'Preferences', route: `${NOTIFICATION_ADMIN_BASE}/preferences`, segment: 'preferences', icon: 'tune', permission: 'Notification.Preference.Manage' },
    { label: 'Subscriptions', route: `${NOTIFICATION_ADMIN_BASE}/subscriptions`, segment: 'subscriptions', icon: 'subscriptions' },
  ];
}

export function resolveNotificationAdminBreadcrumb(url: string): string {
  const path = url.split(/[?#]/)[0].replace(/\/+$/, '');
  const items = getNotificationAdminNavItems().slice().sort((a, b) => b.route.length - a.route.length);
  for (const item of items) {
    if (path === item.route) return item.label;
    if (item.segment && path.startsWith(`${NOTIFICATION_ADMIN_BASE}/${item.segment}`)) return item.label;
  }
  return 'Notifications';
}
