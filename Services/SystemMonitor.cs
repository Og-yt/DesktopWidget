using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace DesktopWidget.Services;

public sealed class SystemMonitor : IDisposable
{
    private readonly PerformanceCounter cpuCounter;
    private readonly PerformanceCounter memoryCounter;
    private bool disposed;

    public SystemMonitor()
    {
        cpuCounter = new PerformanceCounter(
            "Processor", "% Processor Time", "_Total");
        memoryCounter = new PerformanceCounter(
            "Memory", "Available MBytes");

        _ = cpuCounter.NextValue();
        _ = memoryCounter.NextValue();
    }

    public float GetCpuUsage()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return Math.Clamp(cpuCounter.NextValue(), 0f, 100f);
    }

    public float GetAvailableMemoryMb()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return memoryCounter.NextValue();
    }

    public string GetTopProcesses(int count = 5)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var processes = Process.GetProcesses();
        try
        {
            var processInfo = new List<(string Name, long Memory)>();
            foreach (var process in processes)
            {
                try
                {
                    processInfo.Add((process.ProcessName, process.WorkingSet64));
                }
                catch (InvalidOperationException)
                {
                }
                catch (Win32Exception)
                {
                }
                catch (NotSupportedException)
                {
                }
            }

            return string.Join(
                Environment.NewLine,
                processInfo
                    .OrderByDescending(process => process.Memory)
                    .Take(Math.Max(0, count))
                    .Select(process =>
                        $"{process.Name,-20} " +
                        $"{process.Memory / 1024.0 / 1024.0,7:F1} MB"));
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        cpuCounter.Dispose();
        memoryCounter.Dispose();
        disposed = true;
    }
}
