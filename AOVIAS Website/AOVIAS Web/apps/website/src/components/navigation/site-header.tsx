import { PageContainer } from '@/components/layout/page-container';
import { HeaderChrome } from '@/components/navigation/header-chrome';
import { HeaderIconActions } from '@/components/navigation/header-icon-actions';
import { MainNav } from '@/components/navigation/main-nav';
import { MobileDrawer } from '@/components/navigation/mobile-drawer';
import { SearchEntry } from '@/components/navigation/search-entry';
import { SiteLogo } from '@/components/navigation/site-logo';
import { UtilityNav } from '@/components/navigation/utility-nav';
import { ThemeToggle } from '@/providers/theme';

export type SiteHeaderProps = {
  className?: string;
  /** Compact header for auth/error shells. */
  compact?: boolean;
};

/** Header height spacer — keeps page content below the fixed chrome. */
export const SITE_HEADER_HEIGHT_CLASS = 'h-16';

export function SiteHeader({ className, compact = false }: SiteHeaderProps) {
  return (
    <>
      <HeaderChrome {...(className ? { className } : {})}>
        <PageContainer
          width="hero"
          className="relative flex h-16 items-center justify-between gap-4"
        >
          <div className="flex min-w-0 items-center gap-6 lg:gap-8">
            <SiteLogo />
            {compact ? null : <MainNav />}
          </div>

          <div className="flex h-full min-w-0 items-center gap-1.5 sm:gap-2.5">
            {compact ? null : (
              <>
                <UtilityNav className="mr-0.5" />
                <SearchEntry
                  className="hidden sm:inline-flex"
                  variant="button"
                />
                <HeaderIconActions />
              </>
            )}
            <ThemeToggle className="self-center" />
            {compact ? null : <MobileDrawer />}
          </div>
        </PageContainer>
      </HeaderChrome>
      {/* Reserve space so fixed header does not cover page content. */}
      <div className={SITE_HEADER_HEIGHT_CLASS} aria-hidden="true" />
    </>
  );
}
