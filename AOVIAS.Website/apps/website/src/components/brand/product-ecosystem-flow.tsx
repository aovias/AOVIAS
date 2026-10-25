import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type ProductEcosystemFlowProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * Commercial ecosystem flow — Command → Platform → Studio → Customer.
 * One elegant illustration for the Products landing.
 */
export function ProductEcosystemFlow({
  title = 'How AOVIAS products work together',
  caption = 'Command · Platform · Studio · Customer',
  className,
}: ProductEcosystemFlowProps) {
  return (
    <BrandShell
      title={title}
      {...(caption ? { caption } : {})}
      className={cn(className)}
      aspectClassName="min-h-[16rem] aspect-[16/10] xl:min-h-[18rem]"
    >
      <svg
        viewBox="0 0 420 280"
        className="text-domain-accent size-full flex-1"
        role="img"
        aria-label={title}
      >
        <title>{title}</title>
        <BrandGrid width={420} height={280} step={24} opacity={0.045} />

        <BrandPanel
          x={130}
          y={18}
          width={160}
          height={48}
          opacity={brandInk.fillMid}
          stroked
        />
        <BrandLabel x={210} y={46} size={12}>
          Command
        </BrandLabel>

        <BrandConnector d="M210 66 V86" opacity={0.35} />

        <BrandPanel
          x={110}
          y={86}
          width={200}
          height={56}
          opacity={brandInk.fillStrong}
          radius={brandInk.radiusLg}
          stroked
        />
        <BrandLabel x={210} y={112} size={12}>
          Platform
        </BrandLabel>
        <BrandLabel x={210} y={130} size={9} opacity={0.4}>
          Shared foundation
        </BrandLabel>

        <BrandConnector d="M210 142 V162" opacity={0.35} />

        <BrandPanel
          x={130}
          y={162}
          width={160}
          height={48}
          opacity={brandInk.fillMid}
          stroked
        />
        <BrandLabel x={210} y={190} size={12}>
          Studio
        </BrandLabel>

        <BrandConnector d="M210 210 V230" opacity={0.35} />

        <BrandPanel
          x={130}
          y={230}
          width={160}
          height={40}
          opacity={brandInk.fillSoft}
          stroked
        />
        <BrandLabel x={210} y={254} size={11}>
          Customer
        </BrandLabel>
      </svg>
    </BrandShell>
  );
}
