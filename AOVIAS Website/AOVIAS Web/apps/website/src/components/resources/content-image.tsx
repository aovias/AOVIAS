import { KnowledgeCover } from '@/components/brand';
import type { ContentImage } from '@/components/content';
import { OptimizedImage } from '@/components/media';
import { Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

type ContentImageViewProps = {
  image: ContentImage;
  className?: string;
  priority?: boolean;
};

const aspectClass = {
  video: 'aspect-video',
  wide: 'aspect-[21/9]',
  square: 'aspect-square',
} as const;

/**
 * Shared media renderer for ContentImage (OptimizedImage, brand cover, or empty placeholder).
 */
export function ContentImageView({
  image,
  className,
  priority = false,
}: ContentImageViewProps) {
  if (image.kind === 'cover') {
    return (
      <KnowledgeCover
        label={image.label}
        tone={image.tone ?? 'default'}
        className={cn(aspectClass[image.aspect ?? 'video'], className)}
      />
    );
  }

  if (image.kind === 'placeholder' || !('src' in image)) {
    const aspect = image.kind === 'placeholder' ? (image.aspect ?? 'video') : 'video';
    const label =
      image.kind === 'placeholder' ? image.label : 'Image placeholder';
    return (
      <div
        role="img"
        aria-label={label}
        className={cn(
          'border-border bg-muted text-muted-foreground flex items-center justify-center rounded-md border border-dashed',
          aspectClass[aspect],
          className,
        )}
      >
        <Text as="span" size="caption">
          {label}
        </Text>
      </div>
    );
  }

  return (
    <OptimizedImage
      src={image.src}
      alt={image.alt}
      width={image.width ?? 1200}
      height={image.height ?? 675}
      preset="thumbnail"
      priority={priority}
      className={cn('h-full w-full rounded-md object-cover', className)}
    />
  );
}
