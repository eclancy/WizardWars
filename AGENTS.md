# Agent Instructions for Wizard Wars

See `CLAUDE.md` for the full working guide. This file is the short version and must
stay consistent with it.

## Project Shape
- Unity **2022.3.62f3** (`ProjectSettings/ProjectVersion.txt`). The project began on
  Unity 2018.2.10f1; that upgrade is complete.
- Gameplay code is **C# under `Assets/Scripts/`**. The UnityScript-to-C# migration is
  complete: no scene or prefab still references a legacy `.js` GUID.
- The remaining `.js` files (`Assets/StandardAssets/Scripts/`,
  `Assets/Resources/ControllerScript.js`, `Assets/Resources/PlayerStats.js`) are dead
  reference copies that Unity 2022 does not compile. Read them for original behavior;
  do not treat them as live code.
- There is no automated test suite. Validate in the Unity Editor and use
  `docs/smoke-test-checklist.md`. To compile-check without the Editor, run
  `bash tools/compile-check.sh` with the Editor closed, or `bash tools/syntax-check.sh`
  while it is open.

## Key Files and Flow
- Build scenes in `ProjectSettings/EditorBuildSettings.asset`: `Title Screen`,
  `Character Select`, `Stage Select`, `Park`.
- `Assets/Scripts/GlobalHandler.cs` holds persistent match and player-selection state
  (`DontDestroyOnLoad`) and mirrors it into the newer runtime through
  `LegacyGlobalStateBridge`.
- `Assets/Scripts/LocalMultiplayer/` holds the newer state layer (`LocalPlayerManager`,
  `MatchManager`, `MatchConfig`, `PlayerSlotState`, `InputProvider`), reached via the
  static `LocalMultiplayerRuntime`. Both systems hold match state, so join/lock/alive/
  stage changes must update the `GlobalHandler` field **and** the bridge sync.
- Menu flow: `StartToPlay.cs`, `CharacterSelect.cs`, `StageSelect.cs`.
- `ParkBattle.cs` spawns joined players, swaps the stage background, scatters
  obstacles, runs the countdown, and adds an AI when only one player joined.
- `ControllerScript.cs` handles movement, aiming, targeter, and attack dispatch.
  `PlayerStats.cs` handles health, energy, HUD bars, burn/stun, death, and ultimate
  charge.
- Wizards live in `FireWizard.cs`, `FrostWizard.cs`, `EarthWizard.cs`,
  `ElectricityWizard.cs`; each exposes `basicAttack`, `secondAttack`, `thirdAttack`,
  and `ultimateAttack`, still called via `SendMessage`. `fourthAttack` was cut from all
  four wizards; the LB dispatch in `ControllerScript` is now a no-op.

## Unity Conventions
- Keep `.meta` files with their assets. Do not rename, move, or delete Unity assets,
  scenes, prefabs, textures, sounds, or `.meta` files unless the task requires it.
- Prefer editing scripts and project settings directly; let Unity regenerate
  `Library/`, `Temp/`, `Logs/`, and imported asset caches. Never commit those.
- Many prefab and attack references load by exact string name through
  `Resources.Load(...)`, so resource and prefab names are runtime behavior.
- Clone names such as `Player1(Clone)`, tags such as `Player` and `destroyableAttack`,
  scene object names, and serialized field names are load-bearing contracts. Preserve
  them unless you update every caller and every scene/prefab reference.
- Scene loading uses `SceneManager`. Do not reintroduce `Application.LoadLevel`.

## Input and Multiplayer
- Input names live in `ProjectSettings/InputManager.asset`, duplicated per player with
  `_1` through `_4` suffixes: `Start_n`, `A_n`, `B_n`, `X_n`, `LB_n`, `RB_n`,
  `L_XAxis_n`, `L_YAxis_n`, `R_XAxis_n`, `R_YAxis_n`, `Triggers_n`.
- Left stick moves, right stick aims, triggers fire basic/second, RB third, X ultimate.
  Every wizard has four abilities; the LB (fourth) slot was cut.
- `ControllerScript.cs` derives the input suffix from the instantiated clone name. If
  you add players or change clone names, update the input mapping and global state
  together.

## Editing Guidance
- Keep changes minimal and consistent with the style of the file you touch. Avoid
  broad refactors unless asked.
- Watch operator precedence in `if` conditions that mix `&&` and `||`; parenthesize.
- When changing an attack, update the wizard script and the projectile/effect scripts
  or prefabs it relies on. Most attacks take an `args` array containing a target
  rotation vector and target point.
- After script edits, check nearby `SendMessage`, `Resources.Load`, and
  `GameObject.Find` call sites, because those links are not type-checked.
- Unity rewrites `.meta`, scene, and prefab files on import. Re-read a file before
  patching rather than trusting an earlier read.
