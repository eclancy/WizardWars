using System.Collections.Generic;
using Godot;

/// <summary>
/// An area on the ground that does something to whoever stands in it, for the new wizards' LT
/// abilities. Each one spreads out from its centre, lasts its time, then fades:
/// - Smoke (Shadow): a billowing cloud drawn over the wizards. Anyone inside is hidden (faded
///   out, and the computer loses track of them), and enemies are slowed.
/// - Bramble (Nature): thorny vines across the ground. Enemies inside are slowed and scratched.
/// - Updraft (Wind): a rising swirl of air under the caster. The caster and teammates inside run
///   faster.
///
/// The area is an ellipse lying on the floor. Its modifier is keyed to the zone, so it lifts on
/// stepping out or when the zone ends.
/// </summary>
public partial class Zone : Node2D
{
	public enum Kind { Smoke, Bramble, Updraft }

	const float Flatten = 0.6f, SpreadTime = 0.35f, FadeTime = 0.4f;
	static readonly Color Ink = new(0.04f, 0.03f, 0.05f);

	Wizard caster;
	int casterTeam;
	Kind kind;
	float radius, lifetime, elapsed, percent, damagePerSecond;
	bool ended;
	readonly List<(Vector2 at, float r, float phase)> pieces = new();

	public static Zone Create(Wizard caster, Kind kind, Vector2 position, AbilityData data)
	{
		Zone zone = new()
		{
			caster = caster,
			casterTeam = caster.Team,
			kind = kind,
			Position = position,
			radius = Units.U(data.Radius),
			lifetime = data.Duration,
			percent = data.SlowPercent,
			damagePerSecond = data.Damage,
			// Smoke hangs over the wizards; the others lie on the ground under them.
			ZIndex = kind == Kind.Smoke ? 5 : -1,
		};
		RandomNumberGenerator rng = new() { Seed = (ulong)(position.X * 31 + position.Y * 17) };
		int count = kind == Kind.Smoke ? 16 : kind == Kind.Bramble ? 9 : 6;
		for (int i = 0; i < count; i++)
		{
			float angle = rng.Randf() * Mathf.Tau, distance = Mathf.Sqrt(rng.Randf()) * 0.78f;
			zone.pieces.Add((new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * Flatten) * distance, rng.RandfRange(0.22f, 0.4f), rng.Randf() * Mathf.Tau));
		}
		return zone;
	}

	/// <summary>Whether a point is inside the area.</summary>
	public bool Contains(Vector2 point)
	{
		Vector2 d = point - GlobalPosition;
		return new Vector2(d.X, d.Y / Flatten).Length() <= radius * Spread;
	}

	float Spread => Mathf.SmoothStep(0.0f, 1.0f, Mathf.Min(1.0f, elapsed / SpreadTime));
	float Fade => Mathf.Clamp((lifetime - elapsed) / FadeTime, 0.0f, 1.0f);

	public override void _PhysicsProcess(double delta)
	{
		if (ended) return;
		float dt = (float)delta;
		elapsed += dt;
		if (elapsed >= lifetime) { End(); return; }

		foreach (Wizard wizard in Battle.Current.Wizards)
		{
			if (!wizard.Alive) continue;
			bool inside = Contains(wizard.Feet);
			bool enemy = Combat.CanAffect(caster, casterTeam, wizard);
			bool friend = wizard == caster || Teams.Allied(casterTeam, wizard.Team);
			Modifier? effect = kind switch
			{
				Kind.Smoke => enemy ? new Modifier { Hidden = true, Slow = percent } : new Modifier { Hidden = true },
				Kind.Bramble => enemy ? new Modifier { Slow = percent } : null,
				_ => friend ? new Modifier { Haste = percent } : null,
			};
			if (inside && effect is Modifier modifier)
			{
				wizard.Status.Set(this, modifier);
				if (kind == Kind.Bramble && enemy) wizard.TakeDamage(damagePerSecond * dt);
			}
			else wizard.Status.Remove(this);
		}
	}

	void End()
	{
		ended = true;
		foreach (Wizard wizard in Battle.Current.Wizards) wizard.Status.Remove(this);
		QueueFree();
	}

	public override void _Process(double delta) => QueueRedraw();

	public override void _Draw()
	{
		float r = radius * Spread, alpha = Fade;
		switch (kind)
		{
			case Kind.Smoke: DrawSmoke(r, alpha); break;
			case Kind.Bramble: DrawBramble(r, alpha); break;
			default: DrawUpdraft(r, alpha); break;
		}
	}

	/// <summary>Billowing puffs, each slowly swelling and drifting, as one outlined mass.</summary>
	void DrawSmoke(float r, float alpha)
	{
		Color outline = new(Ink, 0.85f * alpha), dark = new(0.27f, 0.25f, 0.33f, 0.92f * alpha), light = new(0.4f, 0.38f, 0.48f, 0.92f * alpha);
		List<(Vector2 at, float size)> puffs = new();
		foreach ((Vector2 at, float size, float phase) in pieces)
		{
			float swell = 1.0f + 0.12f * Mathf.Sin(elapsed * 1.6f + phase);
			Vector2 drift = new Vector2(Mathf.Sin(elapsed * 0.7f + phase), Mathf.Cos(elapsed * 0.5f + phase) * Flatten) * 0.06f;
			puffs.Add(((at + drift) * r, size * r * swell));
		}
		// All the outlines first, then the fills: the mass reads as one cloud, not a pile of circles.
		foreach ((Vector2 at, float size) in puffs) DrawCircle(at, size + 5.0f, outline);
		foreach ((Vector2 at, float size) in puffs) DrawCircle(at, size, dark);
		foreach ((Vector2 at, float size) in puffs) DrawCircle(at + new Vector2(-size * 0.18f, -size * 0.22f), size * 0.62f, light);
	}

	/// <summary>Thorny vines snaking across a darkened patch of ground, growing out from the middle.</summary>
	void DrawBramble(float r, float alpha)
	{
		Color ground = new(0.18f, 0.3f, 0.12f, 0.45f * alpha), vine = new(0.3f, 0.58f, 0.2f, alpha), thorn = new(0.85f, 0.88f, 0.7f, alpha), outline = new(Ink, alpha);
		DrawSetTransform(Vector2.Zero, 0.0f, new Vector2(1.0f, Flatten));
		DrawCircle(Vector2.Zero, r, ground);
		DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
		foreach ((Vector2 at, float size, float phase) in pieces)
		{
			// Each vine curls from near the middle out toward its own point.
			Vector2 end = at * r, start = end * 0.15f;
			Vector2[] line = new Vector2[12];
			for (int i = 0; i < line.Length; i++)
			{
				float t = i / (float)(line.Length - 1);
				Vector2 side = (end - start).Orthogonal().Normalized() * Mathf.Sin(t * Mathf.Pi * 2.0f + phase) * size * 40.0f;
				line[i] = start.Lerp(end, t) + side;
			}
			DrawPolyline(line, outline, 9.0f, true);
			DrawPolyline(line, vine, 5.0f, true);
			for (int i = 2; i < line.Length; i += 3)
			{
				Vector2 along = (line[i] - line[i - 1]).Normalized(), up = along.Orthogonal() * (i % 2 == 0 ? 9.0f : -9.0f);
				Vector2[] spike = { line[i] - along * 4.0f, line[i] + up, line[i] + along * 4.0f };
				DrawColoredPolygon(spike, thorn);
				DrawPolyline(new[] { spike[0], spike[1], spike[2] }, outline, 2.0f, true);
			}
		}
	}

	/// <summary>A pale swirl on the ground and streaks of air rising out of it.</summary>
	void DrawUpdraft(float r, float alpha)
	{
		Color floor = new(0.45f, 0.95f, 0.8f, 0.22f * alpha), ring = new(0.6f, 1.0f, 0.88f, 0.9f * alpha), streak = new(0.92f, 1.0f, 0.98f, alpha), outline = new(Ink, 0.8f * alpha);
		DrawSetTransform(Vector2.Zero, 0.0f, new Vector2(1.0f, Flatten));
		DrawCircle(Vector2.Zero, r, floor);
		DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 48, outline, 8.0f, true);
		DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 48, ring, 4.0f, true);
		// Swirling arcs turning round the centre.
		for (int i = 0; i < 3; i++)
		{
			float start = elapsed * 3.0f + i * Mathf.Tau / 3;
			DrawArc(Vector2.Zero, r * (0.35f + 0.2f * i), start, start + 1.6f, 16, ring, 4.0f, true);
		}
		DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
		// Streaks of air rising from the floor, looping up and fading.
		foreach ((Vector2 at, float size, float phase) in pieces)
		{
			float rise = (elapsed * 1.4f + phase) % 1.0f;
			Vector2 foot = at * r * 0.8f, top = foot + new Vector2(0.0f, -Units.U(1.2f) * rise);
			Color fade = new(streak, alpha * (1.0f - rise));
			DrawLine(top + new Vector2(0, Units.U(0.35f)), top, new Color(Ink, 0.6f * alpha * (1.0f - rise)), 6.0f, true);
			DrawLine(top + new Vector2(0, Units.U(0.35f)), top, fade, 3.0f, true);
		}
	}
}
