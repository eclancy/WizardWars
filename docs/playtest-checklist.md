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

- [ ] Title: the START in the art flashes, and there is no text at the bottom. Start, A,
      Enter or Space goes to Character Select. Escape quits.
- [ ] Each screen fades in from black; the menus' sparkles drift and the portraits bob.
- [ ] Join with a pad's Start and with Enter. The slot shows the right device.
- [ ] Left/right moves the cursor; A or Space locks in; B or Backspace unlocks, then leaves.
- [ ] Tab or Back/View toggles the mode, and the mode switch at the top makes the current
      mode obvious. In Teams, Y or T switches team (Green or Pink).
- [ ] Once a player locks in a wizard, its card greys out ("TAKEN BY P1") and other cursors
      skip it. Player tags are plain white; no player has a colour of their own.
- [ ] Start is blocked, with a message, while both teams are not filled.
- [ ] Holding RS or H shows how-to-play.
- [ ] Stage Select: every stage can be picked. B goes back.

## Battle

- [ ] 3-2-1 countdown with door slams, then "Fight!". Nobody can act before it ends.
- [ ] Each of the 20 abilities looks and sounds right (see the table in README.md),
      including the four on LB: Meteor Strike, Deep Freeze, Tremor, Chain Lightning.
- [ ] Meteors fall straight down. Basic shots pass through each other.
- [ ] Every stage's cover blocks shots and movement, and you can hide behind it (drawn in
      front of you). At a glance, cover (with its shadow) is the only thing that looks solid.
- [ ] On a 16:10 or 3:2 screen everything reaches the bottom: the title's version sits in
      the corner, and in battle the floor beyond the walls is dimmed.
- [ ] The HUD bars, cooldown pips and red fizzle flash are readable at a glance.
- [ ] Teams: the coloured ground discs and HUD strips make the sides obvious.
- [ ] Frozen by Ice Wave or Deep Freeze: a joystick wiggles beside the ice, and wiggling the
      left stick side to side breaks out noticeably sooner.
- [ ] Every ultimate builds up first (its name, a darkened floor, colour pouring in) and only
      then does anything. Each looks dramatic and distinct; Earthquake reads as cracking,
      shaking ground, not a brown circle.
- [ ] Storm Plateau's four stones each carry a different carved rune.
- [ ] Results: "P1 wins" art, "THE COMPUTER WINS", "GREEN/PINK TEAM WINS", or "DRAW". Then
      back to Character Select with everyone still locked in.
- [ ] Solo: the computer wizard is beatable but not trivial.

## Balance notes to collect

- How long do matches last? Does everyone get to use an ultimate? Computer duels run about
  25 s and four-way matches about 28 s, with ultimates in every match.
- Are the restored LB abilities worth their energy, and is any of them too strong?
- Is any wizard or ability clearly too strong or too weak?
