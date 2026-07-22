# Agent Instructions for WizardsArena

## Project Shape
- This is a legacy Unity project for Unity 2018.2.10f1; check `ProjectSettings/ProjectVersion.txt` before changing engine-specific APIs.
- Gameplay scripts are UnityScript `.js` files, not JavaScript for Node or browser runtimes. Preserve UnityScript syntax such as `function Start()`, `var name : Type`, `yield WaitForSeconds(...)`, `GetComponent.<Type>()`, and `#pragma strict` where present.
- There are no repo-level automated build or test scripts. Validate gameplay changes in the Unity Editor when possible, especially scene loading, prefab references, serialized fields, controller input, and collisions.

## Key Files and Flow
- Build scenes are listed in `ProjectSettings/EditorBuildSettings.asset`: `Title Screen`, `Character Select`, `Stage Select`, and `Park`.
- Global match and player-selection state is held by `Assets/StandardAssets/Scripts/GlobalHandler.js`, which persists with `DontDestroyOnLoad`.
- Menu flow is controlled by `Assets/StandardAssets/Scripts/Menus/StartToPlay.js`, `CharacterSelect.js`, and `StageSelect.js`.
- `Assets/StandardAssets/Scripts/StageScripts/ParkBattle.js` instantiates joined players, starts the countdown, and adds an AI opponent when only one player joined.
- Shared movement, aiming, targeter setup, and dispatch to wizard attacks live in `Assets/Resources/ControllerScript.js`.
- Shared health, energy, GUI bars, burn/stun state, and death behavior live in `Assets/Resources/PlayerStats.js`.
- Wizard behavior is split by element under `Assets/StandardAssets/Scripts/Attacks/`; each wizard script exposes `basicAttack`, `secondAttack`, `thirdAttack`, and `ultimateAttack` methods called via `SendMessage`.

## Unity Conventions
- Keep `.meta` files with their assets. Do not rename, move, or delete Unity assets, scenes, prefabs, textures, sounds, or `.meta` files unless the task explicitly requires it.
- Prefer editing scripts and project settings directly; let Unity regenerate `Library/`, `Temp/`, build outputs, and imported asset caches.
- Many prefab and attack references are loaded by exact string names through `Resources.Load(...)`, so resource and prefab names are part of runtime behavior.
- Existing code often relies on scene object names, clone names such as `Player1(Clone)`, tags such as `Player` and `destroyableAttack`, and serialized inspector fields. Preserve those contracts unless updating every caller and scene/prefab reference.
- Scene changes use `Application.LoadLevel(...)` in this codebase. Do not modernize to `SceneManager.LoadScene` as a side refactor unless the task is specifically about upgrading scene APIs.

## Input and Multiplayer
- Controller input names are defined in `ProjectSettings/InputManager.asset` and duplicated per player with suffixes like `_1` through `_4`.
- The main input names used by scripts include `Start_n`, `A_n`, `B_n`, `LB_n`, `RB_n`, `L_XAxis_n`, `L_YAxis_n`, `R_XAxis_n`, `R_YAxis_n`, and `Triggers_n`.
- `ControllerScript.js` maps those inputs from the instantiated player clone name. If adding players or changing clone names, update the input mapping and related global state together.

## Editing Guidance
- Keep changes minimal and consistent with the legacy UnityScript style already in the touched file. Avoid broad C# rewrites or syntax modernization unless requested.
- Be careful with operator precedence in `if` conditions using both `&&` and `||`; add parentheses when changing those expressions.
- When changing attacks, update both the wizard script and the projectile/effect scripts or prefabs it relies on. Most attacks consume an `args` array containing a target rotation vector and target point.
- After script edits, check nearby call sites that use `SendMessage`, string names, or `Resources.Load`, because those links are not type-checked.