import type { Metadata } from 'next';

import type { OpenGraphType } from '@/config/metadata';
import { seoConfig } from '@/config/seo';

import { buildAbsoluteUrl, buildCanonicalUrl } from './canonical';

export type OpenGraphInput = {
  title: string;
  description: string;
  path: string;
  type?: OpenGraphType;
  imagePath?: string;
  imageAlt?: string;
  locale?: string;
  siteName?: string;
};

/**
 * Build Next.js Open Graph metadata from centralized defaults.
 */
export function buildOpenGraph(input: OpenGraphInput): NonNullable<Metadata['openGraph']> {
  const imagePath =
    input.imagePath ?? seoConfig.ogImages.default ?? seoConfig.defaultOgImage.path;
  const imageAlt = input.imageAlt ?? seoConfig.defaultOgImage.alt;

  return {
    type: input.type ?? 'website',
    locale: input.locale ?? seoConfig.locale,
    url: buildCanonicalUrl(input.path),
    title: input.title,
    description: input.description,
    siteName: input.siteName ?? seoConfig.siteName,
    images: [
      {
        url: buildAbsoluteUrl(imagePath),
        width: seoConfig.defaultOgImage.width,
        height: seoConfig.defaultOgImage.height,
        alt: imageAlt,
      },
    ],
  };
}
