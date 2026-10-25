'use client';

import { usePathname } from 'next/navigation';
import type { ReactNode } from 'react';

import { resolveDomainAccentAttrs } from '@/lib/domain/accent';

/**
 * Applies data-product / data-technology / data-industry from the current path
 * so CSS can set --domain-accent. Display:contents keeps layout unchanged.
 */
export function DomainAccentScope({ children }: { children: ReactNode }) {
  const pathname = usePathname() ?? '';
  const attrs = resolveDomainAccentAttrs(pathname);

  return (
    <div className="contents" {...attrs}>
      {children}
    </div>
  );
}
