using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Drives the whole human path headless with two scripted pads and asserts every step:
/// title, joining, picking and locking wizards, the Teams toggle and team switching (including
/// the "both teams need a player" block), stage select, a Teams battle where only the human
/// shoots, the result, and the return to Character Select with everyone still locked in.
///
/// The soak test covers the computer playing; this covers the menus and the device binding
/// that only a human uses. Run with `-- --flowtest` (tools/check.sh does).
/// </summary>
public partial class FlowTest : Node
{
	/// <summary>A pad whose next frame is set by the test. Presses last one frame; holds persist.</summary>
	sealed class ScriptedPad : IInputSource
	{
		public InputState Held;
		InputState next;

		public void Press(Func<InputState, InputState> press) => next = press(next);

		public InputState Poll()
		{
			InputState state = next;
			state.Move = Held.Move; state.Aim = Held.Aim; state.BasicHeld = Held.BasicHeld;
			next = default;
			return state;
		}
	}

	const int PadA = 100, PadB = 101;
	readonly ScriptedPad a = new(), b = new();
	readonly List<(string name, Func<bool> act)> steps = new();
	int step, waited, failures;

	public override void _Ready()
	{
		Name = "FlowTest";
		Devices.Inject(PadA, a);
		Devices.Inject(PadB, b);

		Do("title: Start goes to Character Select", () => a.Press(s => { s.StartPressed = true; return s; }));
		Expect("on Character Select", () => Screen is CharacterSelectScreen);
		Do("pad A joins", () => a.Press(s => { s.StartPressed = true; return s; }));
		Expect("pad A is P1", () => Session.Slots[0].Joined && Session.Slots[0].Device == PadA);
		Do("pad B joins", () => b.Press(s => { s.ConfirmPressed = true; return s; }));
		Expect("pad B is P2", () => Session.Slots[1].Joined && Session.Slots[1].Device == PadB);
		Do("P1 locks in the Fire wizard", () => a.Press(s => { s.ConfirmPressed = true; return s; }));
		Expect("P1 has the Fire wizard", () => Session.Slots[0].Locked && Session.LockedOn(0) == Session.Slots[0]);
		Do("P2 moves left", () => b.Press(s => { s.NavPressed = new Vector2I(-1, 0); return s; }));
		Expect("P2 skips P1's Fire wizard to Electricity", () => Session.Slots[1].WizardIndex == 3);
		Do("P2 moves right", () => b.Press(s => { s.NavPressed = new Vector2I(1, 0); return s; }));
		Expect("P2 skips the Fire wizard again, to Frost", () => Session.Slots[1].WizardIndex == 1);
		Do("P2 moves right again", () => b.Press(s => { s.NavPressed = new Vector2I(1, 0); return s; }));
		Expect("P2 is on the Earth wizard", () => Session.Slots[1].WizardIndex == 2);
		Do("Start before P2 locks does nothing", () => a.Press(s => { s.StartPressed = true; return s; }));
		Expect("still on Character Select", () => Screen is CharacterSelectScreen);
		Do("P2 locks in", () => b.Press(s => { s.ConfirmPressed = true; return s; }));
		Expect("both locked", () => Session.Slots[0].Locked && Session.Slots[1].Locked);
		Do("toggle Teams", () => b.Press(s => { s.ModePressed = true; return s; }));
		Expect("Teams on, P1 Green, P2 Pink", () => Session.TeamsMode && Session.Slots[0].Team == Teams.Green && Session.Slots[1].Team == Teams.Pink);
		Do("P2 switches to Green", () => b.Press(s => { s.TeamPressed = true; return s; }));
		Expect("teams unbalanced", () => Session.TeamsUnbalanced);
		Do("Start while unbalanced does nothing", () => a.Press(s => { s.StartPressed = true; return s; }));
		Expect("still on Character Select", () => Screen is CharacterSelectScreen);
		Do("P2 switches back to Pink", () => b.Press(s => { s.TeamPressed = true; return s; }));
		Expect("teams playable", () => Session.TeamsPlayable);
		Do("Start", () => a.Press(s => { s.StartPressed = true; return s; }));
		Expect("on Stage Select", () => Screen is StageSelectScreen);
		Expect("best of 3 by default", () => Session.RoundsToWin == 2);
		Do("Y: best of 5", () => b.Press(s => { s.TeamPressed = true; return s; }));
		Expect("best of 5", () => Session.RoundsToWin == 3);
		Do("Y again: best of 1", () => b.Press(s => { s.TeamPressed = true; return s; }));
		Expect("best of 1, so one battle decides it", () => Session.RoundsToWin == 1);
		Expect("the goal is knock-out by default", () => Session.Goal == Goal.Knockout);
		Do("RB: King of the Rune", () => b.Press(s => { s.ThirdPressed = true; return s; }));
		Expect("King of the Rune", () => Session.Goal == Goal.KingOfTheRune);
		Do("RB again: back to knock-out", () => b.Press(s => { s.ThirdPressed = true; return s; }));
		Expect("knock-out again", () => Session.Goal == Goal.Knockout);
		// Storm Plateau (down from Woodland): its cover leaves a clear line between the P1 and P2
		// spawns, which the shooting step below needs. Frost Tundra's crystals block it.
		Do("move down on Stage Select", () => b.Press(s => { s.NavPressed = new Vector2I(0, 1); return s; }));
		Do("confirm stage", () => b.Press(s => { s.ConfirmPressed = true; return s; }));
		Expect("in a Teams battle on Storm Plateau", () => Screen is Battle battle && Session.StageIndex == 3 && battle.Wizards.Count == 2);
		Expect("P2 plays the Earth wizard on Pink", () => FindWizard("P2") is Wizard w && w.Data.Id == "EarthWizard" && w.Team == Teams.Pink);
		WaitFor("the countdown ends", () => Screen is Battle battle && battle.Fighting, 300);
		Do("P1 pauses with Start", () => a.Press(s => { s.StartPressed = true; return s; }));
		WaitFor("the battle is paused with the menu open", () => GameRoot.Instance.GetTree().Paused && FindPauseMenu() != null, 30);
		Do("P1 resumes with Start", () => a.Press(s => { s.StartPressed = true; return s; }));
		WaitFor("the battle runs again", () => !GameRoot.Instance.GetTree().Paused && FindPauseMenu() == null, 30);
		Do("P1 aims at P2 and holds fire", () =>
		{
			Wizard p1 = FindWizard("P1"), p2 = FindWizard("P2");
			a.Held.Aim = (p2.GlobalPosition - p1.GlobalPosition).Normalized();
			a.Held.BasicHeld = true;
		});
		WaitFor("P1's fireballs hit P2", () => FindWizard("P2") is Wizard w && w.Health < Wizard.MaxHealth, 300);
		Expect("P2 has not hurt P1 (P2 never fired)", () => FindWizard("P1") is Wizard w && w.Health == Wizard.MaxHealth);
		WaitFor("P2 is knocked out and Green wins", () => Screen is Battle battle && battle.Result == "GREEN", 1200);
		Do("let go of fire", () => a.Held = default);
		WaitFor("back on Character Select after the result", () => Screen is CharacterSelectScreen, 600);
		Expect("players are still joined and locked for a rematch", () => Session.Slots[0].Locked && Session.Slots[1].Locked && Session.TeamsMode);
	}

	static Node Screen => GameRoot.Instance.CurrentScreen;

	static PauseMenu FindPauseMenu()
	{
		if (Screen is not Battle battle) return null;
		foreach (Node node in battle.FindChildren("*", "", true, false)) if (node is PauseMenu menu && !menu.IsQueuedForDeletion()) return menu;
		return null;
	}

	static Wizard FindWizard(string label)
	{
		if (Screen is not Battle battle) return null;
		foreach (Wizard wizard in battle.Wizards) if (wizard.Label == label && GodotObject.IsInstanceValid(wizard)) return wizard;
		return null;
	}

	void Do(string name, Action action) => steps.Add((name, () => { action(); return true; }));
	void Expect(string name, Func<bool> check) => steps.Add(("expect " + name, () => Check(name, check(), true)));

	/// <summary>Waits up to <paramref name="frames"/> physics frames for the condition.</summary>
	void WaitFor(string name, Func<bool> condition, int frames) => steps.Add(("wait for " + name, () =>
	{
		if (condition()) { waited = 0; return Check(name, true, true); }
		if (++waited < frames) return false;
		waited = 0;
		return Check(name, false, true);
	}));

	bool Check(string name, bool ok, bool advance)
	{
		if (ok) GD.Print($"FlowTest: PASS {name}");
		else { failures++; GD.PrintErr($"FlowTest: FAIL {name}"); }
		return advance;
	}

	int settle;

	public override void _PhysicsProcess(double delta)
	{
		// Give each step two frames so a press is polled and acted on before the next check.
		if (++settle < 2) return;
		settle = 0;
		if (step >= steps.Count)
		{
			GD.Print($"FlowTest: {steps.Count} steps, {failures} failure(s)");
			GetTree().Quit(failures == 0 ? 0 : 1);
			SetPhysicsProcess(false);
			return;
		}
		if (steps[step].act()) step++;
	}
}
