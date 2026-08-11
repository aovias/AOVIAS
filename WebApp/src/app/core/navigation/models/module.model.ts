import type { SidebarSemanticIconId } from '../../layout/sidebar-icons.registry';
import type { NavigationFeatureKey } from './feature.model';

export interface NavigationModule {
  readonly id: string;
  readonly name: string;
  readonly icon: SidebarSemanticIconId;
  readonly description?: string;
  readonly order: number;
  readonly featureKey?: NavigationFeatureKey;
  readonly enabled: boolean;
  readonly defaultRoute: string;
  readonly color?: string;
  /** When set, module visibility requires one of these roles (header Compliance Graph). */
  readonly roleKeys?: readonly string[];
}
