using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using ComplianceServiceManager.Models.LocalResources;

namespace ComplianceServiceManager.Services.LocalResources;

/// <summary>
/// Windows local-host resource collector using kernel32 memory status, GetSystemTimes CPU deltas,
/// DriveInfo for disk, and Process APIs for per-PID samples.
/// </summary>
/// <remarks>
/// Host CPU: percent of total machine capacity over the interval between consecutive CollectHost calls
/// (100 − idle%). First call returns CpuPercent = null (warm-up).
/// Process CPU: (ΔTotalProcessorTime / (Δwall × ProcessorCount)) × 100 — normalized to machine capacity.
/// Memory: MEMORYSTATUSEX ullTotalPhys / ullAvailPhys; Used = Total − Available as reported by Windows.
/// </remarks>
public sealed class WindowsLocalResourceCollector : ILocalResourceCollector, IDisposable
{
    private readonly object gate = new();
    private FILETIME previousIdle;
    private FILETIME previousKernel;
    private FILETIME previousUser;
    private bool hasPreviousSystemTimes;
    private readonly Dictionary<int, ProcessCpuSample> processSamples = new();

    public LocalHostResourceMetrics CollectHost(string diskPath, LocalResourceThresholds thresholds)
    {
        thresholds ??= LocalResourceThresholds.Default;
        var notes = new List<string>();

        double? cpuPercent = null;
        try
        {
            cpuPercent = SampleHostCpuPercent();
            if (cpuPercent is null)
            {
                notes.Add("Host CPU warm-up sample — percentage available on next poll.");
            }
        }
        catch (Exception ex)
        {
            notes.Add($"Host CPU unavailable: {ex.GetType().Name}");
        }

        long total = 0;
        long available = 0;
        long used = 0;
        double? memoryPercent = null;
        try
        {
            var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (!GlobalMemoryStatusEx(ref status))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            total = checked((long)status.ullTotalPhys);
            available = checked((long)status.ullAvailPhys);
            used = Math.Max(0, total - available);
            memoryPercent = LocalResourceStatusClassifier.ComputePercent(used, total);
            // dwMemoryLoad is an alternate Windows-reported load percent; we prefer computed used/total.
        }
        catch (Exception ex)
        {
            notes.Add($"Host memory unavailable: {ex.GetType().Name}");
        }

        var diskRoot = ResolveDiskRoot(diskPath);
        long diskTotal = 0;
        long diskFree = 0;
        long diskUsed = 0;
        double? diskPercent = null;
        try
        {
            var drive = new DriveInfo(diskRoot);
            if (!drive.IsReady)
            {
                notes.Add($"Disk '{diskRoot}' is not ready.");
            }
            else
            {
                diskTotal = drive.TotalSize;
                diskFree = drive.TotalFreeSpace;
                diskUsed = Math.Max(0, diskTotal - diskFree);
                diskPercent = LocalResourceStatusClassifier.ComputePercent(diskUsed, diskTotal);
            }
        }
        catch (Exception ex)
        {
            notes.Add($"Disk metrics unavailable: {ex.GetType().Name}");
        }

        return new LocalHostResourceMetrics
        {
            CpuPercent = cpuPercent,
            MemoryTotalBytes = total,
            MemoryUsedBytes = used,
            MemoryAvailableBytes = available,
            MemoryPercent = memoryPercent,
            DiskRootPath = diskRoot,
            DiskTotalBytes = diskTotal,
            DiskUsedBytes = diskUsed,
            DiskFreeBytes = diskFree,
            DiskPercent = diskPercent,
            CpuStatus = LocalResourceStatusClassifier.Classify(
                cpuPercent,
                thresholds.CpuWarningPercent,
                thresholds.CpuCriticalPercent),
            MemoryStatus = LocalResourceStatusClassifier.Classify(
                memoryPercent,
                thresholds.MemoryWarningPercent,
                thresholds.MemoryCriticalPercent),
            DiskStatus = LocalResourceStatusClassifier.Classify(
                diskPercent,
                thresholds.DiskWarningPercent,
                thresholds.DiskCriticalPercent),
            Notes = notes.Count == 0 ? null : string.Join(" ", notes)
        };
    }

    public LocalProcessResourceMetrics CollectProcess(int processId)
    {
        if (processId <= 0)
        {
            return new LocalProcessResourceMetrics
            {
                ProcessId = processId,
                IsAccessible = false,
                Error = "Invalid process id."
            };
        }

        Process? process = null;
        try
        {
            process = Process.GetProcessById(processId);
            process.Refresh();

            var name = Safe(() => process.ProcessName, "unknown");
            DateTimeOffset? start = null;
            try
            {
                start = process.StartTime.ToUniversalTime();
            }
            catch
            {
                // Protected / system processes may deny StartTime.
            }

            long? workingSet = null;
            try
            {
                workingSet = process.WorkingSet64;
            }
            catch (Exception ex)
            {
                return new LocalProcessResourceMetrics
                {
                    ProcessId = processId,
                    ProcessName = name,
                    StartTime = start,
                    IsAccessible = false,
                    Error = $"Working set unavailable: {ex.GetType().Name}"
                };
            }

            double? cpu = SampleProcessCpuPercent(processId, process);

            return new LocalProcessResourceMetrics
            {
                ProcessId = processId,
                ProcessName = name,
                CpuPercent = cpu,
                MemoryWorkingSetBytes = workingSet,
                StartTime = start,
                IsAccessible = true
            };
        }
        catch (ArgumentException)
        {
            ForgetProcess(processId);
            return new LocalProcessResourceMetrics
            {
                ProcessId = processId,
                IsAccessible = false,
                Error = "Process exited."
            };
        }
        catch (Win32Exception ex)
        {
            ForgetProcess(processId);
            return new LocalProcessResourceMetrics
            {
                ProcessId = processId,
                IsAccessible = false,
                Error = $"Access denied ({ex.NativeErrorCode})."
            };
        }
        catch (InvalidOperationException)
        {
            ForgetProcess(processId);
            return new LocalProcessResourceMetrics
            {
                ProcessId = processId,
                IsAccessible = false,
                Error = "Process exited."
            };
        }
        catch (Exception ex)
        {
            ForgetProcess(processId);
            return new LocalProcessResourceMetrics
            {
                ProcessId = processId,
                IsAccessible = false,
                Error = ex.GetType().Name
            };
        }
        finally
        {
            process?.Dispose();
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            processSamples.Clear();
            hasPreviousSystemTimes = false;
        }
    }

    private double? SampleHostCpuPercent()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        lock (gate)
        {
            if (!hasPreviousSystemTimes)
            {
                previousIdle = idle;
                previousKernel = kernel;
                previousUser = user;
                hasPreviousSystemTimes = true;
                return null;
            }

            var idleDelta = Subtract(idle, previousIdle);
            var kernelDelta = Subtract(kernel, previousKernel);
            var userDelta = Subtract(user, previousUser);
            previousIdle = idle;
            previousKernel = kernel;
            previousUser = user;

            // Kernel includes idle time on Windows.
            var total = kernelDelta + userDelta;
            if (total <= 0)
            {
                return null;
            }

            var busy = total - idleDelta;
            if (busy < 0)
            {
                busy = 0;
            }

            return Math.Clamp(busy * 100.0 / total, 0, 100);
        }
    }

    private double? SampleProcessCpuPercent(int processId, Process process)
    {
        TimeSpan totalProcessorTime;
        try
        {
            totalProcessorTime = process.TotalProcessorTime;
        }
        catch
        {
            return null;
        }

        var now = Stopwatch.GetTimestamp();
        lock (gate)
        {
            if (!processSamples.TryGetValue(processId, out var previous))
            {
                processSamples[processId] = new ProcessCpuSample(totalProcessorTime, now);
                return null;
            }

            var cpuDelta = (totalProcessorTime - previous.TotalProcessorTime).TotalMilliseconds;
            var wallDeltaMs = (now - previous.Timestamp) * 1000.0 / Stopwatch.Frequency;
            processSamples[processId] = new ProcessCpuSample(totalProcessorTime, now);

            if (wallDeltaMs <= 0 || Environment.ProcessorCount <= 0)
            {
                return null;
            }

            var percent = cpuDelta / (wallDeltaMs * Environment.ProcessorCount) * 100.0;
            return Math.Clamp(percent, 0, 100);
        }
    }

    private void ForgetProcess(int processId)
    {
        lock (gate)
        {
            processSamples.Remove(processId);
        }
    }

    private static string ResolveDiskRoot(string diskPath)
    {
        try
        {
            var full = Path.GetFullPath(string.IsNullOrWhiteSpace(diskPath) ? AppContext.BaseDirectory : diskPath);
            var root = Path.GetPathRoot(full);
            return string.IsNullOrWhiteSpace(root) ? full : root;
        }
        catch
        {
            return diskPath;
        }
    }

    private static ulong Subtract(FILETIME current, FILETIME previous)
    {
        var currentValue = ((ulong)current.dwHighDateTime << 32) | current.dwLowDateTime;
        var previousValue = ((ulong)previous.dwHighDateTime << 32) | previous.dwLowDateTime;
        return currentValue >= previousValue ? currentValue - previousValue : 0;
    }

    private static T Safe<T>(Func<T> read, T fallback)
    {
        try
        {
            return read();
        }
        catch
        {
            return fallback;
        }
    }

    private readonly record struct ProcessCpuSample(TimeSpan TotalProcessorTime, long Timestamp);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
}
