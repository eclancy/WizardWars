# CLAUDE.md — Wizard Wars

Authoritative working notes for agents in this repo. If this file and another doc
disagree, this file wins; fix the other doc.

## Current state (verified 2026-09-07)

- Unity **2022.3.62f3** (`ProjectSettings/ProjectVersion.txt`). The project was
  originally Unity 2018.2.10f1; that upgrade is **done**, not pending.
- Gameplay is **C# under `Assets/Scripts/`**. The UnityScript-to-C# migration is
  **done**: every scene and prefab `m_Script` reference resolves to a C# GUID, and
  no scene or prefab still references a legacy `.js` GUID.
- The legacy `.js` files under `Assets/StandardAssets/Scripts/`,
  `Assets/Resources/ControllerScript.js`, and `Assets/Resources/PlayerStats.js` are
  **dead reference copies**. Unity 2022 does not compile them. Read them to compare
  original behavior; do not treat them as live code, and do not "fix" them.
- Build scenes (`ProjectSettings/EditorBuildSettings.asset`): `Title Screen`,
  `Character Select`, `Stage Select`, `Park`.
- There is no automated test suite. Validate in the Editor using
  `docs/smoke-test-checklist.md`.

## Compile check without opening the Editor

Unity is installed at `C:\Program Files\Unity\Hub\Editor\2022.3.62f3`. With the
Editor **closed**, run:

```bash
bash tools/compile-check.sh
```

It runs Unity in batch mode, reports C# error and warning counts, and exits non-zero
if anything failed. It takes roughly 30 seconds warm. Batch mode needs an exclusive
lock on `Library/`, so it fails while the Editor is open.

Do this after any non-trivial script edit. It catches compile breaks and broken GUID
references, but it cannot catch runtime or gameplay regressions — those still need the
Editor and `docs/smoke-test-checklist.md`.

**With the Editor open**, use the lighter check instead:

```bash
bash tools/syntax-check.sh
```

It runs Unity's own Roslyn over `Assets/Scripts/` against Unity's assemblies without
opening the project, so it needs no `Library/` lock. It reports the same C# errors and
warnings, but it does not import assets, so it cannot catch broken GUID references.

Baseline as of 2026-09-07: **0 errors, 0 warnings**.

## Architecture

- `GlobalHandler.cs` — persistent (`DontDestroyOnLoad`) join/lock/alive/stage state
  for the legacy flow. It mirrors its state into the newer runtime via
  `LegacyGlobalStateBridge`.
- `Assets/Scripts/LocalMultiplayer/` — the newer state layer: `LocalPlayerManager`,
  `MatchManager`, `MatchConfig`, `PlayerSlotState`, `InputProvider`, reached through
  the static `LocalMultiplayerRuntime` and spawned by `LocalMultiplayerBootstrap`.
  Free-for-all works; team mode is intended but not finished
  (`docs/local-multiplayer-roadmap.md`).
- Two systems hold match state at once. When you change join/lock/alive/stage
  behavior, update **both** the `GlobalHandler` field and the bridge sync, or the
  two will drift.
- `ControllerScript.cs` — movement, aiming, targeter, and dispatch to wizard
  attacks. `PlayerStats.cs` — health, energy, HUD bars, burn/stun, death,
  `ultimateCharge`/`TryUseUltimate()`.
- `ParkBattle.cs` — spawns joined players, swaps the stage background by selected
  stage, scatters rock/cactus obstacles, runs the countdown, adds an AI when one
  player joined.
- Wizards: `FireWizard.cs`, `FrostWizard.cs`, `EarthWizard.cs`,
  `ElectricityWizard.cs`. Each exposes `basicAttack`, `secondAttack`, `thirdAttack`,
  `ultimateAttack`, still invoked by `SendMessage`. There is no `fourthAttack`:
  `ControllerScript` still dispatches it on LB, which is now a harmless no-op.
- `ProceduralFx.cs` — runtime-generated sprites (ice shard, flame, ring outline)
  built from `Texture2D` pixels and cached statically. Abilities that are animated in
  code rather than authored as art use these, so they add no files to `Resources/`.
  It also owns `IsInsideSolid`, the shared "is this point in a wall or obstacle" test.

### Abilities animated in code

Ice Wave (`FrostWizard.SpawnIceWave`) and Ring of Fire (`FireWizard.SpawnRingOfFire`)
are no longer single sprites that move. Each is assembled at cast time from many small
objects and driven by a coroutine:

- Ice Wave spawns nine `IceWaveShard` pieces on an arc that sweeps outward, widening
  and fading. All shards of one cast share a hit set so a player is stunned once.
- Ring of Fire places `FireSource` flames straight onto the circle with no telegraph.
  Each grows up out of the ground and stutters as it catches, then cycles the six
  generated flame frames at its own rate and offset. Each source lives independently —
  one that lands in a wall, obstacle, or mud patch is put out by itself and the rest
  keep burning.
- Earthquake is the Earth ultimate: shockwave rings ripple out to `quakeRadius` while
  anyone inside is slowed by `quakeSlow`. The slow is applied and removed exactly once
  per player via a tracked set, because `ControllerScript.slow` is additive.
- Meteor Storm is the Fire ultimate: `meteorStormCount` meteors rain on random points
  inside `meteorStormArea`, several in the air at once. Each is a shadow that tightens
  and turns fiery as it falls, then a blast disc sized to `meteorBlastRadius`, with
  damage interpolating from `meteorCentreDamage` to `meteorEdgeDamage` at the rim. It
  leaves no fire behind, and never damages the caster.
- `FireSource` finds players with `Physics2D.OverlapCircleAll` on a slow tick instead
  of trigger callbacks, so a source needs no collider or `Rigidbody2D`. This means
  trigger-based interactions elsewhere cannot see it: cross-ability reactions that
  used to match the object name `ringOfFire(Clone)` now live inside `FireSource`.
- Both ground patches have a formation telegraph before they take effect.
  `FrostWizard.FormIcePatch` gathers generated puddles inward and glosses them into
  the sheet; `EarthWizard.FormMudPatch` spurts clods of earth up and lets the patch
  spread from where they land. In both cases the prefab is instantiated only at the
  end, and its `PolygonCollider2D` stays disabled while it grows, so the patch cannot
  slip or slow anyone during the telegraph. Final size is set in code
  (`FrostWizard.icePatchScale`, `EarthWizard.mudPatchScale`), not by the prefab.

`iceWave.prefab` and `ringOfFire.prefab` still exist in `Resources/` but nothing loads
them. Leave them there; they are the fallback if a rework needs reverting.

## Controls

Left stick moves, right stick aims, triggers fire basic/second, RB third, X ultimate.
Every wizard has four abilities; the LB (fourth) slot was cut.

Input names carry `_1`..`_4` suffixes and live in
`ProjectSettings/InputManager.asset`; `ControllerScript` picks the suffix from the
clone name. Ultimates are gated by `PlayerStats.ultimateCharge`, and
`ControllerScript.ultimateActive` blocks all other ability input while an ultimate
coroutine runs.

Player 1 can play without a controller: WASD moves, arrow keys aim, and `1`, `2`, `3`
and `5` fire basic / second / third / ultimate; `4` is bound but does nothing.
Menu keys are Enter to join/start, Backspace to leave, Space to confirm. Players 2-4 are controller-only.

Keyboard bindings are added as **extra entries reusing the same axis name** rather than
by editing the joystick entries — Unity allows duplicate axis names and `GetAxis` /
`GetButtonDown` consider all of them. That is why `InputManager.asset` contains several
entries called `L_XAxis_1`, `RB_1`, and so on. Add keyboard support that way; do not
overwrite a joystick binding.

Note the Y-axis polarity trap: Unity joystick vertical axes report **up as -1**, and the
aim code negates `yAim` (`ControllerScript.cs`, targeter position and `Atan2`). A keyboard
axis standing in for a stick therefore needs `invert: 1`, or up and down come out
backwards.

## Resources

Energy is 100 with a 20/s regen. The basic attack is free on a cooldown; the other
abilities cost 20-35 and are bound by their own cooldowns, so energy limits dumping the
whole kit at once rather than limiting the default action. Ability costs are serialized
fields on each wizard (`basicCost`, `ringCost`, `icePatchCost`, and so on).

Ultimate charge is earned: a slow passive trickle plus charge per energy spent and per
damage taken. `PlayerStats.AddUltimateCharge` is the single entry point.

The HUD shows health, energy, an ultimate meter and three cooldown pips. Wizards expose
readiness through `IWizardAbilities`, which `PlayerStats` resolves lazily because
`CharacterSelect` adds the wizard component at runtime. Note that an interface-typed
reference does not get Unity's overloaded `== null`, so a destroyed wizard has to be
detected by casting to `MonoBehaviour`.

Watch the prefab-override trap here. `Player1` and `Player2` pinned `ultimateChargeRate`
while `Player3`, `Player4` and the AI did not, so changing the code default alone would
have given players 1 and 2 a 3.3x faster ultimate than everyone else. After changing any
tuning default, grep the prefabs for that field name.

## Contracts that are not type-checked — preserve them

- Clone names (`Player1(Clone)`), tags (`Player`, `destroyableAttack`), and scene
  object names are load-bearing. Renaming one silently breaks input mapping or
  collision handling.
- `Resources.Load("...")` uses exact prefab/sprite names under `Assets/Resources/`.
- `SendMessage` targets method names by string.
- Serialized field names and asset GUIDs bind scenes and prefabs to scripts. Never
  rename or move a `.meta` file, and never rename a serialized public field without
  updating every scene and prefab that stores it.
- After any script edit, grep for `SendMessage`, `Resources.Load`, and `GameObject.Find`
  call sites that reference what you touched.

## Working style

- Keep changes minimal and local; match the surrounding style.
- Prefer script fixes over scene or prefab rewrites — scene YAML diffs are large
  and easy to corrupt.
- Scene loading uses `SceneManager` in the C# code. Do not reintroduce
  `Application.LoadLevel`.
- Watch operator precedence when editing `if` conditions that mix `&&` and `||`;
  parenthesize.
- Do not commit or edit `Library/`, `Logs/`, `UserSettings/`, or generated
  `.csproj`/`.sln`.
- Unity rewrites `.meta`, scene, and prefab files on import, so re-read a file
  before patching it rather than trusting an earlier read.

## Companion docs

- `CLAUDE_HANDOFF.md` — migration record, ability design intent, generated art
  inventory, open risks.
- `GAME_COMPLETION_PLAN.md` — current milestone plan.
- `AGENTS.md` / `.github/copilot-instructions.md` — same guidance for other agents.
- `docs/` — architecture, input mapping, controller reference, smoke checklist,
  local-multiplayer roadmap.
