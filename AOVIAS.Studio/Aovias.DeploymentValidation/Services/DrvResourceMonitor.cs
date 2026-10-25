namespace Aovias.DeploymentValidation.Services;

/// <summary>
/// Tracks temporary disk usage during publishing and DRV.
/// </summary>
public class DrvResourceMonitor
{
    private readonly string probePath;
    private long baselineBytes;
    private long peakBytes;
    private long freedBytes;

    public DrvResourceMonitor(string probePath)
    {
        this.probePath = probePath;
        baselineBytes = GetAvailableBytes(probePath);
        peakBytes = baselineBytes;
    }

    public long BaselineAvailableBytes => baselineBytes;

    public long CurrentAvailableBytes => GetAvailableBytes(probePath);

    public long PeakConsumedBytes => Math.Max(0, baselineBytes - peakBytes);

    public long FreedBytes => freedBytes;

    public long RemainingBytes => CurrentAvailableBytes;

    public virtual void Sample()
    {
        var available = GetAvailableBytes(probePath);
        if (available < peakBytes)
        {
            peakBytes = available;
        }
    }

    public virtual void RecordFreed(long bytes) => freedBytes += Math.Max(0, bytes);

    public static DrvDiskBudgetEstimate EstimateDiskBudget(int serviceCount, Models.DeploymentValidationMode mode)
    {
        const long sharedWorkspaceBytes = 600L * 1024 * 1024;
        var perServicePeak = mode switch
        {
            Models.DeploymentValidationMode.Fast => 300L * 1024 * 1024,
            Models.DeploymentValidationMode.Balanced => 900L * 1024 * 1024,
            _ => 2L * 1024 * 1024 * 1024
        };

        const long publishWorkingSetBytes = 1L * 1024 * 1024 * 1024;
        const long safetyMarginBytes = 1L * 1024 * 1024 * 1024;

        return new DrvDiskBudgetEstimate
        {
            SharedWorkspaceBytes = sharedWorkspaceBytes,
            PerServicePeakBytes = perServicePeak,
            PublishWorkingSetBytes = publishWorkingSetBytes,
            SafeMarginBytes = safetyMarginBytes,
            ServiceCount = serviceCount
        };
    }

    public static long EstimateSequentialPeakBytes(int serviceCount, Models.DeploymentValidationMode mode)
        => EstimateDiskBudget(serviceCount, mode).MaximumPeakBytes;

    public static long GetDirectorySizeBytes(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return 0;
        }

        try
        {
            if (File.Exists(path))
            {
                return new FileInfo(path).Length;
            }

            if (!Directory.Exists(path))
            {
                return 0;
            }

            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Sum(file =>
                {
                    try
                    {
                        return new FileInfo(file).Length;
                    }
                    catch
                    {
                        return 0L;
                    }
                });
        }
        catch
        {
            return 0;
        }
    }

    public DrvResourceSnapshot CreateSnapshot()
    {
        Sample();
        return new DrvResourceSnapshot
        {
            CurrentAvailableBytes = CurrentAvailableBytes,
            PeakConsumedBytes = PeakConsumedBytes,
            FreedBytes = FreedBytes,
            RemainingBytes = RemainingBytes
        };
    }

    private static long GetAvailableBytes(string path)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(path)) ?? path;
            return new DriveInfo(root).AvailableFreeSpace;
        }
        catch
        {
            return 0;
        }
    }
}

/// <summary>
/// Point-in-time DRV disk usage snapshot for dashboards.
/// </summary>
public sealed class DrvResourceSnapshot
{
    public long CurrentAvailableBytes { get; init; }

    public long PeakConsumedBytes { get; init; }

    public long FreedBytes { get; init; }

    public long RemainingBytes { get; init; }
}

/// <summary>
/// Disk budget estimate used by preflight before publishing begins.
/// </summary>
public sealed class DrvDiskBudgetEstimate
{
    public int ServiceCount { get; init; }

    public long SharedWorkspaceBytes { get; init; }

    public long PerServicePeakBytes { get; init; }

    public long PublishWorkingSetBytes { get; init; }

    public long SafeMarginBytes { get; init; }

    public long RequiredBytes => SharedWorkspaceBytes + PerServicePeakBytes + PublishWorkingSetBytes;

    public long MaximumPeakBytes => RequiredBytes + SafeMarginBytes;
}
