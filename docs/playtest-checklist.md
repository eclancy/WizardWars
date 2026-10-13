# Playtest checklist

`tools/check.sh` proves the rules: the menus' logic, every ability, friendly fire, and no
leaks. It cannot tell whether the game *feels* right, reads clearly, or works on real pads.
That is what this list is for. Run the game with F5 in the editor, or from an exported build.

## Controls and feel

- [ ] Movement feels responsive on a pad and on WASD, with no drift on a resting stick.
- [ ] Aiming with the right stick or the arrows: the targeter shows, and shots go where aimed.
- [ ] Releasing the stick: shots keep going the last way you aimed.
- [ ] Holding a trigger (or 1 / 2) repeats the ability whenever it is off cooldown.
- [ ] The flamethrower burns while RB (3) is held and stops on release or when energy runs out.
- [ ] Four pads at once: each controls only its own wizard.
- [ ] The game opens fullscreen. F11 or Alt+Enter switches to a window and back, and
      Alt+Enter does not also press Start in a menu.

## Menus

- [ ] Title: Start, A, Enter or Space goes to Character Select. Escape quits.
- [ ] Join with a pad's Start and with Enter. The slot shows the right device.
- [ ] Left/right moves the cursor; A or Space locks in; B or Backspace unlocks, then leaves.
- [ ] Tab or Back/View toggles the mode. In Teams, Y or T switches team.
- [ ] Start is blocked, with a message, while both teams are not filled.
- [ ] Holding RS or H shows how-to-play.
- [ ] Stage Select: every stage can be picked. B goes back.

## Battle

- [ ] 3-2-1 countdown with door slams, then "Fight!". Nobody can act before it ends.
- [ ] Each of the 16 abilities looks and sounds right (see the table in README.md).
- [ ] The HUD bars, cooldown pips and red fizzle flash are readable at a glance.
- [ ] Teams: the coloured ground discs and HUD strips make the sides obvious.
- [ ] Results: "P1 wins" art, "THE COMPUTER WINS", "RED/BLUE TEAM WINS", or "DRAW". Then
      back to Character Select with everyone still locked in.
- [ ] Solo: the computer wizard is beatable but not trivial.

## Balance notes to collect

- How long do matches last? Does everyone get to use an ultimate? Computer matches now run
  about 30 s with ultimates in every match, since health went from 100 to 250.
- Is any wizard or ability clearly too strong or too weak?
