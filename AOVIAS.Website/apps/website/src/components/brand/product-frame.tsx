import { cn } from '@/lib/utils';

import { CommandOperations } from './command-operations';
import {
  AnalyticsBoard,
  DashboardConsole,
  DeploymentPipeline,
  MonitoringConsole,
} from './product-consoles';
import { StudioDelivery } from './studio-delivery';
import {
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type ProductFrameProps = {
  title?: string;
  className?: string;
  /** Visual density variant for product family cues. */
  variant?:
    | 'command'
    | 'studio'
    | 'platform'
    | 'dashboard'
    | 'analytics'
    | 'monitoring'
    | 'deployment'
    | 'default';
};

/**
 * Abstract product UI chrome — routes to dedicated story visuals by variant.
 */
export function ProductFrame({
  title = 'Product workspace',
  className,
  variant = 'default',
}: ProductFrameProps) {
  if (variant === 'command') {
    return (
      <CommandOperations
        title={title}
        {...(className ? { className } : {})}
      />
    );
  }
  if (variant === 'studio') {
    return (
      <StudioDelivery title={title} {...(className ? { className } : {})} />
    );
  }
  if (variant === 'dashboard' || variant === 'platform') {
    return (
      <DashboardConsole title={title} {...(className ? { className } : {})} />
    );
  }
  if (variant === 'analytics') {
    return (
      <AnalyticsBoard title={title} {...(className ? { className } : {})} />
    );
  }
  if (variant === 'monitoring') {
    return (
      <MonitoringConsole title={title} {...(className ? { className } : {})} />
    );
  }
  if (variant === 'deployment') {
    return (
      <DeploymentPipeline title={title} {...(className ? { className } : {})} />
    );
  }

  return (
    <BrandShell title={title} className={cn(className)} aspectClassName="min-h-[12rem]">
      <div className="border-border bg-muted/50 flex items-center gap-1.5 border-b px-3 py-2">
        <span className="bg-border size-2 rounded-full" aria-hidden />
        <span className="bg-border size-2 rounded-full" aria-hidden />
        <span className="bg-border size-2 rounded-full" aria-hidden />
        <span className="text-muted-foreground ms-2 truncate text-caption font-medium tracking-wide">
          {title}
        </span>
      </div>
      <svg viewBox="0 0 320 180" className="text-domain-accent size-full flex-1" aria-hidden>
        <BrandGrid width={320} height={180} step={20} opacity={0.05} />
        <BrandPanel x={16} y={16} width={72} height={148} opacity={brandInk.fillSoft} stroked />
        <BrandPanel x={28} y={28} width={48} height={8} opacity={brandInk.fillAccent} radius={2} />
        <BrandPanel x={28} y={48} width={40} height={6} opacity={brandInk.fillSoft} radius={2} />
        <BrandPanel x={28} y={62} width={36} height={6} opacity={brandInk.fillSoft} radius={2} />
        <BrandPanel x={104} y={16} width={200} height={40} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={148} y={40} size={9} anchor="start">
          Workspace
        </BrandLabel>
        <BrandPanel x={104} y={68} width={96} height={96} opacity={brandInk.fillFaint} stroked />
        <BrandPanel x={216} y={68} width={88} height={96} opacity={brandInk.fillFaint} stroked />
        <BrandPanel x={228} y={88} width={64} height={8} opacity={brandInk.fillAccent} radius={2} />
        <BrandPanel x={228} y={106} width={48} height={6} opacity={brandInk.fillSoft} radius={2} />
      </svg>
    </BrandShell>
  );
}
