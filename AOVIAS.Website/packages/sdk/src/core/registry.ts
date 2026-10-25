import type { SdkAdapterRegistration } from './adapter';

/**
 * Generic typed registry for SDK clients.
 * Composition roots register adapters; consumers resolve by key.
 */

export type SdkRegistryKey =
  | 'identity'
  | 'commerce'
  | 'contact'
  | 'knowledge'
  | 'search';

export class SdkClientRegistry {
  private readonly factories = new Map<string, SdkAdapterRegistration<unknown>>();
  private readonly cache = new Map<string, unknown>();

  register<TClient>(
    key: SdkRegistryKey | string,
    registration: SdkAdapterRegistration<TClient>,
  ): void {
    this.factories.set(key, registration as SdkAdapterRegistration<unknown>);
    this.cache.delete(key);
  }

  resolve<TClient>(key: SdkRegistryKey | string): TClient {
    const cached = this.cache.get(key);
    if (cached !== undefined) {
      return cached as TClient;
    }
    const registration = this.factories.get(key);
    if (!registration) {
      throw new Error(`SDK client not registered: ${key}`);
    }
    const client = registration.create();
    this.cache.set(key, client);
    return client as TClient;
  }

  has(key: SdkRegistryKey | string): boolean {
    return this.factories.has(key);
  }

  mode(key: SdkRegistryKey | string): string | undefined {
    return this.factories.get(key)?.mode;
  }

  clearCache(): void {
    this.cache.clear();
  }
}
