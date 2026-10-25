import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type ScalabilityJourneyProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * Scalability as organizational and product growth — not infrastructure claims.
 */
export function ScalabilityJourney({
  title = 'Scalability journey',
  caption = 'Team · Institution · Multi-product — sustainable architecture',
  className,
}: ScalabilityJourneyProps) {
  return (
    <BrandShell
      title={title}
      {...(caption ? { caption } : {})}
      className={cn('aspect-[4/3] xl:min-h-[18rem]', className)}
      aspectClassName="min-h-[14rem]"
    >
      <svg
        viewBox="0 0 420 280"
        className="text-domain-accent size-full flex-1"
        role="img"
        aria-label={title}
      >
        <title>{title}</title>
        <BrandGrid width={420} height={280} step={28} opacity={0.055} />

        <BrandPanel x={40} y={180} width={90} height={56} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={85} y={204} size={10}>
          Team
        </BrandLabel>
        <BrandLabel x={85} y={220} size={8} opacity={0.4}>
          Local needs
        </BrandLabel>

        <BrandConnector d="M130 208 H160" opacity={0.3} />
        <BrandPanel x={160} y={150} width={100} height={86} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={210} y={184} size={10}>
          Institution
        </BrandLabel>
        <BrandLabel x={210} y={202} size={8} opacity={0.4}>
          Broader scale
        </BrandLabel>

        <BrandConnector d="M260 193 H290" opacity={0.3} />
        <BrandPanel x={290} y={110} width={100} height={126} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={340} y={160} size={10}>
          Portfolio
        </BrandLabel>
        <BrandLabel x={340} y={178} size={8} opacity={0.4}>
          Multi-product
        </BrandLabel>

        <BrandPanel x={60} y={40} width={300} height={44} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={210} y={66} size={10}>
          Shared platform grows without redesigning the foundation
        </BrandLabel>
      </svg>
    </BrandShell>
  );
}
