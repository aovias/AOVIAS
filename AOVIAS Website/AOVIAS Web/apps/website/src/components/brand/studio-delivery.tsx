import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type StudioDeliveryProps = {
  title?: string;
  className?: string;
};

/**
 * Studio product visual — disciplined delivery workspace.
 */
export function StudioDelivery({
  title = 'AOVIAS Studio — delivery workspace',
  className,
}: StudioDeliveryProps) {
  return (
    <BrandShell title={title} className={cn(className)} aspectClassName="min-h-[12rem]">
      <div className="border-border bg-muted/50 flex items-center gap-1.5 border-b px-3 py-2">
        <span className="bg-border size-2 rounded-full" aria-hidden />
        <span className="bg-border size-2 rounded-full" aria-hidden />
        <span className="bg-border size-2 rounded-full" aria-hidden />
        <span className="text-muted-foreground ms-2 truncate text-[0.65rem] font-medium tracking-wide">
          {title}
        </span>
      </div>
      <svg viewBox="0 0 320 180" className="text-domain-accent size-full flex-1" aria-hidden>
        <BrandGrid width={320} height={180} step={20} opacity={0.05} />
        <BrandPanel x={12} y={12} width={64} height={156} opacity={brandInk.fillSoft} stroked />
        <BrandPanel x={24} y={28} width={40} height={6} opacity={brandInk.fillAccent} radius={2} />
        <BrandPanel x={24} y={48} width={28} height={5} opacity={brandInk.fillSoft} radius={2} />
        <BrandPanel x={24} y={62} width={36} height={5} opacity={brandInk.fillSoft} radius={2} />

        <BrandPanel x={88} y={12} width={100} height={156} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={138} y={40} size={9}>
          Design
        </BrandLabel>
        <BrandPanel x={100} y={56} width={76} height={48} opacity={brandInk.fillMid} radius={4} />
        <BrandPanel x={100} y={116} width={60} height={6} opacity={brandInk.fillSoft} radius={2} />
        <BrandPanel x={100} y={130} width={72} height={6} opacity={brandInk.fillSoft} radius={2} />

        <BrandPanel x={200} y={12} width={108} height={72} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={254} y={40} size={9}>
          Standards
        </BrandLabel>
        <BrandPanel x={212} y={52} width={84} height={6} opacity={brandInk.fillAccent} radius={2} />
        <BrandPanel x={212} y={64} width={64} height={5} opacity={brandInk.fillSoft} radius={2} />

        <BrandPanel x={200} y={96} width={108} height={72} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={254} y={124} size={9}>
          Handoff
        </BrandLabel>
        <BrandPanel x={212} y={140} width={72} height={6} opacity={brandInk.fillMid} radius={2} />

        <BrandConnector d="M188 90 H200 M254 84 V96" opacity={0.28} />
      </svg>
    </BrandShell>
  );
}
