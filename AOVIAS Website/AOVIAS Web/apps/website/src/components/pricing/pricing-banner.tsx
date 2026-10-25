import Link from 'next/link';

import type { PricingBannerMessage } from '@/components/pricing/models';
import { Alert, Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * PricingBanner — informational / promotional / enterprise notices.
 * Not for countdown pressure tactics.
 */
export type PricingBannerProps = {
  message: PricingBannerMessage;
  className?: string;
};

const toneMap = {
  info: 'info',
  success: 'success',
  warning: 'warning',
  neutral: 'info',
} as const;

export function PricingBanner({ message, className }: PricingBannerProps) {
  const tone = toneMap[message.tone ?? 'info'];

  return (
    <Alert
      tone={tone}
      {...(message.title !== undefined ? { title: message.title } : {})}
      className={cn(className)}
      live="polite"
    >
      <Text as="span" size="small">
        {message.message}
      </Text>
      {message.href && message.linkLabel ? (
        <>
          {' '}
          <Link
            href={message.href}
            className="text-primary font-medium underline underline-offset-4"
          >
            {message.linkLabel}
          </Link>
        </>
      ) : null}
    </Alert>
  );
}
