import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type SecurePlatformProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * Secure-by-design platform layers — conceptual, no padlock clichés.
 */
export function SecurePlatform({
  title = 'Secure platform architecture',
  caption = 'Secure by design · Platform controls · Operational practice',
  className,
}: SecurePlatformProps) {
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

        <BrandPanel x={50} y={40} width={320} height={48} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={210} y={68} size={11}>
          Products & workflows
        </BrandLabel>

        <BrandConnector d="M210 88 V108" opacity={0.28} />
        <BrandPanel x={50} y={108} width={320} height={48} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={210} y={136} size={11}>
          Shared security controls
        </BrandLabel>

        <BrandConnector d="M210 156 V176" opacity={0.28} />
        <BrandPanel x={50} y={176} width={320} height={48} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={210} y={204} size={11}>
          Platform foundation
        </BrandLabel>

        <BrandPanel x={80} y={236} width={260} height={28} opacity={brandInk.fillFaint} stroked />
        <BrandLabel x={210} y={254} size={9} opacity={0.45}>
          Continuous engineering practice
        </BrandLabel>
      </svg>
    </BrandShell>
  );
}
