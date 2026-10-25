import type { Metadata } from 'next';

import {
  defaultRobotsByType,
  resolveOpenGraphType,
  type PageMetadataType,
} from '@/config/metadata';
import { seoConfig } from '@/config/seo';

import { buildCanonicalUrl } from './canonical';
import { shouldAllowIndexing } from './env';
import { buildOpenGraph } from './opengraph';
import { buildTwitterCard, type TwitterCardType } from './twitter';

export type CreatePageMetadataInput = {
  title: string;
  description?: string;
  /** Site-relative path used for canonical + OG URL. */
  path: string;
  /** Semantic page type — drives OG type and default robots. */
  type?: PageMetadataType;
  keywords?: readonly string[];
  /** Override or disable default OG/Twitter image. */
  image?: string;
  imageAlt?: string;
  /** Force noindex (also applied automatically outside production). */
  noIndex?: boolean;
  robots?: {
    index?: boolean;
    follow?: boolean;
    nocache?: boolean;
    noarchive?: boolean;
  };
  twitterCard?: TwitterCardType;
  /** When true, omit site name title template (absolute title). */
  absoluteTitle?: boolean;
};

function resolveRobots(
  input: CreatePageMetadataInput,
  pageType: PageMetadataType,
): Metadata['robots'] {
  const allowIndex = shouldAllowIndexing() && !input.noIndex;
  const defaults = defaultRobotsByType[pageType];
  const index = allowIndex && (input.robots?.index ?? defaults.index);
  const follow = input.robots?.follow ?? defaults.follow;

  return {
    index,
    follow,
    ...(input.robots?.nocache !== undefined ? { nocache: input.robots.nocache } : {}),
    ...(input.robots?.noarchive !== undefined
      ? { noarchive: input.robots.noarchive }
      : {}),
    googleBot: {
      index,
      follow,
    },
  };
}

/**
 * Typed SEO Builder API — preferred entry for page metadata.
 *
 * @example
 * ```ts
 * export const metadata = createPageMetadata({
 *   title: 'AOVIAS Studio',
 *   description: '…',
 *   path: '/products/studio',
 *   type: 'product',
 * });
 * ```
 *
 * Automatically applies: defaults, canonical, Open Graph, Twitter/X,
 * theme color, icons, robots, and site branding.
 */
export function createPageMetadata(input: CreatePageMetadataInput): Metadata {
  const pageType = input.type ?? 'website';
  const description = input.description ?? seoConfig.defaultDescription;
  const titleValue = input.absoluteTitle
    ? { absolute: input.title }
    : input.title;

  const og = buildOpenGraph({
    title: input.title,
    description,
    path: input.path,
    type: resolveOpenGraphType(pageType),
    ...(input.image ? { imagePath: input.image } : {}),
    ...(input.imageAlt ? { imageAlt: input.imageAlt } : {}),
  });

  const twitter = buildTwitterCard({
    title: input.title,
    description,
    ...(input.image ? { imagePath: input.image } : {}),
    ...(input.twitterCard ? { card: input.twitterCard } : {}),
  });

  const metadata: Metadata = {
    title: titleValue,
    description,
    metadataBase: new URL(buildCanonicalUrl('/')),
    alternates: {
      canonical: buildCanonicalUrl(input.path),
    },
    openGraph: og,
    twitter,
    robots: resolveRobots(input, pageType),
    icons: {
      icon: [...seoConfig.icons.icon],
      apple: [...seoConfig.icons.apple],
    },
    other: {
      'theme-color': seoConfig.themeColor.light,
    },
  };

  if (input.keywords && input.keywords.length > 0) {
    metadata.keywords = [...input.keywords];
  }

  return metadata;
}

/**
 * Root layout metadata — title template + site-wide defaults.
 * Pages should call `createPageMetadata` for route-specific overrides.
 */
function resolveVerification(): Metadata['verification'] {
  const google = process.env[seoConfig.verification.googleEnvKey]?.trim();
  const bing = process.env[seoConfig.verification.bingEnvKey]?.trim();

  if (!google && !bing) return undefined;

  return {
    ...(google ? { google } : {}),
    ...(bing ? { other: { 'msvalidate.01': bing } } : {}),
  };
}

export function createRootMetadata(): Metadata {
  const indexable = shouldAllowIndexing();
  const verification = resolveVerification();

  return {
    metadataBase: new URL(buildCanonicalUrl('/')),
    title: {
      default: seoConfig.defaultTitle,
      template: seoConfig.titleTemplate,
    },
    description: seoConfig.defaultDescription,
    applicationName: seoConfig.siteName,
    authors: [{ name: seoConfig.organization.name }],
    creator: seoConfig.organization.name,
    publisher: seoConfig.organization.name,
    formatDetection: {
      email: false,
      address: false,
      telephone: false,
    },
    openGraph: buildOpenGraph({
      title: seoConfig.defaultTitle,
      description: seoConfig.defaultDescription,
      path: '/',
      type: 'website',
      imagePath: seoConfig.ogImages.home,
    }),
    twitter: buildTwitterCard({
      title: seoConfig.defaultTitle,
      description: seoConfig.defaultDescription,
      imagePath: seoConfig.ogImages.home,
    }),
    robots: {
      index: indexable,
      follow: indexable,
      googleBot: {
        index: indexable,
        follow: indexable,
      },
    },
    icons: {
      icon: [...seoConfig.icons.icon],
      apple: [...seoConfig.icons.apple],
    },
    manifest: seoConfig.manifestPath,
    alternates: {
      canonical: buildCanonicalUrl('/'),
    },
    ...(verification ? { verification } : {}),
    other: {
      'theme-color': seoConfig.themeColor.light,
    },
  };
}
