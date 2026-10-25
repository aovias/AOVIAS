/**
 * Adapter pattern — swap mock / HTTP / GraphQL behind a stable client interface.
 */

export type AdapterMode = 'mock' | 'http' | 'graphql' | 'static';

export type AdapterMeta = {
  id: string;
  mode: AdapterMode;
  /** Human label for diagnostics. */
  label: string;
};

/**
 * Factory that produces a domain client for the active environment.
 */
export type SdkAdapterFactory<TClient> = () => TClient;

export type SdkAdapterRegistration<TClient> = AdapterMeta & {
  create: SdkAdapterFactory<TClient>;
};
