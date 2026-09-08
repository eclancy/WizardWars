# Wizard Wars: Controller Button Reference

This file documents the controller button mapping for local multiplayer gameplay and menu navigation.

## Xbox/Generic Controller Layout

```
        [Y]
    [X] [ ] [B]
        [A]

    [LB]              [RB]

    [LS] (click)      [RS] (click)
    
    [Back]  [Start]
```

## Player Input Mapping

Each player slot (1-4) maps to a controller (Joystick 1-4) in `ProjectSettings/InputManager.asset`.

### Player 1 (Keyboard + Joystick 1)

| Action | Controller | Keyboard |
|--------|-----------|----------|
| Move | Left Stick | W/A/S/D |
| Aim | Right Stick | Arrow Keys |
| Join/Confirm | A (Button 0) | Space |
| Leave/Back | B (Button 1) | Backspace |
| Basic Attack | Triggers (LT/RT) | Q / E |
| Second Attack | Triggers (opposite) | Q / E |
| Third Attack | RB (Button 5) | E |
| Ultimate | LB (Button 4) | Q |
| Start Menu | Start (Button 7) | Return |

### Player 2-4 (Joystick 2-4)

| Action | Controller |
|--------|-----------|
| Move | Left Stick |
| Aim | Right Stick |
| Join/Confirm | A (Button 0) |
| Leave/Back | B (Button 1) |
| Basic Attack | Triggers (LT/RT) |
| Second Attack | Triggers (opposite) |
| Third Attack | RB (Button 5) |
| Ultimate | LB (Button 4) |
| Start Menu | Start (Button 7) |

## Menu Flow

### Title Screen
- **Player 1**: Press Start (Return) to proceed to Character Select
- **Player 2-4**: Press Start (Joystick Button 7) to proceed to Character Select

### Character Select
- **Move cursor**: Left/Right with Left Stick or A/D keys (Player 1 only currently)
- **Select wizard**: Press A (Space for P1, Button 0 for P2-4)
- **Unselect**: Press B (Backspace for P1, Button 1 for P2-4)
- **Join**: Each player presses Start to enter the selection area

### Stage Select
- **Move cursor**: Left/Right/Up/Down with Left Stick or Arrow Keys
- **Select stage**: Press A to confirm

### Battle
- **Move**: Left Stick or W/A/S/D
- **Aim**: Right Stick or Arrow Keys
- **Basic Attack**: Trigger down (LT/RT)
- **Secondary Attack**: Trigger opposite direction
- **Special (Third) Attack**: RB or E key
- **Ultimate**: LB or Q key

## Testing Checklist

### Single Player Test
1. Press Start on Joystick 1 only → Player 1 joins
2. Select wizard for Player 1
3. Select stage
4. Battle should start with Player 1 vs AI

### 2-Player Test
1. Joystick 1: Press Start → Player 1 joins
2. Joystick 2: Press Start → Player 2 joins
3. Player 1: Select wizard and lock
4. Player 2: Select wizard and lock
5. Select stage
6. Battle: Player 1 vs Player 2 (no AI)

### 4-Player Test
1. Joystick 1-4: Each presses Start to join
2. Each player selects and locks a wizard
3. Select stage
4. Battle: All 4 players in arena (no AI)

### Leave/Rejoin Test
1. Player 1 joins and locks
2. Player 2 joins and locks
3. Player 2 presses B to leave
4. Player 2 should return to "Press Start to join" state
5. Player 2 presses Start again to rejoin
6. Player 2 can select a different wizard

## Platform Notes

- **Windows**: Controllers should be connected before launching Unity
- **Controller Compatibility**: Generic XInput-compatible controllers (Xbox 360 style)
- **Keyboard-Only**: Player 1 can play with keyboard; Player 2-4 require controllers
- **Joystick Detection**: Unity detects up to 4 joysticks; verify in Edit > Project Settings > Input

## Troubleshooting

- **No input from controller**: Verify controller is detected in Unity. Run a test scene and check Input.GetAxis("L_XAxis_1") output.
- **Button mapping wrong**: Check `ProjectSettings/InputManager.asset` for correct axis/button definitions per player.
- **Player joins but can't select wizard**: Ensure the correct Start button is pressed to enter the menu, then Left Stick or Arrow Keys move the cursor.

## Future Input System Migration

When migrating to Unity's new Input System, this mapping will be converted to Action Maps:
- Menu Map: Navigate, Confirm, Back, Start
- Combat Map: Move, Aim, Attack, SpecialAttack, Ultimate, CharacterSpecial

The `InputProvider` class will then read from Input Actions instead of legacy ProjectSettings inputs.
