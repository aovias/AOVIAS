import { ADMINISTRATION_CONFIGURATION_CATALOGS } from './administration-configuration.catalogs';
import { AUDIT_CONFIGURATION_CATALOGS } from './audit-configuration.catalogs';
import type { ConfigurationCatalog } from './configuration-catalog';

/**
 * Every configuration category the platform knows about.
 *
 * <p>
 * The single registry the store seeds from, the loader requests and the Configuration Center lists.
 * Splitting the declarations by owning domain keeps each file readable; joining them here is what
 * keeps there being **one** store, one loader and one editor rather than one per domain.
 * </p>
 *
 * <p>
 * Adding a domain means adding a catalog to one of the two modules. Nothing here, in the store, in
 * the loader or in the editor changes — which is the property this registry exists to preserve.
 * </p>
 */
export const PLATFORM_CONFIGURATION_CATALOGS: readonly ConfigurationCatalog[] = [
  ...AUDIT_CONFIGURATION_CATALOGS,
  ...ADMINISTRATION_CONFIGURATION_CATALOGS,
];

/** The catalog for a category code, or `undefined` when it is not registered. */
export function catalogFor(code: string): ConfigurationCatalog | undefined {
  return PLATFORM_CONFIGURATION_CATALOGS.find((c) => c.code === code);
}
