/**
 * Resource preload helpers for critical above-the-fold assets.
 * Prefer next/font and next/image priority for most cases.
 */

export type PreloadAs =
  | 'image'
  | 'font'
  | 'style'
  | 'script'
  | 'fetch'
  | 'document';

export type PreloadDescriptor = {
  href: string;
  as: PreloadAs;
  type?: string;
  crossOrigin?: 'anonymous' | 'use-credentials';
  media?: string;
  /** imageSrcSet / imageSizes for responsive image preloads */
  imageSrcSet?: string;
  imageSizes?: string;
  fetchPriority?: 'high' | 'low' | 'auto';
};

/**
 * Build a React-friendly link preload prop bag for `<link rel="preload" />`.
 */
export function createPreloadLink(descriptor: PreloadDescriptor) {
  return {
    rel: 'preload' as const,
    href: descriptor.href,
    as: descriptor.as,
    ...(descriptor.type ? { type: descriptor.type } : {}),
    ...(descriptor.crossOrigin ? { crossOrigin: descriptor.crossOrigin } : {}),
    ...(descriptor.media ? { media: descriptor.media } : {}),
    ...(descriptor.imageSrcSet ? { imageSrcSet: descriptor.imageSrcSet } : {}),
    ...(descriptor.imageSizes ? { imageSizes: descriptor.imageSizes } : {}),
    ...(descriptor.fetchPriority
      ? { fetchPriority: descriptor.fetchPriority }
      : {}),
  };
}

/**
 * Preload a critical image (LCP candidate) when not using next/image priority.
 */
export function preloadImage(
  href: string,
  options: Omit<PreloadDescriptor, 'href' | 'as'> = {},
) {
  return createPreloadLink({
    href,
    as: 'image',
    fetchPriority: 'high',
    ...options,
  });
}
