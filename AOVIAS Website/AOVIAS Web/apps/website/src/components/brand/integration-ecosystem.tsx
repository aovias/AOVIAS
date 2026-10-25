import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type IntegrationEcosystemProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * Conceptual enterprise integration ecosystem — category labels, no vendors.
 */
export function IntegrationEcosystem({
  title = 'Enterprise integration ecosystem',
  caption = 'AOVIAS connects — it does not isolate institutional systems',
  className,
}: IntegrationEcosystemProps) {
  return (
    <BrandShell
      title={title}
      {...(caption ? { caption } : {})}
      className={cn('aspect-[4/3] xl:min-h-[18rem]', className)}
      aspectClassName="min-h-[14rem]"
    >
      <svg
        viewBox="0 0 420 300"
        className="text-domain-accent size-full flex-1"
        role="img"
        aria-label={title}
      >
        <title>{title}</title>
        <BrandGrid width={420} height={300} step={28} opacity={0.055} />

        <BrandPanel x={145} y={120} width={130} height={52} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={210} y={150} size={11}>
          AOVIAS Platform
        </BrandLabel>

        <BrandPanel x={20} y={28} width={100} height={36} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={70} y={50} size={8}>
          Identity
        </BrandLabel>
        <BrandConnector d="M100 50 Q140 90 160 130" opacity={0.28} />

        <BrandPanel x={160} y={20} width={100} height={36} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={210} y={42} size={8}>
          Healthcare
        </BrandLabel>
        <BrandConnector d="M210 56 V120" opacity={0.28} />

        <BrandPanel x={300} y={28} width={100} height={36} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={350} y={50} size={8}>
          Government
        </BrandLabel>
        <BrandConnector d="M320 50 Q280 90 260 130" opacity={0.28} />

        <BrandPanel x={20} y={200} width={90} height={36} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={65} y={222} size={8}>
          ERP
        </BrandLabel>
        <BrandConnector d="M90 210 Q130 190 160 165" opacity={0.28} />

        <BrandPanel x={120} y={240} width={90} height={36} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={165} y={262} size={8}>
          CRM
        </BrandLabel>
        <BrandConnector d="M165 240 V172" opacity={0.28} />

        <BrandPanel x={220} y={240} width={100} height={36} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={270} y={262} size={8}>
          Communications
        </BrandLabel>
        <BrandConnector d="M250 240 V172" opacity={0.28} />

        <BrandPanel x={330} y={200} width={70} height={36} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={365} y={222} size={8}>
          Analytics
        </BrandLabel>
        <BrandConnector d="M340 210 Q300 190 260 165" opacity={0.28} />
      </svg>
    </BrandShell>
  );
}
