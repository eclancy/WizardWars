# Wizard Wars Completion Plan

Goal: ship a stable and fun v1 on Unity 2022.3.62f3.

Superseded: the earlier version of this plan targeted Unity 2018.2.10f1 and listed the
engine upgrade and the C# rewrite as out of scope. Both have since shipped, so the plan
below starts from the post-migration project.

## Status (2026-10-10)

Shipped since the migration, all compile-clean (0 errors, 0 warnings) but **none of it
playtested in the Editor yet**:

- Four-ability kits for every wizard (basic, second, third, ultimate). The LB "fourth"
  slot was cut on purpose; do not restore it. See `CLAUDE_HANDOFF.md`.
- Resource rebalance: free basic attack, 20-35 energy mid abilities, earned ultimate.
- HUD: health, energy, ultimate meter, cooldown pips, refused-cast flash.
- Code-animated Ice Wave, Ring of Fire, ice and mud patch telegraphs, Meteor Storm,
  Earthquake.
- Teams mode (Red vs Blue, friendly fire off). See the **Teams mode** section of
  `CLAUDE.md`.
- Absolute Zero no longer stacks a permanent slow, and neither Absolute Zero nor
  Earthquake leaves its slow behind if the caster dies mid-ultimate.
- A real solo opponent: `WizardAI` plays a random wizard instead of the old stationary
  target dummy, and a "THE COMPUTER WINS" banner shows if it wins.
- Character Select readability: a status line saying what the room is waiting for,
  PICK A WIZARD / LOCKED IN under each player, each locked player's ability list by
  button, and a how-to-play panel shown while RS (H) is held.

The biggest open risk is that Milestone 1 has never been run: everything above has
only been checked by the compiler.

## Scope Lock
- Engine stays on Unity 2022.3.62f3. No further engine upgrades before v1.
- Prefer script fixes over scene/prefab rewrites.
- Do not edit the dead legacy `.js` files; they are reference copies only.
- Defer the Unity Input System migration, online play, and platform expansion until
  after v1.

## Definition of Done (v1)
- Full game loop works with controllers: Title -> Character Select -> Stage Select ->
  Park -> Win Screen -> Back to Character Select.
- 1 to 4 local players can join/leave, lock character, and start a match reliably, in
  both Free-for-All and Teams.
- All four wizard archetypes are playable, and every ability works: basic (right
  trigger), second (left trigger), third (RB) and ultimate (X).
- Health, energy, ultimate charge and cooldown UI display correctly and only when
  intended.
- All four stages load with the correct background and obstacles.
- No known hard-locks, missing script references, or repeatable crashes in normal play.

## Milestone 1: Validate in the Editor (not started — needs a human at the controls)
- Open the project in Unity 2022.3.62f3 and confirm a clean Console.
- Play through Title -> Character Select -> Stage Select -> Park with 1P, 2P, and 4P.
- Play one 2v2 and one 2v1 Teams match.
- Confirm no `Missing (Mono Script)` on any player prefab, cursor, or attack prefab.
- Confirm `GlobalHandler` state and the `LocalMultiplayer` runtime stay in sync across
  join, leave, lock, match start, death, and rematch.
- Work through `docs/smoke-test-checklist.md` and fix what it surfaces.

## Milestone 2: Ability completeness and reliability
- Verify each wizard's four abilities against the intent recorded in
  `CLAUDE_HANDOFF.md`.
- Confirm ultimate charge gating, `ultimateActive` input lockout, and charge reset
  between matches.
- Validate projectile and zone interactions across elements.
- Normalize obvious outliers in energy cost, cooldown, and damage after a playtest.

## Milestone 3: UX and match flow polish
- ~~Improve readability of key states on Character Select: joined, locked, ready.~~ Done, needs an Editor look.
- ~~A how-to-play overlay that does not depend on the Title Screen help box.~~ Done (hold RS or H on Character Select).
- Ensure stage transitions and the countdown always complete.
- Ensure the single-player AI behaves consistently and is worth playing. `WizardAI` replaces the dummy; tune `reactionDelay`, `aimErrorDegrees` and `dodgeChance` after a playtest.
- Confirm the win screen and return-to-Character-Select loop is repeatable.

## Milestone 4: Release candidate
- Bug bash focused on controller edge cases and rematch loops.
- Freeze gameplay values for v1 unless a bug requires a change.
- Tag a release candidate and capture a known issues list.

## Prioritized Backlog (Only If Time Permits)
- Optional settings screen (audio volume only).
- Keep team choices across a rematch instead of re-picking them each match.

## Session Checklist (Use Every Work Session)
- Reproduce the issue in the Editor.
- Apply a minimal fix.
- Compile-check with `bash tools/compile-check.sh` (Editor closed) or
  `bash tools/syntax-check.sh` (Editor open).
- Retest the full loop: Character Select -> Stage Select -> Park -> Win -> Return.
- Retest 1P and 2P controller flow.
- Log behavior changes in the commit message.

## Out of Scope for v1
- Further Unity version upgrades.
- Unity Input System migration.
- Large scene art overhauls.
- Online multiplayer.
