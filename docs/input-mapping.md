# Wizard Wars: Input and Controller Mapping

## Current legacy model

The project currently depends on input names defined in `ProjectSettings/InputManager.asset` with suffixes such as:

- `Start_n`
- `A_n`
- `B_n`
- `LB_n`
- `RB_n`
- `L_XAxis_n`
- `L_YAxis_n`
- `R_XAxis_n`
- `R_YAxis_n`
- `Triggers_n`

This mapping is duplicated across player slots and is consumed by runtime input lookups in `ControllerScript.cs`.

## Why this is risky

The legacy input design is tightly coupled to:

- specific axis names
- player index suffixes
- scene object naming
- global singleton state

This makes modern local multiplayer expansion more difficult because input behavior is not centralized or explicit.

## Future target

The modern target is to replace this with the Unity Input System using named actions and a per-player device mapping. Example action categories:

- Menu navigation
- Join / leave
- Select / confirm
- Move
- Aim
- Attack
- Special
- Pause

## Abstraction bridge: InputProvider

As a intermediate modernization step, `Assets/Scripts/LocalMultiplayer/InputProvider.cs` provides a slot-based input abstraction:

```csharp
// Old way (legacy, still used in ControllerScript.cs)
float x = Input.GetAxis("L_XAxis_1");

// New way (via InputProvider)
var input = InputProvider.Instance;
Vector2 stick = input.GetLeftStick(0); // slot 0 = player 1
```

This abstraction:
- Centralizes the mapping logic
- Allows future Input System migration without touching gameplay code
- Provides `GetPlayerInputState()` for full snapshot queries
- Maintains backward compatibility with legacy Project Settings inputs

## Migration plan

1. **Phase 1 (current)**: InputProvider abstraction layer created; legacy ControllerScript.cs unchanged.
2. **Phase 2**: Update ControllerScript.cs to use InputProvider (opt-in, low-risk).
3. **Phase 3**: Create Unity Input System actions and bindings.
4. **Phase 4**: Wire InputProvider to read from Input System instead of legacy Project Settings.
5. **Phase 5**: Remove Project Settings input dependencies; validate all menu and combat flows.

## Maintenance note

When editing player input logic, verify that any renamed player objects, clone names, or player slot logic still line up with the input mapping and global state flow.

When adding new input actions or buttons:
1. Update `ProjectSettings/InputManager.asset` with new axes or buttons if still using legacy inputs.
2. Update `InputProvider.cs` to expose the new input in slot-based form.
3. Update `PlayerInputState` struct if the new input is part of the regular gameplay state snapshot.
