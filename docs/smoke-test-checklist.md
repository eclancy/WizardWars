# Wizard Wars: Smoke Test Checklist

This checklist describes the validation steps once Wizard Wars is loaded in the Unity Editor and the local multiplayer runtime is in place. These tests should be performed in sequence to ensure the menu flow, battle flow, and state management are working correctly.

## Pre-flight (Static Validation)

- [ ] All C# files in `Assets/Scripts/LocalMultiplayer/` compile without errors
- [ ] The project compiles with zero errors (Unity batch mode or a clean Editor Console)
- [ ] `LocalMultiplayerBootstrap` exists in the scene or is created on demand
- [ ] `InputManager.asset` has all required input axes defined (L_XAxis_1-4, A_1-4, Start_1-4, etc.)

## Title Screen → Character Select

- [ ] Launch Title Screen
- [ ] Press Start on any connected controller (or Enter on keyboard for Player 1)
- [ ] Character Select scene loads
- [ ] Each joined player shows their portrait slot (P1-P4 boxes)
- [ ] Runtime state: `LocalMultiplayerRuntime.GetJoinedSlots()` returns the joined players

**Verify in Unity Editor:**
- [ ] `LocalMultiplayerBootstrap` GameObject exists in DontDestroyOnLoad
- [ ] Bridge is syncing legacy player state to runtime

## Character Select → Join Players

- [ ] Each controller player presses Start (Player 1: return/space, Player 2-4: joystick 2-4 button 7)
- [ ] Player joins and a cursor appears in the wizard selection area
- [ ] Player selects a wizard (cursor near a wizard portrait) and presses A (space for P1, joystick button 0 for P2-4)
- [ ] Player's wizard sprite appears in their slot box
- [ ] Player is locked in (text changes to show locked state or locks are displayed)

**Verify in Unity Editor:**
- [ ] Each slot in `LocalPlayerManager.PlayerSlots` transitions from Empty → Joined → Locked
- [ ] `SelectedWizard` field is populated with the wizard name (Fire Wizard, etc.)
- [ ] `LegacyGlobalStateBridge.SyncLegacyState()` is updating runtime state each frame

## Character Select → Leave / Unlock

- [ ] A locked player presses B (backspace for P1, joystick button 1 for P2-4)
- [ ] Player is removed from the selection (slot becomes empty)
- [ ] "Press Start to join" message reappears in that slot

**Verify in Unity Editor:**
- [ ] The slot transitions back to Empty
- [ ] `LegacyGlobalStateBridge` correctly calls `LeavePlayer()`

## Stage Select

- [ ] All locked players proceed to Stage Select
- [ ] One player's cursor appears (randomly from joined players)
- [ ] Cursor can move between stage options (1, 2, 3, 4)
- [ ] Player presses A to confirm stage
- [ ] Stage Select loads the Park battle scene

**Verify in Unity Editor:**
- [ ] `MatchManager.SelectedStage` is set to the chosen stage
- [ ] No errors in the console about missing stages or scene names

## Battle Setup

- [ ] Park scene loads
- [ ] Title/countdown displays (3, 2, 1, GO)
- [ ] All locked players instantiate and spawn at correct positions
- [ ] If only 1 player, AI opponent spawns
- [ ] Countdown plays and then battle starts

**Verify in Unity Editor:**
- [ ] `MatchManager.IsMatchActive` is true
- [ ] `MatchManager.CurrentMatch.Participants` contains the correct players
- [ ] `MatchManager.CurrentMatch.Mode` is either `LocalMultiplayer` or `SoloWithAiFallback`
- [ ] Player GameObjects are instantiated (P1(Clone), P2(Clone), etc.)

## Combat Flow

- [ ] Players can move (left stick / WASD)
- [ ] Players can aim (right stick / arrow keys)
- [ ] Players can attack: triggers or 1/2 (basic/second), RB or 3 (third), X or 5 (ultimate)
- [ ] Health bars decrease on hits
- [ ] Status effects apply (burn, stun, slow, speed)
- [ ] Attacks visually appear and hit correctly

**Verify in Unity Editor:**
- [ ] No input errors in the console
- [ ] `InputProvider.GetLeftStick()` is callable and returns Vector2
- [ ] Targeter objects exist for each player

## Battle End - Winner Detection

- [ ] When one player is the last one alive, a "Player X Wins" screen appears
- [ ] 5 seconds pass, then automatically return to Character Select
- [ ] Battle scene unloads

**Verify in Unity Editor:**
- [ ] `MatchManager.Winner` is set to the winning player's slot
- [ ] `MatchManager.IsMatchActive` becomes false
- [ ] Console shows no errors about winner registration

## Reset to Character Select

- [ ] Character Select scene loads again
- [ ] All player slots are reset to Empty (join boxes show "Press Start to join")
- [ ] Global handler has reset player state

**Verify in Unity Editor:**
- [ ] `LocalPlayerManager.ResetSelectionState()` was called
- [ ] Each slot has state `Empty` and `SelectedWizard` is empty
- [ ] `GlobalHandler` fields (`player1Joined`, `player1Locked`, etc.) are false
- [ ] `LegacyGlobalStateBridge.ResetRuntimeState()` was called on scene load

## Rematch Flow

- [ ] From Character Select, players join again in any combination
- [ ] Flow proceeds normally: select → lock → stage select → battle
- [ ] No stale state from previous match

**Verify in Unity Editor:**
- [ ] `LocalPlayerManager.CanStartMatch()` correctly evaluates
- [ ] No duplicate or ghost players

## Input Abstraction Validation

- [ ] Create a test script that uses `InputProvider.Instance.GetLeftStick(0)` (slot 0 = player 1)
- [ ] Verify it returns correct analog values
- [ ] Verify `InputProvider.GetPlayerInputState(0)` returns a complete snapshot

**Example test code:**
```csharp
var input = InputProvider.Instance;
var state = input.GetPlayerInputState(0);
Debug.Log("Player 1 LS: " + state.LeftStick);
Debug.Log("Player 1 RB: " + state.RB);
```

## Known limitations (at this stage)

- Menu navigation input for 2-4 players is not yet fully bound (Character Select and Stage Select expect player 1 to navigate in some flows)
- AI opponent wizard selection is fixed (not runtime-configurable)
- Rematch flow may not properly track all player state transitions (needs final validation)

## Pass Criteria

- [ ] Title → Character Select → 4 players join → lock wizards → Stage Select → Battle → Winner → Character Select
- [ ] Rematch: from Character Select, 2 players join different wizards → lock → stage → battle → winner → back
- [ ] Solo mode: 1 player joins → a computer wizard (not the human's wizard) spawns, moves, dodges and casts all its abilities
- [ ] Solo mode: losing shows "THE COMPUTER WINS" and returns to Character Select
- [ ] Character Select: the top status line tracks join / pick / ready; each player shows PICK A WIZARD or LOCKED IN, and a locked player's ability list
- [ ] Character Select: holding RS (or H) shows the how-to-play panel; releasing hides it
- [ ] Teams: Tab or Back/View toggles the mode banner; Y or T flips a joined player between RED and BLUE
- [ ] Teams: with every player on one team, Start is blocked and "Both teams need at least one player" shows
- [ ] Teams 2v2: teammates spawn together with matching ground markers and HUD strips
- [ ] Teams: a teammate's basic shots pass through you; Ring of Fire, Meteor Storm, Ice Wave, the patches, Lightning Rod, Absolute Zero and Earthquake do not affect you
- [ ] Teams: enemies are still hit by all of the above, and the caster still slips or is slowed on their own patch
- [ ] Teams: the match ends with "RED TEAM WINS" or "BLUE TEAM WINS" once the other team is out, even if the winning team has two players alive
- [ ] Free-for-All after a Teams match: no ground markers, friendly fire on, last player standing wins
- [ ] No console errors or warnings related to null references, missing scenes, or state mismatches
- [ ] `InputProvider` is accessible and provides slot-based input queries

## Next Steps (Post-Smoke Test)

1. If issues appear in combat, check `ControllerScript.cs` for input axis mapping problems.
2. If menu navigation breaks, verify `CharacterSelect.cs` and `StageSelect.cs` are reading the correct input buttons.
3. If state resets fail, trace `LegacyGlobalStateBridge.ResetRuntimeState()` and `MatchManager.ResetForRematch()`.
4. Once smoke tests pass, migrate player-specific input in menus to use `InputProvider` for cleaner navigation.
5. Begin Input System migration using `InputProvider` as the abstraction layer.
