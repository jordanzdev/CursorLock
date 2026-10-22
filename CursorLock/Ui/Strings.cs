using CursorLock.Core;
using CursorLock.Native;

namespace CursorLock.Ui;

/// <summary>
/// User-facing text in Spanish and English. Fixed texts are literals, so switching
/// language never allocates on the lock/unlock path.
/// </summary>
internal static class Strings
{
    static bool s_isEnglish;

    public static void Apply(UiLanguage language) =>
        s_isEnglish = language switch
        {
            UiLanguage.Spanish => false,
            UiLanguage.English => true,
            _ => !IsWindowsInSpanish(),
        };

    static bool IsWindowsInSpanish() =>
        (Kernel32.GetUserDefaultUILanguage() & Kernel32.PRIMARY_LANGUAGE_MASK) == Kernel32.LANG_SPANISH;

    static string Pick(string spanish, string english) => s_isEnglish ? english : spanish;

    // Tray
    public static string TooltipFree(string hotkey) => Pick("CursorLock: Libre (", "CursorLock: Free (") + hotkey + ")";
    public static string TooltipLocked(string hotkey) => Pick("CursorLock: Bloqueado (", "CursorLock: Locked (") + hotkey + ")";
    public static string MenuLock => Pick("Bloquear", "Lock");
    public static string MenuUnlock => Pick("Desbloquear", "Unlock");
    public static string MenuSettings => Pick("Configuración…", "Settings…");
    public static string MenuExit => Pick("Salir", "Exit");

    public static string HotkeyUnavailableTitle => Pick("Atajo no disponible", "Shortcut unavailable");
    public static string HotkeyUnavailableText(string hotkey) => s_isEnglish
        ? $"Could not register {hotkey}: another application is probably using it. You can lock from the tray icon menu or change the shortcut in Settings."
        : $"No se pudo registrar {hotkey}: probablemente lo usa otra aplicación. Puedes bloquear desde el menú del icono o cambiar el atajo en Configuración.";

    // On-screen display
    public static string OsdLocked => Pick("Bloqueado", "Locked");
    public static string OsdFree => Pick("Libre", "Free");

    // Settings window
    public static string SettingsTitle => Pick("CursorLock — Configuración", "CursorLock — Settings");
    public static string LabelHotkey => Pick("Atajo de teclado:", "Keyboard shortcut:");
    public static string LabelMonitor => Pick("Monitor objetivo:", "Target monitor:");
    public static string LabelReapplyInterval => Pick("Reaplicar cada (ms):", "Reapply every (ms):");
    public static string LabelLanguage => Pick("Idioma:", "Language:");
    public static string CheckStartWithWindows => Pick("Iniciar con Windows", "Start with Windows");
    public static string CheckSound => Pick("Sonido", "Sound");
    public static string CheckOsd => Pick("Mostrar aviso en pantalla (OSD)", "Show on-screen notice (OSD)");
    public static string ButtonSave => Pick("Guardar", "Save");
    public static string ButtonCancel => Pick("Cancelar", "Cancel");
    public static string MonitorUnderCursor => Pick("Bajo el cursor", "Under the cursor");
    public static string MonitorPrimary => Pick("Principal", "Primary");
    public static string MonitorDisconnected => Pick("no conectado", "not connected");
    public static string LanguageAuto => Pick("Automático (sistema)", "Automatic (system)");
    public const string LanguageSpanish = "Español";
    public const string LanguageEnglish = "English";

    public static string ErrorInvalidHotkey => Pick(
        "Elige un atajo con al menos un modificador (Ctrl, Alt o Mayús) y una tecla.",
        "Choose a shortcut with at least one modifier (Ctrl, Alt or Shift) and a key.");

    public static string ErrorInvalidReapplyInterval => s_isEnglish
        ? $"The reapply interval must be between {Config.MinReapplyIntervalMs} and {Config.MaxReapplyIntervalMs} ms."
        : $"El intervalo de reaplicación debe estar entre {Config.MinReapplyIntervalMs} y {Config.MaxReapplyIntervalMs} ms.";

    public static string ErrorHotkeyUnavailable(string requested, string kept, int error)
    {
        bool isTaken = error == User32.ERROR_HOTKEY_ALREADY_REGISTERED;
        return s_isEnglish
            ? $"Could not register {requested}: {(isTaken ? "another application is already using it" : "error " + error)}.\n\nKeeping {kept}."
            : $"No se pudo registrar {requested}: {(isTaken ? "ya lo está usando otra aplicación" : "error " + error)}.\n\nSe mantiene {kept}.";
    }

    public static string ErrorNotPersisted => Pick(
        "La configuración se ha aplicado, pero no se pudo guardar en el registro.",
        "The settings were applied, but could not be saved to the registry.");

    // Closing the settings window with the X button
    public static string CloseInstruction => Pick("¿Qué quieres hacer?", "What do you want to do?");
    public static string CloseContent => Pick(
        "Los cambios sin guardar se descartarán.",
        "Unsaved changes will be discarded.");
    public static string CloseMinimizeToTray => Pick(
        "Minimizar a la bandeja\nCursorLock sigue en segundo plano y el atajo sigue funcionando.",
        "Minimize to tray\nCursorLock keeps running in the background and the shortcut keeps working.");
    public static string CloseExitApplication => Pick(
        "Cerrar CursorLock\nLibera el cursor y termina la aplicación.",
        "Close CursorLock\nReleases the cursor and exits the application.");
    public static string CloseFallbackQuestion => Pick(
        "¿Minimizar CursorLock a la bandeja?\n\nSí: sigue en segundo plano y el atajo sigue funcionando.\nNo: libera el cursor y cierra la aplicación.\n\nLos cambios sin guardar se descartarán.",
        "Minimize CursorLock to the tray?\n\nYes: it keeps running in the background and the shortcut keeps working.\nNo: releases the cursor and exits the application.\n\nUnsaved changes will be discarded.");

    // Keys
    public static string ModifierShift => Pick("Mayús", "Shift");

    public static string? KeyName(uint key) => key switch
    {
        VirtualKeys.Back => Pick("Retroceso", "Backspace"),
        VirtualKeys.Tab => "Tab",
        VirtualKeys.Return => Pick("Entrar", "Enter"),
        VirtualKeys.Pause => Pick("Pausa", "Pause"),
        VirtualKeys.CapsLock => Pick("Bloq Mayús", "Caps Lock"),
        VirtualKeys.Escape => "Esc",
        VirtualKeys.Space => Pick("Espacio", "Space"),
        VirtualKeys.PageUp => Pick("RePág", "Page Up"),
        VirtualKeys.PageDown => Pick("AvPág", "Page Down"),
        VirtualKeys.End => Pick("Fin", "End"),
        VirtualKeys.Home => Pick("Inicio", "Home"),
        VirtualKeys.Left => Pick("Izquierda", "Left"),
        VirtualKeys.Up => Pick("Arriba", "Up"),
        VirtualKeys.Right => Pick("Derecha", "Right"),
        VirtualKeys.Down => Pick("Abajo", "Down"),
        VirtualKeys.PrintScreen => Pick("Impr Pant", "Print Screen"),
        VirtualKeys.Insert => "Insert",
        VirtualKeys.Delete => Pick("Supr", "Delete"),
        VirtualKeys.Multiply => "Num *",
        VirtualKeys.Add => "Num +",
        VirtualKeys.Subtract => "Num -",
        VirtualKeys.Decimal => "Num .",
        VirtualKeys.Divide => "Num /",
        VirtualKeys.NumLock => Pick("Bloq Num", "Num Lock"),
        VirtualKeys.ScrollLock => Pick("Bloq Despl", "Scroll Lock"),
        _ => null,
    };
}
