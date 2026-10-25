using ComplianceServiceManager.Models.LocalResources;

namespace ComplianceServiceManager.Services.LocalResources;

internal static class LocalResourceFormatting
{
    public static string FormatBytes(long bytes)
    {
        if (bytes < 0)
        {
            return "—";
        }

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes} {units[unit]}" : $"{value:0.##} {units[unit]}";
    }

    public static string FormatPercent(double? percent)
        => percent is null ? "—" : $"{percent.Value:0.#}%";

    public static string FormatHostMemory(LocalHostResourceMetrics host)
    {
        if (host.MemoryTotalBytes <= 0)
        {
            return "—";
        }

        return $"{FormatBytes(host.MemoryUsedBytes)} / {FormatBytes(host.MemoryTotalBytes)} ({FormatPercent(host.MemoryPercent)})";
    }

    public static string FormatHostDisk(LocalHostResourceMetrics host)
    {
        if (host.DiskTotalBytes <= 0)
        {
            return "—";
        }

        return $"{FormatBytes(host.DiskFreeBytes)} free of {FormatBytes(host.DiskTotalBytes)} ({FormatPercent(host.DiskPercent)} used)";
    }
}
