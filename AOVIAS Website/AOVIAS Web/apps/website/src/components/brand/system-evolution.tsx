import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type SystemEvolutionProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * How architecture evolves without sacrificing stability — conceptual stages.
 */
export function SystemEvolution({
  title = 'System evolution',
  caption = 'Stable core · Modular growth · Extensible capability',
  className,
}: SystemEvolutionProps) {
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

        <BrandPanel x={40} y={180} width={340} height={56} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={210} y={212} size={11}>
          Stable platform foundation
        </BrandLabel>

        <BrandPanel x={60} y={110} width={100} height={48} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={110} y={138} size={10}>
          Module A
        </BrandLabel>
        <BrandPanel x={160} y={110} width={100} height={48} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={210} y={138} size={10}>
          Module B
        </BrandLabel>
        <BrandPanel x={260} y={110} width={100} height={48} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={310} y={138} size={10}>
          Module C
        </BrandLabel>

        <BrandConnector d="M110 158 V180" opacity={0.28} />
        <BrandConnector d="M210 158 V180" opacity={0.28} />
        <BrandConnector d="M310 158 V180" opacity={0.28} />

        <BrandPanel x={120} y={40} width={180} height={44} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={210} y={66} size={10}>
          New capability (composed)
        </BrandLabel>
        <BrandConnector d="M210 84 V110" opacity={0.28} />
      </svg>
    </BrandShell>
  );
}
