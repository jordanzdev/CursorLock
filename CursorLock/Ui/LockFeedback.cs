using CursorLock.Core;
using CursorLock.Native;
using static CursorLock.Native.User32;

namespace CursorLock.Ui;

/// <summary>Tray icon, sound and OSD after each lock state change. Allocation-free.</summary>
internal static class LockFeedback
{
    public static void OnLockStateChanged(LockChange change, nint monitor, in RECT monitorBounds)
    {
        TrayIcon.Refresh();
        if (change == LockChange.Released)
            return;

        bool locked = change == LockChange.Locked;
        ref readonly Config config = ref Config.Current;
        if (config.SoundEnabled)
            MessageBeep(locked ? MB_OK : MB_ICONASTERISK); // asynchronous, unlike Beep()
        if (config.OsdEnabled)
            OsdWindow.Show(locked, monitor, monitorBounds);
    }
}
