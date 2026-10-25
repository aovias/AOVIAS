'use client';

import { useEffect, useRef } from 'react';
import { usePathname } from 'next/navigation';

import { useTelemetry } from './use-telemetry';
import { resolveSiteSection } from '../instrumentation/section';

/**
 * Records page views on pathname changes through the telemetry SDK.
 * Inactive until analytics is enabled + consent granted (R2).
 */
export function usePageView(): void {
  const client = useTelemetry();
  const pathname = usePathname();
  const lastPath = useRef<string | null>(null);

  useEffect(() => {
    if (!pathname || lastPath.current === pathname) return;
    lastPath.current = pathname;

    const section = resolveSiteSection(pathname);
    const referrer =
      typeof document !== 'undefined' && document.referrer
        ? document.referrer
        : undefined;
    const title =
      typeof document !== 'undefined' && document.title
        ? document.title
        : undefined;

    client.trackPageView({
      path: pathname,
      section,
      ...(referrer ? { referrer } : {}),
      ...(title ? { title } : {}),
      attributes: {
        'page.section': section,
      },
    });
  }, [client, pathname]);
}
