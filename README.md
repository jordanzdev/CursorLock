<p align="center">
  <img src="assets/logo.svg" width="128" height="128" alt="CursorLock logo">
</p>

# CursorLock

A Windows 10/11 tray app that **confines the mouse cursor to one monitor** when you press
a hotkey (**Ctrl+0** by default) and releases it when you press it again. Built to stop
the cursor from escaping to a second screen while playing Counter-Strike 2 in borderless
windowed mode, on Valve (VAC) or FACEIT servers.

- C# / .NET 10 compiled with **NativeAOT**: a single native `CursorLock.exe` (~800 KB), no
  .NET runtime to install, no JIT.
- **Plain Win32** UI (no WinForms, WPF, WinUI or NuGet packages).
- User interface in **English and Spanish**.
- No configuration files: everything is stored in the `HKCU` registry.
- Idle: ~3 MB of private memory and 0 % CPU.

## Installation

Download the latest version from **[Releases](../../releases)**:

| File | Purpose |
|---|---|
| `CursorLock-x.y.z.msi` | **Per-user** Windows Installer package: no administrator rights needed. Its first page lets you choose English or Español (for the installer and for CursorLock); it installs to `%LOCALAPPDATA%\Programs\CursorLock`, adds a Start menu shortcut, and its last page offers to create a desktop shortcut and to open CursorLock. Uninstall it from *Settings → Apps*. |
| `CursorLock-x.y.z-portable.exe` | The same executable without installing: copy it anywhere and run it. |
| `SHA256SUMS.txt` | Hashes to verify the downloads: `Get-FileHash <file>` in PowerShell, or `sha256sum -c SHA256SUMS.txt`. |

The binaries are not code-signed, so Windows SmartScreen may warn you the first time
(*More info → Run anyway*). They are built by GitHub Actions from this source code
([workflow](.github/workflows/release.yml)).

**Upgrading** with a newer MSI keeps your settings. **Uninstalling** also removes the
settings and the *Start with Windows* entry. If CursorLock is running, Windows Installer
closes it automatically.

## Usage

| Action | How |
|---|---|
| Lock / unlock | Hotkey (Ctrl+0) or tray icon menu → **Lock / Unlock** |
| Settings | Double-click the tray icon, or menu → **Settings…** |
| Exit | Menu → **Exit** (releases the cursor and removes the icon) |
| Closing Settings with the **X** button | Asks: **Minimize to tray** (the app keeps running) or **Close CursorLock** (releases the cursor and ends the process). *Cancel* and Esc just close the window. |

Indicators: **grey icon = free**, **red icon = locked**; the tooltip shows the state and
the hotkey; optional sound (on by default); optional on-screen notice, OSD (off by
default).

The app always starts **unlocked**. When you open it (Start menu, the exe, or the end of the
installation) it shows the Settings window; when Windows starts it at sign-in
(*Start with Windows*) it stays silently in the tray. If the saved hotkey is taken by
another application, it shows a balloon warning and keeps working from the tray menu.

### Settings

- **Keyboard shortcut**: native Windows hotkey control (`msctls_hotkey32`). At least one
  modifier is required. While the Settings window is active the global hotkey is
  suspended, so you can type any combination into the control, including the current
  one; it becomes active again when you switch to another window or close Settings. If
  the new hotkey is taken, an error is shown and the previous one is kept.
- **Target monitor**:
  - *Under the cursor* (default): the monitor the mouse is on when you press the hotkey.
  - *Primary*.
  - A specific monitor (`DISPLAY2 — 2560×1440`). If it is no longer connected, the
    monitor under the cursor is used.
- **Reapply every (ms)**: 100–2000, 250 by default (see below).
- **Language**: *Automatic (system)* (Spanish if Windows is in Spanish, English
  otherwise), *Español* or *English*.
- **Start with Windows**, **Sound**, **Show on-screen notice (OSD)**.

Changes apply on save, without restarting. The window title shows the installed version.

### Updates

**Check for updates** (in Settings) looks up the latest release on GitHub. If there is a
newer one, it asks first, then downloads the MSI, verifies it against the release's
`SHA256SUMS.txt` and starts it; the installer preselects your current language, replaces the
app, keeps your settings and offers to open CursorLock at the end. Every release's changes
are listed in [CHANGELOG.md](CHANGELOG.md) and in its release notes. With the portable exe it opens the download page
instead. The app never checks for updates on its own.

## Why it adds no latency

Locking uses **only `ClipCursor`**: Windows itself restricts pointer movement inside its
input subsystem. No mouse event ever goes through this process; there are no hooks and
no Raw Input. The game receives exactly the same input as without the app.

Low-level hooks, on the other hand, would route *every* mouse or keyboard event through
this process before it reaches the game. That adds latency, and it is exactly the kind
of thing anti-cheat software looks for.

Resource usage:

- **Free**: the app's only thread sleeps in `GetMessageW`. No active timers: 0 % CPU.
- **Locked**: a single `SetTimer` (250 ms by default). Each tick reads the current clip
  with `GetClipCursor` and only calls `ClipCursor` when needed. No memory allocations in
  the tick or in the message loop.
- On lock/unlock, `ClipCursor` runs first; the icon, sound and OSD come afterwards.

### Why reapply?

Windows or the game itself may remove or change the clip (for example on Alt+Tab). The
timer restores it within one interval. If another program applies a **stricter** clip
that stays inside the locked monitor (for example, the game confining the cursor to its
own window), it is left alone to avoid fighting it: the cursor still cannot leave the
monitor.

If the resolution changes or a monitor is disconnected, the rectangle is recalculated
(`WM_DISPLAYCHANGE`): the same monitor if it still exists, otherwise the one under the
cursor.

## What it does NOT do

Intended for review against anti-cheat software. The code **does not use**:

| Category | APIs that do not appear in the code |
|---|---|
| Hooks | `SetWindowsHookEx` (any type), `SetWinEventHook` |
| Keyboard polling | `GetAsyncKeyState`, `GetKeyState` |
| Input | Raw Input (`RegisterRawInputDevices`, `GetRawInputData`) |
| Input injection | `SendInput`, `keybd_event`, `mouse_event` |
| Other processes | `OpenProcess`, `ReadProcessMemory`, `WriteProcessMemory`, `CreateRemoteThread`, DLL injection, enumerating processes or modules, reading other processes' windows |
| Graphics | DirectX/OpenGL overlays; nothing is drawn inside the game |

Also:

- It does not know or care which program is in the foreground: it behaves the same in
  any window.
- **No network access and no telemetry**, with one explicit exception: when you click
  **Check for updates**, it connects to `github.com` only (nothing about you or your PC is
  sent) and, if you accept an update, starts the downloaded installer or opens the download
  page. Nothing happens automatically or in the background; it never starts other processes
  otherwise.
- No administrator rights (`asInvoker` manifest).
- No packers, obfuscators or compressors (no UPX).

**Core APIs**: `RegisterHotKey`/`UnregisterHotKey`, `ClipCursor`, `GetClipCursor`,
`GetCursorPos`, `MonitorFromPoint`, `GetMonitorInfoW`, `EnumDisplayMonitors`,
`SetTimer`/`KillTimer`. Everything else is windowing, the tray icon
(`Shell_NotifyIconW`), GDI for the icons and the OSD, and the `HKCU` registry.

Quick check over the source code (this README contains the list above, so it is
excluded):

```sh
grep -rnE "SetWindowsHookEx|GetAsyncKeyState|SendInput|OpenProcess|SetWinEventHook|ReadProcessMemory|CreateRemoteThread" --include="*.cs" --include="*.csproj" --include="*.manifest" .
```

It must return no results.

### About the OSD

The OSD is a regular desktop window (`WS_EX_LAYERED | WS_EX_TRANSPARENT |
WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST`) shown with `SW_SHOWNOACTIVATE`: it
never takes focus and never receives clicks. During the second it is visible, a window
on top of the game may force Windows to compose the game through DWM instead of
presenting it directly ("independent flip"). That is why it is **off** by default. It is
not visible in exclusive fullscreen.

## Building

Requirements:

1. **.NET 10 SDK** (`dotnet --list-sdks` must list a 10.x version).
2. **Visual Studio 2022 or later** (or Build Tools) with the **"Desktop development with
   C++"** workload, including the **Windows SDK**. NativeAOT needs it for the linker
   (`link.exe`) and the Windows import libraries. Without it, `dotnet publish` fails with
   *"Platform linker not found"*.

The installer uses [WiX Toolset](https://wixtoolset.org) 5 as an MSBuild SDK: it is
downloaded automatically at build time, nothing else to install.

```powershell
.\build.ps1 -Version 1.2.3
```

Output in `artifacts\`:

```
CursorLock-1.2.3.msi
CursorLock-1.2.3-portable.exe
SHA256SUMS.txt
```

The executable only, without the installer:

```powershell
dotnet publish CursorLock\CursorLock.csproj -c Release
# → CursorLock\bin\Release\net10.0-windows\win-x64\publish\CursorLock.exe
```

> With Visual Studio 2026 (v18), `vcvarsall.bat` calls `vswhere.exe` without a full path
> and linking fails. `build.ps1` works around it by adding the Visual Studio Installer
> folder to `PATH`; if you run `dotnet publish` directly, use the *Developer PowerShell
> for VS*.

### Publishing a release

1. In [CHANGELOG.md](CHANGELOG.md), move the `[Unreleased]` entries to a new
   `## [1.2.3] - YYYY-MM-DD` section (and update the links at the bottom). Commit and push.
2. Tag and push:

   ```powershell
   git tag -a v1.2.3 -m "CursorLock 1.2.3"
   git push origin v1.2.3
   ```

The [`release.yml`](.github/workflows/release.yml) workflow builds the exe and the MSI on
`windows-latest` and creates the GitHub Release with these files. Its notes are the
version's changelog section plus a downloads table (`tools/Get-ReleaseNotes.ps1`); if the
section is missing, the release fails before building anything.

## Registry

| Key | Values |
|---|---|
| `HKCU\Software\CursorLock` | `HotkeyModifiers` (DWORD, `MOD_*`), `HotkeyVk` (DWORD), `TargetMode` (DWORD: 0 under the cursor, 1 primary, 2 specific), `TargetDevice` (REG_SZ, e.g. `\\.\DISPLAY2`), `ReapplyMs` (DWORD), `Sound` (DWORD 0/1), `Osd` (DWORD 0/1), `Language` (DWORD: 0 automatic, 1 Spanish, 2 English) |
| `HKCU\Software\CursorLock\Install` | Used by the MSI to track what it installed |
| `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` | `CursorLock` = `"<path of the exe>" --startup` (only when *Start with Windows* is enabled; `--startup` keeps it in the tray) |

Missing or invalid values fall back to their defaults.

Removing the portable version by hand: untick *Start with Windows*, exit, delete the exe
and, optionally, the `HKCU\Software\CursorLock` key. `CursorLock.exe --uninstall` does the
registry part and removes the desktop shortcut if it points to that exe (it is what the MSI
runs on uninstall); `CursorLock.exe --create-desktop-shortcut` creates the shortcut (what
the installer's last page runs).

## Robustness

- **Single instance** (`Local\CursorLock` mutex): a second launch exits silently.
- Every exit path (menu, session end, error) calls `ClipCursor(NULL)`.
- If Explorer restarts, the tray icon comes back (`TaskbarCreated`).
- If the process is killed forcibly, Windows does not always remove the clip right away:
  **Ctrl+Alt+Del** (and leaving that screen) always releases it.

## Project structure

```
build.ps1                    exe + MSI + hashes into artifacts\
.github/workflows/           automatic release when a v* tag is pushed
CHANGELOG.md                 changes per version (also used as release notes)
installer/                   per-user MSI (WiX 5): Package.wxs (what is installed),
                             Dialogs.wxs (pages, language choice), Texts.wxs (English/Spanish texts)
assets/                      logo (CursorLock.ico, logo.svg) and installer bitmaps
tools/New-Branding.ps1       regenerates every file in assets/ from the same geometry
tools/Get-ReleaseNotes.ps1   release notes of a version from CHANGELOG.md
CursorLock/
  CursorLock.csproj          NativeAOT, no NuGet packages
  app.manifest               Common Controls v6, PerMonitorV2, asInvoker
  Program.cs                 entry point: single instance and start-up
  App.cs                     lifetime and use cases spanning several components
  SingleInstance.cs          single-instance mutex
  Core/                      logic without UI
    CursorLocker.cs          lock state, ClipCursor, timer; reports changes through an event
    GlobalHotkey.cs          RegisterHotKey, suspended while Settings is active
    Monitors.cs              monitors and target rectangle per mode
    Config.cs, Hotkey.cs, VirtualKeys.cs, UiLanguage.cs, AppVersion.cs
  Persistence/               HKCU registry
    ConfigStore.cs, StartupRegistration.cs, InstallRegistration.cs, RegistryKeyHandle.cs,
    DesktopShortcut.cs       optional desktop shortcut (IShellLink)
  Updates/                   "Check for updates" (the only network code, run on click)
    HttpsRequest.cs          synchronous HTTPS GET with WinHTTP
    GitHubReleases.cs        latest version from the releases/latest redirect; asset downloads
    UpdateDownloader.cs      MSI download verified against SHA256SUMS.txt
  Ui/                        Win32
    MainWindow.cs            hidden window: routes messages
    TrayIcon.cs, TrayMenu.cs, StatusIcons.cs, LockFeedback.cs, OsdWindow.cs
    Strings.cs               English and Spanish UI text
    HotkeyText.cs, HotkeyControl.cs, WindowClass.cs, WindowIcons.cs, AppInstance.cs, Dpi.cs, SystemFont.cs
    Settings/                SettingsWindow, SettingsControls, SettingsForm, CloseConfirmation, UpdateCheck
  Native/                    P/Invoke with [LibraryImport], no logic
```

## Contributing

Issues and pull requests are welcome; see [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE) © jordanzdev
