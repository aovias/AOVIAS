import { environment } from '../../../../environments/environment';

export type AdministrationEnvironmentLabel = 'Production' | 'Development' | 'Demo';

export function getAdministrationEnvironmentLabel(): AdministrationEnvironmentLabel {
  if (typeof window !== 'undefined') {
    const host = window.location.hostname.toLowerCase();
    if (host.includes('demo')) {
      return 'Demo';
    }
  }
  return environment.production ? 'Production' : 'Development';
}
