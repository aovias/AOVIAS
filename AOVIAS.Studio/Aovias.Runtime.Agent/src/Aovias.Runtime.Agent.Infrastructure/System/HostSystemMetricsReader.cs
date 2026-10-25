using Aovias.Runtime.Agent.Application.Interfaces;

namespace Aovias.Runtime.Agent.Infrastructure.System;

/// <summary>
/// Collects live host metrics from the operating system.
/// </summary>
internal static class HostSystemMetricsReader
{
    public static SystemMetricsSnapshot Read()
    {
        var (cpu, memory) = ReadProcessorMetrics();
        var disk = ReadDiskUsagePercent();

        return new SystemMetricsSnapshot
        {
            CpuUsagePercent = cpu,
            MemoryUsagePercent = memory,
            DiskUsagePercent = disk,
            OperatingSystem = global::System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            Kernel = Environment.OSVersion.VersionString,
            MachineName = Environment.MachineName
        };
    }

    private static double ReadDiskUsagePercent()
    {
        var drives = DriveInfo.GetDrives().Where(drive => drive.IsReady && drive.TotalSize > 0).ToList();
        if (drives.Count == 0)
        {
            return 0;
        }

        var total = drives.Sum(drive => drive.TotalSize);
        var used = drives.Sum(drive => drive.TotalSize - drive.AvailableFreeSpace);
        return Math.Round(used / (double)total * 100d, 1);
    }

    private static (double Cpu, double Memory) ReadProcessorMetrics()
    {
        if (OperatingSystem.IsWindows())
        {
            return ReadWindowsMetrics();
        }

        if (OperatingSystem.IsLinux())
        {
            return ReadLinuxMetrics();
        }

        return (0, 0);
    }

    private static (double Cpu, double Memory) ReadWindowsMetrics()
    {
        double memoryPercent = 0;
        if (NativeMemoryStatus.TryRead(out var totalPhysical, out var availablePhysical))
        {
            memoryPercent = Math.Round((totalPhysical - availablePhysical) / (double)totalPhysical * 100d, 1);
        }

        return (0, memoryPercent);
    }

    private static (double Cpu, double Memory) ReadLinuxMetrics()
    {
        double memoryPercent = 0;
        if (File.Exists("/proc/meminfo"))
        {
            var lines = File.ReadAllLines("/proc/meminfo");
            var total = ParseMemInfo(lines, "MemTotal:");
            var available = ParseMemInfo(lines, "MemAvailable:");
            if (total > 0)
            {
                memoryPercent = Math.Round((total - available) / total * 100d, 1);
            }
        }

        return (0, memoryPercent);
    }

    private static double ParseMemInfo(IEnumerable<string> lines, string key)
    {
        var line = lines.FirstOrDefault(entry => entry.StartsWith(key, StringComparison.Ordinal));
        if (line is null)
        {
            return 0;
        }

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 && double.TryParse(parts[1], out var value) ? value : 0;
    }

    private static class NativeMemoryStatus
    {
        [global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = global::System.Runtime.InteropServices.CharSet.Auto)]
        private class MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhysical;
            public ulong AvailablePhysical;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtual;
            public ulong AvailableVirtual;
            public ulong AvailableExtendedVirtual;

            public MemoryStatusEx()
            {
                Length = (uint)global::System.Runtime.InteropServices.Marshal.SizeOf<MemoryStatusEx>();
            }
        }

        [global::System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = global::System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        [return: global::System.Runtime.InteropServices.MarshalAs(global::System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(MemoryStatusEx buffer);

        public static bool TryRead(out ulong totalPhysical, out ulong availablePhysical)
        {
            var status = new MemoryStatusEx();
            if (!GlobalMemoryStatusEx(status))
            {
                totalPhysical = 0;
                availablePhysical = 0;
                return false;
            }

            totalPhysical = status.TotalPhysical;
            availablePhysical = status.AvailablePhysical;
            return true;
        }
    }
}
