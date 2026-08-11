using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>Sprint 17 — SSOT for threshold band and trend direction calculations.</summary>
public static class ClinicalIndicatorTrendEngine
{
    public static ThresholdBand EvaluateThreshold(ClinicalIndicator indicator)
    {
        if (!indicator.CurrentValue.HasValue) return ThresholdBand.Unknown;

        var value = indicator.CurrentValue.Value;
        var lowerIsBetter = indicator.LowerIsBetter;

        if (indicator.CriticalThreshold.HasValue && IsBreached(value, indicator.CriticalThreshold.Value, lowerIsBetter, isCritical: true))
            return ThresholdBand.Critical;
        if (indicator.RedThreshold.HasValue && IsBreached(value, indicator.RedThreshold.Value, lowerIsBetter, isCritical: false))
            return ThresholdBand.Red;
        if (indicator.YellowThreshold.HasValue && IsBreached(value, indicator.YellowThreshold.Value, lowerIsBetter, isCritical: false))
            return ThresholdBand.Yellow;
        if (indicator.GreenThreshold.HasValue && IsWithinTarget(value, indicator.GreenThreshold.Value, indicator.TargetValue, lowerIsBetter))
            return ThresholdBand.Green;

        if (indicator.TargetValue.HasValue)
        {
            var delta = Math.Abs(value - indicator.TargetValue.Value);
            var tolerance = Math.Max(Math.Abs(indicator.TargetValue.Value) * 0.05m, 0.01m);
            return delta <= tolerance ? ThresholdBand.Green : ThresholdBand.Yellow;
        }

        return ThresholdBand.Unknown;
    }

    public static TrendDirection ComputeTrend(IReadOnlyList<decimal> values)
    {
        if (values.Count < 2) return TrendDirection.Unknown;
        if (values.Count >= 3)
        {
            var deltas = new List<decimal>();
            for (var i = 1; i < values.Count; i++)
                deltas.Add(values[i] - values[i - 1]);
            var avgDelta = deltas.Average();
            var variance = deltas.Select(d => (d - avgDelta) * (d - avgDelta)).Average();
            if (variance > Math.Abs(avgDelta) * 2 && variance > 0.01m)
                return TrendDirection.Volatile;
        }

        var first = values.First();
        var last = values.Last();
        var change = last - first;
        var threshold = Math.Max(Math.Abs(first) * 0.03m, 0.01m);
        if (Math.Abs(change) <= threshold) return TrendDirection.Stable;
        return change > 0 ? TrendDirection.Improving : TrendDirection.Declining;
    }

    public static TrendDirection ComputeTrendForIndicator(ClinicalIndicator indicator, IEnumerable<IndicatorMeasurement> measurements, bool lowerIsBetter)
    {
        var ordered = measurements.OrderBy(m => m.PeriodEnd).Select(m => m.Value).ToList();
        if (ordered.Count < 2) return indicator.Trend;

        var raw = ComputeTrend(ordered);
        if (!lowerIsBetter) return raw;

        return raw switch
        {
            TrendDirection.Improving => TrendDirection.Declining,
            TrendDirection.Declining => TrendDirection.Improving,
            _ => raw,
        };
    }

    public static decimal? MovingAverage(IReadOnlyList<decimal> values, int window = 3)
    {
        if (values.Count == 0) return null;
        var take = values.TakeLast(Math.Min(window, values.Count)).ToList();
        return Math.Round(take.Average(), 2);
    }

    private static bool IsBreached(decimal value, decimal threshold, bool lowerIsBetter, bool isCritical)
    {
        if (lowerIsBetter) return value >= threshold;
        return value <= threshold;
    }

    private static bool IsWithinTarget(decimal value, decimal greenThreshold, decimal? target, bool lowerIsBetter)
    {
        if (target.HasValue)
        {
            var tolerance = Math.Max(Math.Abs(target.Value) * 0.05m, 0.01m);
            return Math.Abs(value - target.Value) <= tolerance;
        }
        return lowerIsBetter ? value <= greenThreshold : value >= greenThreshold;
    }
}
