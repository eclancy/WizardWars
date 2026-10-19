using System.Collections.Generic;
using Godot;

/// <summary>
/// Plays computer-only matches back to back and checks nothing leaks. Run headless with
/// --fixed-fps so it goes faster than real time (tools/check.sh does).
///
/// After each match ends it lets the arena settle for longer than any effect lives, then
/// requires that every effect node is gone, that the surviving wizards carry no modifiers, and
/// that no ultimate lock is stuck on - the failure class behind Unity's Absolute Zero and
/// Earthquake leaks. A match that runs past the time limit fails too: the AI should always
/// finish a fight.
/// </summary>
public partial class SoakTest : Node
{
	const double MatchTimeLimit = 240.0;
	const double SettleSeconds = 8.0;

	readonly int matches;
	readonly bool teams, king;
	readonly int players = 4;
	int played, failures;
	// Balance report: how long fights last and whether ultimates come up.
	double totalSeconds;
	int totalUltimates, matchesWithUltimate;
	Battle battle;
	double settleUntil = -1.0;
	readonly RandomNumberGenerator rng = new();

	public SoakTest() { }

	public SoakTest(int matches, bool teams, int players = 4, bool king = false)
	{
		this.matches = matches;
		this.teams = teams;
		this.king = king;
		this.players = teams ? 4 : Mathf.Clamp(players, 2, 4);
		Name = "SoakTest";
	}

	public override void _Ready()
	{
		rng.Seed = 12345;
		GD.Seed(12345);
		StartMatch();
	}

	void StartMatch()
	{
		BattleSetup setup = new() { Stage = Content.Stages[played % Content.Stages.Length], TeamsActive = teams, Goal = king ? Goal.KingOfTheRune : Goal.Knockout };
		List<string> names = new();
		for (int i = 0; i < players; i++)
		{
			// Cycle the wizards so every one plays every stage across the run.
			WizardData wizard = Content.Wizards[(i + played) % Content.Wizards.Length];
			names.Add(wizard.DisplayName);
			setup.Fighters.Add(new Fighter { Wizard = wizard, Slot = -1, Spawn = i, Label = "CPU" + (i + 1), Team = teams ? Teams.DefaultForSlot(i) : Teams.NoTeam });
		}
		battle = Battle.Create(setup);
		battle.SkipCountdown = true;
		AddChild(battle);
		settleUntil = -1.0;
		GD.Print($"Soak: match {played + 1}/{matches} on {setup.Stage.DisplayName}{(teams ? " (teams)" : "")}{(king ? " (king of the rune)" : "")}: {string.Join(", ", names)}");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (battle == null) return;

		if (!battle.Over)
		{
			if (battle.Time < MatchTimeLimit) return;
			Fail($"match {played + 1} still running after {MatchTimeLimit:0}s");
			FinishMatch();
			return;
		}

		if (settleUntil < 0.0) settleUntil = battle.Time + SettleSeconds;
		if (battle.Time < settleUntil) return;

		if (battle.EffectCount > 0) Fail($"match {played + 1}: {battle.EffectCount} effect node(s) still alive {SettleSeconds:0}s after the result ({battle.DescribeEffects()})");
		foreach (Wizard wizard in battle.Wizards)
		{
			if (!wizard.Alive) continue;
			if (wizard.Status.Count > 0) Fail($"match {played + 1}: {wizard.Label} still carries {wizard.Status.Count} modifier(s)");
			if (wizard.UltimateActive) Fail($"match {played + 1}: {wizard.Label} is stuck with an ultimate active");
			if (wizard.Status.SpeedMultiplier != 1.0f) Fail($"match {played + 1}: {wizard.Label} speed multiplier is {wizard.Status.SpeedMultiplier}");
		}
		if (battle.Rune != null) CheckRune();
		GD.Print($"Soak: match {played + 1} result {battle.Result} after {battle.EndedAt:0.0}s, {battle.UltimatesCast} ultimate(s)" + (battle.Hazard?.Stage.HazardKind == "lava" ? $", lava dealt {battle.Hazard.DamageDealt:0}, {battle.Hazard.Crossings} bridge crossing(s)" : ""));
		totalSeconds += battle.EndedAt;
		totalUltimates += battle.UltimatesCast;
		if (battle.UltimatesCast > 0) matchesWithUltimate++;
		FinishMatch();
	}

	/// <summary>
	/// King of the Rune ends on the rune: the winner reached the target, or the time ran out (and
	/// then the leader won, or it was a tie). Scores are reported.
	/// </summary>
	void CheckRune()
	{
		List<string> scores = new();
		foreach (KeyValuePair<string, float> pair in battle.Rune.Scores) scores.Add($"{pair.Key} {pair.Value:0}");
		GD.Print($"Soak: rune scores {string.Join(", ", scores)}");
		bool reached = battle.Rune.Scores.TryGetValue(battle.Result, out float won) && won >= RuneHill.Target - 0.05f;
		bool timedOut = battle.Rune.TimedOut;
		if (!reached && !timedOut) Fail($"match {played + 1}: {battle.Result} won King of the Rune without holding it for {RuneHill.Target:0}s or the time running out");
	}

	void Fail(string message)
	{
		failures++;
		GD.PrintErr("Soak: FAIL " + message);
	}

	void FinishMatch()
	{
		played++;
		battle.QueueFree();
		battle = null;
		if (played < matches) { CallDeferred(MethodName.StartMatch); return; }
		if (played > 0)
			GD.Print($"Soak: balance - average match {totalSeconds / played:0.0}s, {(float)totalUltimates / played:0.0} ultimates per match, ultimates in {matchesWithUltimate}/{played} matches");
		GD.Print($"Soak: {played} matches, {failures} failure(s)");
		// Quit a moment later, so the last battle is actually freed first; quitting in the same
		// frame as its QueueFree reports its collision shapes as leaked at exit.
		int exitCode = failures == 0 ? 0 : 1;
		GetTree().CreateTimer(0.2).Timeout += () => GetTree().Quit(exitCode);
	}
}
