using System.Collections.Generic;
using Godot;

/// <summary>
/// King of the Rune: a circle of glowing runes on the arena floor.
/// - A side (a player in Free-for-All, a team in Teams) with someone standing in it, and nobody
///   from any other side in it, scores time.
/// - Two sides in it at once contest it: nobody scores, and it flickers.
/// - The first side to <see cref="Target"/> seconds takes the round.
/// - After <see cref="TimeLimit"/> seconds of fighting the leader takes it, or it is a draw.
///
/// Knocked-out wizards come back (<see cref="Battle"/> respawns them), so a knock-out costs time on
/// the rune, not the round.
///
/// Where the stage's middle is lava or bog the rune has several spots (<see cref="StageData.RunePoints"/>)
/// and moves on to the next every <see cref="MoveEvery"/> seconds. Its next spot glows faintly for
/// the last few seconds first, so nobody is caught out.
///
/// It draws itself flat on the floor (over ground effects, under wizards) in the holder's colour, and
/// puts the scores across the top
/// of the screen.
/// </summary>
public partial class RuneHill : Node2D
{
	/// <summary>Seconds on the rune to take a round.</summary>
	public const float Target = 20.0f;
	/// <summary>Seconds between moves, where a stage has more than one spot.</summary>
	public const float MoveEvery = 20.0f;
	/// <summary>Seconds of fighting before the leader takes the round anyway.</summary>
	public const float TimeLimit = 90.0f;
	/// <summary>Radius in Unity units.</summary>
	public const float RadiusUnits = 1.4f;
	const float Warning = 3.0f;

	static readonly Color Neutral = new(1.0f, 0.78f, 0.35f);
	static readonly Color Contested = new(1.0f, 0.25f, 0.2f);

	Battle battle;
	Vector2[] spots;
	int spot;
	float sinceMove, fighting, time;
	readonly Dictionary<string, float> scores = new();
	/// <summary>The side scoring right now, "" for nobody, or null when contested.</summary>
	string holder = "";
	RuneScoreboard board;

	public static float Radius => Units.U(RadiusUnits);

	/// <summary>Where the rune is now, in arena pixels.</summary>
	public Vector2 Centre => Units.U(spots[spot]);

	/// <summary>Where it goes next (the same as <see cref="Centre"/> if it never moves).</summary>
	Vector2 NextCentre => Units.U(spots[(spot + 1) % spots.Length]);

	public IReadOnlyDictionary<string, float> Scores => scores;

	public static RuneHill Create(Battle battle)
	{
		StageData stage = battle.Setup.Stage;
		RuneHill hill = new()
		{
			battle = battle,
			spots = stage.RunePoints.Length > 0 ? stage.RunePoints : new[] { Vector2.Zero },
			// Over ground effects (patches are 0), under the wizards (2): it is the objective.
			ZIndex = 1,
			ZAsRelative = false,
			Name = "RuneHill",
		};
		foreach (string side in Sides(battle.Setup)) hill.scores[side] = 0.0f;
		return hill;
	}

	public override void _Ready()
	{
		board = new RuneScoreboard(this, battle);
		battle.AddOverlay(board);
	}

	/// <summary>The sides that can score: the two teams, or each fighter by label.</summary>
	public static List<string> Sides(BattleSetup setup)
	{
		List<string> sides = new();
		if (setup.TeamsActive)
		{
			sides.Add(Teams.Name(Teams.Green).ToUpper());
			sides.Add(Teams.Name(Teams.Pink).ToUpper());
		}
		else foreach (Fighter fighter in setup.Fighters) sides.Add(fighter.Label);
		return sides;
	}

	public string SideOf(Wizard wizard) => battle.Setup.TeamsActive ? Teams.Name(wizard.Team).ToUpper() : wizard.Label;

	public Color ColourOf(string side)
	{
		if (battle.Setup.TeamsActive) return side == Teams.Name(Teams.Green).ToUpper() ? Teams.Colour(Teams.Green) : Teams.Colour(Teams.Pink);
		foreach (Fighter fighter in battle.Setup.Fighters) if (fighter.Label == side) return fighter.Wizard.Colour;
		return Colors.White;
	}

	/// <summary>How much the circle is squashed top to bottom, lying on the floor (drawing and scoring agree).</summary>
	public const float Flatten = 0.62f;

	/// <summary>Whether a wizard's feet are inside the circle as drawn.</summary>
	public bool Holds(Wizard wizard)
	{
		Vector2 offset = wizard.Feet - Centre;
		return new Vector2(offset.X, offset.Y / Flatten).Length() <= Radius;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!battle.Fighting || battle.Over) return;
		float dt = (float)delta;
		fighting += dt;

		if (spots.Length > 1)
		{
			sinceMove += dt;
			if (sinceMove >= MoveEvery)
			{
				sinceMove = 0.0f;
				spot = (spot + 1) % spots.Length;
				Sfx.Play("res://assets/sfx/ultimate-release.wav", -10.0f);
			}
		}

		HashSet<string> inside = new();
		foreach (Wizard wizard in battle.Wizards)
			if (GodotObject.IsInstanceValid(wizard) && wizard.Alive && Holds(wizard)) inside.Add(SideOf(wizard));
		holder = inside.Count == 0 ? "" : inside.Count == 1 ? First(inside) : null;
		if (!string.IsNullOrEmpty(holder))
		{
			scores[holder] = (scores.TryGetValue(holder, out float score) ? score : 0.0f) + dt;
			if (scores[holder] >= Target) { battle.EndRound(holder); return; }
		}

		if (fighting >= TimeLimit)
		{
			TimedOut = true;
			battle.EndRound(Leader());
		}
	}

	static string First(HashSet<string> set)
	{
		foreach (string side in set) return side;
		return "";
	}

	/// <summary>The side with the most time on the rune, or "DRAW" if the top is shared.</summary>
	string Leader()
	{
		string best = "DRAW";
		float most = -1.0f;
		bool tied = false;
		foreach (KeyValuePair<string, float> pair in scores)
		{
			if (pair.Value > most + 0.01f) { most = pair.Value; best = pair.Key; tied = false; }
			else if (Mathf.Abs(pair.Value - most) <= 0.01f) tied = true;
		}
		return tied ? "DRAW" : best;
	}

	/// <summary>The round ended on the time limit, not on someone reaching the target.</summary>
	public bool TimedOut { get; private set; }

	/// <summary>The scoring side ("" nobody, null contested), for the scoreboard.</summary>
	public string Holder => holder;

	public override void _Process(double delta)
	{
		time += (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		Color colour = holder == null
			? (Mathf.Sin(time * 22.0f) > 0.0f ? Contested : Colors.White)
			: holder.Length > 0 ? ColourOf(holder) : Neutral;
		float pulse = 0.5f + 0.5f * Mathf.Sin(time * (holder?.Length > 0 ? 7.0f : 3.0f));
		DrawRune(Centre, colour, 1.0f, pulse, true);

		// The next spot, glowing faintly in the last seconds before the move.
		if (spots.Length > 1 && sinceMove > MoveEvery - Warning)
		{
			float warn = (sinceMove - (MoveEvery - Warning)) / Warning;
			DrawRune(NextCentre, Neutral, 0.25f + 0.5f * warn * (0.5f + 0.5f * Mathf.Sin(time * 14.0f)), 0.0f, false);
		}
	}

	/// <summary>
	/// The circle: a tinted floor, two rings with runes slowly turning between them. When the rune
	/// is live the tint pulses and the rings are bold; the next spot's ghost is rings and runes only.
	/// </summary>
	void DrawRune(Vector2 at, Color colour, float strength, float pulse, bool live)
	{
		float r = Radius;
		// Drawn as a squashed circle, so it lies on the floor.
		DrawSetTransform(at, 0.0f, new Vector2(1.0f, Flatten));
		if (live) DrawCircle(Vector2.Zero, r, new Color(colour, (0.16f + 0.12f * pulse) * strength));
		if (live) DrawArc(Vector2.Zero, r + 6.0f, 0.0f, Mathf.Tau, 64, new Color(colour, (0.18f + 0.15f * pulse) * strength), 22.0f, true);
		DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 64, new Color(Ui.Ink, 0.75f * strength), 13.0f, true);
		DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 64, new Color(colour, strength), 7.0f, true);
		DrawArc(Vector2.Zero, r * 0.72f, 0.0f, Mathf.Tau, 48, new Color(colour, 0.8f * strength), 3.0f, true);
		const int Count = 10;
		for (int i = 0; i < Count; i++)
		{
			float angle = i * Mathf.Tau / Count + time * 0.25f;
			RuneGlyphs.Draw(this, RuneGlyphs.All[i % RuneGlyphs.All.Length], Vector2.FromAngle(angle) * r * 0.86f, angle + Mathf.Pi * 0.5f, 9.0f, new Color(colour, 0.9f * strength), 2.5f);
		}
		DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
	}
}
