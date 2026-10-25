import { JsonLdScript } from '@/components/seo';
import { homeSeo } from '@/content/home';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';
import { ogImages } from '@/seo';

import { HomeView } from './home-view';

export const metadata = createPageMetadata({
  title: homeSeo.title,
  description: homeSeo.description,
  path: homeSeo.path,
  type: 'marketing',
  keywords: homeSeo.keywords,
  absoluteTitle: homeSeo.absoluteTitle,
  image: ogImages.home,
  imageAlt: 'AOVIAS — Strengthening institutions through technology',
});

/**
 * AOVIAS Homepage — production assembly from Phase 1–2 building blocks.
 * Header/Footer provided by MarketingLayout → AppShell.
 */
export default function HomePage() {
  return (
    <>
      <JsonLdScript
        id="home-webpage"
        data={buildPageSchemas({
          title: homeSeo.title,
          description: homeSeo.description,
          path: homeSeo.path,
        })}
      />
      <HomeView />
    </>
  );
}
