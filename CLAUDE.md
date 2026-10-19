# CLAUDE.md — Wizard Wars

Authoritative working notes for agents in this repo. If this file and another doc disagree,
this file wins; fix the other doc.

Wizard Wars is a 1–4 player local arena battler in **Godot 4.5 C#**. It was a Unity game
until 2026-10-11. The Unity project is gone from this branch; it survives at the git tag
**`unity-final`**. Check out that tag to see how something worked in Unity. Never restore
Unity files here. `docs/GODOT_PORT_PLAN.md` records how and why it was ported.

## Status

The game is **feature-complete**. It has:
- the title screen (the START painted into the art flashes) and Character Select: join by
  device, one player per wizard, Free-for-All/Teams (Green vs Pink), team switching, kit
  lists, how-to-play
- Stage Select and six stages, each with its own cover to hide behind; the Volcano (lava
  and bridges) and the Swamp (bog pools) have hazards
- matches of best of 1, 3 or 5 rounds with a scoreboard between rounds, and a computer
  opponent on Easy, Normal or Hard (both set on Stage Select)
- a pause menu (Start: resume, rematch, options, quit), options (volume, fullscreen,
  rumble; saved) and controller rumble
- all four wizards with all 20 abilities and their telegraphs: basic (RT), second (LT),
  third (RB), fourth (LB) and ultimate (X), each ultimate announced by a build-up
- the HUD and a computer opponent
- the countdown, results, rematch, and a Windows export

Every automated check passes.

**Open:**
- Human playtesting: `docs/playtest-checklist.md`.
- Tuning beyond the first pass. On 2026-10-13 health went from 100 to 250 and ultimate charge
  was raised, because fights ended in about 10 s, before anyone's ultimate filled. After the
  fourth abilities came back, bolts stopped cancelling each other and ultimates gained their
  build-up (2026-10-15), computer duels last about 25 s (2.6 ultimates) and four-way matches
  about 28 s (5.6), with ultimates in every match. Measure any balance change with
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
5. `--abilities`: `AbilityTest` casts all 20 abilities at an enemy (each must do its job) and
   at a teammate (each must do nothing), on open ground (Woodland without its cover).
   Afterwards the arena must be empty.
6. `--soak`: `SoakTest` plays computer-only matches, Free-for-All (six, one per stage) and
   Teams, faster than real time. After each one, no effect node, modifier or ultimate lock
   may be left behind. On a hazard stage it also reports the lava damage dealt.

Run the steps after `dotnet build` with
`--headless --fixed-fps 60 --path . res://scenes/Main.tscn -- <flag>`.

`dotnet build` alone is **not** verification. A change to feel or visuals also needs a look:

```bash
"$GODOT_BIN" --path . res://scenes/Main.tscn -- --screen=demo --shot=C:/tmp/shot.png --shot-after=8
```

`--shot` forces a 1920×1080 window, because the game otherwise starts fullscreen at the
monitor's resolution. `--shot-size=WxH` picks another window shape: check a layout change at
`1620x1080` (3:2) as well as 16:9.

`--screen` takes `title`, `select`, `stage`, `battle`, `pause`, `options` or `demo` (four
computer wizards; add `--teams` for 2v2, `--stage=0`..`5` to pick the arena, and `--charged`
to start everyone with a full ultimate). `select` also takes `--teams`. Godot's own
`--time-scale 4` (before `--`) fast-forwards a shot run, e.g. to catch a round result.

`--make-title-glow` rebuilds `assets/art/ui/title-start-glow.png`, the mask of the START
letters the title flashes, from `title.png` (`TitleGlowTool`). Rerun it if the title art
changes, and update `TitleScreen.GlowOrigin` with the position it prints.

`--make-icon` rebuilds `icon.png` and `icon.ico` (`IconTool`): a "WW" cut from the title art,
standing behind a fireball, turned slightly toward it and lit by it, on a dark backdrop with a
ring of runes. `--make-stages` repaints the Frost Tundra, Scorched Canyon, Storm Plateau,
Volcano and Swamp floors and paints the bush-like tufts out of Woodland's (`StageArtTool`). `--make-sounds`
synthesises the newer sound effects (ultimate charge and release, ground crack, ice break and
chip) into `assets/sfx` (`SoundTool`). All four write committed files; run them on purpose,
then a headless `--import`.

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
  runtime and the bridge between them. A wizard can be locked in by one player at a time
  (`Session.LockedOn`, `NextFree`): Character Select greys a taken card and cursors skip it.
- **Colour means wizard or team, never player.** Players are only numbered (white P1-P4
  tags). The colour that marks a player is their wizard's element colour, which is unique
  because wizards are. Teams are Green and Pink (`Teams`), colours no wizard wears. Don't
  give players colours of their own again: Unity's red, blue, green and yellow players
  clashed with Fire, Frost, Earth and Electricity.
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
  movement, aim, and ability dispatch, and reads one `IInputSource`. A stun is `Frozen` (an ice
  block: each change of stick direction breaks off `WiggleBreak` seconds, and a `WiggleHint`
  joystick says so; the AI wiggles too) or `Staggered` (Tremor: the wizard just shakes).
- **Abilities** are plain C# (`Ability`, `Ultimate`, built by `AbilityFactory` from the
  ability id). Each wizard has five, in slots named by `WizardData.BasicSlot`..`UltimateSlot`
  (use the names, never bare indices: the ultimate moved from 3 to 4 when the fourth came
  back). What they create is an **effect node** owned by the battle, not the wizard:
  `Projectile`, `FireSource`, `FlameJet`, `Meteor`/`MeteorStorm`, `IcePatch`/`MudPatch`
  (`GroundPatch`), `IceWave`, `Aura` (Absolute Zero, Earthquake), `RockWall`,
  `LightningRod`, `Overcharge`, `SpeedTrail`, and for the fourths `DeepFreeze`, `Tremor`,
  `ChainLightning`/`Bolt` (Meteor Strike reuses `Meteor`). Effects outlive their caster
  correctly and clean up after themselves.
- **Ultimates build up.** `Ultimate.TryCast` first adds an `UltimateBuildUp`
  (`UltimateBuildUp.Duration`, 1.2 s): the floor darkens, the ultimate's name pops up, the
  caster's colour pours in. Only then does `Cast` run, so nothing is hurt or hindered during
  it (`AbilityTest` checks). If the caster dies first, the ultimate never happens. Each one
  then has its own look: Meteor Storm reddens the sky, Absolute Zero spreads a frost sheet
  with a crystal rim (`FrostFx`), Earthquake splits the ground (`QuakeFx` and
  `GroundCracks`, which Tremor shares), and Overcharge crackles. `Battle.Shake` jolts the
  camera for impacts.
- **Fourth abilities (LB)** are the Unity originals cut on 2026-09-25 and restored on
  2026-10-15 (`FourthAbilities.cs`): Meteor Strike, Deep Freeze, Tremor (the Unity
  fourth-slot Earthquake, renamed because Earthquake is the ultimate) and Chain Lightning.
  Same ideas, numbers brought into the 20-35 energy range, and a cooldown of their own;
  `gen-data.sh` records the Unity originals.
- **Screen shapes:** stretch aspect `expand` makes the view taller than 1080 on 16:10 or 3:2
  and wider on 21:9. Menus lay out in a centred 1920×1080 `DesignFrame`; full-bleed parts
  (backdrop, corner labels) anchor to the screen itself. `GameRoot.Swap` gives every screen
  full-rect anchors before it enters the tree. In battle the floor is drawn at a fixed scale
  covering 2560×1440 (`Battle.CoveredView`), so the art sits in the same place on every
  screen, and the floor beyond the arena walls is dimmed.
- **Stage cover:** each stage's `CoverKind` and `Cover` positions place `Cover` pieces
  (stumps, ice crystals, saguaro cacti, rune obelisks each carved with a different Elder
  Futhark rune, basalt boulders and toadstools) drawn by `CoverArt` in the flat cartoon
  style. Cover is solid: it stops wizards, projectiles and flames. `Battle` is y-sorted, so a
  wizard behind a piece is drawn behind it. The AI does not fire into cover
  (`Battle.HasLineOfSight`).
  - **Rule: only solid things look solid.** Every cover piece stands on a hard shadow, and
    nothing else in an arena has one. Floors are plain ground with no painted objects. Don't
    add decoration that looks like it could block.
  - Keep cover small (about 1.5 units) and at least 3 units apart (`RegressionChecks`), and on
    Woodland keep it off the painted path.
- **Hits use geometry, not physics callbacks.** Hit tests are circle against body box,
  ellipse for patches. Physics is only for walking into walls, cover, rock walls and rods.
  That keeps headless runs identical and removes collision masks as a source of bugs.
  Projectiles pass through each other.
- **Menus** share `Ui` (heading and body fonts from the system, rounded panels, pills) and
  `MagicBackdrop` (a banded night sky with drifting sparkles). `GameRoot` fades each new
  screen in from black.
- **Matches and rounds:** `GameRoot.ShowBattle` starts a `Match` (plain C#: rounds, wins per
  side, winner) from `Session.RoundsToWin`; each round is a `Battle` with `Battle.Match` set,
  which records its result and shows the round banner and scoreboard. A drawn round counts
  for nobody, and `Match.MaxRounds` stops endless draws. Tests and the soak build `Battle`s
  with no match, which behave as single battles.
- **Pausing:** `GameRoot` runs with `ProcessMode.Always`, so it keeps polling devices; `Battle`
  is `Pausable`. Start from any fighter's device opens `PauseMenu` (always-processing) and
  pauses the tree; `GameRoot.Swap` always unpauses. `OptionsPanel` works on top of either the
  pause menu or Character Select (X), and `Settings` saves to `user://settings.cfg`, but only
  for a real run: tests and shots keep the defaults (`Settings.Persist`).
- **Rumble:** `Wizard.Rumble` buzzes the wizard's pad (if it is on one and rumble is on) for
  hits, freezes, jolts, ultimates and knock-outs. A weaker buzz never cuts a stronger one
  short.
- **Hazards:** `StageData` holds `HazardKind` ("lava" or "bog"), areas and bridges as
  centre and half-size pairs, and `InHazard`. `StageHazard` draws them live and applies them
  to feet: lava hurts (`HazardDamage` per second) and sets you burning, and bog slows and
  stops regen (a modifier keyed to the hazard). The AI steers out of and around hazards
  (`CpuInputSource.AvoidHazard`), and when lava lies between it and a target that is out of
  range or out of sight, it walks over the bridge with the shortest route
  (`CpuInputSource.Route`, tested in `RegressionChecks`). The soak reports bridge crossings. `HazardPreview` draws them on Stage Select tiles.
- **Computer skill:** `CpuInputSource(Difficulty)` sets reaction, aim error, dodging, how
  often it thinks, how readily it spends abilities, and how fast it wiggles out of ice.
  Normal is the default constructor, which the soak measures.
- **Friendly fire:** `Combat.CanAffect(caster, casterTeam, target, hitsCaster)`. Effects
  record the caster's team when cast. Patches and rods hit their own caster, as in Unity.
  Teammates are never hit by anything. Any new hostile effect must use this check.
- **Status effects:** `StatusEffects` holds modifiers (slow, haste, no-regen, slide) keyed
  by the source node. `Prune` drops any whose source was freed, so effects cannot leak the
  way Unity's additive `slow()`/`removeSlow()` did.
- **`ProceduralFx`** is Unity's pixel generators ported (rows flipped, because Godot images
  are top-down), plus `AreaDisc`.
- **`Units`:** every Unity distance or position goes through `Units.U(...)` (90 px per Unity
  unit; Unity +y is Godot up). `Units.SpriteScale` converts a Unity transform scale for the
  100-ppu art.

## Deliberate differences from the Unity version

- **Not a leak or a bug:** status effects cannot leak, a big slow can never run you
  backwards, and a stun no longer hides an ongoing burn.
- **New feedback:**
  - A hit flashes the wizard red.
  - P1–P4/CPU labels sit over each wizard.
  - Ultimates build up before they go off, and show their real reach: Absolute Zero's frost
    sheet and Earthquake's dashed edge. Unity's overlay image ended up a third of a unit wide.
  - The HUD drops below a wizard standing at the top wall.
- **Projectiles keep the last aim direction** when the stick is released (Unity always fired
  right). Placed abilities still land at your feet with the stick released, as in Unity.
- **The computer can hold the flamethrower.** It has no targeter shown.
- **Rematch is faster:** players stay joined and locked after a match, so Start goes again.
  Unity made everyone rejoin.
- **Stage Select:** any joined player can drive it. Unity picked one at random.
- **Bolts pass through each other.** In Unity every basic attack cancelled every other.
- **Meteors fall straight down.** Unity spun the meteor art, which tilted its tail.
- **Each stage has its own cover, and Unity's decorative rocks and cacti are gone.** They
  looked solid and blocked nothing.
- **Teams are Green and Pink**, not Red and Blue, and players have no colours of their own.
- **One player per wizard.** A locked-in wizard is taken until its player changes their mind.
- **Frozen wizards can wiggle free**, and Tremor's stagger shakes rather than freezes.- **Not ported:** Unity's GlobalHandler/bridge split and the target-dummy AI.

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
  of any new audio on its own page before adding it, and record it there. Prefer synthesising
  new sound effects in `SoundTool`: nothing to license.
