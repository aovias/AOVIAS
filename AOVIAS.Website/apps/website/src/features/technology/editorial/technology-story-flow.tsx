import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from '@/components/brand/brand-primitives';
import { cn } from '@/lib/utils';

export type TechnologyStoryFlowProps = {
  title: string;
  steps: readonly string[];
  caption?: string;
  className?: string;
};

/**
 * Vertical system story — one concept, one flow.
 * Used for Technology Editorial “Technical Story” sections.
 */
export function TechnologyStoryFlow({
  title,
  steps,
  caption,
  className,
}: TechnologyStoryFlowProps) {
  const count = steps.length;
  const panelH = 36;
  const gap = 18;
  const top = 20;
  const height = top * 2 + count * panelH + (count - 1) * gap;
  const width = 420;
  const panelW = 200;
  const x = (width - panelW) / 2;

  return (
    <BrandShell
      title={title}
      {...(caption ? { caption } : {})}
      className={cn(className)}
      aspectClassName="min-h-[14rem]"
    >
      <svg
        viewBox={`0 0 ${width} ${height}`}
        className="text-domain-accent em-diagram-reveal size-full flex-1"
        role="img"
        aria-label={title}
      >
        <title>{title}</title>
        <BrandGrid width={width} height={height} step={24} opacity={0.04} />
        {steps.map((step, index) => {
          const y = top + index * (panelH + gap);
          const midY = y + panelH;
          return (
            <g key={step} className="em-diagram-step">
              <BrandPanel
                x={x}
                y={y}
                width={panelW}
                height={panelH}
                opacity={
                  index === 0 || index === count - 1
                    ? brandInk.fillStrong
                    : brandInk.fillMid
                }
                radius={brandInk.radius}
                stroked
              />
              <BrandLabel x={width / 2} y={y + panelH / 2 + 4} size={11}>
                {step}
              </BrandLabel>
              {index < count - 1 ? (
                <BrandConnector
                  d={`M${width / 2} ${midY} V${midY + gap}`}
                  opacity={0.35}
                />
              ) : null}
            </g>
          );
        })}
      </svg>
    </BrandShell>
  );
}
