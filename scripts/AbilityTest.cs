using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Casts every one of the 16 abilities at a standing target and checks it does what it should,
/// then casts it again at a teammate and checks it does nothing at all. Afterwards the arena
/// must empty: no effect nodes, no modifiers, no stuck ultimate lock.
///
/// Matches are too short for most ultimates to come up in the soak test, so this is where
/// every ability - and friendly fire for every ability - is actually exercised.
/// Run with `-- --abilities` (tools/check.sh does).
/// </summary>
public partial class AbilityTest : Node
{
	const double Observe = 6.0;
	const double Settle = 7.0;

	sealed class Aimer : IInputSource
	{
		public InputState State;
		public InputState Poll() => State;
	}

	readonly List<(WizardData wizard, int slot, bool ally)> cases = new();
	int index = -1, failures;
	Battle battle;
	Wizard caster, target;
	Aimer aimer;
	double castAt;
	bool cast, observed;

	// What the target went through while being watched.
	float lowestHealth;
	bool sawSlow, sawSlide, sawNoRegen, sawStun, sawBurn, sawCasterHaste, sawCasterUltimate;
	int maxEffects, sawFireSources, sawRockWalls, sawMeteors, sawRods;

	public override void _Ready()
	{
		Name = "AbilityTest";
		foreach (WizardData wizard in Content.Wizards)
			for (int slot = 0; slot < 4; slot++)
			{
				cases.Add((wizard, slot, false));
				cases.Add((wizard, slot, true));
			}
		Next();
	}

	void Next()
	{
		battle?.QueueFree();
		index++;
		if (index >= cases.Count)
		{
			GD.Print($"AbilityTest: {cases.Count} casts, {failures} failure(s)");
			GetTree().Quit(failures == 0 ? 0 : 1);
			return;
		}

		(WizardData wizard, int slot, bool ally) = cases[index];
		BattleSetup setup = new() { Stage = Content.Stages[0], TeamsActive = true };
		aimer = new Aimer();
		setup.Fighters.Add(new Fighter { Wizard = wizard, Slot = 0, Spawn = 0, Label = "P1", Team = Teams.Red, Input = aimer });
		// A standing target that never acts. The Earth wizard is used unless he is the caster,
		// so there is always a different wizard to hit.
		WizardData dummy = wizard.Id == "EarthWizard" ? Content.Wizards[0] : Content.Wizards[2];
		setup.Fighters.Add(new Fighter { Wizard = dummy, Slot = 1, Spawn = 1, Label = "P2", Team = ally ? Teams.Red : Teams.Blue, Input = InputState.NoneSource });
		battle = Battle.Create(setup);
		battle.SkipCountdown = true;
		AddChild(battle);

		cast = observed = false;
		lowestHealth = Wizard.MaxHealth;
		sawSlow = sawSlide = sawNoRegen = sawStun = sawBurn = sawCasterHaste = sawCasterUltimate = false;
		maxEffects = sawFireSources = sawRockWalls = sawMeteors = sawRods = 0;
	}

	string CaseName => $"{cases[index].wizard.Abilities[cases[index].slot].Id} at {(cases[index].ally ? "a teammate" : "an enemy")}";

	public override void _PhysicsProcess(double delta)
	{
		if (battle == null || !battle.IsInsideTree() || index >= cases.Count) return;

		if (caster == null)
		{
			// Stand them 3.5 units apart in the open middle of Woodland.
			foreach (Wizard wizard in battle.Wizards) if (wizard.Label == "P1") caster = wizard; else target = wizard;
			caster.GlobalPosition = Units.U(-2.0f, 0.0f);
			target.GlobalPosition = Units.U(1.5f, 0.0f);
			Vector2 offset = target.GlobalPosition - caster.GlobalPosition;
			aimer.State = new InputState { Aim = offset.Normalized() * Mathf.Min(1.0f, offset.Length() / Units.U(4.5f)) };
			return;
		}

		int slot = cases[index].slot;
		if (!cast)
		{
			if (slot == 3) caster.AddUltimateCharge(100.0f);
			if (slot == 2 && cases[index].wizard.Id == "FireWizard") aimer.State.ThirdHeld = true;
			if (!caster.Abilities[slot].TryCast()) Fail("the cast was refused");
			cast = true;
			castAt = battle.Time;
			return;
		}

		double since = battle.Time - castAt;
		if (since < Observe) { Watch(); return; }
		if (!observed)
		{
			observed = true;
			aimer.State = default;
			Judge();
			return;
		}
		if (since < Observe + Settle) return;

		if (battle.EffectCount > 0) Fail($"{battle.EffectCount} effect(s) left: {battle.DescribeEffects()}");
		foreach (Wizard wizard in new[] { caster, target })
		{
			if (!GodotObject.IsInstanceValid(wizard) || !wizard.Alive) continue;
			if (wizard.Status.Count > 0) Fail($"{wizard.Label} still carries {wizard.Status.Count} modifier(s)");
			if (wizard.UltimateActive) Fail($"{wizard.Label} is stuck with an ultimate active");
		}
		caster = target = null;
		CallDeferred(MethodName.Next);
	}

	void Watch()
	{
		if (GodotObject.IsInstanceValid(target) && target.Alive)
		{
			// Keep the target standing still where it was put, so patches and auras stay on it.
			lowestHealth = Mathf.Min(lowestHealth, target.Health);
			sawSlow |= target.Status.SpeedMultiplier < 0.99f;
			sawSlide |= target.Status.Sliding;
			sawNoRegen |= target.Status.NoRegen;
			sawStun |= target.Stunned;
			sawBurn |= target.OnFire;
		}
		else lowestHealth = 0.0f;
		if (GodotObject.IsInstanceValid(caster) && caster.Alive)
		{
			sawCasterHaste |= caster.Status.SpeedMultiplier > 1.01f;
			sawCasterUltimate |= caster.UltimateActive;
		}
		maxEffects = Math.Max(maxEffects, battle.EffectCount);
		sawFireSources = Math.Max(sawFireSources, battle.FireSources.Count);
		sawRockWalls = Math.Max(sawRockWalls, battle.RockWalls.Count);
		int meteors = 0, rods = 0;
		foreach (Node child in battle.GetNode("Effects").GetChildren())
		{
			if (child is Meteor) meteors++;
			if (child is LightningRod) rods++;
		}
		sawMeteors = Math.Max(sawMeteors, meteors);
		sawRods = Math.Max(sawRods, rods);
	}

	void Judge()
	{
		string id = cases[index].wizard.Abilities[cases[index].slot].Id;
		bool ally = cases[index].ally;
		bool hurt = lowestHealth < Wizard.MaxHealth;
		bool touched = hurt || sawSlow || sawSlide || sawNoRegen || sawStun || sawBurn;

		// Effects that do not depend on who they hit.
		switch (id)
		{
			case "ring-of-fire": Expect(sawFireSources >= 10, $"lit {sawFireSources} flames"); break;
			case "rock-wall": Expect(sawRockWalls == 1, "raised a wall"); break;
			case "meteor-storm": Expect(sawMeteors > 0 && sawCasterUltimate, "meteors fell during the ultimate"); break;
			case "overcharge": Expect(sawRods > 0 && sawCasterUltimate, "rods were scattered during the ultimate"); break;
			case "speed-up": Expect(sawCasterHaste, "the caster sped up"); break;
			case "absolute-zero":
			case "earthquake": Expect(sawCasterUltimate, "the ultimate ran"); break;
		}

		if (ally)
		{
			Expect(!touched, "a teammate is untouched",
				$"health {lowestHealth}, slow {sawSlow}, slide {sawSlide}, no-regen {sawNoRegen}, stun {sawStun}, burn {sawBurn}");
			return;
		}

		switch (id)
		{
			case "fireball": case "icicle": case "mudball": case "electricity-ball":
				Expect(hurt, "the bolt hit"); break;
			case "flamethrower": Expect(hurt && sawBurn, "the flame burned"); break;
			case "ice-patch": Expect(sawSlide, "the target was on ice"); break;
			case "ice-wave": Expect(sawStun && hurt, "the target was frozen and hurt"); break;
			case "absolute-zero": Expect(sawSlow && sawNoRegen, "the target was slowed with regen off"); break;
			case "mud-patch": Expect(sawSlow, "the target was slowed"); break;
			case "earthquake": Expect(sawSlow, "the target was slowed"); break;
			case "lightning-rod": Expect(hurt, "the rod shocked the target"); break;
			case "ring-of-fire": case "rock-wall": case "meteor-storm": case "overcharge": case "speed-up":
				break; // placement or chance, checked above
			default: Fail($"no expectation for {id}"); break;
		}
	}

	void Expect(bool ok, string what, string detail = "")
	{
		if (ok) GD.Print($"AbilityTest: PASS {CaseName}: {what}");
		else Fail(what + (detail.Length > 0 ? $" ({detail})" : ""));
	}

	void Fail(string message)
	{
		failures++;
		GD.PrintErr($"AbilityTest: FAIL {CaseName}: {message}");
	}
}
