import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type ReliabilityModelProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * Reliability as layered engineering intent — not SLA claims.
 */
export function ReliabilityModel({
  title = 'Reliability model',
  caption = 'Predictable behavior · Resilience · Continuous monitoring',
  className,
}: ReliabilityModelProps) {
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

        <BrandPanel x={50} y={36} width={320} height={44} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={210} y={62} size={11}>
          Continuous monitoring
        </BrandLabel>

        <BrandConnector d="M210 80 V100" opacity={0.28} />
        <BrandPanel x={50} y={100} width={150} height={56} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={125} y={124} size={10}>
          Fault tolerance
        </BrandLabel>
        <BrandLabel x={125} y={140} size={8} opacity={0.4}>
          Contain · Recover
        </BrandLabel>

        <BrandPanel x={220} y={100} width={150} height={56} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={295} y={124} size={10}>
          Operational stability
        </BrandLabel>
        <BrandLabel x={295} y={140} size={8} opacity={0.4}>
          Calm operations
        </BrandLabel>

        <BrandConnector d="M125 156 V176" opacity={0.28} />
        <BrandConnector d="M295 156 V176" opacity={0.28} />
        <BrandPanel x={80} y={176} width={260} height={48} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={210} y={204} size={11}>
          Predictable institutional outcomes
        </BrandLabel>

        <BrandPanel x={100} y={236} width={220} height={28} opacity={brandInk.fillFaint} stroked />
        <BrandLabel x={210} y={254} size={9} opacity={0.45}>
          Resilience as design intent
        </BrandLabel>
      </svg>
    </BrandShell>
  );
}
