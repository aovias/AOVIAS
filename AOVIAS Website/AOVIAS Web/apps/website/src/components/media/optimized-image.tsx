import Image, { type ImageProps } from 'next/image';

import {
  defaultBlurDataURL,
  resolveImageSizes,
  type ImageSizePreset,
} from '@/lib/performance/images';
import { cn } from '@/lib/utils';

export type OptimizedImageProps = Omit<ImageProps, 'loading'> & {
  /** Responsive sizes preset when `sizes` is omitted. */
  preset?: ImageSizePreset;
  /** Use default blur placeholder when `placeholder="blur"` and no blurDataURL. */
  useDefaultBlur?: boolean;
};

/**
 * Canonical image component — wraps `next/image` with project defaults.
 * Prefer this over raw `<img>` (except justified SVG icons / tracking pixels).
 */
export function OptimizedImage({
  className,
  preset = 'content',
  sizes,
  priority = false,
  placeholder,
  blurDataURL,
  useDefaultBlur = false,
  alt,
  ...props
}: OptimizedImageProps) {
  const resolvedPlaceholder = placeholder ?? (useDefaultBlur ? 'blur' : undefined);
  const resolvedBlur =
    blurDataURL ?? (resolvedPlaceholder === 'blur' ? defaultBlurDataURL : undefined);

  return (
    <Image
      alt={alt}
      className={cn(className)}
      sizes={resolveImageSizes(sizes, preset)}
      priority={priority}
      {...(priority ? {} : { loading: 'lazy' as const })}
      {...(resolvedPlaceholder ? { placeholder: resolvedPlaceholder } : {})}
      {...(resolvedBlur ? { blurDataURL: resolvedBlur } : {})}
      {...props}
    />
  );
}
