# CLAUDE.md — Wizard Wars

Authoritative working notes for agents in this repo. If this file and another doc disagree,
this file wins; fix the other doc.

Wizard Wars is a 1–4 player local arena battler in **Godot 4.5 C#**. It was a Unity game
until 2026-10-11. The Unity project is gone from this branch; it survives at the git tag
**`unity-final`**. Check out that tag to see how something worked in Unity. Never restore
Unity files here. `docs/GODOT_PORT_PLAN.md` records how and why it was ported.

## Status

The game is **feature-complete**. It has:
- the title screen and Character Select: join by device, Free-for-All/Teams, team
  switching, kit lists, how-to-play
- Stage Select and all four stages
- all four wizards with all 16 abilities and their telegraphs
- the HUD and a computer opponent
- the countdown, results, rematch, and a Windows export

Every automated check passes.

**Open:**
- Human playtesting: `docs/playtest-checklist.md`.
- Tuning beyond the first pass. On 2026-10-13 health went from 100 to 250 and ultimate charge
  was raised, because fights ended in about 10 s, before anyone's ultimate filled. Computer
  matches now last about 30 s with an ultimate in every one: 3 per duel, 5.6 per four-way.
  Measure any balance change with
  `--soak=12 --players=2` (and `--players=4`), which prints a balance line.

**Public repo:** `eclancy/WizardWars` is public, so every push publishes. Add only assets with
a licence that allows that, and record their credits (see `assets/music/CREDITS.md`).

## Stack

- Godot **4.5.1 .NET** (mono), C#: `Godot.NET.Sdk/4.5.1`, `net9.0`, `Nullable=annotations`.
- GL Compatibility renderer, 1920×1080, stretch `canvas_items` / `expand`, 2D gravity 0.
- **On-screen text names only gamepad controls.** The keyboard still works for player 1
  (keys in `KeyboardInputSource` and the README), but no screen mentions a key.
- Starts **fullscreen** (`window/size/mode=3`). F11 or Alt+Enter toggles a window
  (`GameRoot._UnhandledInput`). Alt+Enter is not also a menu Start.
- Godot binary: `$GODOT_BIN`, or by default
  `C:/Program Files (x86)/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64_console.exe`.
  Use the **console** exe so output reaches the terminal.

## Verify every change

```bash
bash tools/check.sh
```

It runs, and fails on the first failure:

1. `dotnet build`.
2. A headless `--import`.
3. `--checks`: `RegressionChecks`. This covers settings, every wizard, ability and stage
   resource, stage geometry, the status-effect rules, friendly fire, and ultimate charge.
4. `--flowtest`: `FlowTest` drives two scripted pads through every menu and a Teams battle,
   back to Character Select.
5. `--abilities`: `AbilityTest` casts all 16 abilities at an enemy (each must do its job) and
   at a teammate (each must do nothing). Afterwards the arena must be empty.
6. `--soak`: `SoakTest` plays computer-only matches, Free-for-All and Teams, faster than real
   time. After each one, no effect node, modifier or ultimate lock may be left behind.

Run the steps after `dotnet build` with
`--headless --fixed-fps 60 --path . res://scenes/Main.tscn -- <flag>`.

`dotnet build` alone is **not** verification. A change to feel or visuals also needs a look:

```bash
"$GODOT_BIN" --path . res://scenes/Main.tscn -- --screen=demo --shot=C:/tmp/shot.png --shot-after=8
```

`--shot` forces a 1920×1080 window, because the game otherwise starts fullscreen at the
monitor's resolution.

`--screen` takes `title`, `select`, `stage`, `battle` or `demo` (four computer wizards; add
`--teams` for 2v2).

Export: `"$GODOT_BIN" --headless --path . --export-release "Windows Desktop" build/windows/WizardWars.exe`.
`build/` is not committed. The exported exe accepts the same flags, plus `--no-update`.

**Releases:** `bash tools/release/release.sh X.Y.Z` (on `master`, clean tree) exports, packages
and publishes a GitHub Release; every downloaded copy updates itself from it via `UpdateScreen`,
the first screen of an exported Windows build. Read `docs/releasing.md` before touching the
export preset, `UpdateScreen` or `tools/release/`.

## Architecture

- **`GameRoot`** (on `Main.tscn`) swaps screens as children: `TitleScreen`,
  `CharacterSelectScreen`, `StageSelectScreen`, `Battle`. It polls `Devices` once per
  **physics** step. Menus and wizards read input in `_PhysicsProcess` too, so a press is seen
  exactly once.
- **Input.** `IInputSource.Poll() → InputState`:
  - `KeyboardInputSource` (P1 keys as in Unity) and `GamepadInputSource` (read by device id).
  - `DeviceInputSource` reads a joined player's device.
  - `CpuInputSource` is the AI, ported from Unity's `WizardAI`.

  A player joins a slot with whichever device presses Start, and keeps it.
- **`Session`** is the single source of truth: slots (joined, locked, device, wizard, team),
  `TeamsMode` and `StageIndex`. It replaces Unity's GlobalHandler, the LocalMultiplayer
  runtime and the bridge between them.
- **Data:** `data/abilities/*.tres` (`AbilityData`), `data/wizards/*.tres` (`WizardData`),
  `data/stages/*.tres` (`StageData`), and `data/balance.tres` (`BalanceData`: health, energy,
  speed and ultimate charge for every wizard), all loaded through `Content`.
  - Tuning lives **only** there.
  - `gen-data.sh` does not write `balance.tres`; it is edited by hand or in the inspector.
  - `tools/gen-data.sh` records where every starting number came from in Unity. It
    regenerates the files and **overwrites editor tuning**, so only run it on purpose.
- **`Battle`** builds the arena, spawns from a `BattleSetup` (`FromSession`, or by hand in
  tests), runs the countdown, and decides the result. It keeps the registries effects
  query: `Wizards`, `Projectiles`, `Patches`, `FireSources`, `RockWalls`. It answers
  `IsInsideSolid` with a physics shape query on the solids layer. Effects reach it through
  `Battle.Current`.
- **`Wizard`** (`CharacterBody2D`) owns health, energy, ultimate charge, burn, stun,
  movement, aim, and ability dispatch, and reads one `IInputSource`.
- **Abilities** are plain C# (`Ability`, `Ultimate`, built by `AbilityFactory` from the
  ability id). What they create is an **effect node** owned by the battle, not the wizard:
  `Projectile`, `FireSource`, `FlameJet`, `Meteor`/`MeteorStorm`, `IcePatch`/`MudPatch`
  (`GroundPatch`), `IceWave`, `Aura` (Absolute Zero, Earthquake), `RockWall`,
  `LightningRod`, `Overcharge`, `SpeedTrail`. Effects outlive their caster correctly and
  clean up after themselves.
- **Hits use geometry, not physics callbacks.** Hit tests are circle against body box,
  ellipse for patches. Physics is only for walking into walls, obstacles, rock walls and
  rods. That keeps headless runs identical and removes collision masks as a source of bugs.
- **Friendly fire:** `Combat.CanAffect(caster, casterTeam, target, hitsCaster)`. Effects
  record the caster's team when cast. Patches and rods hit their own caster, as in Unity.
  Teammates are never hit by anything. Any new hostile effect must use this check.
- **Status effects:** `StatusEffects` holds modifiers (slow, haste, no-regen, slide) keyed
  by the source node. `Prune` drops any whose source was freed, so effects cannot leak the
  way Unity's additive `slow()`/`removeSlow()` did.
- **`ProceduralFx`** is Unity's pixel generators ported (rows flipped, because Godot images
  are top-down), plus `AreaDisc` for showing ultimate radii.
- **`Units`:** every Unity distance or position goes through `Units.U(...)` (90 px per Unity
  unit; Unity +y is Godot up). `Units.SpriteScale` converts a Unity transform scale for the
  100-ppu art.

## Deliberate differences from the Unity version

- **Not a leak or a bug:** status effects cannot leak, a big slow can never run you
  backwards, and a stun no longer hides an ongoing burn.
- **New feedback:**
  - A hit flashes the wizard red.
  - P1–P4/CPU labels sit over each wizard.
  - Ultimates show a faint disc at their real radius. Unity's overlay image ended up a third
    of a unit wide.
  - The HUD drops below a wizard standing at the top wall.
- **Projectiles keep the last aim direction** when the stick is released (Unity always fired
  right). Placed abilities still land at your feet with the stick released, as in Unity.
- **The computer can hold the flamethrower.** It has no targeter shown.
- **Rematch is faster:** players stay joined and locked after a match, so Start goes again.
  Unity made everyone rejoin.
- **Stage Select:** any joined player can drive it. Unity picked one at random.
- **Not ported:** the LB "fourth" slot, Unity's GlobalHandler/bridge split, and the
  target-dummy AI.

## Known Godot gotchas

- **Script classes:** a C# **node** class must live in a file of the same name, or Godot never
  attaches it and `_PhysicsProcess` silently never runs. Plain C# classes can share files.
- **Unimported assets:** "Unrecognized UID" or "Failed loading resource" before the first
  import means "not imported yet". A new PNG is `null` from `GD.Load` until imported.
- **Committed files:** `.uid` and `.import` sidecars are committed; `.godot/`, `bin/`, `obj/`
  and `build/` are not. Never read or edit `.godot/`.
- **Line endings:** `.gitattributes` pins LF. This machine's git runs `core.autocrlf`, which
  would otherwise check files out with CRLF and break `tools/check.sh` in bash.
- **Positioning UI:** setting `Position` on a centre-anchored Control places it from the
  parent's corner. Centre with a `CenterContainer` instead (see `Battle.Centred`).
- **Shell:** heredocs on this machine mangle quotes. Write generator scripts as files.
- **Music:** both tracks are CC0, credited in `assets/music/CREDITS.md`. Check the licence
  of any new audio on its own page before adding it, and record it there.
