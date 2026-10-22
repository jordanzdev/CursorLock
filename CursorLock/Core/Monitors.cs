using System.Runtime.InteropServices;
using CursorLock.Native;
using static CursorLock.Native.User32;

namespace CursorLock.Core;

internal readonly record struct MonitorEntry(string Device, int Width, int Height);

/// <summary>Monitor lookup. The lock path is allocation-free; only <see cref="ListAll"/> allocates.</summary>
internal static unsafe class Monitors
{
    const int MaxMonitors = 16;
    const string DevicePrefix = @"\\.\";

    struct MonitorHandles
    {
        public fixed long Items[MaxMonitors];
        public int Count;
    }

    /// <summary>Monitor for the configured mode; falls back to the one under the cursor.</summary>
    public static bool TryGetTarget(in Config config, MONITORINFOEXW* info, out nint monitor)
    {
        bool found = config.TargetMode switch
        {
            TargetMode.Primary => TryGetPrimary(info, out monitor),
            TargetMode.Specific => TryFindByDevice(config.TargetDevice, info, out monitor),
            _ => TryGetUnderCursor(info, out monitor),
        };
        return found || TryGetUnderCursor(info, out monitor);
    }

    public static bool TryGetUnderCursor(MONITORINFOEXW* info, out nint monitor)
    {
        POINT cursor = default;
        GetCursorPos(&cursor);
        monitor = MonitorFromPoint(cursor, MONITOR_DEFAULTTONEAREST);
        return TryGetInfo(monitor, info);
    }

    public static bool TryGetPrimary(MONITORINFOEXW* info, out nint monitor)
    {
        MonitorHandles handles = default;
        Enumerate(&handles);
        for (int i = 0; i < handles.Count; i++)
        {
            monitor = (nint)handles.Items[i];
            if (TryGetInfo(monitor, info) && (info->dwFlags & MONITORINFOF_PRIMARY) != 0)
                return true;
        }

        monitor = MonitorFromPoint(default, MONITOR_DEFAULTTOPRIMARY);
        return TryGetInfo(monitor, info);
    }

    public static bool TryFindByDevice(ReadOnlySpan<char> device, MONITORINFOEXW* info, out nint monitor)
    {
        if (!device.IsEmpty)
        {
            MonitorHandles handles = default;
            Enumerate(&handles);
            for (int i = 0; i < handles.Count; i++)
            {
                monitor = (nint)handles.Items[i];
                if (TryGetInfo(monitor, info) && DeviceName(info).Equals(device, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        monitor = 0;
        return false;
    }

    public static List<MonitorEntry> ListAll()
    {
        MonitorHandles handles = default;
        Enumerate(&handles);

        var monitors = new List<MonitorEntry>(handles.Count);
        MONITORINFOEXW info;
        for (int i = 0; i < handles.Count; i++)
        {
            if (TryGetInfo((nint)handles.Items[i], &info))
                monitors.Add(new MonitorEntry(new string(info.szDevice), info.rcMonitor.Width, info.rcMonitor.Height));
        }
        return monitors;
    }

    public static ReadOnlySpan<char> DeviceName(MONITORINFOEXW* info) =>
        MemoryMarshal.CreateReadOnlySpanFromNullTerminated(info->szDevice);

    /// <summary>\\.\DISPLAY2 → DISPLAY2</summary>
    public static string ShortName(string device) =>
        device.StartsWith(DevicePrefix, StringComparison.Ordinal) ? device[DevicePrefix.Length..] : device;

    static bool TryGetInfo(nint monitor, MONITORINFOEXW* info)
    {
        if (monitor == 0)
            return false;
        *info = default;
        info->cbSize = (uint)sizeof(MONITORINFOEXW);
        return GetMonitorInfoW(monitor, info);
    }

    static void Enumerate(MonitorHandles* handles)
    {
        delegate* unmanaged<nint, nint, RECT*, nint, int> callback = &CollectMonitor;
        EnumDisplayMonitors(0, null, (nint)callback, (nint)handles);
    }

    // No try/catch needed: it cannot throw (bounded write into a fixed buffer).
    [UnmanagedCallersOnly]
    static int CollectMonitor(nint monitor, nint hdc, RECT* bounds, nint state)
    {
        var handles = (MonitorHandles*)state;
        if (handles->Count >= MaxMonitors)
            return 0;
        handles->Items[handles->Count++] = monitor;
        return 1;
    }
}
