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
- [ ] Tab or Select toggles the mode (its button is pictured next to the word Select), and the mode switch at the top makes the current
      mode obvious. In Teams, Y or T switches team (Gold or Pink).
- [ ] Once a player locks in a wizard, its card greys out ("TAKEN BY P1") and other cursors
      skip it. Player tags are plain white; no player has a colour of their own.
- [ ] Start is blocked, with a message, while both teams are not filled.
- [ ] Holding RS or H shows how-to-play.
- [ ] Stage Select: every stage can be picked, and the Volcano and Swamp tiles show their
      hazards. Y cycles best of 1/3/5; X cycles the computer's skill (shown only when one
      player faces the computer). B goes back.
- [ ] X on Character Select opens the options. Volume changes are heard at once; fullscreen
      and rumble toggle; everything is remembered after quitting the game.

## Battle

- [ ] 3-2-1 countdown with door slams, then "Fight!". Nobody can act before it ends.
- [ ] Each of the 20 abilities looks and sounds right (see the table in README.md),
      including the four on LB: Meteor Strike, Deep Freeze, Tremor, Chain Lightning.
- [ ] Meteors fall straight down. Basic shots pass through each other.
- [ ] Every stage's cover blocks shots and movement, and you can hide behind it (drawn in
      front of you). At a glance, cover (with its shadow) is the only thing that looks solid.
- [ ] Cacti (Scorched Canyon) and toadstools (Swamp) never stop you or a shot; they sway as
      you pass. The Volcano's boulders have dark cracks, nothing that looks like lightning.
- [ ] On a 16:10 or 3:2 screen everything reaches the bottom: the title's version sits in
      the corner, and in battle the floor beyond the walls is dimmed.
- [ ] The HUD strip below the arena is readable at a glance: health, energy, the red fizzle
      flash, a chip per button that fills on cooldown and dims without energy, and X filling
      with charge. Nothing over the wizards but their labels; the arena is all on screen.
- [ ] As a wizard loses health, scuffs and then rips appear on the robe and hat; healing
      mends them.
- [ ] Teams: the coloured ground discs and the team strip on each HUD panel make the sides obvious.
- [ ] Duels last around half a minute and ultimates come once or twice a fight; no wizard
      feels hopeless.
- [ ] Frozen by Ice Wave or Deep Freeze: a joystick wiggles beside the ice, and wiggling the
      left stick side to side breaks out noticeably sooner.
- [ ] Every ultimate builds up first (its name, a darkened floor, colour pouring in) and only
      then does anything. Each looks dramatic and distinct; Earthquake reads as cracking,
      shaking ground, not a brown circle.
- [ ] Storm Plateau's four stones each carry a different carved rune.
- [ ] Start pauses the battle. Resume, Rematch (round 1 again), Options and Quit all work, and
      only the player who paused drives the menu.
- [ ] Best of 3: a round banner and scoreboard between rounds, "ROUND 2" at the next start,
      and the match result with the final score.
- [ ] Rumble: hits, freezes, quake jolts and your own ultimate going off are felt on the pad.
- [ ] Volcano: lava burns and hurts, the bridges are safe, and the computer keeps out of the
      lava and crosses on the bridges to reach you.
      Swamp: bog slows you and stops your energy, and lifts the moment you step out.
- [ ] Easy, Normal and Hard computers feel clearly different.
- [ ] Seven wizards fit on Character Select. Shadow, Nature and Wind look like the other four
      and each of their abilities reads clearly: smoke hides, Shadow Step blinks, hands silence
      (only the basic attack works), vines root, Regrowth heals, gusts knock back, the dash goes
      through enemies, the cyclone pulls in, and Tempest hurls out.
- [ ] The new wizards feel fair against the old four (soak hints Nature is strong, Shadow weak).
- [ ] King of the Rune: the rune is easy to see, standing in it alone fills your bar, two sides
      in it show CONTESTED, knocked-out wizards come back, and the first to 20 wins the round.
      On the Volcano and Swamp the next spot glows before the rune moves.
- [ ] Results: "P1 wins" art, "THE COMPUTER WINS", "GREEN/PINK TEAM WINS", or "DRAW". Then
      back to Character Select with everyone still locked in.
- [ ] Solo: the computer wizard is beatable but not trivial.

## Balance notes to collect

- How long do matches last? Does everyone get to use an ultimate? Computer duels run about
  25 s and four-way matches about 28 s, with ultimates in every match.
- Are the restored LB abilities worth their energy, and is any of them too strong?
- Is any wizard or ability clearly too strong or too weak?
