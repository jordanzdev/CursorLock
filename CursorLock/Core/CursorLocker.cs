using CursorLock.Native;
using static CursorLock.Native.User32;

namespace CursorLock.Core;

internal enum LockChange
{
    Locked,
    Unlocked,
    /// <summary>Unlocked without user feedback (exit, session end, error recovery).</summary>
    Released,
}

internal delegate void LockStateChangedHandler(LockChange change, nint monitor, in RECT monitorBounds);

/// <summary>
/// Cursor lock state. Windows enforces the lock through ClipCursor, so no mouse event ever goes
/// through this process. A single timer runs only while locked, to restore the clip if it is lost.
/// </summary>
internal static unsafe class CursorLocker
{
    public const nuint ReapplyTimerId = 1;

    static nint s_timerWindow;
    static bool s_isLocked;
    static nint s_monitor;
    static MONITORINFOEXW s_monitorInfo;

    /// <summary>Raised after every state change, once the clip is already applied or removed.</summary>
    public static event LockStateChangedHandler? StateChanged;

    public static bool IsLocked => s_isLocked;

    public static void Initialize(nint timerWindow) => s_timerWindow = timerWindow;

    public static void Toggle()
    {
        if (s_isLocked)
            Unlock();
        else
            Lock();
    }

    public static void Lock()
    {
        if (s_isLocked)
            return;

        MONITORINFOEXW info;
        if (!Monitors.TryGetTarget(in Config.Current, &info, out nint monitor))
            return;

        RECT bounds = info.rcMonitor;
        ClipCursor(&bounds); // the clip comes first; everything else afterwards
        SetTimer(s_timerWindow, ReapplyTimerId, Config.Current.ReapplyIntervalMs, 0);
        s_monitorInfo = info;
        s_monitor = monitor;
        s_isLocked = true;

        RaiseStateChanged(LockChange.Locked);
    }

    public static void Unlock()
    {
        if (!s_isLocked)
            return;

        ReleaseClip(); // the release comes first
        KillTimer(s_timerWindow, ReapplyTimerId);
        s_isLocked = false;

        RaiseStateChanged(LockChange.Unlocked);
    }

    /// <summary>Unlocks without sound or OSD. Never throws.</summary>
    public static void Release()
    {
        ReleaseClip();
        KillTimer(s_timerWindow, ReapplyTimerId);
        if (!s_isLocked)
            return;

        s_isLocked = false;
        try
        {
            RaiseStateChanged(LockChange.Released);
        }
        catch
        {
            // The cursor is already free; feedback is secondary.
        }
    }

    /// <summary>Removes any clip without touching the state. Safe from any context, including crash handlers.</summary>
    public static void ReleaseClip() => ClipCursor(null);

    /// <summary>
    /// Timer tick, allocation-free. Another program (or the game losing focus) may have removed
    /// or changed the clip. A stricter clip that stays inside the monitor is kept, so we never
    /// fight the game; anything else is replaced.
    /// </summary>
    public static void OnReapplyTimer()
    {
        if (!s_isLocked)
            return;

        RECT target = s_monitorInfo.rcMonitor;
        RECT current;
        if (GetClipCursor(&current) && target.Contains(current))
            return;

        ClipCursor(&target);
    }

    /// <summary>Resolution or monitor layout changed: recompute the bounds of the locked monitor.</summary>
    public static void OnDisplayChanged()
    {
        if (!s_isLocked)
            return;

        MONITORINFOEXW previous = s_monitorInfo;
        MONITORINFOEXW current;
        nint monitor;
        bool found = Monitors.TryFindByDevice(Monitors.DeviceName(&previous), &current, out monitor) ||
                     Monitors.TryGetUnderCursor(&current, out monitor);
        if (!found)
        {
            Release();
            return;
        }

        RECT bounds = current.rcMonitor;
        ClipCursor(&bounds);
        s_monitorInfo = current;
        s_monitor = monitor;
    }

    /// <summary>Applies a new reapply interval while locked (SetTimer with the same id replaces it).</summary>
    public static void OnReapplyIntervalChanged()
    {
        if (s_isLocked)
            SetTimer(s_timerWindow, ReapplyTimerId, Config.Current.ReapplyIntervalMs, 0);
    }

    static void RaiseStateChanged(LockChange change) =>
        StateChanged?.Invoke(change, s_monitor, in s_monitorInfo.rcMonitor);
}
