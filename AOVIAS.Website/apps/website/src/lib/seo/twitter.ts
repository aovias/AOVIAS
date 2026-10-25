import type { Metadata } from 'next';

import { seoConfig } from '@/config/seo';

import { buildAbsoluteUrl } from './canonical';

export type TwitterCardType = 'summary' | 'summary_large_image';

export type TwitterCardInput = {
  title: string;
  description: string;
  imagePath?: string;
  card?: TwitterCardType;
  site?: string;
  creator?: string;
};

/**
 * Build Next.js Twitter/X card metadata from centralized defaults.
 */
export function buildTwitterCard(
  input: TwitterCardInput,
): NonNullable<Metadata['twitter']> {
  const card = input.card ?? seoConfig.twitter.card;
  const imagePath = input.imagePath ?? seoConfig.defaultOgImage.path;

  return {
    card,
    title: input.title,
    description: input.description,
    site: input.site ?? seoConfig.twitter.site,
    creator: input.creator ?? seoConfig.twitter.creator,
    images: [buildAbsoluteUrl(imagePath)],
  };
}
