import { Container, Heading, Paragraph, Stack } from '@/components/primitives';
import { cn } from '@/lib/utils';
import type { ReactNode } from 'react';

export function PlatformSectionBand({
  id,
  eyebrow,
  title,
  description,
  children,
  className,
  muted,
  tone = 'canvas',
}: {
  id: string;
  eyebrow?: string;
  title: string;
  description?: string;
  children?: ReactNode;
  className?: string;
  muted?: boolean;
  /** Visual rhythm — prefers semantic surfaces over flat canvas. */
  tone?: 'canvas' | 'muted' | 'feature' | 'blueprint';
}) {
  const resolvedTone = muted ? 'muted' : tone;
  const toneClass = {
    canvas: 'bg-background',
    muted: 'bg-surface-muted',
    feature: 'bg-gradient-to-b from-surface-section via-card to-surface-muted/60',
    blueprint:
      'bg-surface-section [background-image:linear-gradient(to_right,color-mix(in_oklab,var(--color-border)_45%,transparent)_1px,transparent_1px),linear-gradient(to_bottom,color-mix(in_oklab,var(--color-border)_45%,transparent)_1px,transparent_1px)] [background-size:3.25rem_3.25rem]',
  } as const;

  return (
    <section
      id={id}
      className={cn(
        'border-border relative w-full border-b py-section-y-lg',
        toneClass[resolvedTone],
        className,
      )}
      aria-labelledby={`${id}-heading`}
    >
      <Container width="wide">
        <Stack gap={8}>
          <Stack gap={3} className="max-w-3xl">
            {eyebrow ? (
              <p className="text-domain-accent text-eyebrow font-medium tracking-wide uppercase">
                {eyebrow}
              </p>
            ) : null}
            <Heading level={2} size="section-title" id={`${id}-heading`}>
              {title}
            </Heading>
            {description ? <Paragraph tone="muted">{description}</Paragraph> : null}
          </Stack>
          {children}
        </Stack>
      </Container>
    </section>
  );
}
