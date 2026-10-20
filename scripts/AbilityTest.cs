using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Casts every one of the 20 abilities at a standing target and checks it does what it should,
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
	bool sawSlow, sawSlide, sawNoRegen, sawStun, sawBurn, sawCasterHaste, sawCasterUltimate, touchedInBuildUp;
	// For the Shadow, Nature and Wind kits: roots, silences, a caster that moved or healed.
	bool sawRoot, sawSilenced, sawCasterMoved, sawCasterHealed;
	Vector2 casterStart;
	float casterHealthAtCast;
	int maxEffects, sawFireSources, sawRockWalls, sawMeteors, sawRods;

	public override void _Ready()
	{
		Name = "AbilityTest";
		foreach (WizardData wizard in Content.Wizards)
			for (int slot = 0; slot < WizardData.SlotCount; slot++)
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
		BattleSetup setup = new() { Stage = OpenGround, TeamsActive = true };
		aimer = new Aimer();
		setup.Fighters.Add(new Fighter { Wizard = wizard, Slot = 0, Spawn = 0, Label = "P1", Team = Teams.Gold, Input = aimer });
		// A standing target that never acts. The Earth wizard is used unless he is the caster,
		// so there is always a different wizard to hit.
		WizardData dummy = wizard.Id == "EarthWizard" ? Content.Wizards[0] : Content.Wizards[2];
		setup.Fighters.Add(new Fighter { Wizard = dummy, Slot = 1, Spawn = 1, Label = "P2", Team = ally ? Teams.Gold : Teams.Pink, Input = InputState.NoneSource });
		battle = Battle.Create(setup);
		battle.SkipCountdown = true;
		AddChild(battle);

		cast = observed = false;
		lowestHealth = Wizard.MaxHealth;
		sawSlow = sawSlide = sawNoRegen = sawStun = sawBurn = sawCasterHaste = sawCasterUltimate = touchedInBuildUp = false;
		sawRoot = sawSilenced = sawCasterMoved = sawCasterHealed = false;
		maxEffects = sawFireSources = sawRockWalls = sawMeteors = sawRods = 0;
	}

	/// <summary>
	/// Woodland with its cover taken out. Abilities are measured on open ground: a stump on Ring
	/// of Fire's circle rightly puts flames out, but that is the cover working, not the ring.
	/// </summary>
	static StageData openGround;
	static StageData OpenGround
	{
		get
		{
			if (openGround != null) return openGround;
			openGround = (StageData)Content.Stages[0].Duplicate();
			openGround.Cover = System.Array.Empty<Vector2>();
			return openGround;
		}
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
			if (slot == WizardData.UltimateSlot) caster.AddUltimateCharge(100.0f);
			// Regrowth needs something to heal.
			if (cases[index].wizard.Abilities[slot].Id == "regrowth") caster.TakeDamage(100.0f);
			casterStart = caster.GlobalPosition;
			casterHealthAtCast = caster.Health;
			if (slot == WizardData.ThirdSlot && cases[index].wizard.Id == "FireWizard") aimer.State.ThirdHeld = true;
			if (!caster.Abilities[slot].TryCast()) Fail("the cast was refused");
			cast = true;
			castAt = battle.Time;
			return;
		}

		double since = battle.Time - castAt;
		if (since < Observe) { Watch(since); return; }
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

	void Watch(double since)
	{
		if (GodotObject.IsInstanceValid(target) && target.Alive)
		{
			// An ultimate must leave everyone alone until its build-up is over.
			if (cases[index].slot == WizardData.UltimateSlot && since < UltimateBuildUp.Duration - 0.05)
				touchedInBuildUp |= target.Health < Wizard.MaxHealth || target.Status.Count > 0 || target.Stunned;
			// Keep the target standing still where it was put, so patches and auras stay on it.
			lowestHealth = Mathf.Min(lowestHealth, target.Health);
			sawSlow |= target.Status.SpeedMultiplier < 0.99f;
			sawSlide |= target.Status.Sliding;
			sawNoRegen |= target.Status.NoRegen;
			sawStun |= target.Stunned;
			sawRoot |= target.Status.Rooted;
			sawSilenced |= target.Status.Silenced;
			sawBurn |= target.OnFire;
		}
		else lowestHealth = 0.0f;
		if (GodotObject.IsInstanceValid(caster) && caster.Alive)
		{
			sawCasterHaste |= caster.Status.SpeedMultiplier > 1.01f;
			sawCasterMoved |= caster.GlobalPosition.DistanceTo(casterStart) > Units.U(1.5f);
			sawCasterHealed |= caster.Health > casterHealthAtCast + 15.0f;
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
		bool touched = hurt || sawSlow || sawSlide || sawNoRegen || sawStun || sawBurn || sawRoot || sawSilenced;

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
			case "meteor-strike": Expect(sawMeteors > 0 && !sawCasterUltimate, "a meteor fell without locking the ultimate"); break;
			case "shadow-step": Expect(sawCasterMoved, "the caster stepped away"); break;
			case "regrowth": Expect(sawCasterHealed, "the caster healed"); break;
			case "updraft": Expect(sawCasterHaste, "the caster sped up"); break;
			case "tailwind-dash": Expect(sawCasterMoved, "the caster dashed"); break;
			case "eclipse": case "overgrowth": case "tempest": Expect(sawCasterUltimate, "the ultimate ran"); break;
		}

		if (cases[index].slot == WizardData.UltimateSlot) Expect(!touchedInBuildUp, "nothing happened during the build-up");

		if (ally)
		{
			Expect(!touched, "a teammate is untouched",
				$"health {lowestHealth}, slow {sawSlow}, slide {sawSlide}, no-regen {sawNoRegen}, stun {sawStun}, burn {sawBurn}");
			return;
		}

		switch (id)
		{
			case "fireball": case "icicle": case "mudball": case "electricity-ball": case "shadow-bolt": case "thorn": case "gust":
				Expect(hurt, "the bolt hit"); break;
			case "flamethrower": Expect(hurt && sawBurn, "the flame burned"); break;
			case "ice-patch": Expect(sawSlide, "the target was on ice"); break;
			case "ice-wave": Expect(sawStun && hurt, "the target was frozen and hurt"); break;
			case "absolute-zero": Expect(sawSlow && sawNoRegen, "the target was slowed with regen off"); break;
			case "mud-patch": Expect(sawSlow, "the target was slowed"); break;
			case "earthquake": Expect(sawSlow, "the target was slowed"); break;
			case "lightning-rod": Expect(hurt, "the rod shocked the target"); break;
			// The fourth abilities.
			case "meteor-strike": Expect(hurt && sawBurn, "the meteor hit and set the target burning"); break;
			case "deep-freeze": Expect(sawStun && hurt, "the target was frozen solid"); break;
			case "tremor": Expect(sawStun && hurt, "the target was hurt and staggered"); break;
			case "chain-lightning": Expect(hurt, "the lightning struck the target"); break;
			// Shadow, Nature and Wind.
			case "smoke-cloud": Expect(sawSlow, "the target was slowed in the smoke"); break;
			case "night-grasp": case "eclipse": Expect(hurt && sawSilenced, "the target was hurt and silenced"); break;
			case "bramble-patch": case "overgrowth": Expect(hurt && sawSlow, "the target was slowed and hurt"); break;
			case "entangle": Expect(hurt && sawRoot, "the target was hurt and rooted"); break;
			case "tailwind-dash": Expect(hurt, "the dash hit the target"); break;
			case "cyclone": case "tempest": Expect(hurt, "the wind battered the target"); break;
			case "shadow-step": case "regrowth": case "updraft":
				break; // they act on the caster, checked above
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
