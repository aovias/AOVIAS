import {
  Heading,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';
import type { MarketingAlign, MarketingWidth } from '@/components/marketing/types';
import { cn } from '@/lib/utils';

/**
 * SectionHeader — reusable band title for marketing / product sections.
 *
 * @example
 * <SectionHeader eyebrow="Platform" heading="Built for scale" description="…" />
 */
export type SectionHeaderProps = {
  eyebrow?: string;
  heading: string;
  description?: string;
  align?: MarketingAlign;
  width?: MarketingWidth;
  className?: string;
  headingLevel?: 2 | 3;
};

const widthClass: Record<MarketingWidth, string> = {
  narrow: 'max-w-2xl',
  default: 'max-w-3xl',
  wide: 'max-w-4xl',
};

export function SectionHeader({
  eyebrow,
  heading,
  description,
  align = 'start',
  width = 'default',
  className,
  headingLevel = 2,
}: SectionHeaderProps) {
  return (
    <Stack
      gap={3}
      className={cn(
        widthClass[width],
        align === 'center' && 'mx-auto text-center items-center',
        className,
      )}
    >
      {eyebrow ? (
        <Stack
          gap={2}
          className={cn(align === 'center' && 'items-center')}
        >
          <span
            aria-hidden="true"
            className="bg-domain-accent block h-0.5 w-10 rounded-full"
          />
          <Text as="p" size="eyebrow" className="text-domain-accent font-semibold">
            {eyebrow}
          </Text>
        </Stack>
      ) : null}
      <Heading
        level={headingLevel}
        size={headingLevel === 2 ? 'section-title' : 'h3'}
      >
        {heading}
      </Heading>
      {description ? (
        <Paragraph
          tone="muted"
          {...(align === 'center' ? { className: 'text-center' } : {})}
        >
          {description}
        </Paragraph>
      ) : null}
    </Stack>
  );
}
