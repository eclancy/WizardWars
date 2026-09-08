# Wizard Wars: Local Multiplayer Architecture

## Summary

Wizard Wars was upgraded from a heavily scene-driven Unity 2018 design into a more maintainable local multiplayer runtime architecture on Unity 2022.3.62f3. The original scene flow still works, but match state and local-player data are centralized so that menu logic, combat logic, and scene flow can evolve independently.

## Runtime state model

The new runtime model is built around the following components:

- `LocalMultiplayerBootstrap` creates and owns the runtime services.
- `LocalPlayerManager` owns player slots and join state.
- `MatchManager` owns round setup, stage selection, and match lifecycle.
- `PlayerSlotState` is the per-player runtime object.
- `MatchConfig` is the match setup contract.

## Slot model

Each slot tracks:

- slot index
- device ownership
- join readiness
- lock state
- selected wizard
- runtime controller or prefab reference
- spawn point
- player color

The player slot is intentionally separate from the scene GameObject so a player can be tracked even when the scene changes.

## Match lifecycle

The new lifecycle is:

1. Player joins in character select.
2. Player selects wizard and locks in.
3. Stage is selected.
4. Match begins.
5. The winner is recorded.
6. Game returns to selection screen or rematch loop.

## Runtime stack

```
ControllerScript.cs (legacy)
    ↓
InputProvider (slot-based input abstraction)
    ↓
Input.GetAxis/GetButton (legacy ProjectSettings)
    ↓ [future: swap to Unity Input System]

LocalPlayerManager (join/lock/selection)
    ↓
MatchManager (match lifecycle)
    ↓
LegacyGlobalStateBridge (bridges old scene state to new runtime state)
```

The input layer is designed to be swappable: `InputProvider` currently wraps legacy project settings inputs, but can be retargeted to the Input System without changing gameplay code.

## Input migration target

The current code expects controller and keyboard actions via legacy input names such as `Start_1`, `A_1`, and `B_1`. The modern runtime architecture is designed to decouple player slot identity from exact input-axis names, which makes migration to the Unity Input System much safer.

`InputProvider` is the bridge for this migration:
- **Phase 1 (current)**: Reads from legacy ProjectSettings inputs by slot index.
- **Phase 2+**: Can be retargeted to read from Input System actions without changing callers.

## Compatibility approach

The project keeps the original scene flow and naming conventions while exposing a newer runtime state layer. This allowed the repo to improve without a single large, risky rewrite.

## Example implementation rules

- Preserve scene object names, prefab references, and serialized field names in the legacy scripts.
- Keep the runtime state as a separate concern from the Unity scene graph.
- Let legacy menu scripts call into manager methods instead of directly depending on scene-global state alone.
- Only modernize scenes or scene APIs when the task explicitly targets those systems.

## Why this matters

This architecture gives the project a better foundation for:

- 1–4 local multiplayer support
- cleaner controller mapping
- rematch and reset correctness
- AI fallback logic
- future Unity upgrades
- AI-assisted code changes without brittle global state
