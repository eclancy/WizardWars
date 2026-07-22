# WizardsArena Completion Plan (Current Unity Version)

Goal: ship a stable and fun v1 on Unity 2018.2.10f1 without engine upgrades.

## Scope Lock
- Keep engine and project settings baseline unchanged except required bug fixes.
- Prefer script fixes over scene/prefab rewrites.
- Defer engine upgrades and platform expansion until after v1 release.

## Definition of Done (v1)
- Full game loop works with controllers: Title -> Character Select -> Stage Select -> Park -> Win Screen -> Back to Character Select.
- 1 to 4 local players can join/leave, lock character, and start match reliably.
- All four wizard archetypes are playable and their first three attacks work consistently.
- Health and energy UI bars display correctly and only when intended.
- No known hard-locks, missing references, or repeatable crashes in normal play.

## Milestone 1: Critical Gameplay Stability (Week 1)
- Fix character select lock-in bugs and player-specific script assignment issues.
- Fix HUD and match state bugs that affect readability or flow.
- Verify GlobalHandler state resets correctly between matches.
- Test matrix: 1P, 2P, and 4P join/leave/start flow.

## Milestone 2: Combat Reliability and Balance (Week 2)
- Validate each wizard attack behavior against intended role.
- Normalize obvious outliers in energy cost, cooldown, and damage.
- Validate projectile/zone interactions (fire/ice/earth/electric counters).
- Test matrix: every wizard matchup at least once.

## Milestone 3: UX and Match Flow Polish (Week 3)
- Improve readability of key states (joined, locked, ready, winner).
- Ensure stage transitions and countdown timing always complete.
- Ensure AI fallback in single-player behaves consistently.
- Add minimal quality-of-life tweaks only if they are low-risk.

## Milestone 4: Release Candidate (Week 4)
- Run a bug bash focused on controller edge cases and rematch loops.
- Freeze gameplay values for v1 unless a bug requires change.
- Tag release candidate and capture known issues list.

## Prioritized Backlog (Only If Time Permits)
- Add one low-risk content extension (small stage variant or minor attack variant).
- Add optional simple settings toggle screen (audio volume only).
- Add a quick how-to-play overlay on title/character select.

## Session Checklist (Use Every Work Session)
- Reproduce issue in editor.
- Apply minimal fix.
- Retest full loop: Character Select -> Stage Select -> Park -> Win -> Return.
- Retest 1P and 2P controller flow.
- Log behavior changes in commit message.

## Out of Scope for v1
- Unity version upgrades.
- Rewrite to C# or another engine.
- Large scene art overhauls.
- Online multiplayer.
