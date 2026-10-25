import { cn } from '@/lib/utils';

import {
  BrandGrid,
  BrandPanel,
  brandInk,
} from './brand-primitives';

export type KnowledgeCoverTone =
  | 'default'
  | 'primary'
  | 'secondary'
  | 'info'
  | 'success'
  | 'warning';

export type KnowledgeCoverProps = {
  label: string;
  tone?: KnowledgeCoverTone;
  className?: string;
  /** Optional domain motif for library covers. */
  motif?: 'ai' | 'security' | 'operations' | 'architecture' | 'default';
};

const toneAccent: Record<KnowledgeCoverTone, string> = {
  default: 'text-foreground',
  primary: 'text-primary',
  secondary: 'text-primary',
  info: 'text-info',
  success: 'text-success',
  warning: 'text-warning',
};

/**
 * Category-driven abstract cover — blueprint family, not dashed placeholders.
 */
export function KnowledgeCover({
  label,
  tone = 'default',
  className,
  motif = 'default',
}: KnowledgeCoverProps) {
  return (
    <div
      role="img"
      aria-label={label}
      className={cn(
        'border-border bg-muted/40 relative flex size-full min-h-[8rem] items-end overflow-hidden rounded-md border',
        className,
      )}
    >
      <svg
        viewBox="0 0 320 180"
        className={cn('absolute inset-0 size-full', toneAccent[tone])}
        aria-hidden
      >
        <BrandGrid width={320} height={180} step={20} opacity={0.06} />
        <BrandPanel x={0} y={0} width={320} height={180} opacity={0.03} radius={0} />
        {motif === 'ai' ? (
          <>
            <BrandPanel x={110} y={50} width={100} height={60} opacity={brandInk.fillMid} stroked />
            <BrandPanel x={40} y={40} width={40} height={24} opacity={brandInk.fillSoft} />
            <BrandPanel x={240} y={40} width={40} height={24} opacity={brandInk.fillSoft} />
          </>
        ) : motif === 'security' ? (
          <BrandPanel x={110} y={36} width={100} height={90} opacity={brandInk.fillMid} stroked />
        ) : motif === 'operations' ? (
          <>
            <BrandPanel x={40} y={70} width={50} height={40} opacity={brandInk.fillMid} />
            <BrandPanel x={110} y={70} width={50} height={40} opacity={brandInk.fillMid} />
            <BrandPanel x={180} y={70} width={50} height={40} opacity={brandInk.fillMid} />
            <BrandPanel x={250} y={70} width={50} height={40} opacity={brandInk.fillMid} />
          </>
        ) : motif === 'architecture' ? (
          <>
            <BrandPanel x={24} y={28} width={272} height={28} opacity={brandInk.fillSoft} stroked />
            <BrandPanel x={24} y={72} width={128} height={40} opacity={brandInk.fillMid} />
            <BrandPanel x={168} y={72} width={128} height={40} opacity={brandInk.fillMid} />
          </>
        ) : (
          <>
            <BrandPanel x={24} y={28} width={96} height={8} opacity={brandInk.fillAccent} radius={2} />
            <BrandPanel x={24} y={44} width={64} height={6} opacity={brandInk.fillSoft} radius={2} />
            <BrandPanel x={24} y={72} width={140} height={4} opacity={brandInk.fillSoft} radius={2} />
            <BrandPanel x={24} y={84} width={120} height={4} opacity={brandInk.fillFaint} radius={2} />
            <circle cx={260} cy={40} r={48} fill="currentColor" opacity={0.07} />
          </>
        )}
      </svg>
      <span className="text-muted-foreground relative z-10 line-clamp-2 px-3 pb-3 text-xs font-medium">
        {label}
      </span>
    </div>
  );
}
