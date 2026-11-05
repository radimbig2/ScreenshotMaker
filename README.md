# Screenshot Maker

A small Windows tray app that automatically saves every screenshot you take to a folder.

Press **PrtSc**, **Alt+PrtSc** or **Win+Shift+S** as usual — the image Windows puts on the clipboard is saved as
`Screenshot 2026-11-05 14.03.22.png` in `Pictures\Screenshots` (or any folder you choose).

## Features

- Captures Print Screen and Snipping Tool (Win+Shift+S) screenshots; optionally any image copied to the clipboard.
- Lives in the system tray: left-click to open, right-click for the menu (open folder, last screenshot, pause, exit).
- Gallery of recent screenshots with open / show in folder / move to Recycle Bin.
- PNG or JPEG, custom save folder, optional notification after each save.
- Starts with Windows (optional), single instance, high-DPI and dark-mode aware.
- Installs per user without admin rights and updates itself from GitHub Releases.

## How it works

The app listens for clipboard changes (`AddClipboardFormatListener`) instead of polling or waiting. A screenshot
shortcut, seen through a low-level keyboard hook that never blocks the key, "arms" the app; the next image that lands
on the clipboard is saved. Identical images arriving within a few seconds are saved only once.

Settings are stored in `%AppData%\ScreenshotMaker\settings.json`.

## Development

Requirements: .NET 10 SDK on Windows.

```powershell
dotnet build
dotnet run -- --show
```

`--show` opens the main window on start; without it the app starts in the tray.

Project layout:

| Folder | Contents |
|---|---|
| `Native/` | Win32 interop: clipboard listener, keyboard hook |
| `Services/` | Settings, capture logic, saving, startup registration, updates, single instance |
| `UI/` | Tray icon context, main window, gallery helpers |
| `build/` | Installer build script |

## Building the installer

The installer is built with [Velopack](https://velopack.io):

```powershell
./build/build-installer.ps1
```

This produces `artifacts/releases/ScreenshotMaker-win-Setup.exe` (self-contained, no .NET install needed).
The setup installs to `%LocalAppData%\ScreenshotMaker`, creates Start menu and desktop shortcuts and registers an
uninstaller in *Apps & features*.

## Releasing

Create a release on GitHub with a tag like `v2.1.0` (or `gh release create v2.1.0 --generate-notes`).
The **Release** workflow then builds the installer for that tag, signs it and attaches it to the release.
It can also be re-run manually from the Actions tab for an existing tag.

Installed copies check GitHub Releases on startup, download the update in the background and offer to restart.
To publish from your machine instead, set `GITHUB_TOKEN` and run `./build/build-installer.ps1 -Version 2.1.0 -Publish`.

## Code signing

Signing uses an Authenticode code-signing certificate in PFX format. Add two repository secrets:

| Secret | Value |
|---|---|
| `WINDOWS_CERT_PFX_BASE64` | The `.pfx` file encoded as base64 |
| `WINDOWS_CERT_PASSWORD` | The PFX password |

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes('codesign.pfx')) | gh secret set WINDOWS_CERT_PFX_BASE64
gh secret set WINDOWS_CERT_PASSWORD
```

Without these secrets the workflow still builds the installer, just unsigned. Locally, pass
`-CertificatePath codesign.pfx` and set `SIGN_CERT_PASSWORD`.
