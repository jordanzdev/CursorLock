# Changelog

All notable changes to CursorLock are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/). Each version's section becomes the notes of its
GitHub Release.

## [Unreleased]

### Added

- The last page of the installer offers to **create a desktop shortcut** (unchecked by
  default, and not offered if you already have one). Uninstalling removes it.

### Changed

- The first page of the installer is titled *Install CursorLock* and its button is
  **Install**, since it starts the installation right away.

## [1.0.1] - 2026-10-22

### Added

- **Check for updates** button in Settings. Only when you click it, CursorLock looks up the
  latest release on GitHub; if there is a newer one it asks first, then downloads the
  installer, verifies its SHA-256 checksum and runs it. Portable copies open the download
  page instead. CursorLock never checks for updates on its own.
- The Settings window title shows the installed version.
- The installer starts with a language page (English / Español): the whole installer
  switches to the chosen language, and so does CursorLock. Your current CursorLock language
  is preselected; otherwise Spanish if Windows uses a Spanish locale, else English.
- The last page of the installer offers to open CursorLock, and the installer uses the
  CursorLock logo.

### Changed

- Opening CursorLock from the Start menu, the exe or the end of the installation now shows
  the Settings window. When Windows starts it at sign-in it still stays quietly in the tray:
  the *Start with Windows* entry now passes `--startup` (existing entries are updated
  automatically).
- Installing a CursorLock MSI replaces the installed copy, even if it is the same version.

### Fixed

- `SHA256SUMS.txt` uses LF line endings, so `sha256sum -c SHA256SUMS.txt` works on Linux,
  macOS and Git Bash.

## [1.0.0] - 2026-10-22

First release.

### Added

- Confines the mouse cursor to one monitor with a global hotkey (**Ctrl+0** by default) and
  releases it with the same hotkey. Locking uses `ClipCursor` only: no hooks, no Raw Input,
  no input injection and no access to other processes.
- Target monitor: under the cursor, primary, or a specific one.
- Tray icon (grey = free, red = locked), optional sound and optional on-screen notice.
- Settings window: hotkey, target monitor, reapply interval, language (English / Spanish),
  start with Windows, sound and on-screen notice. Closing it with **X** asks whether to
  minimize to the tray or exit.
- Per-user MSI installer (no administrator rights) and portable exe.

[Unreleased]: https://github.com/jordanzdev/CursorLock/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/jordanzdev/CursorLock/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/jordanzdev/CursorLock/releases/tag/v1.0.0
