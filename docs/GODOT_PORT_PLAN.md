# Wizard Wars: Godot Port Plan

Status: **planned, not started** (2026-10-11).

## Decisions

| Question | Decision |
|---|---|
| Engine | Godot **4.5.1 .NET** (already installed; same as DrawFight and Wizard Survivors) |
| Language | **C#**: `Godot.NET.Sdk/4.5.1`, `net9.0` |
| Location | **`godot/` subfolder of this repo**, beside the Unity project |
| First goal | **Parity, then improve.** Port the current game faithfully. Redesign only after it plays the same. |
| Reuse | **Borrow patterns** from DrawFight and Wizard Survivors by copying them. No shared code and no live dependency. |

Consequences of these choices:

- C# rules out a **web build**. Godot 4 cannot export C# projects to the web. The targets are Windows desktop (and later Linux/macOS).
- The Unity project stays playable and **is the reference** until parity sign-off. Freeze Unity gameplay changes while the port runs, allowing only bug fixes. Otherwise the target keeps moving.

## What "parity" means

The Godot build must do everything in `docs/smoke-test-checklist.md` and match the
tuning recorded in `CLAUDE.md` / `CLAUDE_HANDOFF.md`:

- **Flow:** Title → Character Select → Stage Select → Battle → result → back to Character Select.
- **Players:** 1–4 local players on gamepads, with keyboard for player 1. Join, leave, lock and rematch.
- **Wizards:** 4 wizards × 4 abilities, with every telegraph and interaction:

  | Wizard | Basic | Second | Third | Ultimate |
  |---|---|---|---|---|
  | Fire | Fireball | Ring of Fire | Flamethrower | Meteor Storm |
  | Frost | Icicle | Ice Patch | Ice Wave | Absolute Zero |
  | Earth | Mudball | Mud Patch | Rock Wall | Earthquake |
  | Electricity | Electricity Ball | Lightning Rod | Speed Up | Overcharge |

- **Resources:** 100 energy at 20/s, a free basic attack, 20–35 energy for mid abilities, and an earned ultimate charge.
- **HUD:** health, energy, ultimate meter, three cooldown pips, a red flash when a cast is refused, and team strips.
- **Stages:** 4 stages with their backgrounds, spawn points and obstacles.
- **Modes:** Free-for-All and Teams (friendly fire off, team win banner).
- **Solo:** a computer wizard opponent.
- **Character Select:** status line, per-player status and kit list, how-to-play panel.

### Unity behaviour that is deliberately *not* ported

These are workarounds for Unity-era problems. The port fixes them by design.

| Unity behaviour | Godot replacement |
|---|---|
| Two state systems (`GlobalHandler` + `LocalMultiplayer` bridge, synced by reflection) | One match-state object |
| `SendMessage` by string, `ApplyDamage(int)` with no attacker | A typed `Hit` record (attacker, team, amount, kind) through one damage entry point |
| Additive `slow()` / `removeSlow()` that leak when an effect dies early | Status effects keyed by source, so they expire on their own |
| Prefab-serialized tuning that silently overrides code defaults | Tuning lives only in `.tres` data resources |
| Prefab *assets* mutated at runtime (`AttachWizard` on `basePlayerN`) | A player is built fresh each match from data |
| Per-axis `InputManager` names with `_1`…`_4` suffixes | Poll devices directly by id (DrawFight's `IInputSource`) |
| AI as a separate mover that cannot channel the flamethrower | AI is an input source, so it can do anything a player can |
| The LB "fourth attack" dispatch that does nothing | Not ported |
| The target-dummy fallback and the `OnGUI` HUD | Not ported; HUD uses Control nodes |

## Architecture

### Project layout

The layout follows DrawFight and Wizard Survivors: flat `scripts/` and `scenes/`, with data next to them.

```
godot/
  project.godot  WizardWars.csproj  WizardWars.sln  CLAUDE.md
  assets/
    art/
      wizards/
      attacks/
      stages/
      ui/
    sfx/
    music/
  data/
    wizards/   Fire.tres …
    abilities/ Fireball.tres …
    stages/    Woodland.tres …
  scenes/
    Main.tscn
    Battle.tscn
    Wizard.tscn
    abilities/*.tscn
    ui/*.tscn
    _*.tscn   (debug/probe scenes, prefixed _)
  scripts/
  tools/
```

`.uid` and `.import` sidecars are committed. `.godot/`, `bin/` and `obj/` are ignored.

### Core pieces

- **Screens.** `Main.tscn` has a `GameRoot` that swaps screens in place: Title, CharacterSelect, StageSelect, Battle, Result. This is DrawFight's pattern. There's no scene-to-scene `DontDestroyOnLoad` state.
- **`Session`** (a static class or a single autoload) is the one source of truth. It holds:
  - player slots (device, joined, locked, wizard, team)
  - the mode (FFA or Teams)
  - the selected stage
  - the last result

  It replaces `GlobalHandler`, `LocalPlayerManager`, `MatchManager` and the bridge.
- **Input.** `IInputSource.Poll() → InputState`, where `InputState` holds move, aim, the basic/second/third/ultimate buttons, menu actions and press edges. There are three implementations:
  - `GamepadInputSource(deviceId)`. Godot reports triggers as separate axes, unlike Unity's combined `Triggers` axis.
  - `KeyboardInputSource`, with the current P1 layout: WASD, arrows, 1/2/3/5, Enter, Backspace, Space, Tab, T and H.
  - `CpuInputSource`.

  `ControllerAssignment` hands out pads in connection order and handles hot-plug. All of this is copied from DrawFight.
- **Data resources** use `[GlobalClass]`, a `Resource` base and `[Export]` properties:
  - `WizardData`: id, name, portrait, sprite/`SpriteFrames`, four `AbilityData`.
  - `AbilityData`: id, name, slot, energy cost, cooldown, damage values, radius/range, durations, the scene to spawn, and description text for the kit list.
  - `StageData`: background, four spawn points, Teams spawn grouping, obstacle placements.

  A `ContentValidator` checks them at startup, as in Wizard Survivors.
- **Wizard.** `Wizard.tscn` is a `CharacterBody2D`, so `MoveAndSlide` replaces Unity's transform movement and rigidbody push-back. It has:
  - `Stats`: health, energy, ultimate charge.
  - `StatusEffects`: slow, haste, stun, burn and slide, each keyed by source.
  - `AbilityCaster`: cooldowns, energy checks, the ultimate lock.
  - A team marker.

  It reads one `IInputSource`.
- **Combat.**
  - **Hits:** every hostile effect builds a `Hit` and calls `Combat.Apply(hit, target)`. That one function applies the friendly-fire rule, damage, ultimate charge (it can now credit damage *dealt*) and kill credit.
  - **Collisions:** use named physics layers (wizards, projectiles, ground zones, solids) instead of the `Player` and `destroyableAttack` tags.
  - **Projectile reactions:** when two projectiles meet, they react through an interface rather than by matching clone names.
- **Abilities.** Each ability is a small scene plus a script, configured from its `AbilityData`. Effects Unity built in code (`ProceduralFx`) are ported first: the same pixel generators produce an `ImageTexture`. Particles or shaders can replace them later, once parity is reached.
- **AI.** `WizardBrain` decides, and `CpuInputSource` turns decisions into an `InputState`. It ports `WizardAI`'s behaviour: preferred range, strafing, dodging, ability rules per wizard, reaction delay, aim error. As a plain input source it can also hold the flamethrower button.
- **HUD and menus.**
  - The HUD is a `Control` above each wizard.
  - Banners and panels are themed Control scenes.
  - Menus grab focus so they work with a pad.

## Phases

Each phase ends with a **gate**: something you play or check before the next phase starts.

### Phase 0: Scaffolding (1 session)

- Create `godot/` with:
  - `project.godot`, set to 1920×1080, `canvas_items`/`expand`, 2D gravity 0, named physics layers
  - `.csproj` and `.sln`
  - `godot/.gitignore`, and the root `.gitignore` updated. Its unanchored `[Bb]uild` rule already covers `godot/build`.
- Add `godot/CLAUDE.md`, adapted from Wizard Survivors and DrawFight:
  - Godot comes from `$GODOT_BIN`.
  - `dotnet build` is a compile check, not verification.
  - Run a headless import after adding art.
  - "Unrecognized UID" before the first import is normal.
- Add `godot/tools/check.sh`, which runs:
  1. `dotnet build`
  2. a headless `--import`
  3. a headless run of a `RegressionChecks` scene that prints pass/fail.

  This is the Godot equivalent of `tools/compile-check.sh`.
- **Gate:** an empty project builds and the headless check passes.

### Phase 1: Vertical slice (2–3 sessions)

- Input sources and controller assignment.
- One stage (Woodland) and one wizard (Fire) with only a basic attack.
- 2-player FFA, health and energy HUD, death, and a result screen.
- **Gate (playtest):** movement, aim and shooting feel at least as good as Unity, side by side. This is the cheapest point to adjust core feel (speed, scale, camera), so do it here.

### Phase 2: Combat systems and all kits (4–6 sessions)

- `Hit` / `Combat.Apply`, status effects, energy, ultimate charge, cooldowns.
- All 16 abilities with their telegraphs, port the generated effects, and cross-element interactions:
  - mud puts out fire
  - fire melts ice
  - projectiles cancel each other
  - rock walls block projectiles
- `RegressionChecks` covers the number rules: costs, damage falloff, slow application and release, and no friendly fire.
- **Gate:** every row of the kit table above works with two players.

### Phase 3: Menus and match flow (2–3 sessions)

- Title, then Character Select:
  - join, leave and lock
  - mode toggle and team switch
  - status line and kit list
  - how-to-play panel
- Stage Select and a 3-2-1 countdown.
- Result banners: player win, team win, computer win.
- Rematch.
- **Gate:** the flow section of the smoke checklist with 1, 2 and 4 players.

### Phase 4: Teams and AI (2 sessions)

- Teams spawns, markers and win condition.
- `WizardBrain` and `CpuInputSource`, including the Fire AI using the flamethrower.
- **Gate:** a 2v2 Teams match, and a solo match against each AI wizard.

### Phase 5: Stages, audio, export (1–2 sessions)

- All four stages from `StageData`, with SFX and music.
- A Windows export preset. The exact mono export templates (about 1.2 GB) go in `%APPDATA%\Godot\export_templates\4.5.1.stable.mono\`.
- **Gate:** the exported `.exe` runs the full loop on a machine without Godot.

### Phase 6: Parity sign-off and Unity retirement (1 session)

- Run the whole smoke checklist against both builds side by side and record any differences.
- When it passes:
  1. Tag the last Unity commit `unity-final`.
  2. Remove the Unity project (`Assets/`, `ProjectSettings/`, `Packages/`, the Unity tools and docs) on a branch.
  3. Optionally promote `godot/` to the repo root.
  4. Rewrite the root `CLAUDE.md` and `README.md`.
- Only *then* start the "improve" list.

Rough total: **13–18 working sessions**. Phase 2 is the bulk and the least predictable.

## Asset migration

- **Copy, don't link.** Godot can only import files inside the project, so every asset is copied from `Assets/` into `godot/assets/`. This temporarily duplicates about 45 MB in the repo, until Unity is retired. Copy only what the game uses. Skip the unused images from cut abilities (`PhoenixRebirth`, `MeteorStrike`, `DeepFreeze`, `ChainLightning`, `FortressRing`, `BlizzardField`) and the old prefabs nothing loads (`iceWave.prefab`, `ringOfFire.prefab`).
- **Sprites:**
  - Wizards are currently single static images.
  - Four unused animated sheets exist: `Resources/*WizardSpritesheet.png`. Treat animating the wizards as an *improvement*, not parity.
  - The five Unity `.anim` clips (fireball, mudball, electricity ball, rod shock, ice wave) become `SpriteFrames`. The sheets are sliced with their `spriteMode: 2` metadata as a guide.
- **Scale:** Unity's Park camera shows about 21 × 11.9 world units, and wizards are scaled to 0.15. Choose one Godot pixels-per-unit (for example 90 px per Unity unit at 1080p) and convert every position, radius and speed through it. Put the factor in one constant, so tuning values can be copied straight from the Unity docs.
- **Music licensing:** `The_Final_Confrontation_-_Castle_Crashers.wav`, `Gunz The Second Duel OST- Battle Theme 1.mp3` and `DST-DawnOfWar.mp3` are commercial soundtracks. They're fine for private play, but **must be replaced before any public release** (itch.io, Steam, a public GitHub release). Port them for parity, and list this as a release blocker.

## Risks and how to handle them

| Risk | Mitigation |
|---|---|
| Feel drifts from Unity: speed, scale, deadzones, hitboxes | Phase 1 playtest gate, one units constant, deadzones copied from DrawFight |
| Two codebases drift during the port | Freeze Unity gameplay changes except bug fixes |
| Generated effects look different | Port the `ProceduralFx` generators first, restyle later |
| Gamepad differences: Godot triggers are separate axes; Windows has a 4-pad limit; button layouts differ | DrawFight's `GamepadInputSource` and `GamepadLayout`, plus a `_PadProbe` debug scene |
| `dotnet build` passes but scenes are miswired | The headless `RegressionChecks` run in `tools/check.sh`, and an Editor playtest at every gate |
| Repo bloat from duplicated assets | Temporary; removed at Unity retirement |
| No web build with C# | Accepted. Revisit only if web becomes a goal: port to GDScript or wait for Godot C# web support. |
| The Unity license has expired, so the Unity compile check is down | Sign in to Unity Hub again so the reference build stays runnable during the port |

## After parity: the "improve" list (not scheduled)

- Animated wizards from the existing spritesheets.
- Damage-dealt ultimate charge, now possible because `Hit` carries the attacker.
- Teams kept across a rematch.
- An AI difficulty setting.
- Settings screen (audio volume), controller remapping.
- Replacement music with a clear license.
- Particles and shaders for the generated effects.
