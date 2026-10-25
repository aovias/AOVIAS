import dynamic, { type DynamicOptions, type Loader } from 'next/dynamic';
import type { ComponentType } from 'react';

/**
 * Lazy-loading helpers for non-critical client components and heavy libraries.
 * Prefer Server Components; only dynamic-import what is below-the-fold or optional.
 */

export type LazyOptions<P> = {
  /** Optional loading UI while the chunk loads. */
  loading?: DynamicOptions<P>['loading'];
  /** Disable SSR when the module requires browser APIs. */
  ssr?: boolean;
};

/**
 * Typed wrapper around `next/dynamic` with project defaults (SSR on).
 */
export function lazyComponent<P extends object>(
  loader: Loader<P>,
  options: LazyOptions<P> = {},
): ComponentType<P> {
  return dynamic(loader, {
    ssr: options.ssr ?? true,
    ...(options.loading ? { loading: options.loading } : {}),
  });
}

/**
 * Dynamically import a heavy library only when needed (client-side).
 * @example
 * const mod = await lazyImport(() => import('framer-motion'));
 */
export async function lazyImport<T>(loader: () => Promise<T>): Promise<T> {
  return loader();
}

/**
 * Create a named dynamic import factory for reuse.
 */
export function createLazyImporter<T>(loader: () => Promise<T>) {
  let promise: Promise<T> | null = null;

  return () => {
    promise ??= loader();
    return promise;
  };
}
