import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type CommandOperationsProps = {
  title?: string;
  className?: string;
};

/**
 * Command product visual — operational visibility and accountable control.
 */
export function CommandOperations({
  title = 'AOVIAS Command — operational workspace',
  className,
}: CommandOperationsProps) {
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
        <BrandPanel x={24} y={44} width={36} height={5} opacity={brandInk.fillSoft} radius={2} />
        <BrandPanel x={24} y={56} width={32} height={5} opacity={brandInk.fillSoft} radius={2} />
        <BrandPanel x={24} y={68} width={40} height={5} opacity={brandInk.fillSoft} radius={2} />

        <BrandPanel x={88} y={12} width={220} height={44} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={140} y={30} size={9} anchor="start">
          Operational view
        </BrandLabel>
        <BrandPanel x={100} y={36} width={100} height={5} opacity={brandInk.fillSoft} radius={2} />

        <BrandPanel x={88} y={68} width={104} height={100} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={140} y={92} size={9}>
          Visibility
        </BrandLabel>
        <BrandPanel x={100} y={108} width={48} height={36} opacity={brandInk.fillMid} radius={4} />
        <BrandPanel x={156} y={108} width={24} height={36} opacity={brandInk.fillAccent} radius={4} />

        <BrandPanel x={204} y={68} width={104} height={100} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={256} y={92} size={9}>
          Control
        </BrandLabel>
        <BrandPanel x={216} y={108} width={80} height={8} opacity={brandInk.fillAccent} radius={2} />
        <BrandPanel x={216} y={124} width={64} height={6} opacity={brandInk.fillSoft} radius={2} />
        <BrandPanel x={216} y={138} width={72} height={6} opacity={brandInk.fillSoft} radius={2} />

        <BrandConnector d="M140 156 H256" opacity={0.3} />
      </svg>
    </BrandShell>
  );
}
