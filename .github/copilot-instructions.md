# Copilot Instructions for Wizard Wars

## Project context

This is a Unity **2022.3.62f3** project. Gameplay code is **C# under `Assets/Scripts/`**.
The project started on Unity 2018.2.10f1 with UnityScript gameplay code; both the engine
upgrade and the C# migration are complete. The `.js` files still in the tree are dead
reference copies that Unity 2022 does not compile — read them to compare original
behavior, but do not edit them or treat them as live code.

See `CLAUDE.md` for the full working guide.

## Do not do these casually

- Do not edit or "fix" the legacy `.js` files; they are not compiled.
- Do not rename Unity assets, scenes, prefabs, textures, sounds, or `.meta` files
  without an explicit reason.
- Do not break runtime name contracts for clone names, tags, serialized fields, or
  `Resources.Load(...)` lookups.
- Do not reintroduce `Application.LoadLevel(...)`; scene loading uses `SceneManager`.
- Do not commit `Library/`, `Logs/`, `UserSettings/`, or generated `.csproj`/`.sln`.

## Preferred working style

- Prefer minimal, surgical edits.
- Match the surrounding coding style in the file you touch.
- Keep changes localized to the touched script or gameplay subsystem.
- Prefer script fixes over scene or prefab rewrites; scene YAML diffs are large and
  easy to corrupt.
- Validate the full menu loop in the Unity Editor after gameplay changes, using
  `docs/smoke-test-checklist.md`.

## Critical gameplay files

- `Assets/Scripts/GlobalHandler.cs`
- `Assets/Scripts/ControllerScript.cs`
- `Assets/Scripts/PlayerStats.cs`
- `Assets/Scripts/StartToPlay.cs`
- `Assets/Scripts/CharacterSelect.cs`
- `Assets/Scripts/StageSelect.cs`
- `Assets/Scripts/ParkBattle.cs`
- `Assets/Scripts/LocalMultiplayer/` (newer match/player state layer)
- `ProjectSettings/InputManager.asset`

Note that `GlobalHandler` and `Assets/Scripts/LocalMultiplayer/` both hold match state,
bridged by `LegacyGlobalStateBridge`. Changes to join, lock, alive, or stage state must
update both sides or they will drift.

## Special note on AI assistance

This repo is easiest to improve when changes are small, documented, and testable. Prefer:

1. understanding the actual gameplay flow,
2. fixing one root cause,
3. validating the affected scene or menu flow,
4. documenting the change in the repo when it affects architecture or inputs.

## Documentation references

- `CLAUDE.md`
- `AGENTS.md`
- `CLAUDE_HANDOFF.md`
- `README.md`
- `docs/architecture.md`
- `docs/local-multiplayer-roadmap.md`
- `docs/input-mapping.md`
- `docs/smoke-test-checklist.md`
