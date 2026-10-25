import { StatisticCard } from '@/components/marketing';
import type { TrustMetric } from '@/components/trust/models';
import { Grid, type GridCols } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * SuccessMetrics — metric band from TrustMetric models.
 */
export type SuccessMetricsProps = {
  metrics: readonly TrustMetric[];
  cols?: Extract<GridCols, 2 | 3 | 4>;
  className?: string;
};

export function SuccessMetrics({
  metrics,
  cols = 3,
  className,
}: SuccessMetricsProps) {
  return (
    <Grid cols={cols} gap={6} className={cn(className)}>
      {metrics.map((metric) => (
        <StatisticCard
          key={metric.id}
          value={metric.value}
          label={metric.label}
          {...(metric.description ? { description: metric.description } : {})}
          {...(metric.trend ? { trend: metric.trend } : {})}
          {...(metric.trendLabel ? { trendLabel: metric.trendLabel } : {})}
        />
      ))}
    </Grid>
  );
}
