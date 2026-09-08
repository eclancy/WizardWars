# Wizard Wars

Wizard Wars is a local multiplayer arena battler built in Unity 2022.3.62f3. Gameplay code is C# under `Assets/Scripts/`, wired through menu scenes, controller input, and a shared global match state.

## Project status

- Engine: Unity 2022.3.62f3 (upgraded from 2018.2.10f1)
- Primary gameplay flow: Title Screen -> Character Select -> Stage Select -> Park
- Local multiplayer target: 1 to 4 players
- Current architecture: C# gameplay scripts plus scene-based object wiring, with a newer local multiplayer runtime state layer

## Modernization status

✅ **Engine upgrade complete**: the project runs on Unity 2022.3.62f3.

✅ **UnityScript to C# migration complete**: all gameplay logic lives in `Assets/Scripts/`, and every scene and prefab script reference resolves to a C# script. The `.js` files still in the tree are dead reference copies that Unity 2022 does not compile.

✅ **Phase 1 complete**: local multiplayer runtime state layer is in place.

- `LocalPlayerManager` tracks player join/lock/selection state.
- `MatchManager` owns match lifecycle and winner registration.
- `LegacyGlobalStateBridge` syncs `GlobalHandler` scene state to the runtime state.
- `InputProvider` provides slot-based input abstraction (bridge to future Input System).
- All menu and battle flows are wired to update runtime state.

**Next**: verify the full flow in the Unity Editor using [`docs/smoke-test-checklist.md`](docs/smoke-test-checklist.md).

## Quick start

1. Open the project in Unity 2022.3.62f3.
2. Load the main scene flow from the build settings.
3. Play the Title Screen scene and follow the smoke test checklist to validate the menu and match flow.
4. Validate controller input and rematch behavior before making gameplay changes.

To compile-check without opening the Editor, close Unity and run `bash tools/compile-check.sh`.

## Core flow

- Menu scenes are under `Assets/Scenes/`
- Shared match state is maintained by `Assets/Scripts/GlobalHandler.cs`
- Main menu transition starts in `Assets/Scripts/StartToPlay.cs`
- Player movement and attacks are coordinated from `Assets/Scripts/ControllerScript.cs`
- Player health and energy logic is in `Assets/Scripts/PlayerStats.cs`

## Repo goals

This repository is being modernized in stages:

1. Stabilize the local multiplayer loop.
2. Reduce brittle global state and scene object lookups.
3. Improve repo clarity for AI-assisted development and future contributors.
4. Prepare for a cleaner Unity Input System migration.

## AI-assisted development guidance

- Edit the C# scripts under `Assets/Scripts/`; the remaining `.js` files are dead reference copies that Unity 2022 does not compile.
- Preserve serialized field names, scene object names, and prefab names unless updating all callers.
- Prefer minimal changes that match the surrounding style.
- Before changing gameplay, validate the full menu flow in the Unity Editor.

## Relevant files and docs

- [`CLAUDE.md`](CLAUDE.md) — working guide for agents (authoritative)
- [`AGENTS.md`](AGENTS.md) — AI editing guidelines for this project
- [`CLAUDE_HANDOFF.md`](CLAUDE_HANDOFF.md) — migration record and ability design intent
- [`GAME_COMPLETION_PLAN.md`](GAME_COMPLETION_PLAN.md) — game completion status tracker
- [`docs/local-multiplayer-architecture.md`](docs/local-multiplayer-architecture.md) — runtime state model
- [`docs/input-mapping.md`](docs/input-mapping.md) — controller input mapping and future Input System migration
- [`docs/smoke-test-checklist.md`](docs/smoke-test-checklist.md) — validation steps after loading in Unity Editor
- [`Assets/Scripts/GlobalHandler.cs`](Assets/Scripts/GlobalHandler.cs) — global match state
- [`Assets/Scripts/ControllerScript.cs`](Assets/Scripts/ControllerScript.cs) — player input and movement
- [`Assets/Scripts/PlayerStats.cs`](Assets/Scripts/PlayerStats.cs) — player health and status effects
- [`Assets/Scripts/LocalMultiplayer/`](Assets/Scripts/LocalMultiplayer/) — local multiplayer runtime state layer

## Planned modernization path

See [`docs/local-multiplayer-roadmap.md`](docs/local-multiplayer-roadmap.md) for the staged upgrade plan.
