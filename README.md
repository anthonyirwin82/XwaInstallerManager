# XwaInstallerManager (cross-platform)

**Install X-Wing Alliance mods without the headaches on Windows, Linux or macOS.**

If you've ever tried to set up XWAU or TIE Fighter Total Conversion you know the drill: hunt down the right download, work out which patch goes on top of which, keep a clean copy of the original game somewhere, and hope nothing breaks. XwaInstallerManager does all of that for you.

If you are on Linux or macOS then you need to try to get the original X-Wing Alliance game files which can be a pain if you don't have access to a Windows machine. If you have the GOG version then this app can extract the game files for you without needing Windows or wine.

---

## What it does for you

### 🎮 Everything in one list

Open the app and you get a single, always-current list of the major X-Wing Alliance mods — XWAU 2025, TFTC, Embers of the Empire and more. No forum threads, no broken links, no guessing which file is the latest. It tells you the version, what each mod needs installed first, and when it was released.

### ⬇️ One click to download, one click to install

Pick a mod, hit download, hit install. The app fetches the right file for **your** operating system and walks you through the setup. Progress is shown as it goes, so you're never staring at a frozen window wondering if it worked.

### 🛡️ Your original game stays safe

Point the app at a clean copy of X-Wing Alliance once, and it keeps that untouched. Every mod installs into its own separate folder, so you can have several setups side by side and never risk your vanilla install.

### 📦 Bring your own GOG installer

Got the game on GOG? Select the installer file and the app extracts the game files for you, tidies them into a folder called `xwa-original`, and sets it up as your clean copy automatically. No manual unzipping, no hunting for the right subfolder.

### ⚡ Opens instantly, works offline

The mod list is remembered between sessions, so the app opens with your list already there instead of a blank screen. If your internet is down, you still see everything you had before.

### 🧩 Add your own sources

Found a mod the app doesn't know about? Add its link in Settings and it appears in your list alongside everything else. You can add as many as you like.

### 🖥️ Actually cross-platform

The original was Windows-only. This version runs natively on **Windows, Linux and macOS** — including proper desktop shortcuts and menu entries on Linux, and correct handling of the different file naming each platform uses.

For best results use the open source OpenXWA https://github.com/elyosh/OpenXWA which runs on Linux, macOS and Windows and provides updates to make X-Wing Alliance work better on modern systems.

To run the mods in OpenXWA run the `OpenXWA Mod Importer' app after you have downloaded and installed the mods using this app. 

If you want to change your pilot's Tour of Duty Mission progress then I recommend xwa-pilot https://github.com/anthonyirwin82/xwa-pilot another tool I made and can be downloaded using this XwaInstallManager.

---

## Installing innoextract

The app can extract your game files straight from a GOG installer, but it needs **innoextract** to do it. Install it once and make sure it's on your `PATH`.

### Linux

**Debian / Ubuntu / Mint**

```bash
sudo apt update
sudo apt install innoextract
```

**Fedora**

```bash
sudo dnf install innoextract
```

**Arch Linux / Manjaro**

```bash
sudo pacman -S innoextract
```

### macOS

Using [Homebrew](https://brew.sh/):

```bash
brew install innoextract
```

### Windows

Using [Scoop](https://scoop.sh/):

```powershell
scoop install innoextract
```

### Checking it worked

```bash
innoextract --version
```

If that prints a version number, you're ready — the app will find it automatically.

---

## For developers

Want your own XWA tool or mod to appear in the list? See [For developers: publishing your own XWA software](#for-developers-publishing-your-own-xwa-software) — you just need to host a small JSON file.

---

## About this port

A cross-platform port of [XwaInstallerManager](https://github.com/JeremyAnsel/XwaInstallerManager). The original is a WPF application targeting .NET Framework 4.8 on Windows only. This port rebuilds the UI on **Avalonia 12** so the same tool runs on **Linux, Windows and macOS**, and adds a number of features on top.

---

## Requirements

| | |
|---|---|
| **.NET** | .NET 10 SDK (the projects target `net10.0`) |
| **Avalonia** | 12.1.2 |
| **OS** | Linux, Windows 10+, macOS |

Optional, for extracting game files from a GOG installer:

- **[innoextract](https://constexpr.org/innoextract/)** — must be on your `PATH`

---

## Building

```bash
dotnet restore XwaInstallerManager.slnx
dotnet build   XwaInstallerManager.slnx
dotnet run --project src/XwaInstallerManager/XwaInstallerManager.csproj
```

Run the tests:

```bash
dotnet test XwaInstallerManager.slnx
```

### Publishing a self-contained build

```bash
# Linux
dotnet publish src/XwaInstallerManager/XwaInstallerManager.csproj \
  -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true

# Windows
dotnet publish src/XwaInstallerManager/XwaInstallerManager.csproj \
  -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# macOS (Apple Silicon)
dotnet publish src/XwaInstallerManager/XwaInstallerManager.csproj \
  -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
```

---

## Project layout

```
XwaInstallerManager.slnx
├── src/
│   ├── XwaInstallerLibCore/     # Installer engine (no WPF dependency)
│   │   ├── Platform/            # Per-OS helpers (paths, shortcuts, registry, encoding)
│   │   ├── WizardWindow         # Install wizard
│   │   ├── ConsoleWindow        # Install log / progress
│   │   └── MainCore             # Config + install orchestration
│   └── XwaInstallerManager/     # Avalonia UI (manager window, settings, dialogs)
└── tests/
    └── XwaInstallerManager.Tests/   # xUnit v3 + Avalonia.Headless
```

`XwaInstallerLibCore` contains the installer engine and has **no WPF dependency** — it was ported to Avalonia so it can run on any platform.

---

## Cross-platform work

Porting from WPF to Avalonia was not just a UI swap; several Windows-only assumptions had to be replaced.

### UI framework

- **WPF → Avalonia 12.** All windows rewritten in `.axaml` with compiled bindings.
- **MVVM** via `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`).
- **`MessageBox.Show` → `MessageService`**, a small Avalonia dialog with error/warning/info/question variants.

### Platform abstraction (`XwaInstallerLibCore/Platform`)

| Concern | Windows | Linux | macOS |
|---|---|---|---|
| Desktop path | `SpecialFolder.DesktopDirectory` | same, falls back to `$HOME` | same |
| Shortcuts | `.lnk` via ShellLink COM | `.desktop` + menu entry | hidden (see below) |
| Registry | `HKLM\...\X-Wing Alliance` | returns `null` | returns `null` |
| Encoding | Code pages via `EncodingSetup` | same | same |

### Case-sensitive filesystems

Windows is case-insensitive; Linux and macOS are not. A GOG/Steam install stores files as `XWINGALLIANCE.EXE` and `FLIGHTMODELS/SPACECRAFT0.LST`, while packages reference them in mixed case.

Rather than renaming the user's game files (which risks clobbering), the app **resolves paths case-insensitively**:

- `XwaSettings.ResolvePathIgnoringCase` walks each path segment, preferring an exact match and falling back to a case-insensitive one.
- `PackageInstallerContext.GetXwaFilename` — the single choke point for every patch read, write and backup — resolves through it, so patching works regardless of on-disk casing.
- `EnumerateFilesIgnoringCase` replaces exact-case globs for `XWing95.exe`, `Tie95.exe` and `z_xvt__.exe`.

### Desktop shortcuts

- **Linux** — writes a `.desktop` file to the desktop **and** to `~/.local/share/applications` (honouring `XDG_DATA_HOME`) so it appears in the application menu. The launcher and the target binary are both made executable, and the file is marked trusted with `gio set metadata::trusted true` so GNOME/KDE will launch it.
- **Windows** — a real `.lnk` via the ShellLink COM interface.
- **macOS** — the option is **hidden**. A `.command` launcher opens a Terminal window and does not behave like a real app icon.

---

## Features added over the original

### 1. Mod list caching

The mod list is fetched from the network, but the app no longer waits for it:

- **Instant startup** — the cached list is rendered from disk before any network call, so the grid is never empty.
- **Conditional requests** — `ETag` / `Last-Modified` are stored alongside each cached document, so an unchanged file returns `304 Not Modified` and is served from disk.
- **Per-URL cache keys** — different hosts commonly serve the same file name (`versions.json`); a hash of the URL keeps their caches separate.
- **Offline fallback** — if the network fails, the cached copy is used.

### 2. User-supplied mod sources

Settings → **Additional mod sources** lets you add any number of JSON URLs. Their mods are appended to the list after the built-in sources. URLs are validated (absolute `http`/`https`) and duplicates are ignored.

### 3. Per-platform downloads

The mod schema gained an optional `Platform` field on each source, so a mod can ship different binaries per OS:

```json
"Sources": [
  { "Platform": "linux",   "Url": "https://.../tool-linux-amd64" },
  { "Platform": "windows", "Url": "https://.../tool-windows-amd64.exe" },
  { "Platform": "mac",     "Url": "https://.../tool-darwin-arm64" }
]
```

The app picks the source matching the current platform, falling back to a platform-independent one.

## For developers: publishing your own XWA software

You can make your tool or mod appear in the manager's download list by hosting a small JSON file. Users add its URL under **Settings → Additional mod sources**, and your entries are appended to the list.

### 1. Host the JSON

Any HTTPS URL works — a GitHub raw file is the simplest option:

```
https://raw.githubusercontent.com/<user>/<repo>/refs/heads/main/your-app-versions.json
```

The file must be served over **HTTPS** and return **HTTP 200**. The app sends conditional requests (`ETag` / `Last-Modified`), so serving those headers means unchanged files are not re-downloaded.

### 2. Choose a package format

The manager supports three kinds of download:

| Format | Detection | Install behaviour |
|---|---|---|
| **Zip package for mods** | archive containing `script.nsi` **and** `Package.json` | Runs the full installer wizard (the standard XWAU/TFTC format) |
| **Plain zip for anything xwa releated** | `"IsPlainZip": true` in the version | Extracted into `<mod directory>/<BaseName>/`; no installer script needed |
| **Standalone executable** | any non-zip file (ELF, `.exe`, Mach-O) | Copied into `<mod directory>/<BaseName>/` and made executable on Linux/macOS |

For a simple tool, a standalone executable is the easiest route. If your software is a **folder of files** rather than a single binary, use a plain zip (see below).

### Plain zip example (software that is not a mod)

If your software is a normal zip — no `script.nsi`, no `Package.json` — set `"IsPlainZip": true`. The archive is extracted into its own folder under the mod directory instead of being run through the installer wizard.

This example ships a **separate zip per platform**, so each OS downloads only the files it needs:

```json
{
  "Mods": [
    {
      "ModId": "MYTOOL",
      "BaseKey": "MYTOOL",
      "Name": "My XWA Tool",
      "ShortName": "My Tool",
      "Company": "Your Name",
      "Url": "https://github.com/you/my-xwa-tool",
      "Versions": [
        {
          "Version": "1.0.0",
          "InstallerFileName": "MyTool_1.0.0.zip",
          "InstallerFileSHA256": "",
          "Date": "2026-11-09 00:00 UTC",
          "ChangeLog": "https://github.com/you/my-xwa-tool/releases",
          "IsPlainZip": true,
          "Sources": [
            {
              "SourceName": "github-windows",
              "Platform": "windows",
              "Url": "https://github.com/you/my-xwa-tool/releases/download/V1.0.0/MyTool_1.0.0_windows.zip"
            },
            {
              "SourceName": "github-linux",
              "Platform": "linux",
              "Url": "https://github.com/you/my-xwa-tool/releases/download/V1.0.0/MyTool_1.0.0_linux.zip"
            },
            {
              "SourceName": "github-mac",
              "Platform": "mac",
              "Url": "https://github.com/you/my-xwa-tool/releases/download/V1.0.0/MyTool_1.0.0_mac.zip"
            }
          ],
          "IsBaseVersion": true,
          "PrerequisiteModId": "",
          "PrerequisiteVersion": ""
        }
      ]
    }
  ]
}
```

Each platform downloads its own archive, but all three install into the **same** folder — the folder name comes from `InstallerFileName` (`MyTool_1.0.0.zip` → `MyTool`), not from the source URL. So a user who switches platforms gets a consistent layout.

A zip laid out as:

```
MyTool_1.0.0_linux.zip
└── mytool/
    ├── README.txt
    └── bin/mytool
```

installs as:

```
<mod directory>/MyTool/
├── README.txt
└── bin/mytool        ← made executable on Linux/macOS
```

Notes:

- If every entry in the zip shares a single top-level folder, that folder is stripped so you do not get `MyTool/mytool/...`.
- Files with no extension, `.sh` or `.command` are made executable on Linux/macOS.
- Entries that would escape the install folder (zip-slip) are skipped.
- `IsPlainZip` is ignored for archives that contain `script.nsi` and `Package.json` — those still run the installer wizard.

### Plain zip example (one release for all platforms)

If your software is a single cross-platform zip — for example a .NET or Java tool, or a folder of data files — omit the `Platform` field. The one source is then used on every OS.

```json
{
  "Mods": [
    {
      "ModId": "MYTOOL",
      "BaseKey": "MYTOOL",
      "Name": "My XWA Tool",
      "ShortName": "My Tool",
      "Company": "Your Name",
      "Url": "https://github.com/you/my-xwa-tool",
      "Versions": [
        {
          "Version": "1.0.0",
          "InstallerFileName": "MyTool_1.0.0.zip",
          "InstallerFileSHA256": "",
          "Date": "2026-11-09 00:00 UTC",
          "ChangeLog": "https://github.com/you/my-xwa-tool/releases",
          "IsPlainZip": true,
          "Sources": [
            {
              "SourceName": "github",
              "Url": "https://github.com/you/my-xwa-tool/releases/download/V1.0.0/MyTool_1.0.0.zip"
            }
          ],
          "IsBaseVersion": true,
          "PrerequisiteModId": "",
          "PrerequisiteVersion": ""
        }
      ]
    }
  ]
}
```

The same archive is downloaded on Windows, Linux and macOS, and extracted into `<mod directory>/MyTool/`.

You can also mix the two styles — a platform-independent source acts as the fallback when no platform-specific source matches. This complete file gives Windows its own zip and falls back to a generic one everywhere else:

```json
{
  "Mods": [
    {
      "ModId": "MYTOOL",
      "BaseKey": "MYTOOL",
      "Name": "My XWA Tool",
      "ShortName": "My Tool",
      "Company": "Your Name",
      "Url": "https://github.com/you/my-xwa-tool",
      "Versions": [
        {
          "Version": "1.0.0",
          "InstallerFileName": "MyTool_1.0.0.zip",
          "InstallerFileSHA256": "",
          "Date": "2026-11-09 00:00 UTC",
          "ChangeLog": "https://github.com/you/my-xwa-tool/releases",
          "IsPlainZip": true,
          "Sources": [
            {
              "SourceName": "github-windows",
              "Platform": "windows",
              "Url": "https://github.com/you/my-xwa-tool/releases/download/V1.0.0/MyTool_1.0.0_windows.zip"
            },
            {
              "SourceName": "github-generic",
              "Url": "https://github.com/you/my-xwa-tool/releases/download/V1.0.0/MyTool_1.0.0.zip"
            }
          ],
          "IsBaseVersion": true,
          "PrerequisiteModId": "",
          "PrerequisiteVersion": ""
        }
      ]
    }
  ]
}
```

Here Windows gets the Windows zip, and Linux/macOS fall back to the generic one.

### Field reference

**Mod**

| Field | Required | Notes |
|---|---|---|
| `ModId` | yes | Unique identifier. Two mods with the same `ModId` are merged; different `ModId`s are always separate rows, even if they share a download URL. |
| `BaseKey` | no | Grouping key for related mods. |
| `Name` | yes | Shown in the **Name** column. |
| `ShortName` | no | Short label. |
| `Company` | no | Author / publisher. |
| `Url` | no | Website, opened by the **web** button. |
| `Versions` | yes | One or more versions (see below). |

**Version**

| Field | Required | Notes |
|---|---|---|
| `Version` | yes | Displayed in the **Version** column. |
| `InstallerFileName` | yes | File name. For a **directory** source URL it is appended to the URL; for a **direct file** URL it is only used for naming. |
| `InstallerFileSHA256` | no | If set, the **Check hash** button verifies the download. |
| `Date` | yes | Format `yyyy-MM-dd HH:mm 'UTC'` — e.g. `2026-11-09 00:00 UTC`. |
| `ChangeLog` | no | URL opened by the **changelog** button. |
| `Sources` | yes | One or more download locations. |
| `IsBaseVersion` | no | Marks the base install of a mod. |
| `IsPlainZip` | no | `true` = extract the download as a normal zip instead of running the NSIS installer. For software that is not a mod. |
| `PrerequisiteModId` / `PrerequisiteVersion` | no | Shown in the **Prerequisite** column; empty means `XWA`. |

**Source**

| Field | Required | Notes |
|---|---|---|
| `SourceName` | no | Label only. |
| `Url` | yes | Either a **directory** (ending in `/`, the file name is appended) or a **direct file** URL. |
| `Platform` | no | `linux`, `windows`, `mac` (also accepts `win`, `macos`, `osx`). Empty = platform-independent. |

### Platform selection

When a version has several sources, the manager picks the one matching the current OS, falling back to a source with no `Platform`:

```json
"Sources": [
  { "Platform": "linux",   "Url": "https://.../tool-linux-amd64" },
  { "Platform": "windows", "Url": "https://.../tool-windows-amd64.exe" },
  { "Platform": "mac",     "Url": "https://.../tool-darwin-arm64" }
]
```

If you ship a single cross-platform file, omit `Platform` entirely.

### Naming your binaries

For standalone executables, the `-os-arch` suffix is stripped on install:

| Download file | Installed as |
|---|---|
| `xwa-pilot-linux-amd64` | `xwa-pilot` |
| `xwa-pilot-windows-amd64.exe` | `xwa-pilot.exe` |
| `xwa-pilot-darwin-arm64` | `xwa-pilot` |

Recognised OS tokens: `windows`, `win`, `linux`, `macos`, `mac`, `osx`, `darwin`, `freebsd`.
Recognised arch tokens: `amd64`, `x86_64`, `x64`, `arm64`, `aarch64`, `arm`, `x86`, `i386`, `i686`, `386`, `riscv64`.

The install folder is the file name up to the first `_` (or the whole name), so `MyTool_Full_1.0.0.zip` installs into `MyTool/`.

### Testing your JSON

Add the URL under **Settings → Additional mod sources**, then press **Check updates**. If a source cannot be loaded, the manager shows a dialog naming the URL and the reason (download failure, invalid JSON, or no mods).

---

## How updates work

The app checks for a newer version on startup (if **Check updates on startup** is enabled) and from the **Check updates** button.

**1. Version check** — it downloads a plain-text file from the default branch:

```
https://github.com/anthonyirwin82/XwaInstallerManager/raw/main/version.txt
```

The file contains a single line, e.g. `1.0.0`. If that version is newer than the running build, you're asked whether to update.

**2. Download** — the release asset matching your platform is fetched from the latest GitHub release:

| Platform | Asset |
|---|---|
| Windows (x64) | `XwaInstallerManager-win-x64.zip` |
| Linux (x64) | `XwaInstallerManager-linux-x64.zip` |
| macOS (Apple Silicon) | `XwaInstallerManager-macos-arm64.zip` |
| macOS (Intel) | `XwaInstallerManager-macos-x64.zip` |

**3. Apply** — the archive is extracted to a temporary folder and copied over the install. Your `XwaInstallerManager.json` settings are never overwritten. Files that are locked while running (the executable) are renamed to `.bak` and removed on the next start. On macOS the `.app` bundle is updated in place.

The app then restarts automatically.

> **Note:** `version.txt` is updated automatically by the release workflow, so it always matches the published build. You don't need to edit it by hand.

---

## Tests

```bash
dotnet test XwaInstallerManager.slnx
```

The suite covers the installer engine, package model, case-insensitive path resolution, standalone executable handling, shortcut creation, and headless UI smoke tests (windows construct, bind and lay out).

---

## Credits

- Original **XwaInstallerManager** by [Jérémy Ansel](https://github.com/JeremyAnsel/XwaInstallerManager).
- Mod content by [XWA Upgrade](https://www.xwaupgrade.com/), [TFTC](https://tftc.xwaupgrade.com/) and the community.
- [Avalonia](https://avaloniaui.net/) for the cross-platform UI framework.

---

## License

[MIT](LICENSE) © 2026 Anthony Irwin

This project is a cross-platform port of the original XwaInstallerManager by Jérémy Ansel. The original work is credited above; this port and the features added to it are released under the MIT license.
