import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type IdentityPlatformProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * Identity as a shared platform service — conceptual access model.
 */
export function IdentityPlatformVisual({
  title = 'Identity platform',
  caption = 'Authenticate · Authorize · Shared access — secure collaboration',
  className,
}: IdentityPlatformProps) {
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

        <BrandPanel x={130} y={100} width={160} height={60} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={210} y={126} size={11}>
          Identity service
        </BrandLabel>
        <BrandLabel x={210} y={144} size={8} opacity={0.4}>
          Shared platform capability
        </BrandLabel>

        <BrandPanel x={40} y={36} width={100} height={40} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={90} y={60} size={9}>
          Product A
        </BrandLabel>
        <BrandConnector d="M90 76 V100 H160" opacity={0.28} />

        <BrandPanel x={160} y={36} width={100} height={40} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={210} y={60} size={9}>
          Product B
        </BrandLabel>
        <BrandConnector d="M210 76 V100" opacity={0.28} />

        <BrandPanel x={280} y={36} width={100} height={40} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={330} y={60} size={9}>
          Product C
        </BrandLabel>
        <BrandConnector d="M330 76 V100 H260" opacity={0.28} />

        <BrandPanel x={60} y={200} width={120} height={44} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={120} y={226} size={10}>
          Authentication
        </BrandLabel>
        <BrandConnector d="M150 200 V160" opacity={0.28} />

        <BrandPanel x={240} y={200} width={120} height={44} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={300} y={226} size={10}>
          Authorization
        </BrandLabel>
        <BrandConnector d="M270 200 V160" opacity={0.28} />
      </svg>
    </BrandShell>
  );
}
