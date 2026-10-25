import Image from 'next/image';
import Link from 'next/link';

import { siteConfig } from '@/config/site';
import { cn } from '@/lib/utils';

export type SiteLogoProps = {
  className?: string;
};

/**
 * AOVIAS lockup — server-rendered, CSS theme swap (no client hydrate).
 * Sized ~20% larger for brand presence in header chrome.
 */
export function SiteLogo({ className }: SiteLogoProps) {
  return (
    <Link
      href="/"
      aria-label={siteConfig.name}
      className={cn(
        'focus-visible:ring-ring inline-flex h-10 shrink-0 items-center focus-visible:ring-2 focus-visible:outline-none sm:h-[3.9375rem]',
        className,
      )}
    >
      <Image
        src="/brand/aovias-logo-light.png"
        alt=""
        width={240}
        height={48}
        sizes="(max-width: 380px) 132px, (max-width: 640px) 156px, 240px"
        className="h-8 w-auto max-w-[8.25rem] object-contain object-left dark:hidden min-[380px]:h-[2.4rem] min-[380px]:max-w-[10.5rem] sm:h-[3.9375rem] sm:max-w-[15rem]"
      />
      <Image
        src="/brand/aovias-logo-dark.png"
        alt=""
        width={240}
        height={49}
        sizes="(max-width: 380px) 132px, (max-width: 640px) 156px, 240px"
        className="hidden h-8 w-auto max-w-[8.25rem] object-contain object-left dark:block min-[380px]:h-[2.4rem] min-[380px]:max-w-[10.5rem] sm:h-[3.9375rem] sm:max-w-[15rem]"
      />
    </Link>
  );
}
