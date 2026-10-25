import type { ImageProps } from 'next/image';

import { imageDefaults } from '@/config/performance';

/**
 * Image sizing presets — use with OptimizedImage `sizes` prop.
 */
export const imageSizePresets = {
  content: imageDefaults.contentSizes,
  hero: imageDefaults.heroSizes,
  thumbnail: imageDefaults.thumbnailSizes,
  full: '100vw',
} as const;

export type ImageSizePreset = keyof typeof imageSizePresets;

export type OptimizedImageOptions = {
  /** Named sizes preset; ignored if `sizes` is provided. */
  preset?: ImageSizePreset;
  /** Priority for LCP candidates (disables lazy load). */
  priority?: boolean;
  /** Blur / empty / data URL placeholder. */
  placeholder?: ImageProps['placeholder'];
  blurDataURL?: string;
};

/**
 * Resolve `sizes` from a preset or explicit value.
 */
export function resolveImageSizes(
  sizes?: string,
  preset: ImageSizePreset = 'content',
): string {
  return sizes ?? imageSizePresets[preset];
}

/**
 * Recommended defaults for next/image consumers.
 * Prefer `<OptimizedImage>` over raw `<img>`.
 */
export function getOptimizedImageDefaults(
  options: OptimizedImageOptions = {},
): Pick<ImageProps, 'sizes' | 'loading' | 'priority' | 'placeholder' | 'blurDataURL'> {
  const priority = options.priority ?? false;
  const preset = options.preset ?? 'content';

  return {
    sizes: resolveImageSizes(undefined, preset),
    priority,
    loading: priority ? undefined : 'lazy',
    ...(options.placeholder ? { placeholder: options.placeholder } : {}),
    ...(options.blurDataURL ? { blurDataURL: options.blurDataURL } : {}),
  };
}

/** Tiny neutral blur data URL for placeholders when no image-specific blur is available. */
export const defaultBlurDataURL =
  'data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAYEBQYFBAYGBQYHBwYIChAKCgkJChQODwwQFxQYGBcUFhYaHSUfGhsjHBYWICwgIyYnKSopGR8tMC0oMCUoKSj/2wBDAQcHBwoIChMKChMoGhYaKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCj/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAn/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIQAxAAAAGcP//EABQQAQAAAAAAAAAAAAAAAAAAAAD/2gAIAQEAAQUCf//EABQRAQAAAAAAAAAAAAAAAAAAAAD/2gAIAQMBAT8Bf//EABQRAQAAAAAAAAAAAAAAAAAAAAD/2gAIAQIBAT8Bf//Z';
