# Wizard Wars: Architecture Notes

## High-level overview

Wizard Wars is organized around a legacy Unity scene-based game loop. The project uses several persistent objects and menu scenes to manage local multiplayer selection and match state.

## Scene flow

The project build order is expected to include:

- Title Screen
- Character Select
- Stage Select
- Park

Menu and match transitions are controlled through scene-load calls rather than a modern scene-management abstraction.

## Primary systems

### Global state

`Assets/Scripts/GlobalHandler.cs` stores cross-scene state such as joined players, selected wizard types, and information needed for match setup.

This remains a very important contract to preserve because many menu scripts depend on it.

### Player controller layer

`Assets/Scripts/ControllerScript.cs` is the shared player behavior script. It contains movement, aiming, targeter setup, input mapping, and dispatch to the selected wizard-specific attack logic.

### Combat and gameplay stats

`Assets/Scripts/PlayerStats.cs` manages health, energy, HUD bars, burn/stun state, and death behavior. This is the central combat state object for a player's runtime state.

### Wizard attack modules

Wizard behavior is distributed under `Assets/StandardAssets/Scripts/Attacks/` and each wizard script exposes attack methods such as:

- basicAttack
- secondAttack
- thirdAttack
- ultimateAttack

The actual method dispatch is performed through string-based messages such as `SendMessage`, so name contracts are important.

## Legacy constraints

This codebase depends on several older Unity patterns:

- C# scripts under `Assets/Scripts/` (the remaining `.js` files are dead reference copies Unity 2022 does not compile)
- scene names used as string identifiers
- global singleton-like state persistence
- `Application.LoadLevel(...)` for transitions
- per-player input names in `ProjectSettings/InputManager.asset`

These patterns are not inherently wrong for a legacy project, but they are the biggest barriers to modern local-multiplayer expansion and AI-guided refactors.

## Modernization target

The desired modernization path is:

1. Stabilize the current match loop.
2. Introduce well-defined local-player state models.
3. Replace fragile scene-object lookups with centralized runtime state.
4. Migrate input to the Unity Input System.
5. Break gameplay logic into maintainable systems instead of scene-level glue code.

## Good maintenance practices

- Keep the scene and prefab contracts stable unless a focused refactor is intended.
- Update both the wizard script and any projectile/effect scripts when changing attack behavior.
- Preserve object names and tags where they are part of gameplay behavior.
- Document each state transition and player-selection flow before making broad changes.
