# Wizard Wars

A local multiplayer arena battler for 1–4 players. Pick a wizard of fire, frost, earth or
electricity and fight to be the last one standing, or team up in Red vs Blue. Playing alone?
You face a computer-controlled wizard.

Built with **Godot 4.5 (.NET / C#)**. It started life as a Unity game. The last Unity
version is preserved at the git tag [`unity-final`](../../tree/unity-final).

## Play

- **Run:** open `project.godot` in Godot 4.5.1 .NET and press F5, or export a Windows build
  (below).
- **Pads:** each player presses **Start** on Character Select to join.
- **Keyboard (player 1):**

| | Pad | Keyboard |
|---|---|---|
| Move | Left stick | W A S D |
| Aim | Right stick | Arrow keys |
| Basic attack (free) | RT | 1 |
| Second ability | LT | 2 |
| Third ability | RB | 3 |
| Ultimate (when the gold bar is full) | X | 5 |
| Join / start | Start | Enter |
| Confirm / lock in | A | Space |
| Back / leave | B | Backspace |
| Free-for-All or Teams | Back/View | Tab |
| Switch team | Y | T |
| How to play (hold) | Right stick click | H |

### The wizards

| Wizard | Basic | Second | Third | Ultimate |
|---|---|---|---|---|
| Fire | Fireball | Ring of Fire | Flamethrower | Meteor Storm |
| Frost | Icicle | Ice Patch | Ice Wave | Absolute Zero |
| Earth | Mudball | Mud Patch | Rock Wall | Earthquake |
| Electricity | Electricity Ball | Lightning Rod | Speed Up | Overcharge |

## Develop

Requirements: Godot 4.5.1 .NET and the .NET 9 SDK.

```bash
bash tools/check.sh
```

This builds the project, imports its assets, and runs the automated tests headless: the
rule checks, a scripted run through every menu, every ability against an enemy and a
teammate, and leak-checked computer matches. See `CLAUDE.md` for the architecture and
conventions.

To make a Windows build (the Godot 4.5.1 mono export templates must be installed):

```bash
godot --headless --path . --export-release "Windows Desktop" build/windows/WizardWars.exe
```

To publish a release that every downloaded copy updates itself to, see `docs/releasing.md`.

## Credits

The music is CC0, from OpenGameArt; see `assets/music/CREDITS.md`.
