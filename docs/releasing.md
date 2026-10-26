# Releasing

How a downloadable copy of Wizard Wars gets to people, and how it keeps itself up to date.
Ported from DrawFight's release system (2026-10-12), which works the same way.

**In one line:** `bash tools/release/release.sh X.Y.Z` makes a GitHub Release. The next time any
copy of the game starts, it finds the release and updates itself.

## Where things live

| What | Where | Why |
|---|---|---|
| The download page | `ecec.dev/wizard-wars/download` (`public/wizard-wars/download/` in the EricClancyEngineeringAndCrafts repo) | the page people are sent |
| The game files | GitHub Releases on `eclancy/WizardWars` | GitHub serves release downloads free with no bandwidth limit, so bots downloading it over and over cost nothing. The droplet's bandwidth is left alone. |
| The update check | `ecec.dev/wizard-wars/version.json` | an nginx 302 to `github.com/eclancy/WizardWars/releases/latest/download/version.json` |

The update address is on ecec.dev, not GitHub, because it is the one address compiled into every
copy ever downloaded. Behind a redirect, the releases can move somewhere else later without
breaking old copies. The download button links to GitHub's `releases/latest/download/...`, which
always points at the newest release, so **neither the page nor ecec.dev needs a deploy for a
release**.

## What a release contains

`tools/release/package.py` makes three files from the export in `build/windows/`, and Inno Setup
makes a fourth:

- **`WizardWars-Setup.exe`** (about 63 MB): the installer, compiled from
  `tools/release/installer.iss`. This is the file a new player downloads: run it, and play from
  the Start menu or the desktop.
- **`WizardWars-windows.zip`** (about 80 MB): everything, in a `WizardWars/` folder, with a
  `How to play.txt`. The portable version, for anyone who would rather not install: unzip it
  anywhere writable and run `WizardWars.exe`.
- **`WizardWars-patch.zip`** (about 13 MB): `WizardWars.pck` (every image, sound, setting and
  scene; most of its size is the music) and `WizardWars.dll` with its two JSON files (all the
  C#). These are the only files that change when the game changes and Godot does not. The rest
  of the full zip is the Godot engine and the .NET runtime.
- **`version.json`**: the version, the Godot version it was built with, and the URLs of both
  zips and of the installer (`installer`; the game itself does not use it) - and, under `linux`,
  the Linux pair below.

## Linux

Added 2026-10-26, on Eric's call, for every game on ecec.dev (the same design as DrawFight's).
`release.sh` exports the `Linux` preset to `build/linux/` too, and `package.py` makes two more
files from it:

- **`WizardWars-linux.tar.gz`**: the game in a `WizardWars/` folder - the `WizardWars.x86_64`
  binary, the `.pck`, and `data_WizardWars_linuxbsd_x86_64/` with the .NET runtime, so nothing
  has to be installed first - plus `install.sh`, `icon.png` and `How to play.txt`. A tarball
  rather than a zip, because a tarball carries the "can be run" permission on the binary and the
  script; `package.py` sets it, which it can do from Windows.
- **`WizardWars-linux-patch.zip`**: the Linux export's `.pck` and the game's DLL - the same idea
  as the Windows patch, from the Linux export.

**`install.sh`** (`tools/release/install.sh`) is the Linux installer. It copies the game to
`~/.local/share/WizardWars` and puts a `wizardwars.desktop` in `~/.local/share/applications`, so
it is on the app menu. No sudo and nothing outside the home folder, for the same reason the
Windows installer installs per user: the game updates itself, so its folder has to stay writable
by the player. `./install.sh --uninstall` takes it out again; saves (in
`~/.local/share/godot/app_userdata/Wizard Wars`) are never touched. Running `./WizardWars.x86_64`
straight from the unpacked folder works too.

**Updating** works as on Windows, from `version.json`'s `linux` pair: the patch on the same
engine, the tarball otherwise (unpacked with `System.Formats.Tar`, so the binary stays
runnable). The game writes `apply.sh`, starts it with `/bin/sh` and quits; it waits for the game
to close, copies the new files over, `chmod +x`es the binary and starts it again. A release from
before Linux has no `linux` pair, and a Linux copy just goes on to the title.

**Tested so far:** the export, the packaging and the permissions in the tarball, on Windows. It
has not yet been run on Linux - Eric is testing it on a Linux machine.

The export preset keeps the `.pck` next to the exe (`binary_format/embed_pck=false`) so the
patch can replace it on its own.

## The installer

`WizardWars-Setup.exe` installs **per user, with no admin prompt**, into
`%LOCALAPPDATA%\Programs\WizardWars` (`PrivilegesRequired=lowest`). It adds a Start menu
shortcut and, unless unticked, a desktop one, and registers an uninstaller under Settings > Apps.
Uninstalling removes the whole folder, including anything updates added, but never the save data
and settings in `%APPDATA%\Godot\app_userdata\Wizard Wars`.

Why per user: the self-updater copies each update into the game's own folder, so that folder must
stay writable by the player. Program Files is not, and installing there would also need an admin
prompt. Tested 2026-10-18: a default silent install landed in `%LOCALAPPDATA%\Programs\WizardWars`
with no prompt, registered under HKCU only, the folder passed the same write probe `UpdateScreen`
uses, the game started, and the uninstaller left nothing behind.

The `AppId` GUID in `installer.iss` must never change: it is how a newer installer finds and
upgrades an installed copy (into the same folder, `UsePreviousAppDir`). Running a newer installer
over an older copy is fine; so is letting the copy update itself.

## How a copy updates

`UpdateScreen` is the first screen of an exported Windows build. It never runs in the editor, from
`godot --path .`, or with any of the test flags (`--checks`, `--screen=...`, and so on).

1. It fetches `version.json` (5-second timeout) and compares versions number by number, so
   0.10.0 beats 0.9.0. The game's own version is `application/config/version` in
   `project.godot`, which the release script sets.
2. If the release was built with the same Godot version as this copy, it downloads the patch;
   otherwise it downloads the full zip. It unpacks to `user://update/`
   (`%APPDATA%\Godot\app_userdata\Wizard Wars\update\`).
3. Windows will not let a running program overwrite its own files. So the game writes
   `apply.cmd`, starts it, and quits. The script waits for the game to close, `robocopy`s the new
   files over the old ones, and starts the game again.

Anything that goes wrong, including no internet, GitHub being down, or a folder the game cannot
write to, just goes on to the title screen. **An update is never worth not being able to play.**
The current version shows in the bottom-right corner of the title screen.

Tested end to end on 2026-10-12: a 0.0.9 build updated itself to 0.1.0 from a local server (via
the patch), and its `.pck` and DLL came out byte-identical to the 0.1.0 export. Restarted, it
reported itself up to date. With the server down it went straight to the title, and
`--no-update` skipped the check.

## Making a release

```sh
bash tools/release/release.sh 0.2.0 "New stage: the Volcano. Ice Patch lasts a little longer."
```

The quoted text is optional: it is what players see under "What's new" on
`ecec.dev/games/wizard-wars`, so write it for them. Leave it out and the release just says how to
download. The script puts it above the download instructions, which always start with
"Download **" because the site strips the paragraph that starts that way.

Run it on `master` with a clean working tree. The script sets the version, builds, exports,
packages, compiles the installer, commits "Release v0.2.0", tags it, pushes master and the tag,
and runs `gh release create` with the installer, both zips and `version.json`. It takes about a minute. Run `bash tools/check.sh` first; the release script
does not.

Version numbers: bump the last number for fixes and balance changes, and the middle one for a new
wizard, ability or stage.

**One-time setup on a new machine:** install Godot's export templates for the exact editor
version. They come from the `godotengine/godot-builds` GitHub release, file
`Godot_v<version>-stable_mono_export_templates.tpz`, about 1.2 GB. Only the `windows_*` files and
`linux_release.x86_64` / `linux_debug.x86_64` are needed, unpacked into
`%APPDATA%\Godot\export_templates\<version>.stable.mono\`. The export
fails with "No export template found" until they are there. `package.py` reads the engine version
from the name of `$GODOT_BIN` (default: the path in `CLAUDE.md`), and runs on Python 2.7 or 3.

The installer needs **Inno Setup 6** on the releasing machine: `winget install JRSoftware.InnoSetup`.
`release.sh` looks for `ISCC.exe` at `$ISCC`, or by default at
`C:/Users/ericc/AppData/Local/Programs/Inno Setup 6/ISCC.exe` (a per-user install), and stops
before tagging anything if it is missing. To build the installer by hand after an export and
`package.py`: `ISCC /DAppVersion=X.Y.Z tools/release/installer.iss` (from Git Bash, prefix
`MSYS2_ARG_CONV_EXCL="*"` so the `/` options are not turned into paths).

For the exe to carry the game's icon in Explorer, also download `rcedit-x64.exe` from the
`electron/rcedit` GitHub releases (MIT) to somewhere outside the repo, and set Godot's editor
setting `export/windows/rcedit` to it (on this machine: `C:/Users/ericc/tools/rcedit-x64.exe`).
Without it the export still works, but the exe shows Godot's icon. The game window uses
`icon.ico` either way (`config/windows_native_icon`).

**Testing an update without publishing:**

1. Set `config/version` in `project.godot` to an older number, export to `build/test/`, and put
   the version back.
2. Copy the two zips from `build/release/` to a folder, with a `version.json` whose `full` and
   `patch` URLs point at that folder on a local server (for example
   `python -m http.server 8765`, or `python -m SimpleHTTPServer 8765` on Python 2).
3. Run `build/test/WizardWars.exe` with `WIZARDWARS_UPDATE_URL` set to that `version.json`. It
   should update, restart, and show the new version bottom-right. The log is in
   `%APPDATA%\Godot\app_userdata\Wizard Wars\logs\`.

Run a build with `-- --no-update` to skip the check entirely.

## Things to know

- **Windows SmartScreen** warns "Windows protected your PC" the first time, because the
  installer and the exe are not code-signed. `How to play.txt` and the download page tell people
  to click "More info", then "Run anyway". Signing costs money every year, and is not worth it for family and friends.
- **Upgrading Godot** changes the engine version, so every copy downloads the full zip once.
  Install the new export templates before releasing.
- **The game must be somewhere it can write to.** The installer sees to that. The portable zip
  unzipped into Program Files cannot update itself and silently stays on its version.
  `How to play.txt` says to keep it in Documents or on the Desktop.
- **The download page** (`ecec.dev/wizard-wars/download`) offers the installer
  (`releases/latest/download/WizardWars-Setup.exe`) first, and the zip as the portable fallback.
- **Windows and Linux are exported.** A Mac build needs Apple notarization to open without a fight.
- **The repo is public**, so a release is public too.
