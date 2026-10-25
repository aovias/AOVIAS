'use client';

import dynamic from 'next/dynamic';

/**
 * Defer Form Engine until after hero paint — client-only so Form Engine JS
 * does not compete with LCP on mobile (contact Perf gate).
 */
export const ContactFormsBandLazy = dynamic(
  () =>
    import('@/features/contact/forms/forms-band').then((mod) => mod.ContactFormsBand),
  {
    loading: () => (
      <section
        className="bg-muted/40 w-full scroll-mt-24 py-12 sm:py-16 lg:py-20"
        aria-busy="true"
        aria-label="Contact forms loading"
      >
        <div className="mx-auto max-w-content px-4 sm:px-6 lg:px-8">
          <div className="bg-muted h-40 max-w-3xl animate-pulse rounded-lg" />
        </div>
      </section>
    ),
  },
);
