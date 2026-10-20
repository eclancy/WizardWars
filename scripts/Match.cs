using System.Collections.Generic;
using Godot;

/// <summary>
/// A match of one or more rounds on the same stage with the same fighters: best of 1, 3 or 5
/// (<see cref="Session.RoundsToWin"/>). Each round is a <see cref="Battle"/>; its result (a
/// player label like "P1" or "CPU", a team like "GOLD", or "DRAW") is recorded here. The first
/// side to win <see cref="RoundsToWin"/> rounds takes the match. A drawn round counts for
/// nobody, but after <see cref="MaxRounds"/> rounds the match ends anyway: the leader wins, or
/// it is a draw.
///
/// Plain C#, so RegressionChecks can play it out without a battle.
/// </summary>
public sealed class Match
{
	public readonly BattleSetup Setup;
	public readonly int RoundsToWin;
	/// <summary>The round being played (or just played), from 1.</summary>
	public int Round { get; private set; } = 1;
	/// <summary>The match result once decided, else null.</summary>
	public string Winner { get; private set; }
	public bool Decided => Winner != null;
	public int MaxRounds => RoundsToWin * 2 - 1 + 2;

	readonly Dictionary<string, int> wins = new();

	public Match(BattleSetup setup, int roundsToWin)
	{
		Setup = setup;
		RoundsToWin = Mathf.Max(1, roundsToWin);
		foreach (string side in Sides) wins[side] = 0;
	}

	/// <summary>Everyone who can win a round: the two teams, or each fighter by label.</summary>
	public List<string> Sides
	{
		get
		{
			List<string> sides = new();
			if (Setup.TeamsActive)
			{
				sides.Add(Teams.Name(Teams.Gold).ToUpper());
				sides.Add(Teams.Name(Teams.Pink).ToUpper());
				return sides;
			}
			foreach (Fighter fighter in Setup.Fighters) sides.Add(fighter.Label);
			return sides;
		}
	}

	public int Wins(string side) => wins.TryGetValue(side, out int count) ? count : 0;

	/// <summary>Records one round's result and moves on to the next round (or decides the match).</summary>
	public void Record(string result)
	{
		if (Decided) return;
		if (result != "DRAW" && result != null)
		{
			wins[result] = Wins(result) + 1;
			if (wins[result] >= RoundsToWin) { Winner = result; return; }
		}
		if (Round >= MaxRounds)
		{
			Winner = Leader();
			return;
		}
		Round++;
	}

	/// <summary>The side with the most wins, or "DRAW" if the top is shared.</summary>
	string Leader()
	{
		string best = "DRAW";
		int most = -1;
		bool tied = false;
		foreach (KeyValuePair<string, int> pair in wins)
		{
			if (pair.Value > most) { most = pair.Value; best = pair.Key; tied = false; }
			else if (pair.Value == most) tied = true;
		}
		return tied ? "DRAW" : best;
	}

	/// <summary>The colour that marks a side: its team colour, or its wizard's colour.</summary>
	public Color ColourOf(string side)
	{
		if (Setup.TeamsActive) return side == Teams.Name(Teams.Gold).ToUpper() ? Teams.Colour(Teams.Gold) : Teams.Colour(Teams.Pink);
		foreach (Fighter fighter in Setup.Fighters) if (fighter.Label == side) return fighter.Wizard.Colour;
		return Colors.White;
	}
}
