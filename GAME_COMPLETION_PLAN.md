# Wizard Wars Completion Plan

Goal: ship a stable and fun v1 on Unity 2022.3.62f3.

Superseded: the earlier version of this plan targeted Unity 2018.2.10f1 and listed the
engine upgrade and the C# rewrite as out of scope. Both have since shipped, so the plan
below starts from the post-migration project.

## Scope Lock
- Engine stays on Unity 2022.3.62f3. No further engine upgrades before v1.
- Prefer script fixes over scene/prefab rewrites.
- Do not edit the dead legacy `.js` files; they are reference copies only.
- Defer the Unity Input System migration, online play, and platform expansion until
  after v1.

## Definition of Done (v1)
- Full game loop works with controllers: Title -> Character Select -> Stage Select ->
  Park -> Win Screen -> Back to Character Select.
- 1 to 4 local players can join/leave, lock character, and start a match reliably.
- All four wizard archetypes are playable, and every ability slot works: basic, second,
  third, fourth (LB), and ultimate (X).
- Health, energy, and ultimate charge UI display correctly and only when intended.
- All four stages load with the correct background and obstacles.
- No known hard-locks, missing script references, or repeatable crashes in normal play.

## Milestone 1: Validate the migration
- Open the project in Unity 2022.3.62f3 and confirm a clean Console.
- Play through Title -> Character Select -> Stage Select -> Park with 1P, 2P, and 4P.
- Confirm no `Missing (Mono Script)` on any player prefab, cursor, or attack prefab.
- Confirm `GlobalHandler` state and the `LocalMultiplayer` runtime stay in sync across
  join, leave, lock, match start, death, and rematch.
- Work through `docs/smoke-test-checklist.md` and fix what it surfaces.

## Milestone 2: Ability completeness and reliability
- Verify each wizard's five abilities against the intent recorded in
  `CLAUDE_HANDOFF.md`.
- Confirm ultimate charge gating, `ultimateActive` input lockout, and charge reset
  between matches.
- Validate projectile and zone interactions across elements.
- Normalize obvious outliers in energy cost, cooldown, and damage.

## Milestone 3: UX and match flow polish
- Improve readability of key states: joined, locked, ready, winner.
- Ensure stage transitions and the countdown always complete.
- Ensure the single-player AI fallback behaves consistently.
- Confirm the win screen and return-to-Character-Select loop is repeatable.

## Milestone 4: Release candidate
- Bug bash focused on controller edge cases and rematch loops.
- Freeze gameplay values for v1 unless a bug requires a change.
- Tag a release candidate and capture a known issues list.

## Prioritized Backlog (Only If Time Permits)
- Team mode with shared win/loss and friendly fire disabled
  (`docs/local-multiplayer-roadmap.md`).
- Optional settings screen (audio volume only).
- A quick how-to-play overlay on the title or character select screen.

## Session Checklist (Use Every Work Session)
- Reproduce the issue in the Editor.
- Apply a minimal fix.
- Compile-check with `bash tools/compile-check.sh` (Editor must be closed).
- Retest the full loop: Character Select -> Stage Select -> Park -> Win -> Return.
- Retest 1P and 2P controller flow.
- Log behavior changes in the commit message.

## Out of Scope for v1
- Further Unity version upgrades.
- Unity Input System migration.
- Large scene art overhauls.
- Online multiplayer.
