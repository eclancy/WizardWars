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

`tools/release/package.py` makes three files from the export in `build/windows/`:

- **`WizardWars-windows.zip`** (about 80 MB): everything, in a `WizardWars/` folder, with a
  `How to play.txt`. This is the file a new player downloads.
- **`WizardWars-patch.zip`** (about 13 MB): `WizardWars.pck` (every image, sound, setting and
  scene; most of its size is the music) and `WizardWars.dll` with its two JSON files (all the
  C#). These are the only files that change when the game changes and Godot does not. The rest
  of the full zip is the Godot engine and the .NET runtime.
- **`version.json`**: the version, the Godot version it was built with, and the URLs of both
  zips.

The export preset keeps the `.pck` next to the exe (`binary_format/embed_pck=false`) so the
patch can replace it on its own.

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
packages, commits "Release v0.2.0", tags it, pushes master and the tag, and runs
`gh release create`. It takes about a minute. Run `bash tools/check.sh` first; the release script
does not.

Version numbers: bump the last number for fixes and balance changes, and the middle one for a new
wizard, ability or stage.

**One-time setup on a new machine:** install Godot's export templates for the exact editor
version. They come from the `godotengine/godot-builds` GitHub release, file
`Godot_v<version>-stable_mono_export_templates.tpz`, about 1.2 GB. Only the `windows_*` files are
needed, unpacked into `%APPDATA%\Godot\export_templates\<version>.stable.mono\`. The export
fails with "No export template found" until they are there. `package.py` reads the engine version
from the name of `$GODOT_BIN` (default: the path in `CLAUDE.md`), and runs on Python 2.7 or 3.

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

- **Windows SmartScreen** warns "Windows protected your PC" the first time, because the exe is
  not code-signed. `How to play.txt` and the download page tell people to click "More info", then
  "Run anyway". Signing costs money every year, and is not worth it for family and friends.
- **Upgrading Godot** changes the engine version, so every copy downloads the full zip once.
  Install the new export templates before releasing.
- **The game must be somewhere it can write to.** Unzipped into Program Files, it cannot update
  itself and silently stays on its version. `How to play.txt` says to keep it in Documents or on
  the Desktop.
- **Only Windows is exported.** A Mac build needs Apple notarization to open without a fight.
- **The repo is public**, so a release is public too.
