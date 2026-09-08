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
  `ultimateAttack`, still invoked by `SendMessage`.

## Controls

Left stick moves, right stick aims, triggers fire basic/second, RB third, LB fourth,
X ultimate. Input names carry `_1`..`_4` suffixes and live in
`ProjectSettings/InputManager.asset`; `ControllerScript` picks the suffix from the
clone name. Ultimates are gated by `PlayerStats.ultimateCharge`, and
`ControllerScript.ultimateActive` blocks all other ability input while an ultimate
coroutine runs.

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
