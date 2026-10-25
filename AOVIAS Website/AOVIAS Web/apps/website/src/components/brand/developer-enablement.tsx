import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type DeveloperEnablementProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * Developer platform vision — enablement without claiming current availability.
 */
export function DeveloperEnablement({
  title = 'Developer enablement',
  caption = 'Strategic direction — extensibility, SDKs, and experience over time',
  className,
}: DeveloperEnablementProps) {
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

        <BrandPanel x={130} y={40} width={160} height={48} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={210} y={68} size={11}>
          Builder teams
        </BrandLabel>

        <BrandConnector d="M210 88 V112" opacity={0.28} />
        <BrandPanel x={40} y={112} width={100} height={52} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={90} y={136} size={9}>
          SDKs
        </BrandLabel>
        <BrandLabel x={90} y={152} size={8} opacity={0.4}>
          Philosophy
        </BrandLabel>

        <BrandPanel x={160} y={112} width={100} height={52} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={210} y={136} size={9}>
          Extensibility
        </BrandLabel>
        <BrandLabel x={210} y={152} size={8} opacity={0.4}>
          Compose
        </BrandLabel>

        <BrandPanel x={280} y={112} width={100} height={52} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={330} y={136} size={9}>
          Tooling
        </BrandLabel>
        <BrandLabel x={330} y={152} size={8} opacity={0.4}>
          Future
        </BrandLabel>

        <BrandConnector d="M90 164 V190" opacity={0.25} />
        <BrandConnector d="M210 164 V190" opacity={0.25} />
        <BrandConnector d="M330 164 V190" opacity={0.25} />

        <BrandPanel x={70} y={190} width={280} height={52} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={210} y={220} size={10}>
          Platform capabilities for responsible extension
        </BrandLabel>
      </svg>
    </BrandShell>
  );
}
