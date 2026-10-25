import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type EngineeringLifecycleProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * Conceptual engineering lifecycle — discipline across design, build, operate, improve.
 */
export function EngineeringLifecycle({
  title = 'Engineering lifecycle',
  caption = 'Design · Build · Operate · Improve — continuous stewardship',
  className,
}: EngineeringLifecycleProps) {
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

        <BrandPanel x={28} y={48} width={84} height={52} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={70} y={72} size={10}>
          Design
        </BrandLabel>
        <BrandLabel x={70} y={88} size={8} opacity={0.4}>
          Intent
        </BrandLabel>

        <BrandConnector d="M112 74 H140" opacity={0.3} />
        <BrandPanel x={140} y={48} width={84} height={52} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={182} y={72} size={10}>
          Build
        </BrandLabel>
        <BrandLabel x={182} y={88} size={8} opacity={0.4}>
          Quality
        </BrandLabel>

        <BrandConnector d="M224 74 H252" opacity={0.3} />
        <BrandPanel x={252} y={48} width={84} height={52} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={294} y={72} size={10}>
          Operate
        </BrandLabel>
        <BrandLabel x={294} y={88} size={8} opacity={0.4}>
          Stability
        </BrandLabel>

        <BrandConnector d="M336 74 H364" opacity={0.3} />
        <BrandPanel x={364} y={48} width={28} height={52} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={378} y={78} size={9}>
          →
        </BrandLabel>

        <BrandPanel x={80} y={140} width={260} height={48} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={210} y={168} size={11}>
          Continuous improvement
        </BrandLabel>

        <BrandConnector d="M210 188 V210" opacity={0.28} />
        <BrandPanel x={100} y={210} width={220} height={44} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={210} y={236} size={10}>
          Institutions can depend on the outcome
        </BrandLabel>
      </svg>
    </BrandShell>
  );
}
