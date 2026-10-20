using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// A round-robin of computer duels for balance: every pair of wizards fights <c>rounds</c>
/// times, swapping sides and moving through the stages, all on Normal. Then it prints each
/// wizard's record and win rate, its worst matchup, and the average match length and
/// ultimates. Run with `-- --balance[=N]` (N rounds per pair, default 4) headless with
/// --fixed-fps 60, like the soak.
///
/// It is a measuring tool, not a pass/fail check: the soak test covers leaks and stuck states.
/// </summary>
public partial class BalanceTest : Node
{
	const double TimeLimit = 240.0;

	readonly int rounds;
	readonly List<(int a, int b, int stage, bool swap)> duels = new();
	readonly Dictionary<string, int> wins = new(), losses = new(), draws = new();
	readonly Dictionary<(string, string), int> beat = new();
	int next;
	double totalSeconds;
	int totalUltimates, withUltimate;
	Battle battle;
	string[] names;

	public BalanceTest() { }

	public BalanceTest(int rounds) => this.rounds = Mathf.Max(1, rounds);

	public override void _Ready()
	{
		Name = "BalanceTest";
		GD.Seed(4242);
		WizardData[] wizards = Content.Wizards;
		names = wizards.Select(w => w.DisplayName.Replace(" Wizard", "")).ToArray();
		int stage = 0;
		for (int r = 0; r < rounds; r++)
			for (int a = 0; a < wizards.Length; a++)
				for (int b = a + 1; b < wizards.Length; b++)
					duels.Add((a, b, stage++ % Content.Stages.Length, r % 2 == 1));
		foreach (string name in names) { wins[name] = 0; losses[name] = 0; draws[name] = 0; }
		GD.Print($"Balance: {duels.Count} duels ({rounds} per pair)");
		Start();
	}

	void Start()
	{
		(int a, int b, int stage, bool swap) = duels[next];
		BattleSetup setup = new() { Stage = Content.Stages[stage] };
		int first = swap ? b : a, second = swap ? a : b;
		setup.Fighters.Add(new Fighter { Wizard = Content.Wizards[first], Slot = -1, Spawn = 0, Label = "CPU1" });
		setup.Fighters.Add(new Fighter { Wizard = Content.Wizards[second], Slot = -1, Spawn = 1, Label = "CPU2" });
		battle = Battle.Create(setup);
		battle.SkipCountdown = true;
		AddChild(battle);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (battle == null) return;
		if (!battle.Over && battle.Time < TimeLimit) return;

		(int a, int b, int stage, bool swap) = duels[next];
		int first = swap ? b : a, second = swap ? a : b;
		string one = names[first], two = names[second];
		if (!battle.Over || battle.Result == "DRAW") { draws[one]++; draws[two]++; }
		else
		{
			bool firstWon = battle.Result == "CPU1";
			string winner = firstWon ? one : two, loser = firstWon ? two : one;
			wins[winner]++;
			losses[loser]++;
			beat[(winner, loser)] = beat.GetValueOrDefault((winner, loser)) + 1;
		}
		totalSeconds += battle.Over ? battle.EndedAt : TimeLimit;
		totalUltimates += battle.UltimatesCast;
		if (battle.UltimatesCast > 0) withUltimate++;

		battle.QueueFree();
		battle = null;
		next++;
		if (next < duels.Count) { CallDeferred(MethodName.Start); return; }
		Report();
	}

	void Report()
	{
		GD.Print($"Balance: average duel {totalSeconds / duels.Count:0.0}s, {(float)totalUltimates / duels.Count:0.0} ultimates, ultimates in {withUltimate}/{duels.Count}");
		foreach (string name in names.OrderByDescending(n => wins[n] / (float)Mathf.Max(1, wins[n] + losses[n])))
		{
			int played = wins[name] + losses[name];
			// The opponent this wizard does worst against.
			string worst = names.Where(o => o != name)
				.OrderBy(o => beat.GetValueOrDefault((name, o)) - beat.GetValueOrDefault((o, name)))
				.First();
			GD.Print($"Balance: {name,-12} {wins[name],3}-{losses[name],-3} ({(played == 0 ? 0 : 100 * wins[name] / played),3}%)  draws {draws[name]}  worst vs {worst} ({beat.GetValueOrDefault((name, worst))}-{beat.GetValueOrDefault((worst, name))})");
		}
		GetTree().CreateTimer(0.2).Timeout += () => GetTree().Quit(0);
	}
}
