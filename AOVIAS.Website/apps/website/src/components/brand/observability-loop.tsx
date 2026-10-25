import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type ObservabilityLoopProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * Observability as a feedback loop enabling reliable operations.
 */
export function ObservabilityLoop({
  title = 'Observability feedback loop',
  caption = 'Monitor · Diagnose · Learn · Improve operations',
  className,
}: ObservabilityLoopProps) {
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

        <BrandPanel x={150} y={36} width={120} height={44} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={210} y={62} size={10}>
          Monitor
        </BrandLabel>

        <BrandConnector d="M270 58 H320" opacity={0.3} />
        <BrandPanel x={300} y={100} width={90} height={44} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={345} y={126} size={10}>
          Diagnose
        </BrandLabel>

        <BrandConnector d="M345 144 V180" opacity={0.28} />
        <BrandPanel x={150} y={190} width={120} height={44} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={210} y={216} size={10}>
          Improve
        </BrandLabel>

        <BrandConnector d="M150 212 H100" opacity={0.3} />
        <BrandPanel x={30} y={100} width={90} height={44} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={75} y={126} size={10}>
          Insights
        </BrandLabel>

        <BrandConnector d="M75 100 V58 H150" opacity={0.28} />

        <BrandPanel x={130} y={110} width={160} height={52} opacity={brandInk.fillFaint} stroked />
        <BrandLabel x={210} y={132} size={10}>
          Operational clarity
        </BrandLabel>
        <BrandLabel x={210} y={148} size={8} opacity={0.4}>
          Metrics · Logs · Health
        </BrandLabel>
      </svg>
    </BrandShell>
  );
}
