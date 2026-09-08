# Wizard Wars Handoff

## Project

- Legacy Unity project upgraded toward Unity `2022.3.62f3`.
- Original project version was Unity `2018.2.10f1`.
- Gameplay was originally UnityScript `.js`; Unity 2022 cannot load those components.
- The migration is now primarily C# under `Assets/Scripts/`.
- Do not commit or edit generated `Library/` files.

## Important Instructions

Read `AGENTS.md` and `.github/copilot-instructions.md` before changing gameplay.
Preserve prefab names, scene names, tags, clone names, Resources paths, serialized field names, and asset GUID contracts.
Do not casually rename Unity assets or move `.meta` files.
The project has no automated Unity build/test pipeline. Validate in Unity Editor and run the smoke checklist in `docs/smoke-test-checklist.md`.

## Completed Migration

C# replacements and relinks were created for:

- Core: `GlobalHandler`, `ControllerScript`, `PlayerStats`
- Menus: `StartToPlay`, `CharacterSelect`, `StageSelect`
- Battle: `ParkBattle`
- Wizards: `FireWizard`, `FrostWizard`, `EarthWizard`, `ElectricityWizard`
- Basic projectiles: `Fireball`, `Icicle`, `Mudball`, `ElectricityBall`
- Effects: `IcePatch`, `MudPatch`, `RingOfFire`, `IceWave`, `RockWall`
- Other attacks: `FlameThrower`, `LightningStaff`, `IceCubeIgnoreCollision`
- Cursor scripts: `Player1MoveCursor`, `Player2MoveCursor`, `Player3MoveCursor`, `Player4MoveCursor`

The player prefabs, Character Select, Stage Select, Park, Title Screen, and Resources attack prefabs were relinked during the migration.

## Current Controls

`ControllerScript.cs` routes combat input as follows:

- Left stick: movement
- Right stick: aim
- Triggers: basic and second attacks
- RB: third attack
- LB: fourth attack
- X: ultimate

The legacy input names use suffixes `_1` through `_4`. `X_1` has keyboard fallback `R`; existing keyboard attack fallbacks are documented in `ProjectSettings/InputManager.asset` and the Title Screen help text in `StartToPlay.cs`.

The aim reticle now hides when the right stick is neutral and reappears when the player aims.

## Ultimate / Fourth Ability Design

Ultimates are gated by `PlayerStats.ultimateCharge`:

- Charges over time at `ultimateChargeRate` (currently 4 per second).
- Starts unavailable.
- `TryUseUltimate()` consumes the charge.
- `ControllerScript.ultimateActive` blocks all other ability input while an ultimate coroutine runs.

Current intended ability mapping:

- Fire X ultimate: Phoenix Rebirth, heals over time and damages nearby enemies.
- Fire LB fourth: targeted Meteor Strike, telegraphed before damage and burn.
- Frost X ultimate: Absolute Zero, slows enemies and disables their energy recharge; no stun.
- Frost LB fourth: Deep Freeze, targeted long-duration stun.
- Earth X ultimate: Fortress, four deliberate walls around the caster, then collapse damage.
- Earth LB fourth: Earthquake, area movement-stun/knockup approximation.
- Electricity X ultimate: Overcharge, rapidly fires electricity balls and randomly places lightning rods around the caster. Rods self-destroy after 2.5 seconds.
- Electricity LB fourth: Chain Lightning, hits up to three nearest opponents.

## Current Visual Assets

Generated PNG art exists in `Assets/Resources/` for:

- `PhoenixRebirth.png`
- `AbsoluteZero.png`
- `Fortress.png`
- `Overcharge.png`
- `MeteorStrike.png`
- `DeepFreeze.png`
- `Earthquake.png`
- `ChainLightning.png`

These have Sprite `.meta` files and are loaded by wizard scripts. The AOE visuals pulse during active durations. Meteor Strike and Deep Freeze display a targeted marker/visual.

## Stage Art

Generated stage backgrounds exist in `Assets/Textures/Stages/`:

- `frost-tundra.png`
- `scorched-canyon.png`
- `storm-plateau.png`

They were regenerated at `2044x1196` to match `woodland-arena2.png`.

Stage Select mapping:

- Stage 1: original woodland art
- Stage 2: Frost Tundra
- Stage 3: Scorched Canyon
- Stage 4: Storm Plateau

`ParkBattle.cs` swaps the Park background sprite based on selected stage.

Generated obstacle assets:

- `Assets/Resources/RockObstacle.png` and `RockObstacle.prefab` with a CircleCollider2D
- `Assets/Resources/CactusObstacle.png` and `CactusObstacle.prefab` with a PolygonCollider2D

Earth walls were removed from Park stage spawning. Park now places sparse rock/cactus obstacles instead.

## Known Risks / Next Checks

1. Open Unity 2022.3.62f3 and let it reimport all new PNGs and prefabs.
2. Clear Console, enter Play mode, and test the complete flow:
   Title Screen -> Character Select -> Stage Select -> Park.
3. Verify X, LB, RB, trigger attacks, ultimate charge, and ultimate lockout with one player.
4. Test all four stages and confirm each has the correct background art.
5. Confirm Fortress has exactly four walls: left, right, top, bottom.
6. Confirm Overcharge rods spawn randomly and disappear over time.
7. Confirm Meteor Strike and Deep Freeze targeted visuals are visible and their effects occur at the target point.
8. Verify no `Missing (Mono Script)`, broken GUID, or `Could not extract GUID` errors remain.
9. Check whether generated visual GameObjects should use sorting layers/order explicitly; current code uses `SpriteRenderer.sortingOrder` only for targeted indicators in some places.
10. Review gameplay balance and refine ultimate timings, damage, mana costs, and charge rate after a real playtest.

## Multiplayer Mode Intent

The game should eventually support both:

- Free-for-All as the default mode.
- Teams with shared win/loss, friendly-fire disabled, and team-aware victory conditions.

This mode should live in explicit match state and drive friendly-fire rules, win condition, and UI. See `docs/local-multiplayer-roadmap.md`.

## Critical Files

- `Assets/Scripts/ControllerScript.cs`
- `Assets/Scripts/PlayerStats.cs`
- `Assets/Scripts/GlobalHandler.cs`
- `Assets/Scripts/CharacterSelect.cs`
- `Assets/Scripts/StageSelect.cs`
- `Assets/Scripts/ParkBattle.cs`
- `Assets/Scripts/FireWizard.cs`
- `Assets/Scripts/FrostWizard.cs`
- `Assets/Scripts/EarthWizard.cs`
- `Assets/Scripts/ElectricityWizard.cs`
- `ProjectSettings/InputManager.asset`
- `Assets/Scenes/Title Screen.unity`
- `Assets/Scenes/Character Select.unity`
- `Assets/Scenes/Stage Select.unity`
- `Assets/Scenes/Park.unity`

## Caution About History

This worktree contains many pre-existing and migration-related uncommitted changes. Do not reset or revert unrelated edits. Inspect current files before patching because Unity or formatters may have rewritten `.meta`, scene, or prefab data.
