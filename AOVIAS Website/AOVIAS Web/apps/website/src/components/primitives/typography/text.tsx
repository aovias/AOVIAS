import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Text — tokenized text primitive (R1.DS.3 roles).
 */
export type TextSize =
  | 'lead'
  | 'body-lg'
  | 'body'
  | 'small'
  | 'label'
  | 'helper'
  | 'caption'
  | 'eyebrow';
export type TextTone = 'default' | 'muted' | 'subtle' | 'primary' | 'accent' | 'danger';
export type TextWeight = 'normal' | 'medium' | 'semibold';

export type TextProps = {
  children: ReactNode;
  size?: TextSize;
  tone?: TextTone;
  weight?: TextWeight;
  className?: string;
  as?: 'span' | 'p' | 'div';
};

const sizeClass: Record<TextSize, string> = {
  lead: 'text-lead leading-relaxed',
  'body-lg': 'text-body-lg leading-relaxed',
  body: 'text-body leading-relaxed',
  small: 'text-small leading-normal',
  label: 'text-label leading-normal',
  helper: 'text-helper leading-normal',
  caption: 'text-caption leading-normal',
  eyebrow: 'text-eyebrow font-medium tracking-wide uppercase leading-normal',
};

const toneClass: Record<TextTone, string> = {
  default: 'text-ink',
  muted: 'text-ink-muted',
  subtle: 'text-ink-muted',
  primary: 'text-primary',
  accent: 'text-accent',
  danger: 'text-danger',
};

const weightClass: Record<TextWeight, string> = {
  normal: 'font-normal',
  medium: 'font-medium',
  semibold: 'font-semibold',
};

export function Text({
  children,
  size = 'body',
  tone = 'default',
  weight = 'normal',
  className,
  as: Tag = 'span',
}: TextProps) {
  return (
    <Tag
      className={cn(
        sizeClass[size],
        toneClass[tone],
        size === 'eyebrow' ? undefined : weightClass[weight],
        className,
      )}
    >
      {children}
    </Tag>
  );
}
