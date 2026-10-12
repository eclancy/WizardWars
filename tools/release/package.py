# -*- coding: utf-8 -*-
"""Turn an export in build/windows/ into the three files a GitHub Release carries.

    python tools/release/package.py 0.2.0

Writes build/release/:

  WizardWars-windows.zip  everything, in a WizardWars/ folder - what a new player downloads
  WizardWars-patch.zip    just the game: the .pck and the game's own assembly. A few MB, against
                          tens of MB for the full zip, because the engine and the .NET runtime
                          only change when Godot is upgraded.
  version.json            what the game's update check reads (see scripts/UpdateScreen.cs)

Normally run by tools/release/release.sh rather than by hand. See docs/releasing.md.
Works with Python 2.7 and 3.
"""
from __future__ import print_function

import io
import json
import os
import sys
import zipfile

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
EXPORT = os.path.join(ROOT, "build", "windows")
OUT = os.path.join(ROOT, "build", "release")
REPO = "eclancy/WizardWars"
DATA = "data_WizardWars_windows_x86_64"

# The engine the export was made with. A patch is only offered to a copy on the same engine;
# anything else downloads the full zip. Read from the editor binary's name rather than
# hard-coded, so upgrading Godot cannot leave this behind. Same default as tools/check.sh.
GODOT = os.environ.get("GODOT_BIN") or \
    "C:/Program Files (x86)/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64_console.exe"

# What the patch carries: everything that changes when the game changes and the engine does
# not. The .pck is every scene, image, sound and setting; the WizardWars.* files are the C#.
PATCH = ["WizardWars.pck", DATA + "/WizardWars.dll", DATA + "/WizardWars.deps.json",
         DATA + "/WizardWars.runtimeconfig.json"]

HOW_TO_PLAY = u"""Wizard Wars
===========

A wizard battle for 1 to 4 players on one computer. Pick fire, frost, earth or electricity
and be the last wizard standing - every player for themselves, or Red vs Blue teams.
Playing alone? You fight a computer wizard.

STARTING
1. Double-click WizardWars.exe.

2. The first time, Windows may say "Windows protected your PC". That is because this game
   is not from a big company that paid for a signature. Click "More info", then "Run anyway".

3. Plug in controllers (Xbox-style pads work best). Each player presses Start on the
   character screen to join. One player can use the keyboard instead.

CONTROLS            Controller            Keyboard (one player)
Move                Left stick            W A S D
Aim                 Right stick           Arrow keys
Basic attack        RT                    1
Second ability      LT                    2
Third ability       RB                    3
Ultimate            X                     5   (when the gold bar is full)
Join / start        Start                 Enter
Pick / lock in      A                     Space
Back / leave        B                     Backspace
Free-for-All/Teams  Back (View)           Tab
Switch team         Y                     T
How to play (hold)  Right stick click     H
Quit (title screen)                       Esc

The game updates itself: every time it starts, it checks for a new version and installs it.
Keep this folder somewhere you can write to, like Documents or the Desktop, so it can.

Made at https://ecec.dev - https://ecec.dev/wizard-wars/download
"""


def engine_version():
    name = os.path.basename(GODOT)
    # Godot_v4.5.1-stable_mono_win64_console.exe -> 4.5.1
    if name.startswith("Godot_v"):
        return name[len("Godot_v"):].split("-")[0]
    raise SystemExit("set GODOT_BIN to the Godot editor, so the engine version is known")


def add_tree(z, src, prefix):
    for dirpath, _dirs, files in os.walk(src):
        for f in sorted(files):
            full = os.path.join(dirpath, f)
            rel = os.path.relpath(full, src).replace(os.sep, "/")
            z.write(full, prefix + rel)


def main(argv):
    if not argv:
        print(__doc__)
        return 1
    version = argv[0]
    if not os.path.isfile(os.path.join(EXPORT, "WizardWars.pck")):
        raise SystemExit("no export in build/windows - run the Godot export first")
    if not os.path.isdir(OUT):
        os.makedirs(OUT)

    full = os.path.join(OUT, "WizardWars-windows.zip")
    z = zipfile.ZipFile(full, "w", zipfile.ZIP_DEFLATED, allowZip64=True)
    add_tree(z, EXPORT, "WizardWars/")
    z.writestr("WizardWars/How to play.txt", HOW_TO_PLAY.replace(u"\n", u"\r\n").encode("utf-8"))
    z.close()

    patch = os.path.join(OUT, "WizardWars-patch.zip")
    z = zipfile.ZipFile(patch, "w", zipfile.ZIP_DEFLATED)
    for rel in PATCH:
        z.write(os.path.join(EXPORT, rel.replace("/", os.sep)), rel)
    z.close()

    base = "https://github.com/%s/releases/download/v%s/" % (REPO, version)
    info = {
        "version": version,
        "engine": engine_version(),
        "patch": base + "WizardWars-patch.zip",
        "full": base + "WizardWars-windows.zip",
    }
    text = json.dumps(info, indent=2, sort_keys=True, separators=(",", ": ")) + "\n"
    if not isinstance(text, type(u"")):
        text = text.decode("utf-8")
    with io.open(os.path.join(OUT, "version.json"), "w", encoding="utf-8", newline="\n") as f:
        f.write(text)

    for name in ("WizardWars-windows.zip", "WizardWars-patch.zip", "version.json"):
        print("%-24s %6.1f MB" % (name, os.path.getsize(os.path.join(OUT, name)) / 1048576.0))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
