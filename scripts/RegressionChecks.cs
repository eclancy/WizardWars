using System.Collections.Generic;
using Godot;

/// <summary>
/// The checks dotnet build cannot do: project settings, data wiring, and the game's rules.
/// Run headless by tools/check.sh (`-- --checks`), which reads the exit code. The soak test
/// (`-- --soak`) covers what only a running match can show.
///
/// Every line is printed as "RegressionChecks: PASS|FAIL name - detail" so a failure is
/// greppable. Add a check whenever a rule is easy to break silently.
/// </summary>
public static class RegressionChecks
{
	static int passed;
	static int failed;

	/// <summary>Runs every check and returns the number that failed.</summary>
	public static int RunAll()
	{
		passed = 0;
		failed = 0;

		CheckPhysicsLayers();
		CheckViewport();
		CheckUnits();
		CheckWizards();
		CheckStages();
		CheckStatusEffects();
		CheckFriendlyFire();
		CheckUltimateCharge();
		CheckMusic();
		CheckBalance();

		GD.Print($"RegressionChecks: {passed} passed, {failed} failed");
		return failed;
	}

	static void Expect(bool condition, string name, string detail = "")
	{
		if (condition) passed++; else failed++;
		string suffix = detail.Length > 0 ? " - " + detail : "";
		if (condition) GD.Print($"RegressionChecks: PASS {name}{suffix}");
		else GD.PrintErr($"RegressionChecks: FAIL {name}{suffix}");
	}

	/// <summary>Collision masks are set by layer number; renaming or reordering breaks them silently.</summary>
	static void CheckPhysicsLayers()
	{
		string[] expected = { "solids", "wizards", "projectiles", "zones" };
		for (int i = 0; i < expected.Length; i++)
		{
			string actual = (string)ProjectSettings.GetSetting($"layer_names/2d_physics/layer_{i + 1}", "");
			Expect(actual == expected[i], $"physics layer {i + 1}", $"expected '{expected[i]}', found '{actual}'");
		}
	}

	static void CheckViewport()
	{
		int width = (int)ProjectSettings.GetSetting("display/window/size/viewport_width", 0);
		int height = (int)ProjectSettings.GetSetting("display/window/size/viewport_height", 0);
		Expect(width == 1920 && height == 1080, "viewport is 1920x1080", $"{width}x{height}");
	}

	/// <summary>The Unity arena has to fit the 1080p viewport at the chosen scale.</summary>
	static void CheckUnits()
	{
		Vector2 arena = Units.ArenaSize * Units.PixelsPerUnit;
		Expect(arena.Y <= 1080.0f, "arena height fits 1080", $"{arena.Y:0} px");
		Expect(Units.U(1.0f, 1.0f).Y < 0.0f, "Unity +y maps to Godot up");
	}

	/// <summary>Every wizard loads with art and four abilities the factory knows how to build.</summary>
	static void CheckWizards()
	{
		WizardData[] wizards = Content.Wizards;
		Expect(wizards.Length == 4, "four wizards", $"{wizards.Length}");
		HashSet<string> known = new(AbilityFactory.KnownIds);
		HashSet<string> seen = new();
		foreach (WizardData wizard in wizards)
		{
			if (wizard == null) { Expect(false, "wizard loads"); continue; }
			Expect(wizard.Portrait != null && wizard.Targeter != null, $"{wizard.Id} has portrait and targeter");
			AbilityData[] abilities = wizard.Abilities;
			for (int i = 0; i < 4; i++)
			{
				AbilityData ability = abilities[i];
				bool ok = ability != null && known.Contains(ability.Id) && ability.DisplayName.Length > 0;
				Expect(ok, $"{wizard.Id} ability {i + 1}", ability == null ? "missing" : ability.Id);
				if (ability != null) seen.Add(ability.Id);
			}
			// Basics are free and fast; everything else costs energy or the ultimate meter.
			Expect(abilities[0] != null && abilities[0].Cost == 0.0f && abilities[0].Cooldown > 0.0f, $"{wizard.Id} basic attack is free with a cooldown");
			Expect(abilities[0]?.Sound != null, $"{wizard.Id} basic attack has a sound");
		}
		Expect(seen.Count == 16, "16 distinct abilities", $"{seen.Count}");
	}

	static bool InsideArena(Vector2 unity, float margin) =>
		Mathf.Abs(unity.X) <= Battle.ArenaHalf.X - margin && Mathf.Abs(unity.Y) <= Battle.ArenaHalf.Y - margin;

	static void CheckStages()
	{
		StageData[] stages = Content.Stages;
		Expect(stages.Length == 4, "four stages", $"{stages.Length}");
		foreach (StageData stage in stages)
		{
			if (stage == null) { Expect(false, "stage loads"); continue; }
			Expect(stage.Background != null, $"{stage.Id} has a background");
			Expect(stage.Spawns.Length == 4, $"{stage.Id} has four spawns", $"{stage.Spawns.Length}");
			Expect(stage.ComputerSpawn >= 0 && stage.ComputerSpawn < stage.Spawns.Length, $"{stage.Id} computer spawn is valid");
			bool spawnsInside = true;
			foreach (Vector2 spawn in stage.Spawns) spawnsInside &= InsideArena(spawn, 0.7f);
			Expect(spawnsInside, $"{stage.Id} spawns are inside the arena walls");
			bool clear = true;
			foreach (Vector2 spawn in stage.Spawns)
			{
				foreach (Vector2 rock in stage.Rocks) clear &= spawn.DistanceTo(rock) > 1.5f;
				foreach (Vector2 cactus in stage.Cacti) clear &= spawn.DistanceTo(cactus) > 1.5f;
			}
			Expect(clear, $"{stage.Id} no spawn inside an obstacle");
		}
	}

	/// <summary>The rules that replaced Unity's additive slow()/removeSlow().</summary>
	static void CheckStatusEffects()
	{
		StatusEffects status = new();
		object patch = new(), quake = new();
		status.Set(patch, new Modifier { Slow = 35 });
		status.Set(patch, new Modifier { Slow = 35 });
		Expect(Mathf.IsEqualApprox(status.SpeedMultiplier, 0.65f), "re-applying a source does not stack", $"{status.SpeedMultiplier}");
		status.Set(quake, new Modifier { Slow = 55 });
		Expect(Mathf.IsEqualApprox(status.SpeedMultiplier, 0.10f), "different sources do stack", $"{status.SpeedMultiplier}");
		status.Set(new object(), new Modifier { Slow = 50 });
		Expect(status.SpeedMultiplier == 0.0f, "speed never goes negative", $"{status.SpeedMultiplier}");
		status.Clear();
		Expect(status.SpeedMultiplier == 1.0f && !status.NoRegen && !status.Sliding, "a clear status is neutral");

		Node source = new();
		status.Set(source, new Modifier { Slide = true, NoRegen = true });
		source.Free();
		status.Prune();
		Expect(status.Count == 0 && !status.Sliding, "a freed source's modifier is pruned");
	}

	/// <summary>Teammates are never hit; the caster only by patches and rods; everyone else always.</summary>
	static void CheckFriendlyFire()
	{
		WizardData data = Content.Wizards[0];
		Wizard caster = Wizard.Create(null, data, 0, "P1", Teams.Red, InputState.NoneSource, Vector2.Zero);
		Wizard ally = Wizard.Create(null, data, 2, "P3", Teams.Red, InputState.NoneSource, Vector2.Zero);
		Wizard enemy = Wizard.Create(null, data, 1, "P2", Teams.Blue, InputState.NoneSource, Vector2.Zero);
		Wizard loner = Wizard.Create(null, data, 3, "P4", Teams.NoTeam, InputState.NoneSource, Vector2.Zero);

		Expect(Combat.CanAffect(caster, caster.Team, enemy), "an enemy is hit");
		Expect(!Combat.CanAffect(caster, caster.Team, ally), "a teammate is never hit");
		Expect(!Combat.CanAffect(caster, caster.Team, ally, hitsCaster: true), "a teammate is not hit even by caster-hitting effects");
		Expect(!Combat.CanAffect(caster, caster.Team, caster), "the caster is spared by default");
		Expect(Combat.CanAffect(caster, caster.Team, caster, hitsCaster: true), "patches and rods hit their caster");
		Expect(Combat.CanAffect(loner, Teams.NoTeam, caster) && Combat.CanAffect(caster, Teams.NoTeam, loner), "Free-for-All hits everyone");
		Expect(!Teams.Allied(Teams.NoTeam, Teams.NoTeam), "no-team players are not allies");

		foreach (Wizard wizard in new[] { caster, ally, enemy, loner }) wizard.Free();
	}

	/// <summary>
	/// Both tracks must load and loop. The loop is an import setting, so a re-import with
	/// default settings would quietly reintroduce a gap or silence at the end of the track.
	/// </summary>
	static void CheckMusic()
	{
		AudioStream title = Content.Sound("res://assets/music/title.mp3");
		AudioStream battle = Content.Sound("res://assets/music/battle.wav");
		Expect(title is AudioStreamMP3 { Loop: true }, "title music loads and loops");
		Expect(battle is AudioStreamWav { LoopMode: AudioStreamWav.LoopModeEnum.Forward }, "battle music loads and loops");
	}

	/// <summary>
	/// The balance file must load (a missing file would silently fall back to the old Unity
	/// numbers, where fights ended before anyone's ultimate filled), and the game starts
	/// fullscreen.
	/// </summary>
	static void CheckBalance()
	{
		BalanceData balance = ResourceLoader.Load<BalanceData>(Content.BalancePath);
		Expect(balance != null, "data/balance.tres loads");
		if (balance == null) return;
		Expect(balance.MaxHealth > 0 && balance.MaxEnergy > 0 && balance.MoveSpeed > 0, "balance values are positive");
		// The passive trickle alone must fill the meter well inside a fight's length.
		Expect(balance.UltimateChargeRate > 0 && 100.0f / balance.UltimateChargeRate < 60.0f, "the passive charge alone fills the ultimate within a minute", $"{100.0f / balance.UltimateChargeRate:0}s");
		int mode = (int)ProjectSettings.GetSetting("display/window/size/mode", 0);
		Expect(mode == (int)DisplayServer.WindowMode.Fullscreen, "the game starts fullscreen", $"window mode {mode}");
	}

	static void CheckUltimateCharge()
	{
		Wizard wizard = Wizard.Create(null, Content.Wizards[0], 0, "P1", Teams.NoTeam, InputState.NoneSource, Vector2.Zero);
		wizard.AddUltimateCharge(99.0f);
		Expect(!wizard.UltimateReady, "99 charge is not ready");
		wizard.AddUltimateCharge(5.0f);
		Expect(wizard.UltimateReady && wizard.UltimateCharge == 100.0f, "charge caps at 100 and is ready");
		Expect(wizard.TryUseUltimate() && !wizard.UltimateReady && wizard.UltimateCharge == 0.0f, "using the ultimate empties the meter");
		Expect(!wizard.TryUseUltimate(), "an empty meter cannot fire");
		wizard.Free();
	}
}
