import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

type ConsoleProps = {
  title?: string;
  className?: string;
};

/**
 * Platform services / dashboard console — institutional overview surface.
 */
export function DashboardConsole({
  title = 'Platform services — institutional dashboard',
  className,
}: ConsoleProps) {
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
        <BrandPanel x={12} y={12} width={296} height={28} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={24} y={30} size={9} anchor="start">
          Institutional overview
        </BrandLabel>
        <BrandPanel x={12} y={52} width={92} height={56} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={58} y={72} size={8}>
          Services
        </BrandLabel>
        <BrandPanel x={28} y={84} width={60} height={10} opacity={brandInk.fillAccent} radius={2} />
        <BrandPanel x={112} y={52} width={92} height={56} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={158} y={72} size={8}>
          Health
        </BrandLabel>
        <BrandPanel x={128} y={84} width={40} height={10} opacity={brandInk.fillMid} radius={2} />
        <BrandPanel x={212} y={52} width={96} height={56} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={260} y={72} size={8}>
          Capacity
        </BrandLabel>
        <BrandPanel x={228} y={84} width={64} height={10} opacity={brandInk.fillSoft} radius={2} />
        <BrandPanel x={12} y={120} width={296} height={48} opacity={brandInk.fillFaint} stroked />
        <BrandLabel x={24} y={140} size={8} anchor="start">
          Shared platform services
        </BrandLabel>
        <BrandPanel x={24} y={148} width={80} height={6} opacity={brandInk.fillAccent} radius={2} />
        <BrandPanel x={116} y={148} width={64} height={6} opacity={brandInk.fillSoft} radius={2} />
        <BrandPanel x={192} y={148} width={72} height={6} opacity={brandInk.fillSoft} radius={2} />
      </svg>
    </BrandShell>
  );
}

/**
 * Analytics insight board — structured signal, not spectacle.
 */
export function AnalyticsBoard({
  title = 'Analytics — structured institutional signal',
  className,
}: ConsoleProps) {
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
        <BrandPanel x={12} y={12} width={180} height={156} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={24} y={32} size={9} anchor="start">
          Trend
        </BrandLabel>
        <BrandConnector d="M28 140 L60 110 L96 120 L132 78 L168 92" opacity={0.45} />
        <BrandPanel x={40} y={100} width={8} height={40} opacity={brandInk.fillMid} radius={2} />
        <BrandPanel x={72} y={88} width={8} height={52} opacity={brandInk.fillAccent} radius={2} />
        <BrandPanel x={104} y={96} width={8} height={44} opacity={brandInk.fillMid} radius={2} />
        <BrandPanel x={136} y={72} width={8} height={68} opacity={brandInk.fillAccent} radius={2} />
        <BrandPanel x={204} y={12} width={104} height={72} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={256} y={36} size={8}>
          KPI
        </BrandLabel>
        <BrandPanel x={220} y={48} width={72} height={16} opacity={brandInk.fillAccent} radius={2} />
        <BrandPanel x={204} y={96} width={104} height={72} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={256} y={120} size={8}>
          Cohorts
        </BrandLabel>
        <BrandPanel x={220} y={132} width={56} height={6} opacity={brandInk.fillSoft} radius={2} />
        <BrandPanel x={220} y={144} width={72} height={6} opacity={brandInk.fillSoft} radius={2} />
      </svg>
    </BrandShell>
  );
}

/**
 * Monitoring console — observe → diagnose → improve.
 */
export function MonitoringConsole({
  title = 'Monitoring — operational continuity',
  className,
}: ConsoleProps) {
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
        <BrandPanel x={12} y={12} width={70} height={156} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={47} y={36} size={8}>
          Streams
        </BrandLabel>
        <BrandPanel x={24} y={52} width={46} height={8} opacity={brandInk.fillAccent} radius={2} />
        <BrandPanel x={24} y={68} width={40} height={6} opacity={brandInk.fillSoft} radius={2} />
        <BrandPanel x={24} y={82} width={44} height={6} opacity={brandInk.fillSoft} radius={2} />
        <BrandPanel x={94} y={12} width={214} height={48} opacity={brandInk.fillMid} stroked />
        <BrandLabel x={108} y={32} size={9} anchor="start">
          Service health
        </BrandLabel>
        <BrandPanel x={108} y={40} width={120} height={6} opacity={brandInk.fillAccent} radius={2} />
        <BrandPanel x={94} y={72} width={102} height={96} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={145} y={96} size={8}>
          Alerts
        </BrandLabel>
        <BrandPanel x={110} y={112} width={70} height={8} opacity={brandInk.fillMid} radius={2} />
        <BrandPanel x={110} y={128} width={54} height={6} opacity={brandInk.fillSoft} radius={2} />
        <BrandPanel x={206} y={72} width={102} height={96} opacity={brandInk.fillSoft} stroked />
        <BrandLabel x={257} y={96} size={8}>
          Latency
        </BrandLabel>
        <BrandConnector d="M220 140 L240 110 L260 124 L280 96" opacity={0.4} />
      </svg>
    </BrandShell>
  );
}

/**
 * Deployment pipeline — controlled release stages.
 */
export function DeploymentPipeline({
  title = 'Deployment — controlled release',
  className,
}: ConsoleProps) {
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
        <BrandLabel x={160} y={28} size={9}>
          Release path
        </BrandLabel>
        {[
          { x: 20, label: 'Build' },
          { x: 96, label: 'Verify' },
          { x: 172, label: 'Stage' },
          { x: 248, label: 'Release' },
        ].map((stage) => (
          <g key={stage.label}>
            <BrandPanel
              x={stage.x}
              y={52}
              width={52}
              height={76}
              opacity={brandInk.fillSoft}
              stroked
            />
            <BrandLabel x={stage.x + 26} y={90} size={8}>
              {stage.label}
            </BrandLabel>
            <BrandPanel
              x={stage.x + 10}
              y={104}
              width={32}
              height={6}
              opacity={brandInk.fillAccent}
              radius={2}
            />
          </g>
        ))}
        <BrandConnector d="M72 90 H96" opacity={0.35} />
        <BrandConnector d="M148 90 H172" opacity={0.35} />
        <BrandConnector d="M224 90 H248" opacity={0.35} />
        <BrandPanel x={20} y={144} width={280} height={20} opacity={brandInk.fillFaint} stroked />
        <BrandLabel x={160} y={158} size={8}>
          Governance gates at each stage
        </BrandLabel>
      </svg>
    </BrandShell>
  );
}
