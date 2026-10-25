'use client';

import type { PricingFaqItem } from '@/components/pricing/models';
import { Faq } from '@/components/trust';

/**
 * PricingFaq — pricing questions via Trust FAQ (search + categories).
 * Avoids duplicating accordion behaviour.
 */
export type PricingFaqProps = {
  items: readonly PricingFaqItem[];
  groupByCategory?: boolean;
  searchable?: boolean;
  searchPlaceholder?: string;
  className?: string;
  allowMultiple?: boolean;
};

export function PricingFaq({
  items,
  groupByCategory = true,
  searchable = true,
  searchPlaceholder = 'Search pricing questions…',
  className,
  allowMultiple = true,
}: PricingFaqProps) {
  return (
    <Faq
      items={items}
      groupByCategory={groupByCategory}
      searchable={searchable}
      searchPlaceholder={searchPlaceholder}
      allowMultiple={allowMultiple}
      {...(className !== undefined ? { className } : {})}
    />
  );
}
