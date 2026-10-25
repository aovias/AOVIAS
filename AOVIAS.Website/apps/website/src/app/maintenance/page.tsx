import { createPageMetadata } from '@/lib/seo';

import { MaintenanceView } from './maintenance-view';

export const metadata = createPageMetadata({
  title: 'Maintenance',
  description: 'AOVIAS is undergoing scheduled maintenance.',
  path: '/maintenance',
  type: 'marketing',
  noIndex: true,
  absoluteTitle: true,
});

/**
 * Maintenance surface — also reachable via MAINTENANCE_MODE middleware rewrite.
 */
export default function MaintenancePage() {
  return <MaintenanceView />;
}
