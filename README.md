# Wizard Wars

A local multiplayer arena battler for 1–4 players. Pick a wizard of fire, frost, earth,
electricity, shadow, nature or wind (each wizard can be played by one player at a time) and
fight to be the last one standing (or the one who holds the rune longest, in King of the
Rune), or team up in Gold vs Pink. Matches are best of 1, 3 or 5 rounds. Playing alone? You
face a computer wizard on Easy, Normal or Hard.

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
| Fourth ability | LB | 4 |
| Ultimate (when the gold bar is full) | X | 5 |
| Join / start / pause | Start | Enter |
| Confirm / lock in | A | Space |
| Back / leave | B | Backspace |
| Free-for-All or Teams | Select (Back/View) | Tab |
| Switch team | Y | T |
| How to play (hold) | Right stick click | H |
| Options (Character Select) | X | 5 |
| Rounds / goal / computer skill (Stage Select) | Y / RB / X | T / 3 / 5 |
| Fullscreen / window | | F11 or Alt+Enter |

### The wizards

| Wizard | Basic | Second | Third | Fourth | Ultimate |
|---|---|---|---|---|---|
| Fire | Fireball | Ring of Fire | Flamethrower | Meteor Strike | Meteor Storm |
| Frost | Icicle | Ice Patch | Ice Wave | Deep Freeze | Absolute Zero |
| Earth | Mudball | Mud Patch | Rock Wall | Tremor | Earthquake |
| Electricity | Electricity Ball | Lightning Rod | Speed Up | Chain Lightning | Overcharge |
| Shadow | Shadow Bolt | Smoke Cloud | Shadow Step | Night Grasp | Eclipse |
| Nature | Thorn | Bramble Patch | Regrowth | Entangle | Overgrowth |
| Wind | Gust | Updraft | Tailwind Dash | Cyclone | Tempest |

Some abilities root you (no moving), silence you (basic attack only), hide you in smoke, heal,
or push and pull.

### The stages

Every stage has its own cover to hide behind, and two have a hazard:

| Stage | Cover | Hazard |
|---|---|---|
| Woodland | Tree stumps | |
| Frost Tundra | Ice crystals | |
| Scorched Canyon | Sandstone rocks | |
| Storm Plateau | Rune stones | |
| Volcano | Basalt boulders | A lava river down the middle: it burns. Cross on the bridges. |
| Swamp | Mossy rocks | Bog pools: they slow you and stop your energy recharging. |

Cover stops shots. Anything that casts a shadow is solid; the rest is floor. Cacti and toadstools
are only plants: walk and shoot straight through them.

Each player's health, energy, cooldowns and ultimate charge sit in a strip below the arena, and
wizards' robes and hats get scuffed and torn as they are hurt.

**King of the Rune** (RB on Stage Select): stand alone in the glowing rune circle to score; the
first to 20 seconds takes the round. Two sides in it contest it, and knocked-out wizards come
back after 3 seconds. On the Volcano and the Swamp the rune moves between two spots.

Start pauses a battle (resume, rematch, options or quit). The options are music and sound
volume, fullscreen and controller rumble, and are saved between sessions.

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

The music is CC0, from OpenGameArt; see `assets/music/CREDITS.md`. The newer sound effects are
synthesised in code (`scripts/SoundTool.cs`).
