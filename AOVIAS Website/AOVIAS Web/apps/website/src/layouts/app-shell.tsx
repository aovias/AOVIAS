import type { ReactNode } from 'react';

import {
  BrandAtmosphere,
} from '@/components/brand';
import {
  Breadcrumbs,
  ContentWrapper,
  MainContent,
  PageContainer,
  SidebarPlaceholder,
  type PageContainerWidth,
} from '@/components/layout';
import { SiteFooter, SiteHeader } from '@/components/navigation';
import { Container } from '@/components/primitives';
import { DomainAccentScope } from '@/layouts/domain-accent-scope';
import { LayoutAtmosphereProvider } from '@/layouts/layout-atmosphere';
import { ShellTransition } from '@/layouts/shell-transition';
import type { BreadcrumbItem } from '@/lib/navigation/breadcrumbs';
import { cn } from '@/lib/utils';
import type { AvlfAtmosphereId } from '@/registries/avlf';
import { resolveAvlfAtmosphere } from '@/registries/avlf/atmosphere';

export type AppShellVariant =
  | 'marketing'
  | 'product'
  | 'documentation'
  | 'legal'
  | 'authentication'
  | 'error';

export type AppShellProps = {
  children: ReactNode;
  variant: AppShellVariant;
  showHeader?: boolean;
  showFooter?: boolean;
  showSidebar?: boolean;
  showBreadcrumb?: boolean;
  /** Explicit crumbs; when omitted, breadcrumbs derive from the pathname. */
  breadcrumbItems?: readonly BreadcrumbItem[];
  compactChrome?: boolean;
  contentWidth?: PageContainerWidth;
  className?: string;
  /**
   * AVLS EBAS atmosphere requested by this layout family.
   * Wired into LayoutAtmosphereProvider for registry-aware consumers.
   */
  atmosphere?: AvlfAtmosphereId;
  /**
   * When true, render a subtle layout-level BrandAtmosphere behind content.
   * Prefer for legal/docs shells without rich page heroes.
   */
  injectAtmosphere?: boolean;
  /** Density for injected layout atmosphere. */
  atmosphereDensity?: 'subtle' | 'default' | 'rich';
};

/**
 * Shared structural shell. Specialized layouts configure this shell
 * rather than duplicating header/footer/spacing/a11y structure.
 *
 * When `contentWidth` is `full`, gutters are owned by nested section
 * `Container`s so left edges match the header (`PageContainer` hero).
 */
export function AppShell({
  children,
  variant,
  showHeader = true,
  showFooter = true,
  showSidebar = false,
  showBreadcrumb = false,
  breadcrumbItems,
  compactChrome = false,
  contentWidth = 'content',
  className,
  atmosphere,
  injectAtmosphere = false,
  atmosphereDensity = 'subtle',
}: AppShellProps) {
  const sectionOwnedGutters = contentWidth === 'full';
  const bandPy =
    variant === 'marketing' || variant === 'product' || sectionOwnedGutters
      ? 'py-0'
      : 'py-8 lg:py-12';

  const shell = (
    <div
      data-layout={variant}
      {...(atmosphere ? { 'data-layout-atmosphere': atmosphere } : {})}
      className={cn('bg-background text-foreground flex min-h-dvh flex-col', className)}
    >
      <DomainAccentScope>
        {showHeader ? <SiteHeader compact={compactChrome} /> : null}

        <MainContent className="relative flex flex-1 flex-col">
          {injectAtmosphere && atmosphere ? (
            <BrandAtmosphere
              variant={resolveAvlfAtmosphere(atmosphere)}
              density={atmosphereDensity}
            />
          ) : null}
          <PageContainer
            width={contentWidth}
            className={cn(
              'relative flex flex-1 flex-col',
              bandPy,
              sectionOwnedGutters && 'px-0',
            )}
          >
            <ShellTransition>
              {showBreadcrumb ? (
                sectionOwnedGutters ? (
                  <Container width="wide" className="pt-6">
                    {breadcrumbItems ? (
                      <Breadcrumbs items={breadcrumbItems} className="mb-4" />
                    ) : (
                      <Breadcrumbs className="mb-4" />
                    )}
                  </Container>
                ) : (
                  <div className="pt-6">
                    {breadcrumbItems ? (
                      <Breadcrumbs items={breadcrumbItems} className="mb-4" />
                    ) : (
                      <Breadcrumbs className="mb-4" />
                    )}
                  </div>
                )
              ) : null}
              <div className={cn('flex flex-1 gap-8', showSidebar && 'lg:items-start')}>
                {showSidebar ? <SidebarPlaceholder label="Section navigation" /> : null}
                <ContentWrapper>{children}</ContentWrapper>
              </div>
            </ShellTransition>
          </PageContainer>
        </MainContent>

        {showFooter ? <SiteFooter compact={compactChrome} /> : null}
      </DomainAccentScope>
    </div>
  );

  if (!atmosphere) {
    return shell;
  }

  return (
    <LayoutAtmosphereProvider atmosphere={atmosphere}>
      {shell}
    </LayoutAtmosphereProvider>
  );
}
