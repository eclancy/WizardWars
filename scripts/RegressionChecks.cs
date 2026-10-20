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
		CheckFreezeWiggle();
		CheckOneWizardPerPlayer();
		CheckMatch();
		CheckDifficulty();
		CheckBridgeRoutes();
		CheckSettings();
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

	/// <summary>Every wizard loads with art and five abilities the factory knows how to build.</summary>
	static void CheckWizards()
	{
		WizardData[] wizards = Content.Wizards;
		Expect(wizards.Length == 7, "seven wizards", $"{wizards.Length}");
		HashSet<string> known = new(AbilityFactory.KnownIds);
		HashSet<string> seen = new();
		foreach (WizardData wizard in wizards)
		{
			if (wizard == null) { Expect(false, "wizard loads"); continue; }
			Expect(wizard.Portrait != null && wizard.Targeter != null, $"{wizard.Id} has portrait and targeter");
			AbilityData[] abilities = wizard.Abilities;
			for (int i = 0; i < WizardData.SlotCount; i++)
			{
				AbilityData ability = abilities[i];
				bool ok = ability != null && known.Contains(ability.Id) && ability.DisplayName.Length > 0;
				Expect(ok, $"{wizard.Id} ability {i + 1} ({WizardData.PadButtons[i]})", ability == null ? "missing" : ability.Id);
				if (ability != null) seen.Add(ability.Id);
			}
			// Basics are free and fast; the abilities between cost energy and have their own
			// cooldown (Unity's fourths borrowed the third's); the ultimate costs only the meter.
			AbilityData basic = abilities[WizardData.BasicSlot], fourth = abilities[WizardData.FourthSlot], ultimate = abilities[WizardData.UltimateSlot];
			Expect(basic != null && basic.Cost == 0.0f && basic.Cooldown > 0.0f, $"{wizard.Id} basic attack is free with a cooldown");
			Expect(basic?.Sound != null, $"{wizard.Id} basic attack has a sound");
			Expect(fourth != null && fourth.Cost > 0.0f && fourth.Cost <= 40.0f && fourth.Cooldown > 0.0f, $"{wizard.Id} fourth ability costs 1-40 energy and has a cooldown", fourth == null ? "missing" : $"{fourth.Cost} energy");
			Expect(ultimate != null && ultimate.Cost == 0.0f, $"{wizard.Id} ultimate costs no energy");
		}
		Expect(seen.Count == 35, "35 distinct abilities", $"{seen.Count}");
	}

	/// <summary>
	/// Lava and bog must leave every spawn and cover piece on solid ground, and lava must be
	/// crossable: each bridge spans the river with ground at both ends.
	/// </summary>
	static void CheckHazard(StageData stage)
	{
		Expect(stage.HazardKind is "lava" or "bog", $"{stage.Id} hazard kind '{stage.HazardKind}' is known");
		Expect(stage.Hazards.Length >= 2 && stage.Hazards.Length % 2 == 0, $"{stage.Id} hazard areas come in centre, half-size pairs");
		bool spawnsSafe = true, coverSafe = true;
		foreach (Vector2 spawn in stage.Spawns) spawnsSafe &= !stage.InHazard(spawn) && !stage.InHazard(spawn + new Vector2(0, -0.6f));
		foreach (Vector2 piece in stage.Cover) coverSafe &= !stage.InHazard(piece);
		Expect(spawnsSafe, $"{stage.Id} nobody starts in the {stage.HazardKind}");
		Expect(coverSafe, $"{stage.Id} no cover stands in the {stage.HazardKind}");
		if (stage.HazardKind == "lava")
		{
			Expect(stage.Bridges.Length >= 2 && stage.HazardDamage > 0.0f, $"{stage.Id} lava hurts and has bridges");
			bool crossable = true;
			for (int i = 0; i + 1 < stage.Bridges.Length; i += 2)
			{
				Vector2 centre = stage.Bridges[i], half = stage.Bridges[i + 1];
				for (float x = -half.X; x <= half.X; x += 0.1f) crossable &= !stage.InHazard(centre + new Vector2(x, 0));
				crossable &= !stage.InHazard(centre + new Vector2(half.X + 0.3f, 0)) && !stage.InHazard(centre - new Vector2(half.X + 0.3f, 0));
			}
			Expect(crossable, $"{stage.Id} every bridge crosses from ground to ground");
			Expect(stage.InHazard(new Vector2(stage.Hazards[0].X, 0.0f)), $"{stage.Id} the river itself is lava");
		}
		else Expect(stage.HazardSlow is > 0.0f and < 100.0f, $"{stage.Id} bog slows but never stops you", $"{stage.HazardSlow}%");
	}

	/// <summary>
	/// King of the Rune's spots: inside the arena, on safe ground (not in the lava or bog), clear of
	/// cover, and not on top of anyone's start.
	/// </summary>
	static void CheckRuneSpots(StageData stage)
	{
		Vector2[] spots = stage.RunePoints.Length > 0 ? stage.RunePoints : new[] { Vector2.Zero };
		bool inside = true, safe = true, clearOfCover = true, awayFromSpawns = true;
		foreach (Vector2 spot in spots)
		{
			inside &= InsideArena(spot, RuneHill.RadiusUnits + 0.5f);
			safe &= !stage.InHazard(spot);
			foreach (Vector2 piece in stage.Cover) clearOfCover &= spot.DistanceTo(piece) > RuneHill.RadiusUnits + 0.3f;
			foreach (Vector2 spawn in stage.Spawns) awayFromSpawns &= spot.DistanceTo(spawn) > 3.0f;
		}
		Expect(inside && safe, $"{stage.Id} the rune sits inside the arena on safe ground");
		Expect(clearOfCover, $"{stage.Id} no cover stands in the rune");
		Expect(awayFromSpawns, $"{stage.Id} nobody starts on the rune");
	}

	static bool InsideArena(Vector2 unity, float margin) =>
		Mathf.Abs(unity.X) <= Battle.ArenaHalf.X - margin && Mathf.Abs(unity.Y) <= Battle.ArenaHalf.Y - margin;

	static void CheckStages()
	{
		StageData[] stages = Content.Stages;
		Expect(stages.Length == 6, "six stages", $"{stages.Length}");
		foreach (StageData stage in stages)
		{
			if (stage == null) { Expect(false, "stage loads"); continue; }
			Expect(stage.Background != null, $"{stage.Id} has a background");
			Expect(stage.Caption.Length > 0, $"{stage.Id} has a Stage Select caption");
			if (stage.HazardKind.Length > 0) CheckHazard(stage);
			CheckRuneSpots(stage);
			Expect(stage.Spawns.Length == 4, $"{stage.Id} has four spawns", $"{stage.Spawns.Length}");
			Expect(stage.ComputerSpawn >= 0 && stage.ComputerSpawn < stage.Spawns.Length, $"{stage.Id} computer spawn is valid");
			bool spawnsInside = true;
			foreach (Vector2 spawn in stage.Spawns) spawnsInside &= InsideArena(spawn, 0.7f);
			Expect(spawnsInside, $"{stage.Id} spawns are inside the arena walls");
			// The cover must keep clear of spawns, so nobody starts pinned behind (or against) it.
			bool clear = true;
			foreach (Vector2 spawn in stage.Spawns)
				foreach (Vector2 piece in stage.Cover) clear &= spawn.DistanceTo(piece) > 2.5f;
			Expect(clear, $"{stage.Id} no spawn next to cover");

			Expect(stage.Cover.Length >= 3, $"{stage.Id} has cover to hide behind", $"{stage.Cover.Length} pieces");
			bool known = true;
			try { CoverArt.For(stage.CoverKind); } catch (System.ArgumentException) { known = false; }
			Expect(known, $"{stage.Id} cover kind '{stage.CoverKind}' has art");
			bool coverInside = true;
			foreach (Vector2 piece in stage.Cover) coverInside &= InsideArena(piece, 1.5f);
			Expect(coverInside, $"{stage.Id} cover is inside the arena");
			// Cover is for hiding behind, not for walling the arena in: there is always room to walk
			// between two pieces.
			float closest = float.MaxValue;
			for (int a = 0; a < stage.Cover.Length; a++)
				for (int b = a + 1; b < stage.Cover.Length; b++) closest = Mathf.Min(closest, stage.Cover[a].DistanceTo(stage.Cover[b]));
			Expect(closest >= 3.0f, $"{stage.Id} cover pieces are spread out", $"closest pair {closest:0.0} units apart");
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
		Wizard caster = Wizard.Create(null, data, 0, "P1", Teams.Gold, InputState.NoneSource, Vector2.Zero);
		Wizard ally = Wizard.Create(null, data, 2, "P3", Teams.Gold, InputState.NoneSource, Vector2.Zero);
		Wizard enemy = Wizard.Create(null, data, 1, "P2", Teams.Pink, InputState.NoneSource, Vector2.Zero);
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
	/// A freeze can be wiggled out of: each change of stick direction breaks off
	/// <see cref="Wizard.WiggleBreak"/> (holding one way does nothing), and enough wiggling frees
	/// you. A Tremor stagger is not ice, so there is nothing to wiggle out of.
	/// </summary>
	static void CheckFreezeWiggle()
	{
		Wizard wizard = Wizard.Create(null, Content.Wizards[0], 0, "P1", Teams.NoTeam, InputState.NoneSource, Vector2.Zero);
		wizard.Stun(1.5f);
		Expect(wizard.Frozen, "Ice Wave's stun is a freeze");
		wizard.WiggleFree(Vector2.Left);
		wizard.WiggleFree(Vector2.Left);
		Expect(Mathf.IsEqualApprox(wizard.StunRemaining, 1.5f), "holding the stick one way breaks nothing", $"{wizard.StunRemaining:0.00}s left");
		wizard.WiggleFree(Vector2.Right);
		wizard.WiggleFree(Vector2.Left);
		Expect(Mathf.IsEqualApprox(wizard.StunRemaining, 1.5f - 2 * Wizard.WiggleBreak), "each wiggle breaks off some of the freeze", $"{wizard.StunRemaining:0.00}s left");
		for (int i = 0; i < 20 && wizard.Stunned; i++) wizard.WiggleFree(i % 2 == 0 ? Vector2.Right : Vector2.Left);
		Expect(!wizard.Stunned && !wizard.Frozen, "wiggling breaks out of the ice");
		wizard.Stun(0.75f, StunKind.Staggered);
		Expect(wizard.Stunned && !wizard.Frozen, "a stagger is not a freeze");
		wizard.Free();
	}

	/// <summary>Best of 3: first to two round wins takes it; a drawn round counts for nobody; it cannot run forever.</summary>
	static void CheckMatch()
	{
		BattleSetup setup = new() { Stage = Content.Stages[0] };
		setup.Fighters.Add(new Fighter { Wizard = Content.Wizards[0], Label = "P1" });
		setup.Fighters.Add(new Fighter { Wizard = Content.Wizards[1], Label = "P2" });
		Match match = new(setup, 2);
		match.Record("P1");
		Expect(!match.Decided && match.Round == 2 && match.Wins("P1") == 1, "one round won is not the match");
		match.Record("DRAW");
		Expect(!match.Decided && match.Round == 3, "a drawn round counts for nobody");
		match.Record("P1");
		Expect(match.Decided && match.Winner == "P1", "two round wins take a best of 3");
		Match endless = new(setup, 2);
		for (int i = 0; i < 20 && !endless.Decided; i++) endless.Record("DRAW");
		Expect(endless.Decided && endless.Winner == "DRAW" && endless.Round <= endless.MaxRounds, "all draws end the match as a draw", $"after {endless.Round} rounds");
		setup.TeamsActive = true;
		Expect(new Match(setup, 2).Sides.Count == 2, "a Teams match is scored by team");
		Expect(new Match(setup, 1) is { RoundsToWin: 1 } single && RecordAndCheck(single, "GOLD"), "best of 1 is decided by one round");
	}

	static bool RecordAndCheck(Match match, string result)
	{
		match.Record(result);
		return match.Decided && match.Winner == result;
	}

	/// <summary>
	/// The computer's route over the Volcano's lava: from one bank to the other it heads for a
	/// bridge's near end, then its far end; on the same bank it needs no route.
	/// </summary>
	static void CheckBridgeRoutes()
	{
		StageData volcano = System.Array.Find(Content.Stages, s => s.Id == "volcano");
		if (volcano == null) { Expect(false, "the Volcano exists"); return; }
		Vector2? start = CpuInputSource.Route(volcano, new Vector2(-6, 0), new Vector2(7, 0));
		Expect(start is Vector2 s1 && s1.X < -1.5f && Mathf.Abs(Mathf.Abs(s1.Y) - 3.0f) < 0.1f, "across the lava, head for a bridge on this bank", $"{start}");
		Vector2? across = CpuInputSource.Route(volcano, new Vector2(-2.2f, 3.0f), new Vector2(7, 0));
		Expect(across is Vector2 s2 && s2.X > 1.5f, "at the bridge, cross to the far end", $"{across}");
		Vector2? middle = CpuInputSource.Route(volcano, new Vector2(0.0f, -3.0f), new Vector2(-7, 4));
		Expect(middle is Vector2 s3 && s3.X < -1.5f, "mid-bridge, carry on toward the target's side", $"{middle}");
		Expect(CpuInputSource.Route(volcano, new Vector2(-6, 4), new Vector2(-6, -4)) == null, "on the same bank, no route is needed");
		Expect(CpuInputSource.Route(Content.Stages[0], new Vector2(-6, 0), new Vector2(7, 0)) == null, "a stage without lava needs no route");
	}

	/// <summary>Easy is slower and looser than Normal, and Hard sharper.</summary>
	static void CheckDifficulty()
	{
		CpuInputSource easy = new(Difficulty.Easy), normal = new(Difficulty.Normal), hard = new(Difficulty.Hard);
		Expect(easy.AimErrorDegrees > normal.AimErrorDegrees && normal.AimErrorDegrees > hard.AimErrorDegrees, "aim tightens with difficulty");
		Expect(easy.ReactionDelay > normal.ReactionDelay && normal.ReactionDelay > hard.ReactionDelay, "reactions quicken with difficulty");
		Expect(easy.DodgeChance < normal.DodgeChance && normal.DodgeChance < hard.DodgeChance, "dodging improves with difficulty");
		Expect(normal.AimErrorDegrees == new CpuInputSource().AimErrorDegrees, "Normal is the soak test's computer");
	}

	/// <summary>Volumes step by tenths and stay in 0..1 (checks never save: Settings.Persist is off).</summary>
	static void CheckSettings()
	{
		Expect(!Settings.Persist, "checks do not touch the saved settings");
		float before = Settings.MusicVolume;
		Settings.StepMusic(20);
		Expect(Settings.MusicVolume == 1.0f, "volume tops out at 100%");
		Settings.StepMusic(-30);
		Expect(Settings.MusicVolume == 0.0f, "volume bottoms out at off");
		Settings.StepMusic(Mathf.RoundToInt(before * 10.0f));
		Expect(Mathf.IsEqualApprox(Settings.MusicVolume, before), "volume steps by tenths", $"{Settings.MusicVolume}");
	}

	/// <summary>One player per wizard: cursors skip a wizard someone else has locked in.</summary>
	static void CheckOneWizardPerPlayer()
	{
		Session.Reset();
		PlayerSlot p1 = Session.Slots[0], p2 = Session.Slots[1];
		p1.Joined = p2.Joined = true;
		p1.WizardIndex = 0;
		p1.Locked = true;
		Expect(Session.LockedOn(0, p2) == p1 && Session.LockedOn(0, p1) == null, "a locked wizard is taken for everyone else");
		int last = Content.Wizards.Length - 1;
		Expect(Session.NextFree(1, -1, p2) == last, "moving left skips a taken wizard, round to the last", $"{Session.NextFree(1, -1, p2)}");
		Expect(Session.FirstFree(0, p2) == 1, "a player on a taken wizard moves to the next free one");
		p2.WizardIndex = 1;
		p2.Locked = true;
		Expect(Session.NextFree(last, 1, Session.Slots[2]) == 2, "two taken wizards are both skipped", $"{Session.NextFree(last, 1, Session.Slots[2])}");
		Session.Reset();
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
