'use client';

import dynamic from 'next/dynamic';
import { Suspense, type ComponentType, type ReactNode } from 'react';

import { IllustrationPlaceholder } from '@/components/system-states/illustration-placeholder';
import { cn } from '@/lib/utils';
import {
  getIllustrationEntry,
  type AvlfIllustrationId,
  type IllustrationComponentKey,
} from '@/registries/avlf';

// eslint-disable-next-line @typescript-eslint/no-explicit-any -- brand illustration prop shapes vary by component
type IllustrationComponent = ComponentType<any>;

const illustrationLoaders: Record<
  IllustrationComponentKey,
  () => Promise<{ default: IllustrationComponent }>
> = {
  PlatformOrchestration: () =>
    import('@/components/brand/platform-orchestration').then((m) => ({
      default: m.PlatformOrchestration,
    })),
  PlatformEcosystem: () =>
    import('@/components/brand/platform-ecosystem').then((m) => ({
      default: m.PlatformEcosystem,
    })),
  ProductEcosystemFlow: () =>
    import('@/components/brand/product-ecosystem-flow').then((m) => ({
      default: m.ProductEcosystemFlow,
    })),
  CommandOperations: () =>
    import('@/components/brand/command-operations').then((m) => ({
      default: m.CommandOperations,
    })),
  CommandMissionControl: () =>
    import('@/components/brand/command-mission-control').then((m) => ({
      default: m.CommandMissionControl,
    })),
  StudioDelivery: () =>
    import('@/components/brand/studio-delivery').then((m) => ({
      default: m.StudioDelivery,
    })),
  StudioAuthoringWorkspace: () =>
    import('@/components/brand/studio-authoring-workspace').then((m) => ({
      default: m.StudioAuthoringWorkspace,
    })),
  PlatformIntelligence: () =>
    import('@/components/brand/platform-intelligence').then((m) => ({
      default: m.PlatformIntelligence,
    })),
  PlatformServicesConsole: () =>
    import('@/components/brand/platform-services-console').then((m) => ({
      default: m.PlatformServicesConsole,
    })),
  HumanAiCollaboration: () =>
    import('@/components/brand/human-ai-collaboration').then((m) => ({
      default: m.HumanAiCollaboration,
    })),
  SecurePlatform: () =>
    import('@/components/brand/secure-platform').then((m) => ({
      default: m.SecurePlatform,
    })),
  TrustEcosystem: () =>
    import('@/components/brand/trust-ecosystem').then((m) => ({
      default: m.TrustEcosystem,
    })),
  WorkflowPanel: () =>
    import('@/components/brand/workflow-panel').then((m) => ({
      default: m.WorkflowPanel,
    })),
  ArchitectureDiagram: () =>
    import('@/components/brand/architecture-diagram').then((m) => ({
      default: m.ArchitectureDiagram,
    })),
  IdentityPlatformVisual: () =>
    import('@/components/brand/identity-platform').then((m) => ({
      default: m.IdentityPlatformVisual,
    })),
  KnowledgeCover: () =>
    import('@/components/brand/knowledge-cover').then((m) => ({
      default: m.KnowledgeCover,
    })),
  InstitutionalScene: () =>
    import('@/components/brand/institutional-scene').then((m) => ({
      default: m.InstitutionalScene,
    })),
  DomainScene: () =>
    import('@/components/brand/domain-scene').then((m) => ({
      default: m.DomainScene,
    })),
  IndustryPlatformComposition: () =>
    import('@/components/brand/industry-platform-composition').then((m) => ({
      default: m.IndustryPlatformComposition,
    })),
};

const lazyCache = new Map<string, IllustrationComponent>();

function getLazyIllustration(
  key: IllustrationComponentKey,
  /** Above-the-fold only — SSR into HTML. Below-fold stays client to protect LCP. */
  priority: boolean,
): IllustrationComponent {
  const cacheKey = `${key}:${priority ? 'priority' : 'lazy'}`;
  const cached = lazyCache.get(cacheKey);
  if (cached) return cached;

  const Component = dynamic(illustrationLoaders[key], {
    ssr: priority,
    loading: () => (
      <IllustrationPlaceholder
        illustration={{ label: 'Loading illustration' }}
        className="min-h-[12rem] w-full"
      />
    ),
  });
  lazyCache.set(cacheKey, Component);
  return Component;
}

export type RegisteredIllustrationProps = {
  id: AvlfIllustrationId;
  /** Prefer an alternate registry mapping when available. */
  preferAlternate?: boolean;
  /** Force a specific component key (must be primary or alternate for the ID). */
  componentKey?: IllustrationComponentKey;
  /**
   * When true, SSR the illustration (hero / LCP-adjacent only).
   * Default false — client-only below the fold to keep HTML lean for mobile LCP.
   */
  priority?: boolean;
  className?: string;
  title?: string;
  /** Extra props forwarded to the brand illustration component (e.g. variant). */
  illustrationProps?: Record<string, unknown>;
  /** Custom fallback when registry lookup or load fails. */
  fallback?: ReactNode;
};

/**
 * Illustration Registry pipeline — lookup, lazy load, theme-aware brand art, fallback.
 *
 * Release 1 certification: page call-sites may import BrandShell-family components
 * directly when they share brandInk primitives and DomainScene/InstitutionalScene
 * variants. RegisteredIllustration remains the preferred lazy pipeline for catalog IDs.
 * Expanding call-site migration continues post-R1 without blocking launch quality.
 */
export function RegisteredIllustration({
  id,
  preferAlternate = false,
  componentKey: componentKeyOverride,
  priority = false,
  className,
  title,
  illustrationProps: extraProps,
  fallback,
}: RegisteredIllustrationProps) {
  const entry = getIllustrationEntry(id);

  if (!entry) {
    return (
      fallback ?? (
        <IllustrationPlaceholder
          illustration={{ label: `Missing illustration: ${id}` }}
          className={cn('min-h-[12rem] w-full', className)}
        />
      )
    );
  }

  const componentKey =
    componentKeyOverride ??
    (preferAlternate && entry.alternateKeys?.[0]
      ? entry.alternateKeys[0]
      : entry.componentKey);

  const Illustration = getLazyIllustration(componentKey, priority);
  const illustrationProps =
    componentKey === 'KnowledgeCover'
      ? { label: title ?? entry.purpose, className: 'w-full', ...extraProps }
      : {
          ...(title ? { title } : {}),
          className: 'w-full',
          ...extraProps,
        };

  return (
    <div
      data-illustration={id}
      data-illustration-component={componentKey}
      className={cn(
        'text-foreground w-full min-w-0 [&_svg]:h-auto [&_svg]:w-full',
        className,
      )}
    >
      <Suspense
        fallback={
          fallback ?? (
            <IllustrationPlaceholder
              illustration={{ label: entry.purpose }}
              className="min-h-[12rem] w-full"
            />
          )
        }
      >
        <Illustration {...illustrationProps} />
      </Suspense>
    </div>
  );
}
