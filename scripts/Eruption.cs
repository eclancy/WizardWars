using System.Collections.Generic;
using Godot;

/// <summary>
/// Something bursting out of the ground at a target point, after a telegraph so it can be
/// dodged:
/// - Night Grasp (Shadow): a dark pool spreads, then shadowy hands claw up out of it. Enemies
///   caught are hurt and silenced, so they can only use their basic attack.
/// - Entangle (Nature): green shoots pierce the ground, then thorny vines whip up. Enemies
///   caught are hurt and rooted to the spot.
///
/// Telegraph for <c>Duration</c>; the effect lasts <c>Duration2</c> on whoever is inside
/// <c>Radius</c> at the moment of the burst.
/// </summary>
public partial class Eruption : Node2D
{
	public enum Kind { Grasp, Vines }

	const float Flatten = 0.6f, BurstTime = 0.6f;
	static readonly Color Ink = new(0.04f, 0.03f, 0.05f);

	Wizard caster;
	int casterTeam;
	Kind kind;
	AbilityData data;
	float radius, elapsed;
	bool burst;
	readonly List<(Vector2 at, float angle, float size)> limbs = new();

	public static Eruption Create(Wizard caster, Kind kind, Vector2 target, AbilityData data)
	{
		Eruption eruption = new() { caster = caster, casterTeam = caster.Team, kind = kind, data = data, Position = target, radius = Units.U(data.Radius), ZIndex = 2 };
		RandomNumberGenerator rng = new() { Seed = (ulong)(target.X * 13 + target.Y * 7) };
		int count = kind == Kind.Grasp ? 5 : 7;
		for (int i = 0; i < count; i++)
		{
			float angle = i * Mathf.Tau / count + rng.RandfRange(-0.3f, 0.3f), distance = rng.RandfRange(0.25f, 0.8f);
			eruption.limbs.Add((new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * Flatten) * distance, rng.RandfRange(-0.35f, 0.35f), rng.RandfRange(0.8f, 1.2f)));
		}
		return eruption;
	}

	public override void _PhysicsProcess(double delta)
	{
		elapsed += (float)delta;
		if (!burst && elapsed >= data.Duration)
		{
			burst = true;
			Battle.Current.Shake(4.0f, 0.2f);
			foreach (Wizard wizard in Battle.Current.Wizards)
			{
				if (!wizard.Alive || !Combat.CanAffect(caster, casterTeam, wizard)) continue;
				Vector2 d = wizard.Feet - GlobalPosition;
				if (new Vector2(d.X, d.Y / Flatten).Length() > radius) continue;
				wizard.TakeDamage(data.Damage);
				if (kind == Kind.Grasp) TimedEffect.Apply(wizard, new Modifier { Silenced = true }, data.Duration2, TimedEffect.Look.Shadow);
				else TimedEffect.Apply(wizard, new Modifier { Rooted = true }, data.Duration2, TimedEffect.Look.Vines);
			}
		}
		if (elapsed >= data.Duration + BurstTime) QueueFree();
	}

	public override void _Process(double delta) => QueueRedraw();

	public override void _Draw()
	{
		float gather = Mathf.Min(1.0f, elapsed / data.Duration);
		float after = burst ? Mathf.Clamp((elapsed - data.Duration) / BurstTime, 0.0f, 1.0f) : 0.0f;
		// Up fast, hold, sink back.
		float reach = burst ? Mathf.Sin(Mathf.Min(1.0f, after * 1.6f) * Mathf.Pi * 0.5f) * (1.0f - Mathf.Max(0.0f, after - 0.6f) / 0.4f) : 0.0f;

		// The ground marker: spreads during the telegraph, fades after the burst.
		Color pool = kind == Kind.Grasp ? new Color(0.12f, 0.08f, 0.18f) : new Color(0.25f, 0.4f, 0.14f);
		Color rim = kind == Kind.Grasp ? new Color(0.55f, 0.42f, 0.82f) : new Color(0.55f, 0.85f, 0.3f);
		float fade = 1.0f - after;
		DrawSetTransform(Vector2.Zero, 0.0f, new Vector2(1.0f, Flatten));
		DrawCircle(Vector2.Zero, radius * Mathf.Lerp(0.3f, 1.0f, gather), new Color(pool, 0.55f * fade));
		DrawArc(Vector2.Zero, radius, 0.0f, Mathf.Tau, 48, new Color(Ink, 0.7f * fade), 7.0f, true);
		DrawArc(Vector2.Zero, radius, 0.0f, Mathf.Tau, 48, new Color(rim, (0.4f + 0.5f * gather) * fade), 3.5f, true);
		DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);

		foreach ((Vector2 at, float angle, float size) in limbs)
		{
			Vector2 foot = at * radius;
			if (!burst)
			{
				// Telegraph: the tips just breaking the surface, twitching.
				float peek = gather * 10.0f * size;
				DrawCircle(foot, peek * 0.6f + 2.0f, new Color(rim, 0.8f));
				continue;
			}
			if (kind == Kind.Grasp) DrawHand(foot, angle, size * reach);
			else DrawVine(foot, angle, size * reach);
		}
	}

	/// <summary>A shadowy hand reaching up: a palm and four clawing fingers.</summary>
	void DrawHand(Vector2 foot, float lean, float reach)
	{
		if (reach <= 0.01f) return;
		Color shadow = new(0.16f, 0.12f, 0.24f), light = new(0.36f, 0.28f, 0.52f);
		Vector2 up = Vector2.Up.Rotated(lean), palm = foot + up * Units.U(0.55f) * reach;
		DrawLine(foot, palm, Ink, 20.0f * reach + 4.0f, true);
		DrawLine(foot, palm, shadow, 20.0f * reach, true);
		DrawCircle(palm, 14.0f * reach + 3.0f, Ink);
		DrawCircle(palm, 14.0f * reach, shadow);
		for (int f = 0; f < 4; f++)
		{
			float spread = (f - 1.5f) * 0.38f;
			Vector2 knuckle = palm + up.Rotated(spread) * 12.0f * reach;
			Vector2 tip = knuckle + up.Rotated(spread * 1.6f + 0.5f * Mathf.Sign(spread)) * 22.0f * reach;
			DrawLine(knuckle, tip, Ink, 9.0f, true);
			DrawLine(knuckle, tip, light, 5.0f, true);
		}
	}

	/// <summary>A thorny vine whipping up and curling over at the top.</summary>
	void DrawVine(Vector2 foot, float lean, float reach)
	{
		if (reach <= 0.01f) return;
		Color vine = new(0.32f, 0.62f, 0.2f), thorn = new(0.85f, 0.88f, 0.7f);
		Vector2[] line = new Vector2[12];
		for (int i = 0; i < line.Length; i++)
		{
			float t = i / (float)(line.Length - 1);
			// Up, then curling over sideways near the top.
			float bend = lean * 2.5f * t * t;
			line[i] = foot + new Vector2(Mathf.Sin(bend) * 30.0f * t, -Mathf.Cos(bend) * Units.U(1.0f) * t) * reach;
		}
		DrawPolyline(line, Ink, 11.0f, true);
		DrawPolyline(line, vine, 6.0f, true);
		for (int i = 2; i < line.Length - 1; i += 3)
		{
			Vector2 along = (line[i] - line[i - 1]).Normalized(), out_ = along.Orthogonal() * (i % 2 == 0 ? 10.0f : -10.0f);
			Vector2[] spike = { line[i] - along * 4.0f, line[i] + out_, line[i] + along * 4.0f };
			DrawColoredPolygon(spike, thorn);
			DrawPolyline(spike, Ink, 2.0f, true);
		}
	}
}
