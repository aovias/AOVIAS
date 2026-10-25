import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type TechnologyEvolutionProps = {
  title?: string;
  caption?: string;
  className?: string;
};

const stages = [
  'Foundation',
  'Platform Services',
  'Artificial Intelligence',
  'Enterprise Integrations',
  'Developer Platform',
  'Automation',
  'Ecosystem Expansion',
  'Continuous Innovation',
] as const;

/**
 * Technology evolution roadmap — capability themes, no dates.
 */
export function TechnologyEvolution({
  title = 'Technology evolution roadmap',
  caption = 'Capability progression — not a release calendar',
  className,
}: TechnologyEvolutionProps) {
  return (
    <BrandShell
      title={title}
      {...(caption ? { caption } : {})}
      className={cn('aspect-[3/4] md:aspect-[4/3] xl:min-h-[20rem]', className)}
      aspectClassName="min-h-[18rem]"
    >
      <svg
        viewBox="0 0 420 360"
        className="text-domain-accent size-full flex-1"
        role="img"
        aria-label={title}
      >
        <title>{title}</title>
        <BrandGrid width={420} height={360} step={30} opacity={0.05} />

        {stages.map((label, index) => {
          const y = 20 + index * 40;
          const opacity =
            index === 0
              ? brandInk.fillStrong
              : index < 4
                ? brandInk.fillMid
                : brandInk.fillSoft;
          return (
            <g key={label}>
              {index > 0 ? (
                <BrandConnector
                  d={`M210 ${y - 8} V${y}`}
                  opacity={0.28}
                />
              ) : null}
              <BrandPanel
                x={90}
                y={y}
                width={240}
                height={28}
                opacity={opacity}
                stroked
              />
              <BrandLabel x={210} y={y + 18} size={9}>
                {label}
              </BrandLabel>
            </g>
          );
        })}
      </svg>
    </BrandShell>
  );
}
