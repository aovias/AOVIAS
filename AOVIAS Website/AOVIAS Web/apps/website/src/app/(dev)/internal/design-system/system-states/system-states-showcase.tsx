'use client';

import { useState } from 'react';

import {
  CardSkeleton,
  ComingSoon,
  CountdownPlaceholder,
  EmptyState,
  ErrorState,
  GridSkeleton,
  HeroSkeleton,
  ImageSkeleton,
  ListSkeleton,
  LoadingState,
  MaintenanceScreen,
  NoResults,
  SearchEmptyState,
  TableSkeleton,
  TextSkeleton,
} from '@/components/system-states';
import {
  Heading,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';

function ShowcaseBlock({
  id,
  title,
  notes,
  children,
}: {
  id: string;
  title: string;
  notes?: string;
  children: React.ReactNode;
}) {
  return (
    <section id={id} className="border-border scroll-mt-20 border-b py-12">
      <Stack gap={6}>
        <Stack gap={2}>
          <Heading level={2} size="h2">
            {title}
          </Heading>
          {notes ? <Paragraph tone="muted">{notes}</Paragraph> : null}
        </Stack>
        {children}
      </Stack>
    </section>
  );
}

const nav = [
  { href: '#empty', label: 'Empty' },
  { href: '#error', label: 'Error' },
  { href: '#loading', label: 'Loading' },
  { href: '#skeletons', label: 'Skeletons' },
  { href: '#maintenance', label: 'Maintenance' },
  { href: '#coming-soon', label: 'Coming soon' },
  { href: '#search-empty', label: 'Search empty' },
  { href: '#no-results', label: 'No results' },
] as const;

export function SystemStatesShowcase() {
  const [progress, setProgress] = useState(42);

  return (
    <div>
      <nav
        aria-label="System state components"
        className="border-border mb-8 border-b pb-6"
      >
        <ul className="flex flex-wrap gap-x-4 gap-y-2">
          {nav.map((item) => (
            <li key={item.href}>
              <a
                href={item.href}
                className="text-muted-foreground hover:text-foreground text-small underline-offset-4 hover:underline"
              >
                {item.label}
              </a>
            </li>
          ))}
        </ul>
      </nav>

      <ShowcaseBlock
        id="empty"
        title="Empty State"
        notes="Variants with illustration placeholder and optional CTAs. Copy is always props-driven."
      >
        <div className="grid gap-6 lg:grid-cols-2">
          <EmptyState
            title="Nothing here yet"
            description="When this section has content, it will appear in this space."
            illustration={{ label: 'Empty illustration' }}
            primaryAction={{ label: 'Browse resources', href: '#skeletons' }}
            reveal={false}
          />
          <EmptyState
            title="Workspace is ready"
            description="Subtle variant without a heavy border treatment."
            variant="subtle"
            secondaryAction={{ label: 'Learn more', href: '#' }}
            reveal={false}
          />
        </div>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="error"
        title="Error State"
        notes="Retry and support actions. No user-blaming copy."
      >
        <ErrorState
          title="We could not complete that request"
          description="Please try again. If the issue continues, contact support."
          retryAction={{
            label: 'Try again',
            onClick: () => setProgress((value) => (value + 17) % 100),
          }}
          supportAction={{ label: 'Contact support', href: '#maintenance' }}
          reveal={false}
        />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="loading"
        title="Loading State"
        notes="Spinner sizes and determinate progress indicator."
      >
        <div className="grid gap-6 md:grid-cols-3">
          <LoadingState size="sm" message="Preparing…" />
          <LoadingState size="md" message="Loading content…" />
          <LoadingState
            progress={progress}
            message="Uploading sample progress…"
            label="Upload progress"
          />
        </div>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="skeletons"
        title="Skeleton System"
        notes="Text, card, image, grid, list, hero, and table placeholders."
      >
        <Stack gap={8}>
          <div>
            <Text as="p" size="small" weight="medium" className="mb-3">
              Text
            </Text>
            <TextSkeleton lines={4} />
          </div>
          <div>
            <Text as="p" size="small" weight="medium" className="mb-3">
              Card + Image
            </Text>
            <div className="grid gap-4 md:grid-cols-2">
              <CardSkeleton />
              <ImageSkeleton aspect="square" />
            </div>
          </div>
          <div>
            <Text as="p" size="small" weight="medium" className="mb-3">
              Grid
            </Text>
            <GridSkeleton count={3} cols={3} />
          </div>
          <div>
            <Text as="p" size="small" weight="medium" className="mb-3">
              List
            </Text>
            <ListSkeleton rows={3} />
          </div>
          <div>
            <Text as="p" size="small" weight="medium" className="mb-3">
              Hero
            </Text>
            <HeroSkeleton />
          </div>
          <div>
            <Text as="p" size="small" weight="medium" className="mb-3">
              Table
            </Text>
            <TableSkeleton rows={4} columns={4} />
          </div>
        </Stack>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="maintenance"
        title="Maintenance Screen"
        notes="Planned maintenance with estimated availability and contact."
      >
        <MaintenanceScreen
          statusLabel="Planned maintenance"
          title="We will be back shortly"
          description="Systems are undergoing scheduled maintenance to improve reliability."
          estimatedAvailability="19 July 2026, 22:00 UTC"
          contactLabel="status@aovias.example"
          contactHref="mailto:status@aovias.example"
          primaryAction={{ label: 'View status updates', href: '#' }}
          reveal={false}
        />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="coming-soon"
        title="Coming Soon"
        notes="CTA plus countdown and newsletter placeholder slots."
      >
        <ComingSoon
          title="This capability is coming soon"
          description="We are preparing a thoughtful release. Leave your details when the form is wired."
          primaryAction={{ label: 'Notify me', href: '#coming-soon' }}
          countdownPlaceholder={
            <CountdownPlaceholder label="Countdown placeholder (no timer logic)" />
          }
          newsletterPlaceholder={
            <Text as="p" size="caption" tone="muted">
              Newsletter form placeholder — compose Form Engine NewsletterForm here.
            </Text>
          }
          reveal={false}
        />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="search-empty"
        title="Search Empty State"
        notes="Query display, suggestions, clear and browse actions."
      >
        <SearchEmptyState
          query="zzzz-no-match"
          title="No search results"
          description="Adjust your keywords or browse published resources."
          suggestions={[
            'Check spelling',
            'Use fewer keywords',
            'Browse categories instead',
          ]}
          clearSearchAction={{ label: 'Clear search', href: '#search-empty' }}
          browseAction={{ label: 'Browse resources', href: '#no-results' }}
          reveal={false}
        />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="no-results"
        title="No Results"
        notes="Generic, filtered, and category variants with recovery suggestions."
      >
        <div className="grid gap-6 lg:grid-cols-3">
          <NoResults
            title="No items available"
            description="There is nothing to show in this list right now."
            variant="generic"
            primaryAction={{ label: 'Refresh view', href: '#no-results' }}
            reveal={false}
          />
          <NoResults
            title="No matches for these filters"
            variant="filtered"
            contextLabel="Security · 2026"
            suggestions={['Clear filters', 'Widen the date range']}
            primaryAction={{ label: 'Clear filters', href: '#no-results' }}
            reveal={false}
          />
          <NoResults
            title="No resources in this category"
            variant="category"
            contextLabel="Whitepapers"
            secondaryAction={{ label: 'View all resources', href: '#' }}
            reveal={false}
          />
        </div>
      </ShowcaseBlock>
    </div>
  );
}
