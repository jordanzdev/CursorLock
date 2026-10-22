using System.Runtime.InteropServices;
using static CursorLock.Native.User32;

namespace CursorLock.Core;

/// <summary>
/// The single global hotkey (RegisterHotKey, delivered as WM_HOTKEY). It can be suspended while
/// the settings window is active, so the shortcut can be typed into the hotkey control.
/// </summary>
internal static class GlobalHotkey
{
    public const int Id = 1;

    static nint s_window;
    static bool s_isRegistered;
    static bool s_isSuspended;
    static bool s_isShutDown;

    public static void Initialize(nint window) => s_window = window;

    /// <summary>Registers <see cref="Config.Current"/>'s hotkey. Returns false if another app owns it.</summary>
    public static bool Register()
    {
        if (!s_isRegistered && !s_isShutDown)
            s_isRegistered = TryRegister(Config.Current.Hotkey);
        return s_isRegistered;
    }

    public static void Suspend()
    {
        s_isSuspended = true;
        Unregister();
    }

    public static void Resume()
    {
        if (!s_isSuspended)
            return;
        s_isSuspended = false;
        Register();
    }

    /// <summary>
    /// Registers <paramref name="hotkey"/> in place of the current one. On failure the previous
    /// registration is restored and <paramref name="error"/> holds the Win32 error code.
    /// </summary>
    public static bool TryReplace(Hotkey hotkey, out int error)
    {
        error = 0;
        bool wasRegistered = s_isRegistered;
        Unregister();

        if (TryRegister(hotkey))
        {
            s_isRegistered = true;
            return true;
        }

        error = Marshal.GetLastPInvokeError();
        if (wasRegistered)
            Register();
        return false;
    }

    public static void Shutdown()
    {
        s_isShutDown = true;
        Unregister();
    }

    static bool TryRegister(Hotkey hotkey) =>
        RegisterHotKey(s_window, Id, hotkey.Modifiers | MOD_NOREPEAT, hotkey.VirtualKey);

    static void Unregister()
    {
        if (!s_isRegistered)
            return;
        UnregisterHotKey(s_window, Id);
        s_isRegistered = false;
    }
}
