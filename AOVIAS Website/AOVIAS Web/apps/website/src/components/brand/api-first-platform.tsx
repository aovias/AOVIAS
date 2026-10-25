import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type ApiFirstPlatformProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * API-first platform philosophy — extensible services, no endpoint detail.
 */
export function ApiFirstPlatform({
  title = 'API-first platform',
  caption = 'Secure · Extensible · Service-oriented — documentation forthcoming',
  className,
}: ApiFirstPlatformProps) {
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

        <BrandPanel x={40} y={40} width={100} height={48} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={90} y={68} size={9}>
          Products
        </BrandLabel>

        <BrandPanel x={160} y={40} width={100} height={48} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={210} y={68} size={9}>
          Partners
        </BrandLabel>

        <BrandPanel x={280} y={40} width={100} height={48} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={330} y={68} size={9}>
          Automation
        </BrandLabel>

        <BrandConnector d="M90 88 V120" opacity={0.28} />
        <BrandConnector d="M210 88 V120" opacity={0.28} />
        <BrandConnector d="M330 88 V120" opacity={0.28} />

        <BrandPanel x={70} y={120} width={280} height={52} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={210} y={150} size={11}>
          Platform API layer
        </BrandLabel>

        <BrandConnector d="M210 172 V200" opacity={0.28} />
        <BrandPanel x={50} y={200} width={100} height={44} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={100} y={226} size={9}>
          Services
        </BrandLabel>
        <BrandPanel x={160} y={200} width={100} height={44} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={210} y={226} size={9}>
          Identity
        </BrandLabel>
        <BrandPanel x={270} y={200} width={100} height={44} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={320} y={226} size={9}>
          Data
        </BrandLabel>
      </svg>
    </BrandShell>
  );
}
