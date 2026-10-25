import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type DataProtectionLifecycleProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * Data protection lifecycle — minimize, protect, steward, retire.
 */
export function DataProtectionLifecycle({
  title = 'Data protection lifecycle',
  caption = 'Minimize · Protect · Steward · Retire — engineering intent',
  className,
}: DataProtectionLifecycleProps) {
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

        <BrandPanel x={28} y={100} width={80} height={56} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={68} y={124} size={9}>
          Collect
        </BrandLabel>
        <BrandLabel x={68} y={140} size={8} opacity={0.4}>
          Minimize
        </BrandLabel>

        <BrandConnector d="M108 128 H128" opacity={0.3} />
        <BrandPanel x={128} y={100} width={80} height={56} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={168} y={124} size={9}>
          Protect
        </BrandLabel>
        <BrandLabel x={168} y={140} size={8} opacity={0.4}>
          Handle
        </BrandLabel>

        <BrandConnector d="M208 128 H228" opacity={0.3} />
        <BrandPanel x={228} y={100} width={80} height={56} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={268} y={124} size={9}>
          Steward
        </BrandLabel>
        <BrandLabel x={268} y={140} size={8} opacity={0.4}>
          Govern
        </BrandLabel>

        <BrandConnector d="M308 128 H328" opacity={0.3} />
        <BrandPanel x={328} y={100} width={64} height={56} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={360} y={132} size={9}>
          Retire
        </BrandLabel>

        <BrandPanel x={80} y={40} width={260} height={36} opacity={brandInk.fillFaint} stroked />
        <BrandLabel x={210} y={62} size={10}>
          Confidentiality · Integrity · Availability
        </BrandLabel>

        <BrandPanel x={100} y={200} width={220} height={48} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={210} y={228} size={10}>
          Lifecycle awareness by design
        </BrandLabel>
      </svg>
    </BrandShell>
  );
}
