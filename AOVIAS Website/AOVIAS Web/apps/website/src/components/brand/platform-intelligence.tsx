import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type PlatformIntelligenceProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * Conceptual intelligence layer across the platform — assists products, never replaces governance.
 */
export function PlatformIntelligence({
  title = 'Intelligence across the platform',
  caption,
  className,
}: PlatformIntelligenceProps) {
  return (
    <BrandShell
      title={title}
      {...(caption ? { caption } : {})}
      className={cn('aspect-[4/3] xl:min-h-[18rem]', className)}
      aspectClassName="min-h-[14rem]"
    >
      <svg
        viewBox="0 0 400 300"
        className="text-domain-accent size-full flex-1"
        role="img"
        aria-label={title}
      >
        <title>{title}</title>
        <BrandGrid width={400} height={300} step={25} opacity={0.055} />

        <BrandPanel
          x={100}
          y={24}
          width={200}
          height={44}
          opacity={brandInk.fillSoft}
          stroked
        />
        <BrandLabel x={200} y={50} size={11}>
          Products · Command · Studio
        </BrandLabel>

        <BrandConnector d="M200 68 V96" opacity={0.28} />

        <BrandPanel
          x={70}
          y={96}
          width={260}
          height={56}
          opacity={brandInk.fillStrong}
          radius={brandInk.radiusLg}
          stroked
        />
        <BrandLabel x={200} y={122} size={11}>
          Shared intelligence capability
        </BrandLabel>
        <BrandLabel x={200} y={140} size={9} opacity={0.4}>
          Assist · Recommend · Explain
        </BrandLabel>

        <BrandConnector d="M200 152 V180" opacity={0.28} />

        <BrandPanel x={40} y={180} width={100} height={48} opacity={brandInk.fillMid} stroked />
        <BrandPanel x={150} y={180} width={100} height={48} opacity={brandInk.fillMid} stroked />
        <BrandPanel x={260} y={180} width={100} height={48} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={90} y={208} size={9}>
          Oversight
        </BrandLabel>
        <BrandLabel x={200} y={208} size={9}>
          Audit
        </BrandLabel>
        <BrandLabel x={310} y={208} size={9}>
          Privacy
        </BrandLabel>

        <BrandConnector d="M200 228 V248" opacity={0.24} />
        <BrandPanel x={110} y={248} width={180} height={36} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={200} y={270} size={10}>
          Human accountability
        </BrandLabel>
      </svg>
    </BrandShell>
  );
}
