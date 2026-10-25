import {
  Card,
  CardContent,
  Caption,
  Stack,
  Text,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * StatisticCard — metric value with label and optional trend.
 */
export type StatisticTrend = 'up' | 'down' | 'neutral';

export type StatisticCardProps = {
  value: string;
  label: string;
  description?: string;
  trend?: StatisticTrend;
  trendLabel?: string;
  className?: string;
};

const trendClass: Record<StatisticTrend, string> = {
  up: 'text-success',
  down: 'text-error',
  neutral: 'text-muted-foreground',
};

export function StatisticCard({
  value,
  label,
  description,
  trend,
  trendLabel,
  className,
}: StatisticCardProps) {
  return (
    <Card
      variant="flat"
      className={cn(
        'border-domain-accent/15 from-primary-muted/45 to-surface-muted bg-gradient-to-br',
        className,
      )}
    >
      <CardContent>
        <Stack gap={2}>
          <Text
            as="p"
            className="text-domain-accent text-h2 font-semibold tracking-tight"
            weight="semibold"
          >
            {value}
          </Text>
          <Text as="p" weight="medium" size="small">
            {label}
          </Text>
          {description ? <Caption>{description}</Caption> : null}
          {trend && trendLabel ? (
            <Text
              as="p"
              size="caption"
              className={trendClass[trend]}
              aria-label={`Trend ${trend}: ${trendLabel}`}
            >
              {trend === 'up' ? '↑' : trend === 'down' ? '↓' : '→'} {trendLabel}
            </Text>
          ) : null}
        </Stack>
      </CardContent>
    </Card>
  );
}
