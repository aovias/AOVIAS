import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Container — centered page/content width with responsive gutters.
 *
 * Widths: `default` (content), `narrow`, `wide` (hero), `full`.
 * Theme-aware via tokenized max-widths and spacing utilities.
 */
export type ContainerWidth = 'default' | 'narrow' | 'wide' | 'full';

/** @deprecated Prefer ContainerWidth; retained for PageContainer compatibility. */
export type PageContainerWidth = 'narrow' | 'content' | 'hero' | 'full';

const widthClass: Record<ContainerWidth, string> = {
  default: 'max-w-content',
  narrow: 'max-w-3xl',
  wide: 'max-w-hero',
  full: 'max-w-none',
};

const legacyWidthMap: Record<PageContainerWidth, ContainerWidth> = {
  narrow: 'narrow',
  content: 'default',
  hero: 'wide',
  full: 'full',
};

export type ContainerProps = {
  children: ReactNode;
  /** Preferred width API for Sprint 1.5+. */
  width?: ContainerWidth;
  className?: string;
  as?: 'div' | 'section' | 'article';
};

export function Container({
  children,
  width = 'default',
  className,
  as: Tag = 'div',
}: ContainerProps) {
  return (
    <Tag className={cn('mx-auto w-full px-3 min-[360px]:px-4 sm:px-6 lg:px-8', widthClass[width], className)}>
      {children}
    </Tag>
  );
}

export type PageContainerProps = {
  children: ReactNode;
  width?: PageContainerWidth;
  className?: string;
  as?: 'div' | 'section' | 'article';
};

/** Layout-shell alias mapping legacy width names onto Container. */
export function PageContainer({
  children,
  width = 'content',
  className,
  as,
}: PageContainerProps) {
  return (
    <Container
      width={legacyWidthMap[width]}
      {...(className ? { className } : {})}
      {...(as ? { as } : {})}
    >
      {children}
    </Container>
  );
}
