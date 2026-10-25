'use client';

import { ChevronLeft, ChevronRight, X } from 'lucide-react';
import {
  forwardRef,
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
} from 'react';

import { MediaSlotView, type MediaSlot } from '@/components/product/media-slot';
import { Caption, Grid, IconButton } from '@/components/primitives';
import { useBodyScrollLock, useFocusTrap } from '@/lib/navigation/use-focus-trap';
import { cn } from '@/lib/utils';

export type ScreenshotItem = {
  id: string;
  /** Prefer screenshot / image / placeholder media slots. */
  media: MediaSlot;
  caption?: string;
};

export type ScreenshotGalleryProps = {
  items: readonly ScreenshotItem[];
  mode?: 'grid' | 'carousel';
  className?: string;
  /** Enable modal preview on item activate. */
  preview?: boolean;
};

type GalleryModalProps = {
  titleId: string;
  item: ScreenshotItem;
  index: number;
  total: number;
  onClose: () => void;
  onPrev: () => void;
  onNext: () => void;
};

const GalleryModal = forwardRef<HTMLDivElement, GalleryModalProps>(
  function GalleryModal(
    { titleId, item, index, total, onClose, onPrev, onNext },
    ref,
  ) {
    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div
          className="bg-background/80 absolute inset-0 backdrop-blur-sm"
          aria-hidden="true"
          onClick={onClose}
        />
        <div
          ref={ref}
          role="dialog"
          aria-modal="true"
          aria-labelledby={titleId}
          className="border-border bg-card shadow-modal relative z-10 w-full max-w-4xl rounded-lg border p-4"
        >
          <div className="mb-3 flex items-center justify-between gap-3">
            <h2 id={titleId} className="text-foreground text-small font-medium">
              {item.caption ?? `Screenshot ${index + 1} of ${total}`}
            </h2>
            <IconButton aria-label="Close preview" variant="ghost" onClick={onClose}>
              <X />
            </IconButton>
          </div>
          <div role="status" aria-live="polite" className="sr-only">
            {item.caption
              ? `${item.caption}. Image ${index + 1} of ${total}.`
              : `Image ${index + 1} of ${total}.`}
          </div>
          <MediaSlotView media={item.media} preset="hero" />
          <div className="mt-4 flex items-center justify-between">
            <IconButton aria-label="Previous" variant="outline" onClick={onPrev}>
              <ChevronLeft />
            </IconButton>
            <Caption>
              {index + 1} / {total}
            </Caption>
            <IconButton aria-label="Next" variant="outline" onClick={onNext}>
              <ChevronRight />
            </IconButton>
          </div>
        </div>
      </div>
    );
  },
);

/**
 * ScreenshotGallery — grid or carousel with optional modal preview.
 * Items are MediaSlots (screenshots, placeholders, etc.).
 */
export function ScreenshotGallery({
  items,
  mode = 'grid',
  className,
  preview = true,
}: ScreenshotGalleryProps) {
  const [activeIndex, setActiveIndex] = useState(0);
  const [open, setOpen] = useState(false);
  const dialogRef = useRef<HTMLDivElement>(null);
  const titleId = useId();

  const close = useCallback(() => {
    setOpen(false);
  }, []);

  useFocusTrap(dialogRef, open, close);
  useBodyScrollLock(open);

  useEffect(() => {
    if (!open) return;
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'ArrowRight') {
        setActiveIndex((i) => (i + 1) % items.length);
      }
      if (event.key === 'ArrowLeft') {
        setActiveIndex((i) => (i - 1 + items.length) % items.length);
      }
    };
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('keydown', onKey);
    };
  }, [open, items.length]);

  const openAt = (index: number) => {
    if (!preview) return;
    setActiveIndex(index);
    setOpen(true);
  };

  if (mode === 'carousel') {
    const current = items[activeIndex];
    if (!current) return null;

    return (
      <div className={cn('w-full', className)}>
        <div className="relative">
          <button
            type="button"
            className="focus-visible:ring-ring w-full rounded-lg text-left focus-visible:ring-2 focus-visible:outline-none"
            onClick={() => {
              openAt(activeIndex);
            }}
            aria-label={
              current.caption
                ? `View ${current.caption}`
                : `View screenshot ${activeIndex + 1}`
            }
          >
            <MediaSlotView media={current.media} />
          </button>
          <div className="mt-4 flex items-center justify-between gap-3">
            <IconButton
              aria-label="Previous screenshot"
              variant="outline"
              onClick={() => {
                setActiveIndex((i) => (i - 1 + items.length) % items.length);
              }}
            >
              <ChevronLeft />
            </IconButton>
            <Caption>
              {activeIndex + 1} / {items.length}
              {current.caption ? ` — ${current.caption}` : ''}
            </Caption>
            <IconButton
              aria-label="Next screenshot"
              variant="outline"
              onClick={() => {
                setActiveIndex((i) => (i + 1) % items.length);
              }}
            >
              <ChevronRight />
            </IconButton>
          </div>
        </div>
        {open ? (
          <GalleryModal
            ref={dialogRef}
            titleId={titleId}
            item={current}
            index={activeIndex}
            total={items.length}
            onClose={close}
            onPrev={() => {
              setActiveIndex((i) => (i - 1 + items.length) % items.length);
            }}
            onNext={() => {
              setActiveIndex((i) => (i + 1) % items.length);
            }}
          />
        ) : null}
      </div>
    );
  }

  return (
    <div className={cn('w-full', className)}>
      <Grid cols={items.length === 2 ? 2 : 3} gap={4}>
        {items.map((item, index) => (
          <figure key={item.id} className="min-w-0">
            <button
              type="button"
              className="focus-visible:ring-ring w-full rounded-lg text-left focus-visible:ring-2 focus-visible:outline-none"
              onClick={() => {
                openAt(index);
              }}
              aria-label={
                item.caption ? `View ${item.caption}` : `View screenshot ${index + 1}`
              }
            >
              <MediaSlotView media={item.media} preset="thumbnail" />
            </button>
            {item.caption ? (
              <figcaption className="mt-2">
                <Caption>{item.caption}</Caption>
              </figcaption>
            ) : null}
          </figure>
        ))}
      </Grid>
      {open && items[activeIndex] ? (
        <GalleryModal
          ref={dialogRef}
          titleId={titleId}
          item={items[activeIndex]}
          index={activeIndex}
          total={items.length}
          onClose={close}
          onPrev={() => {
            setActiveIndex((i) => (i - 1 + items.length) % items.length);
          }}
          onNext={() => {
            setActiveIndex((i) => (i + 1) % items.length);
          }}
        />
      ) : null}
    </div>
  );
}
