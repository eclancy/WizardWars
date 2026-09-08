# Wizard Wars: Local Multiplayer Modernization Roadmap

## Objective

Turn Wizard Wars into a modern, maintainable, local multiplayer arena game while preserving the existing gameplay feel and the current scene flow.

## Game mode intent

- Support **Free-for-All** (every player for themselves) as the default match mode.
- Support **Teams** (players split into squads that share win/lose and can't damage each other) as a selectable match mode.
- Keep the mode choice explicit in match setup/state so it can drive friendly-fire rules, win condition, and UI.

## Phase 1: Stabilize the game loop

- Validate Title Screen -> Character Select -> Stage Select -> Park -> win flow.
- Confirm 1P, 2P, and 4P player join flows work consistently.
- Test rematch behavior and reset state between matches.
- Capture all controller edge cases and missing references.

## Phase 2: Remove brittle runtime assumptions

- Audit `GameObject.Find(...)` and `DontDestroyOnLoad` patterns.
- Centralize match, player, and menu state in explicit runtime objects.
- Define a single source of truth for selected characters, ready state, and winner state.

## Phase 3: Modernize input

- Replace the legacy per-axis input name assumptions with the Unity Input System.
- Model local player devices explicitly.
- Include keyboard fallback and controller support for each local player.
- Add a single, documented action map for menu navigation and combat input.

## Phase 4: Improve maintainability

- Split gameplay concerns into `MatchManager`, `PlayerSetup`, `CombatSystem`, and `HUDController` responsibilities.
- Move repeated string constants into centralized definitions.
- Standardize scene and prefab references using stronger runtime contracts.

## Phase 5: AI-friendly repo structure

- Add consistent project documentation.
- Ensure code follows a predictable style.
- Keep scripts small and single-purpose where possible.
- Ensure all key gameplay flows are captured in repo docs.

## Phase 6: Optional engine upgrade

- Upgrade to a modern Unity LTS after gameplay stability is strong.
- Convert legacy scripts incrementally.
- Do not do a risky wholesale rewrite until local play is stable and test coverage exists.

## Success criteria

A successful modernization pass should include:

- 1-4 local players working reliably
- consistent rematch and reset states
- cleaner input architecture
- clearer repo documentation
- better AI readability and safer refactors
