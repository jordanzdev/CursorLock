using System.Runtime.InteropServices;

namespace CursorLock.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct INITCOMMONCONTROLSEX
{
    public uint dwSize;
    public uint dwICC;
}

// commctrl.h declares the TaskDialog structures inside pshpack1: Pack = 1 on x64 too.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TASKDIALOG_BUTTON
{
    public int nButtonID;
    public char* pszButtonText;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TASKDIALOGCONFIG
{
    public uint cbSize;
    public nint hwndParent;
    public nint hInstance;
    public uint dwFlags;
    public uint dwCommonButtons;
    public char* pszWindowTitle;
    public nint hMainIcon;
    public char* pszMainInstruction;
    public char* pszContent;
    public uint cButtons;
    public TASKDIALOG_BUTTON* pButtons;
    public int nDefaultButton;
    public uint cRadioButtons;
    public TASKDIALOG_BUTTON* pRadioButtons;
    public int nDefaultRadioButton;
    public char* pszVerificationText;
    public char* pszExpandedInformation;
    public char* pszExpandedControlText;
    public char* pszCollapsedControlText;
    public nint hFooterIcon;
    public char* pszFooter;
    public nint pfCallback;
    public nint lpCallbackData;
    public uint cxWidth;
}

internal static unsafe partial class Comctl32
{
    const string Lib = "comctl32.dll";

    public const uint ICC_HOTKEY_CLASS = 0x00000040;
    public const uint ICC_STANDARD_CLASSES = 0x00004000;

    public const string HOTKEY_CLASSW = "msctls_hotkey32";

    public const uint HKM_SETHOTKEY = User32.WM_USER + 1;
    public const uint HKM_GETHOTKEY = User32.WM_USER + 2;
    public const uint HKM_SETRULES = User32.WM_USER + 3;

    // Hotkey control modifiers. They do NOT match the MOD_* flags of RegisterHotKey.
    public const uint HOTKEYF_SHIFT = 0x01;
    public const uint HOTKEYF_CONTROL = 0x02;
    public const uint HOTKEYF_ALT = 0x04;
    public const uint HOTKEYF_EXT = 0x08;

    // Combinations that HKM_SETRULES can declare invalid.
    public const uint HKCOMB_NONE = 0x0001;
    public const uint HKCOMB_S = 0x0002;

    public const uint TDF_ALLOW_DIALOG_CANCELLATION = 0x0008;
    public const uint TDF_USE_COMMAND_LINKS = 0x0010;
    public const uint TDF_POSITION_RELATIVE_TO_WINDOW = 0x1000;
    public const uint TDCBF_CANCEL_BUTTON = 0x0008;

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool InitCommonControlsEx(INITCOMMONCONTROLSEX* controls);

    [LibraryImport(Lib)]
    public static partial int TaskDialogIndirect(TASKDIALOGCONFIG* config, int* button, int* radioButton, int* verificationChecked);
}
