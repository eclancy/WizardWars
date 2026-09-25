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

The legacy input names use suffixes `_1` through `_4`. Player 1 has keyboard bindings for every ability (`1`-`5`), defined in `ProjectSettings/InputManager.asset` and shown by the Title Screen help text in `StartToPlay.cs`. The old `J K E Q R` aliases were removed.

The aim reticle now hides when the right stick is neutral and reappears when the player aims.

## Ability Design

Every wizard has exactly four abilities. The LB "fourth" slot was cut: three of the
four fourth abilities were the worst in the game (Earthquake dealt no damage, Chain
Lightning dealt 9 for 85 energy), and they all shared a cooldown flag with the third.
`ControllerScript` still dispatches `fourthAttack` on LB; nothing receives it.

Ultimates are gated by `PlayerStats.ultimateCharge`:

- Charges over time at `ultimateChargeRate` (currently 4 per second).
- Starts unavailable.
- `TryUseUltimate()` consumes the charge.
- `ControllerScript.ultimateActive` blocks all other ability input while an ultimate coroutine runs.

Current kits:

| Wizard | Basic | Second | Third | Ultimate |
|---|---|---|---|---|
| Fire | Fireball | Ring of Fire | Flamethrower | Meteor Storm |
| Frost | Icicle | Ice Patch | Ice Wave | Absolute Zero |
| Earth | Mudball | Mud Patch | Rock Wall | Earthquake |
| Electricity | Electricity Ball | Lightning Rod | Speed Up | Overcharge |

- Fire ultimate: Meteor Storm rains `meteorStormCount` meteors on random points inside
  `meteorStormArea`, several in the air at once, damage falling off from
  `meteorCentreDamage` to `meteorEdgeDamage`. It leaves no fire.
- Frost ultimate: Absolute Zero slows enemies and disables their energy recharge; no stun.
- Earth ultimate: Earthquake shakes the ground around the caster and slows anyone inside
  `quakeRadius` by `quakeSlow` for as long as they stay in it. The caster is not slowed.
- Electricity ultimate: Overcharge rapidly fires electricity balls and randomly places
  lightning rods around the caster. Those rods self-destroy after 2.5 seconds.

The fire wizard is immune to his own fire. Projectiles and the flamethrower rely on
`Physics2D.IgnoreCollision` against the caster; Ring of Fire's sources and the meteor
blasts find targets with overlap queries, which ignore that, so they skip the caster
explicitly.

Cut and not replaced: Phoenix Rebirth, Fortress, Deep Freeze, Chain Lightning, and the
old single-target Meteor Strike. Their generated PNGs remain in `Assets/Resources/`
(`PhoenixRebirth`, `MeteorStrike`, `DeepFreeze`, `ChainLightning`, `FortressRing`,
`BlizzardField`) but nothing loads them.

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

## Abilities Reworked as Generated Animations

Ice Wave and Ring of Fire were converted from single authored sprites into effects
that are generated and animated in code, in the same spirit as the ultimates.

- `ProceduralFx.cs` builds their art at runtime from `Texture2D` pixels — an ice
  shard, a flame blob, and a ring outline — and caches each one. No new PNGs.
- Ice Wave (`FrostWizard.SpawnIceWave`) is an arc of nine shards that sweeps outward,
  widens, and fades, replacing the sprite that used to slide across the arena.
- Ring of Fire (`FireWizard.SpawnRingOfFire`) places twelve independent `FireSource`
  flames around the circle, each growing out of the ground where it lands. A source
  that lands in a wall, an obstacle, or a mud patch is put out on its own while the
  rest of the ring keeps burning.
- Ring behaviour is tunable on `FireWizard`: `ringRadius`, `ringSourceCount`,
  `ringLifetime`, `ringBurnDuration`, `ringDamagePerTick`.
- `iceWave.prefab` and `ringOfFire.prefab` remain in `Resources/` but are unloaded.

Still to validate in the Editor: ring spacing against obstacles on stages 2-4, and
whether twelve sources feel right.

## Ground Patch Telegraphs

Ice Patch and Mud Patch now animate into existence instead of appearing instantly,
and neither affects anyone until its formation finishes.

- Ice: six generated puddles seep out around the target, slide together, and the
  sheet glosses over from wet blue to plain ice (~0.83s total).
- Mud: eight clods of earth spurt up out of the ground on parabolic arcs, land
  around the point, and the patch spreads outward from there (~0.75s total).
- The prefab is instantiated only at the end of the spurt/gather, and its
  `PolygonCollider2D` is disabled while it grows, so the telegraph is honest.
- The ice patch is smaller than before: `FrostWizard.icePatchScale` is 0.95 where the
  prefab is authored at 1.5. Mud is unchanged at `EarthWizard.mudPatchScale` 1.5.

The ice patch expires. `IcePatch` self-destructs `lifetime` seconds after
`beginLife()` (2s, started when the gloss finishes so the telegraph does not count
against it), and releases any player still standing on it first — destroying a trigger
does not run its own `OnTriggerExit2D`, so without that a player would stay stuck
sliding for the rest of the match. `FrostWizard.Second` still broadcasts `notSliding`
to every player a second later; that is now a redundant safety net rather than the
only cleanup. Mud patches self-destruct after 3s as they always did.


## Fire Rework (current)

- The flame is a six-frame generated loop with a full, broad silhouette and a fully
  opaque body. An earlier version used a square-root envelope with fast waves, which
  broke the flame into thin tongues that were mostly soft edge and read as translucent
  no matter how opaque the pixels were. Transparent pixels carry the flame's colour so
  bilinear filtering cannot fringe the edges toward transparent black.
- Ring of Fire has no telegraph. Sources are placed immediately and each grows out of
  the ground over 0.4s, stuttering as it catches.
- Meteor Strike leaves no fire. Damage runs from `meteorCentreDamage` (45) at the
  impact point down to `meteorEdgeDamage` (8) at `meteorBlastRadius` (3).
- `ProceduralFx.RingOutline` is no longer used by anything now that the ring telegraph
  is gone. It is a lazily built generator, so it costs nothing while unused.

## Resource Rebalance

The single energy pool used to throttle the basic attack (33 energy against a 0.3s
cooldown, so energy bound it by 5.5x) while every other ability was cooldown-bound.
Three basics emptied the bar, and it took five to kill — so you could not finish a kill
on one bar, and an empty bar left you with no action at all.

- **The basic attack is free**, on a `basicCooldown` of 0.55s. You always have an
  action; a dry bar costs you options, not agency.
- **Mid abilities dropped to ~20-35** so energy budgets them instead of denying them:
  Ring of Fire 50 to 35, Ice Patch 50 to 30, Ice Wave 66 to 35, Mud Patch 50 to 30,
  Rock Wall 25 to 20, Speed Up 30 to 25, Lightning Rod unchanged at 30. Costs are
  serialized fields on each wizard so they can be tuned in the Inspector.
- **The ultimate is earned, not waited out.** `ultimateChargeRate` dropped from 4/s to
  1.2/s, and charge is now also granted by `ultimateChargePerEnergy` (0.3 per energy
  spent) and `ultimateChargePerDamageTaken` (0.3 per damage). Active play reaches an
  ultimate in roughly 30s; standing still takes over 80s. Taking damage charging it
  gives the losing player a way back in.

Damage *dealt* does not grant charge. `ApplyDamage` is invoked by `SendMessage` with no
attacker reference, so crediting the dealer would mean threading a source through every
attack call site. Energy spent is used as the aggression proxy instead.

## HUD

The bar block above each wizard is now three rows in 28px instead of two in 20px:
health, energy, and a gold ultimate meter that flashes white when ready. The energy bar
flashes red for 0.35s whenever a cast is refused for want of energy — previously
`DepleteEnergy` returned false and the ability silently did nothing, which is the main
reason it was unclear when a spell could be cast.

Three cooldown pips sit under the bars, one each for the basic, second and third
abilities (the ultimate has the gold meter). Readiness comes from the wizard through
`IWizardAbilities.IsAbilityReady`; the pip fill rate is learned by timing how long the
ability stayed unavailable last time, so no ability has to report its own cooldown
length. The first use of each ability fills from a 4s guess and self-corrects after
that, and a pip never reads ready early because readiness is always the wizard flag.
