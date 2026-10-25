import type { CSSProperties } from 'react';

import { cn } from '@/lib/utils';

type AtmosphereDefinition = {
  label: string;
  intent: string;
  base: string;
  pattern?: string;
  patternSize?: string;
  patternMask?: string;
  patternOpacity?: number;
  detail?: string;
  detailSize?: string;
  /** Optional family mark for unmistakable silhouette. */
  mark?: 'none' | 'frame' | 'diamond' | 'nodes' | 'rings' | 'lanes';
};

/**
 * EBAS registry — every background is a named, reusable design asset.
 * All colors resolve through semantic surface, border, and domain-accent tokens.
 */
export const enterpriseAtmosphereRegistry = {
  'enterprise-abstract': {
    label: 'Enterprise Abstract',
    intent: 'Institutional platform identity with restrained aurora depth.',
    base:
      'radial-gradient(ellipse 62% 78% at 82% 8%, color-mix(in srgb, var(--domain-accent) 26%, transparent), transparent 64%), radial-gradient(ellipse 52% 62% at 6% 88%, var(--brand-atmosphere), transparent 70%), radial-gradient(ellipse 38% 48% at 48% 42%, color-mix(in srgb, var(--domain-accent) 10%, transparent), transparent 72%), linear-gradient(135deg, var(--surface-hero), var(--background) 48%, var(--surface-muted))',
    pattern:
      'linear-gradient(118deg, transparent 0 40%, color-mix(in srgb, var(--color-border) 58%, transparent) 40% 40.18%, transparent 40.18% 100%), linear-gradient(28deg, transparent 0 66%, color-mix(in srgb, var(--domain-accent) 22%, transparent) 66% 66.16%, transparent 66.16% 100%)',
    patternSize: 'clamp(14rem, 30vw, 30rem) clamp(11rem, 22vw, 22rem)',
    patternOpacity: 0.62,
    mark: 'none',
  },
  'blueprint-grid': {
    label: 'Blueprint Grid',
    intent: 'Engineering precision and inspectable systems.',
    base:
      'radial-gradient(ellipse 58% 72% at 76% 24%, var(--brand-atmosphere), transparent 72%), linear-gradient(135deg, var(--surface-hero), var(--background), var(--surface-muted))',
    pattern:
      'linear-gradient(to right, color-mix(in oklab, var(--color-border) 78%, transparent) 1px, transparent 1px), linear-gradient(to bottom, color-mix(in oklab, var(--color-border) 78%, transparent) 1px, transparent 1px)',
    patternSize: 'clamp(2.5rem, 3.8vw, 3.25rem) clamp(2.5rem, 3.8vw, 3.25rem)',
    patternMask: 'radial-gradient(ellipse 78% 94% at 72% 34%, black 10%, transparent 78%)',
    patternOpacity: 0.52,
    mark: 'none',
  },
  'platform-grid': {
    label: 'Platform Grid',
    intent: 'Modular platform services and shared foundations.',
    base:
      'radial-gradient(ellipse 64% 70% at 74% 20%, color-mix(in srgb, var(--domain-accent) 22%, transparent), transparent 68%), linear-gradient(145deg, var(--surface-hero), var(--background) 58%, var(--surface-muted))',
    pattern:
      'radial-gradient(circle, color-mix(in srgb, var(--domain-accent) 55%, transparent) 2px, transparent 2.4px), linear-gradient(to right, color-mix(in srgb, var(--color-border) 62%, transparent) 1px, transparent 1px), linear-gradient(to bottom, color-mix(in srgb, var(--color-border) 62%, transparent) 1px, transparent 1px)',
    patternSize:
      'clamp(4.5rem, 7.5vw, 6.5rem) clamp(4.5rem, 7.5vw, 6.5rem), clamp(4.5rem, 7.5vw, 6.5rem) clamp(4.5rem, 7.5vw, 6.5rem), clamp(2.25rem, 3.8vw, 3.25rem) clamp(2.25rem, 3.8vw, 3.25rem)',
    patternMask: 'linear-gradient(90deg, transparent, black 18%, black 94%, transparent)',
    patternOpacity: 0.5,
    mark: 'frame',
  },
  'infrastructure-mesh': {
    label: 'Infrastructure Mesh',
    intent: 'Connected infrastructure, interoperability, and scale.',
    base:
      'radial-gradient(ellipse 66% 74% at 78% 16%, color-mix(in srgb, var(--domain-accent) 20%, transparent), transparent 68%), linear-gradient(135deg, var(--surface-hero), var(--background), var(--surface-muted))',
    pattern:
      'repeating-linear-gradient(32deg, transparent 0 3.6rem, color-mix(in srgb, var(--color-border) 68%, transparent) 3.7rem 3.88rem), repeating-linear-gradient(-32deg, transparent 0 3.6rem, color-mix(in srgb, var(--domain-accent) 28%, transparent) 3.7rem 3.88rem), radial-gradient(circle, color-mix(in srgb, var(--domain-accent) 58%, transparent) 2px, transparent 2.5px)',
    patternSize: 'auto, auto, 6.5rem 4rem',
    patternMask: 'radial-gradient(ellipse 74% 90% at 70% 40%, black 10%, transparent 78%)',
    patternOpacity: 0.48,
    mark: 'nodes',
  },
  workflow: {
    label: 'Workflow',
    intent: 'Operational lanes, handoffs, and controlled delivery.',
    base:
      'radial-gradient(ellipse 58% 66% at 82% 14%, color-mix(in srgb, var(--domain-accent) 20%, transparent), transparent 66%), linear-gradient(140deg, var(--surface-hero), var(--background) 56%, var(--surface-muted))',
    pattern:
      'repeating-linear-gradient(to bottom, transparent 0 4.8rem, color-mix(in srgb, var(--domain-accent) 14%, transparent) 4.8rem 6.1rem, transparent 6.1rem 6.1rem), repeating-linear-gradient(to bottom, transparent 0 4.85rem, color-mix(in srgb, var(--color-border) 72%, transparent) 4.9rem 5.02rem)',
    patternSize: 'auto',
    patternMask: 'linear-gradient(90deg, transparent 2%, black 28%, black 96%, transparent)',
    patternOpacity: 0.55,
    mark: 'lanes',
  },
  architecture: {
    label: 'Architecture',
    intent: 'Layered systems, boundaries, and structural clarity.',
    base:
      'radial-gradient(ellipse 62% 70% at 75% 16%, color-mix(in srgb, var(--domain-accent) 20%, transparent), transparent 68%), linear-gradient(145deg, var(--surface-hero), var(--background), var(--surface-muted))',
    pattern:
      'repeating-linear-gradient(to bottom, transparent 0 4.35rem, color-mix(in srgb, var(--color-border) 70%, transparent) 4.4rem 4.55rem), linear-gradient(to right, transparent 0 28%, color-mix(in srgb, var(--domain-accent) 28%, transparent) 28% 28.18%, transparent 28.18% 66%, color-mix(in srgb, var(--domain-accent) 28%, transparent) 66% 66.18%, transparent 66.18%)',
    patternMask: 'radial-gradient(ellipse 74% 92% at 72% 40%, black 12%, transparent 80%)',
    patternOpacity: 0.52,
    mark: 'frame',
  },
  'ai-neural-network': {
    label: 'AI Neural Network',
    intent: 'Assistive intelligence, relationships, and accountable inference.',
    base:
      'radial-gradient(circle at 78% 24%, color-mix(in srgb, var(--domain-accent) 28%, transparent), transparent 30%), radial-gradient(circle at 42% 78%, color-mix(in srgb, var(--color-accent) 16%, transparent), transparent 32%), linear-gradient(140deg, var(--surface-hero), var(--background), var(--surface-muted))',
    pattern:
      'radial-gradient(circle, color-mix(in srgb, var(--domain-accent) 72%, transparent) 2.4px, transparent 3px), repeating-linear-gradient(34deg, transparent 0 5.4rem, color-mix(in srgb, var(--domain-accent) 28%, transparent) 5.5rem 5.62rem), repeating-linear-gradient(-34deg, transparent 0 5.4rem, color-mix(in srgb, var(--color-accent) 22%, transparent) 5.5rem 5.62rem)',
    patternSize: 'clamp(3.75rem, 7vw, 6rem) clamp(3.75rem, 7vw, 6rem), auto, auto',
    patternMask: 'radial-gradient(ellipse 72% 90% at 74% 40%, black 8%, transparent 78%)',
    patternOpacity: 0.56,
    detail:
      'radial-gradient(circle, color-mix(in srgb, var(--color-accent) 48%, transparent) 1.5px, transparent 2px)',
    detailSize: 'clamp(7rem, 12vw, 10rem) clamp(7rem, 12vw, 10rem)',
    mark: 'nodes',
  },
  'security-geometry': {
    label: 'Security Geometry',
    intent: 'Layered protection, bounded controls, and resilience.',
    base:
      'radial-gradient(ellipse 58% 70% at 76% 20%, color-mix(in srgb, var(--domain-accent) 24%, transparent), transparent 66%), linear-gradient(140deg, var(--surface-hero), var(--background), var(--surface-muted))',
    pattern:
      'repeating-linear-gradient(60deg, transparent 0 3.5rem, color-mix(in srgb, var(--domain-accent) 26%, transparent) 3.6rem 3.74rem), repeating-linear-gradient(-60deg, transparent 0 3.5rem, color-mix(in srgb, var(--color-border) 64%, transparent) 3.6rem 3.74rem), repeating-linear-gradient(0deg, transparent 0 6.4rem, color-mix(in srgb, var(--color-border) 58%, transparent) 6.45rem 6.56rem)',
    patternMask: 'radial-gradient(ellipse 70% 88% at 74% 38%, black 10%, transparent 78%)',
    patternOpacity: 0.5,
    mark: 'diamond',
  },
  'trust-geometry': {
    label: 'Trust Geometry',
    intent: 'Identity, assurance, and concentric trust boundaries.',
    base:
      'radial-gradient(circle at 76% 36%, color-mix(in srgb, var(--domain-accent) 22%, transparent), transparent 34%), linear-gradient(140deg, var(--surface-hero), var(--background), var(--surface-muted))',
    pattern:
      'repeating-radial-gradient(circle at 74% 40%, transparent 0 3.3rem, color-mix(in srgb, var(--domain-accent) 30%, transparent) 3.35rem 3.5rem), linear-gradient(to right, transparent 0 73.85%, color-mix(in srgb, var(--color-border) 68%, transparent) 73.95% 74.15%, transparent 74.15%), linear-gradient(to bottom, transparent 0 41.85%, color-mix(in srgb, var(--color-border) 68%, transparent) 41.95% 42.15%, transparent 42.15%)',
    patternMask: 'radial-gradient(circle at 74% 40%, black, transparent 58%)',
    patternOpacity: 0.58,
    mark: 'rings',
  },
  'knowledge-pattern': {
    label: 'Knowledge Pattern',
    intent: 'Editorial structure, indexed knowledge, and findability.',
    base:
      'radial-gradient(ellipse 55% 68% at 84% 16%, color-mix(in srgb, var(--domain-accent) 16%, transparent), transparent 68%), linear-gradient(145deg, var(--surface-hero), var(--background), var(--surface-muted))',
    pattern:
      'repeating-linear-gradient(to bottom, transparent 0 2.45rem, color-mix(in srgb, var(--color-border) 66%, transparent) 2.5rem 2.62rem), linear-gradient(to right, transparent 0 58%, color-mix(in srgb, var(--domain-accent) 26%, transparent) 58% 58.16%, transparent 58.16% 76%, color-mix(in srgb, var(--color-border) 55%, transparent) 76% 76.14%, transparent 76.14%)',
    patternMask: 'linear-gradient(90deg, transparent 4%, black 28%, black 96%, transparent)',
    patternOpacity: 0.48,
    detail:
      'radial-gradient(circle, color-mix(in srgb, var(--domain-accent) 45%, transparent) 1.5px, transparent 2px)',
    detailSize: '5rem 5rem',
    mark: 'lanes',
  },
  aurora: {
    label: 'Aurora',
    intent: 'Human invitation and calm, responsive engagement.',
    base:
      'radial-gradient(ellipse 60% 76% at 80% 4%, color-mix(in srgb, var(--domain-accent) 32%, transparent), transparent 62%), radial-gradient(ellipse 54% 68% at 18% 96%, color-mix(in srgb, var(--color-accent) 18%, transparent), transparent 68%), radial-gradient(ellipse 44% 54% at 50% 30%, var(--brand-atmosphere), transparent 70%), linear-gradient(145deg, var(--surface-hero), var(--background), var(--surface-muted))',
    mark: 'none',
  },
  'technology-atmosphere': {
    label: 'Technology Atmosphere',
    intent: 'Modern platform depth combining connectivity and light.',
    base:
      'radial-gradient(ellipse 64% 78% at 78% 8%, color-mix(in srgb, var(--domain-accent) 26%, transparent), transparent 64%), radial-gradient(ellipse 44% 56% at 12% 82%, var(--brand-atmosphere), transparent 70%), linear-gradient(140deg, var(--surface-hero), var(--background), var(--surface-muted))',
    pattern:
      'radial-gradient(circle, color-mix(in srgb, var(--domain-accent) 55%, transparent) 2px, transparent 2.5px), repeating-linear-gradient(24deg, transparent 0 5.4rem, color-mix(in srgb, var(--color-border) 58%, transparent) 5.5rem 5.62rem)',
    patternSize: 'clamp(3.75rem, 6.5vw, 5.75rem) clamp(3.75rem, 6.5vw, 5.75rem), auto',
    patternMask: 'radial-gradient(ellipse 74% 90% at 72% 36%, black 8%, transparent 78%)',
    patternOpacity: 0.5,
    mark: 'nodes',
  },
  'corporate-gradient': {
    label: 'Corporate Gradient',
    intent: 'Institutional authority without technical ornament.',
    base:
      'radial-gradient(ellipse 68% 88% at 88% 6%, color-mix(in srgb, var(--domain-accent) 22%, transparent), transparent 68%), linear-gradient(118deg, var(--surface-hero) 0%, var(--background) 42%, var(--surface-muted) 100%)',
    pattern:
      'linear-gradient(112deg, transparent 0 56%, color-mix(in srgb, var(--color-border) 60%, transparent) 56% 56.16%, transparent 56.16% 70%, color-mix(in srgb, var(--domain-accent) 18%, transparent) 70% 70.14%, transparent 70.14%)',
    patternOpacity: 0.58,
    mark: 'none',
  },
  'subtle-texture': {
    label: 'Subtle Texture',
    intent: 'Commercial clarity with restrained material depth.',
    base:
      'radial-gradient(ellipse 54% 66% at 80% 14%, color-mix(in srgb, var(--domain-accent) 16%, transparent), transparent 68%), linear-gradient(145deg, var(--surface-hero), var(--background), var(--surface-muted))',
    pattern:
      'radial-gradient(circle, color-mix(in srgb, var(--color-border) 78%, transparent) 1.15px, transparent 1.5px)',
    patternSize: '1.05rem 1.05rem',
    patternMask: 'linear-gradient(90deg, transparent, black 24%, black 94%, transparent)',
    patternOpacity: 0.42,
    mark: 'none',
  },
  'minimal-neutral': {
    label: 'Minimal Neutral',
    intent: 'Quiet governance and long-form reading.',
    base:
      'radial-gradient(ellipse 48% 60% at 86% 2%, color-mix(in srgb, var(--domain-accent) 10%, transparent), transparent 72%), linear-gradient(145deg, var(--surface), var(--background))',
    pattern:
      'linear-gradient(to right, color-mix(in srgb, var(--color-border) 42%, transparent) 1px, transparent 1px)',
    patternSize: 'clamp(16rem, 26vw, 26rem) 100%',
    patternMask: 'linear-gradient(90deg, transparent 10%, black 42%, transparent 90%)',
    patternOpacity: 0.28,
    mark: 'none',
  },
} as const satisfies Record<string, AtmosphereDefinition>;

export type EnterpriseAtmosphereVariant = keyof typeof enterpriseAtmosphereRegistry;

export type EnterpriseAtmosphereProps = {
  variant: EnterpriseAtmosphereVariant;
  className?: string;
  /** Soften visual activity behind a right-column illustration. */
  quietRight?: boolean;
  density?: 'subtle' | 'default' | 'rich';
};

const densityScale = {
  subtle: 0.62,
  default: 0.85,
  rich: 1,
} as const;

/**
 * Enterprise Background & Atmosphere System renderer.
 * Decorative only; responsive by construction and theme-aware through CSS variables.
 */
export function EnterpriseAtmosphere({
  variant,
  className,
  quietRight = false,
  density = 'default',
}: EnterpriseAtmosphereProps) {
  const asset: AtmosphereDefinition = enterpriseAtmosphereRegistry[variant];
  const activity = densityScale[density];
  const mark = asset.mark ?? 'none';

  const patternStyle: CSSProperties | undefined = asset.pattern
    ? {
        backgroundImage: asset.pattern,
        backgroundSize: asset.patternSize,
        maskImage: quietRight
          ? 'linear-gradient(90deg, black 0%, black 52%, transparent 88%)'
          : asset.patternMask,
        opacity: (asset.patternOpacity ?? 0.4) * activity,
      }
    : undefined;

  return (
    <div
      aria-hidden="true"
      data-atmosphere={variant}
      className={cn(
        'pointer-events-none absolute inset-0 isolate overflow-hidden em-atmosphere-enter',
        className,
      )}
    >
      <div className="absolute inset-0" style={{ backgroundImage: asset.base }} />

      {patternStyle ? (
        <div className="absolute inset-0" style={patternStyle} />
      ) : null}

      {asset.detail ? (
        <div
          className="absolute inset-0"
          style={{
            backgroundImage: asset.detail,
            backgroundSize: asset.detailSize,
            opacity: 0.28 * activity,
            maskImage: quietRight
              ? 'radial-gradient(ellipse 52% 74% at 26% 44%, black, transparent 76%)'
              : 'radial-gradient(ellipse 64% 78% at 76% 38%, black, transparent 76%)',
          }}
        />
      ) : null}

      <div
        className={cn(
          'absolute rounded-full blur-3xl',
          quietRight
            ? 'top-[10%] left-[2%] h-[15rem] w-[20rem] md:h-[22rem] md:w-[28rem]'
            : 'top-[-14%] right-[-8%] h-[22rem] w-[26rem] md:h-[32rem] md:w-[36rem]',
        )}
        style={{
          background:
            'radial-gradient(circle, color-mix(in srgb, var(--domain-accent) 22%, transparent), transparent 70%)',
          opacity: activity,
        }}
      />

      {variant === 'aurora' || variant === 'enterprise-abstract' ? (
        <div
          className="absolute bottom-[-18%] left-[8%] h-[16rem] w-[22rem] rounded-full blur-3xl md:h-[24rem] md:w-[30rem]"
          style={{
            background:
              'radial-gradient(circle, color-mix(in srgb, var(--color-accent) 14%, transparent), transparent 72%)',
            opacity: 0.85 * activity,
          }}
        />
      ) : null}

      {mark === 'diamond' ? (
        <>
          <div className="border-domain-accent/25 absolute top-[14%] right-[7%] hidden aspect-square w-[36%] rotate-45 rounded-[18%] border lg:block" />
          <div className="border-domain-accent/15 absolute top-[22%] right-[12%] hidden aspect-square w-[24%] rotate-45 rounded-[18%] border lg:block" />
        </>
      ) : null}

      {mark === 'rings' ? (
        <>
          <div className="border-domain-accent/20 absolute top-[18%] right-[10%] hidden aspect-square w-[32%] rounded-full border lg:block" />
          <div className="border-domain-accent/14 absolute top-[24%] right-[14%] hidden aspect-square w-[22%] rounded-full border lg:block" />
          <div className="border-domain-accent/10 absolute top-[29%] right-[17.5%] hidden aspect-square w-[14%] rounded-full border lg:block" />
        </>
      ) : null}

      {mark === 'frame' || mark === 'lanes' ? (
        <div className="border-domain-accent/18 absolute top-[16%] right-[7%] hidden h-[66%] w-[38%] rounded-xl border lg:block" />
      ) : null}

      {mark === 'nodes' ? (
        <div className="absolute top-[20%] right-[10%] hidden h-[58%] w-[34%] lg:block">
          <div className="bg-domain-accent/35 absolute top-[8%] left-[18%] size-2 rounded-full" />
          <div className="bg-domain-accent/30 absolute top-[42%] left-[62%] size-2.5 rounded-full" />
          <div className="bg-domain-accent/28 absolute top-[72%] left-[28%] size-2 rounded-full" />
          <div className="via-domain-accent/25 absolute top-[12%] left-[22%] h-px w-[42%] rotate-[28deg] bg-gradient-to-r from-transparent to-transparent" />
          <div className="via-domain-accent/20 absolute top-[48%] left-[30%] h-px w-[38%] -rotate-[18deg] bg-gradient-to-r from-transparent to-transparent" />
        </div>
      ) : null}

      {(variant === 'blueprint-grid' || variant === 'architecture') && (
        <div className="from-transparent via-border to-transparent absolute inset-y-8 right-[47%] hidden w-px bg-gradient-to-b opacity-50 xl:block" />
      )}
      <div className="border-border/70 absolute top-6 left-6 hidden size-7 border-t border-l sm:block" />
      <div className="border-border/70 absolute right-6 bottom-6 hidden size-7 border-r border-b lg:block" />
    </div>
  );
}
