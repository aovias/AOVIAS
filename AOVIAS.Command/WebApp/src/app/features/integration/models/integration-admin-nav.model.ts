/** Stage 5A: integrations live under the Administration Workspace "System" tab. */
export const INTEGRATION_ADMIN_BASE = '/app/settings/system/integrations';

export interface IntegrationAdminNavItem {
  label: string;
  route: string;
  segment: string;
  icon: string;
}

export function getIntegrationAdminNavItems(): IntegrationAdminNavItem[] {
  return [
    { label: 'Overview', route: INTEGRATION_ADMIN_BASE, segment: '', icon: 'dashboard' },
    { label: 'Connectors', route: `${INTEGRATION_ADMIN_BASE}/connectors`, segment: 'connectors', icon: 'hub' },
    { label: 'Mappings', route: `${INTEGRATION_ADMIN_BASE}/mappings`, segment: 'mappings', icon: 'swap_horiz' },
    { label: 'Synchronizations', route: `${INTEGRATION_ADMIN_BASE}/synchronizations`, segment: 'synchronizations', icon: 'sync' },
    { label: 'Webhooks', route: `${INTEGRATION_ADMIN_BASE}/webhooks`, segment: 'webhooks', icon: 'webhook' },
    { label: 'Imports', route: `${INTEGRATION_ADMIN_BASE}/imports`, segment: 'imports', icon: 'upload' },
    { label: 'Exports', route: `${INTEGRATION_ADMIN_BASE}/exports`, segment: 'exports', icon: 'download' },
    { label: 'Monitoring', route: `${INTEGRATION_ADMIN_BASE}/monitoring`, segment: 'monitoring', icon: 'monitor_heart' },
    { label: 'Audit', route: `${INTEGRATION_ADMIN_BASE}/audit`, segment: 'audit', icon: 'history' },
  ];
}

export function resolveIntegrationAdminBreadcrumb(url: string): string {
  const path = url.split(/[?#]/)[0].replace(/\/+$/, '');
  const items = getIntegrationAdminNavItems().slice().sort((a, b) => b.route.length - a.route.length);
  for (const item of items) {
    if (path === item.route) return item.label;
    if (item.segment && path.startsWith(`${INTEGRATION_ADMIN_BASE}/${item.segment}`)) return item.label;
  }
  return 'Enterprise Integrations';
}
