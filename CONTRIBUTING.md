# Contributing to CursorLock

Thanks for helping! Bug reports, ideas and pull requests are welcome.

## Reporting a bug or suggesting a feature

Open an [issue](../../issues). For bugs, include your Windows version, monitor setup
(number of monitors, resolutions, scaling) and the steps to reproduce.

## Making a change

1. Fork the repository and create a branch from `main`.
2. Make your change and build it locally (see [Building](README.md#building)):
   ```powershell
   .\build.ps1 -Version 0.0.0
   ```
3. Open a pull request against `main`, describing what changed and how you tested it.

Every pull request is built automatically by the [Build](.github/workflows/build.yml)
workflow, which also checks for forbidden APIs. The resulting MSI and portable exe are
attached to the workflow run, so reviewers can try them.

## Rules that keep CursorLock safe and fast

These are the reasons the app exists; pull requests that break them cannot be accepted.

- **Anti-cheat safety.** Never use the APIs listed in
  [What it does NOT do](README.md#what-it-does-not-do): no hooks, no keyboard polling, no
  Raw Input, no input injection, no access to other processes, no overlays. The cursor is
  confined with `ClipCursor` only. No network access, no telemetry, no administrator rights.
- **Zero added latency.** No code may run per mouse or keyboard event.
- **Minimal resources.** One UI thread, no `Task`/`async`/thread pool. Nothing runs while
  the cursor is free (the thread sleeps in `GetMessageW`). No allocations in the message
  loop, the lock/unlock path or the timer tick.
- **NativeAOT clean.** No reflection, no new NuGet packages in the app, and the build must
  produce no warnings, including trimming/AOT ones (IL2xxx/IL3xxx).
- **Always release the cursor.** Every exit path must end with `ClipCursor(NULL)`.

## Code style

- All code, comments and documentation in English.
- Follow the existing structure: `Core` (logic, no UI), `Persistence` (registry), `Ui`
  (Win32 windows), `Native` (P/Invoke declarations only, with `[LibraryImport]`).
- User-facing text goes in `Ui/Strings.cs`, in both English and Spanish.
- Window procedures are `[UnmanagedCallersOnly]` and wrapped in `try`/`catch`: an exception
  must never cross into native code.

## Releases

Maintainers publish a release by pushing a version tag:

```powershell
git tag -a v1.2.3 -m "CursorLock 1.2.3"
git push origin v1.2.3
```

The [Release](.github/workflows/release.yml) workflow builds the MSI and the portable exe
and attaches them, with their SHA-256 hashes, to a new GitHub Release.
