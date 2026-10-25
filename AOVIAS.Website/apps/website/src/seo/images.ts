import { seoConfig } from '@/config/seo';

/** Central OG / social preview image paths — prefer these over hardcoded strings. */
export const ogImages = seoConfig.ogImages;

export type OgImageKey = keyof typeof ogImages;
