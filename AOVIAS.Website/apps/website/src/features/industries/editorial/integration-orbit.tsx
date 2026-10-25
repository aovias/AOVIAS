import type { IndustryIntegrations } from '@/features/industries/editorial/types';
import { cn } from '@/lib/utils';

export type IntegrationOrbitProps = {
  integrations: IndustryIntegrations;
  className?: string;
};

/**
 * Compact institutional integration map — Command as operational center.
 */
export function IntegrationOrbit({
  integrations,
  className,
}: IntegrationOrbitProps) {
  return (
    <div className={cn('mx-auto w-full max-w-3xl', className)}>
      <div className="border-border from-card via-card to-primary-muted/30 relative overflow-hidden rounded-2xl border bg-gradient-to-br p-6 sm:p-8">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 opacity-[0.07]"
          style={{
            backgroundImage:
              'linear-gradient(to right, currentColor 1px, transparent 1px), linear-gradient(to bottom, currentColor 1px, transparent 1px)',
            backgroundSize: '28px 28px',
          }}
        />
        <div className="relative flex flex-col items-center gap-6">
          <div className="border-domain-accent/50 bg-domain-accent/10 text-foreground shadow-[0_0_20px_rgba(59,130,246,0.25)] rounded-xl border px-5 py-3 text-center">
            <p className="text-body font-semibold tracking-tight">
              {integrations.hubLabel}
            </p>
            <p className="text-muted-foreground mt-1 text-caption">
              Operational center
            </p>
          </div>
          <ul className="grid w-full gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {integrations.nodes.map((node) => (
              <li
                key={node.id}
                className="border-border/80 bg-background/60 text-foreground rounded-lg border px-3 py-2.5 text-center text-small font-medium backdrop-blur-sm"
              >
                {node.label}
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
}
