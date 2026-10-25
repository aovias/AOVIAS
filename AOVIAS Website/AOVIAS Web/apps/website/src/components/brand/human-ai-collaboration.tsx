import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type HumanAiCollaborationProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * Human-in-the-loop workflow — AI proposes; humans review, approve, remain accountable.
 * Blueprint language only; no robots or generic AI tropes.
 */
export function HumanAiCollaboration({
  title = 'Human and AI collaboration',
  caption = 'AI proposes · Humans review · Humans approve · Humans remain accountable',
  className,
}: HumanAiCollaborationProps) {
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

        <BrandPanel x={40} y={36} width={140} height={56} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={110} y={60} size={11}>
          Operator
        </BrandLabel>
        <BrandLabel x={110} y={76} size={9} opacity={0.4}>
          Human judgment
        </BrandLabel>

        <BrandPanel x={240} y={36} width={140} height={56} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={310} y={60} size={11}>
          Platform AI
        </BrandLabel>
        <BrandLabel x={310} y={76} size={9} opacity={0.4}>
          Decision support
        </BrandLabel>

        <BrandConnector d="M180 64 H240" opacity={0.3} />

        <BrandPanel x={130} y={120} width={160} height={40} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={210} y={144} size={10}>
          1 · AI proposes
        </BrandLabel>

        <BrandConnector d="M210 160 V176" opacity={0.28} />
        <BrandPanel x={130} y={176} width={160} height={40} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={210} y={200} size={10}>
          2 · Human reviews
        </BrandLabel>

        <BrandConnector d="M210 216 V232" opacity={0.28} />
        <BrandPanel x={80} y={232} width={120} height={36} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={140} y={254} size={10}>
          3 · Approves
        </BrandLabel>
        <BrandPanel x={220} y={232} width={120} height={36} opacity={brandInk.fillStrong} stroked />
        <BrandLabel x={280} y={254} size={10}>
          4 · Accountable
        </BrandLabel>
      </svg>
    </BrandShell>
  );
}
