import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type ComplianceSupportProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * Compliance readiness as platform support — not a certification claim.
 */
export function ComplianceSupport({
  title = 'Compliance support model',
  caption = 'Controls · Traceability · Audit support — customer owns compliance',
  className,
}: ComplianceSupportProps) {
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

        <BrandPanel x={50} y={160} width={320} height={56} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={210} y={192} size={11}>
          Platform controls & evidence support
        </BrandLabel>

        <BrandPanel x={40} y={48} width={100} height={72} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={90} y={76} size={9}>
          Healthcare
        </BrandLabel>
        <BrandLabel x={90} y={94} size={8} opacity={0.4}>
          Context
        </BrandLabel>
        <BrandConnector d="M90 120 V160" opacity={0.28} />

        <BrandPanel x={160} y={48} width={100} height={72} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={210} y={76} size={9}>
          Government
        </BrandLabel>
        <BrandLabel x={210} y={94} size={8} opacity={0.4}>
          Context
        </BrandLabel>
        <BrandConnector d="M210 120 V160" opacity={0.28} />

        <BrandPanel x={280} y={48} width={100} height={72} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={330} y={76} size={9}>
          Enterprise
        </BrandLabel>
        <BrandLabel x={330} y={94} size={8} opacity={0.4}>
          Governance
        </BrandLabel>
        <BrandConnector d="M330 120 V160" opacity={0.28} />

        <BrandPanel x={80} y={232} width={260} height={28} opacity={brandInk.fillFaint} stroked />
        <BrandLabel x={210} y={250} size={9} opacity={0.45}>
          Compliance depends on customer implementation
        </BrandLabel>
      </svg>
    </BrandShell>
  );
}
