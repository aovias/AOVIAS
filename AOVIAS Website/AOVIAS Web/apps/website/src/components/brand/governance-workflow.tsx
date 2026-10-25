import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type GovernanceWorkflowProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * Governance and human responsibility around trust and privacy.
 */
export function GovernanceWorkflow({
  title = 'Governance workflow',
  caption = 'Policy · Oversight · Accountability — human responsibility',
  className,
}: GovernanceWorkflowProps) {
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

        <BrandPanel x={40} y={48} width={100} height={48} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={90} y={76} size={10}>
          Policy
        </BrandLabel>

        <BrandConnector d="M140 72 H180" opacity={0.3} />
        <BrandPanel x={180} y={48} width={100} height={48} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={230} y={76} size={10}>
          Oversight
        </BrandLabel>

        <BrandConnector d="M280 72 H320" opacity={0.3} />
        <BrandPanel x={320} y={48} width={60} height={48} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={350} y={76} size={10}>
          Act
        </BrandLabel>

        <BrandConnector d="M230 96 V130" opacity={0.28} />
        <BrandPanel x={120} y={130} width={180} height={52} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={210} y={160} size={10}>
          Transparent decisions
        </BrandLabel>

        <BrandConnector d="M210 182 V210" opacity={0.28} />
        <BrandPanel x={90} y={210} width={240} height={44} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={210} y={236} size={10}>
          People remain accountable
        </BrandLabel>
      </svg>
    </BrandShell>
  );
}
