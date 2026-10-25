import type { ReactNode } from 'react';

import { OptimizedImage } from '@/components/media';
import { Code, Surface, Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * Media Slot Architecture — typed visual areas for product components.
 * Render screenshots, diagrams, video, code, demos, or placeholders without
 * coupling components to a specific product or media type.
 */

export type MediaAspect = 'video' | 'square' | 'wide' | 'auto';

type MediaSlotBase = {
  /** Accessible label when visual content is not self-describing. */
  alt?: string;
  className?: string;
  aspect?: MediaAspect;
  priority?: boolean;
};

export type ImageMediaSlot = MediaSlotBase & {
  kind: 'image' | 'screenshot' | 'diagram';
  src: string;
  alt: string;
  width?: number;
  height?: number;
};

export type VideoMediaSlot = MediaSlotBase & {
  kind: 'video';
  src: string;
  alt: string;
  poster?: string;
};

export type CodeMediaSlot = MediaSlotBase & {
  kind: 'code';
  code: string;
  language?: string;
};

export type PlaceholderMediaSlot = MediaSlotBase & {
  kind: 'placeholder';
  label: string;
};

export type NodeMediaSlot = MediaSlotBase & {
  kind: 'custom' | 'demo' | 'animation';
  node: ReactNode;
};

export type MediaSlot =
  | ImageMediaSlot
  | VideoMediaSlot
  | CodeMediaSlot
  | PlaceholderMediaSlot
  | NodeMediaSlot;

const aspectClass: Record<MediaAspect, string> = {
  video: 'aspect-video',
  square: 'aspect-square',
  wide: 'aspect-[21/9]',
  auto: '',
};

export type MediaSlotViewProps = {
  media: MediaSlot;
  className?: string;
  /** Image sizes preset for OptimizedImage. */
  preset?: 'content' | 'hero' | 'thumbnail' | 'full';
};

/**
 * Renders a MediaSlot value. Prefer this over ad-hoc image/video markup in product UI.
 */
export function MediaSlotView({
  media,
  className,
  preset = 'content',
}: MediaSlotViewProps) {
  const aspect = media.aspect ?? (media.kind === 'code' ? 'auto' : 'video');
  const shell = cn(
    'border-border from-card to-surface-muted/60 shadow-raised relative w-full overflow-hidden rounded-xl border bg-gradient-to-br',
    'before:from-domain-accent/40 before:absolute before:inset-x-0 before:top-0 before:h-px before:bg-gradient-to-r before:to-transparent',
    aspectClass[aspect],
    media.className,
    className,
  );

  switch (media.kind) {
    case 'image':
    case 'screenshot':
    case 'diagram':
      return (
        <div className={shell}>
          <OptimizedImage
            src={media.src}
            alt={media.alt}
            width={media.width ?? 1280}
            height={media.height ?? 720}
            preset={preset}
            priority={media.priority ?? false}
            className="size-full object-cover"
            useDefaultBlur
          />
        </div>
      );
    case 'video':
      return (
        <div className={shell}>
          <video
            className="size-full object-cover"
            controls
            playsInline
            aria-label={media.alt}
            {...(media.poster ? { poster: media.poster } : {})}
          >
            <source src={media.src} />
          </video>
        </div>
      );
    case 'code':
      return (
        <Surface tone="muted" padding="md" className={cn('rounded-lg', className)}>
          <Code block>{media.code}</Code>
          {media.language ? (
            <Text as="p" size="caption" tone="muted" className="mt-2">
              {media.language}
            </Text>
          ) : null}
        </Surface>
      );
    case 'placeholder':
      return (
        <div
          className={cn(shell, 'flex items-center justify-center p-6')}
          role="img"
          aria-label={media.alt ?? media.label}
        >
          <Text tone="muted" size="small" weight="medium">
            {media.label}
          </Text>
        </div>
      );
    case 'custom':
    case 'demo':
    case 'animation':
      return (
        <div
          className={cn(
            'relative w-full overflow-hidden rounded-lg',
            aspectClass[aspect],
            media.className,
            className,
          )}
          {...(media.alt ? { role: 'img', 'aria-label': media.alt } : {})}
        >
          {media.node}
        </div>
      );
    default: {
      const _exhaustive: never = media;
      return _exhaustive;
    }
  }
}
