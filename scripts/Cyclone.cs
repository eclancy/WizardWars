using System.Collections.Generic;
using Godot;

/// <summary>
/// Cyclone, Wind's LB: a whirlwind at the target for <c>Duration</c> seconds. Enemies within
/// <c>Radius</c> are dragged toward its eye at <c>Knockback</c> units a second and take
/// <c>Damage</c> a second. Drawn as a funnel of spinning wind bands, wider at the top, with
/// leaves and grit whirling round it and a swirl on the ground.
/// </summary>
public partial class Cyclone : Node2D
{
	const float Flatten = 0.6f, FormTime = 0.3f;
	static readonly Color Ink = new(0.04f, 0.03f, 0.05f);
	static readonly Color Air = new(0.92f, 1.0f, 0.98f);
	static readonly Color Mint = new(0.45f, 0.92f, 0.78f);

	Wizard caster;
	int casterTeam;
	AbilityData data;
	float radius, elapsed;
	readonly List<(float angle, float height, float speed)> debris = new();

	public static Cyclone Create(Wizard caster, AbilityData data, Vector2 target)
	{
		Cyclone cyclone = new() { caster = caster, casterTeam = caster.Team, data = data, Position = target, radius = Units.U(data.Radius), ZIndex = 4 };
		for (int i = 0; i < 10; i++) cyclone.debris.Add((i * Mathf.Tau / 10, (float)GD.RandRange(0.1, 1.0), (float)GD.RandRange(4.0, 7.0)));
		return cyclone;
	}

	float Strength => Mathf.Min(1.0f, elapsed / FormTime) * Mathf.Clamp((data.Duration - elapsed) / 0.3f, 0.0f, 1.0f);

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		elapsed += dt;
		if (elapsed >= data.Duration) { QueueFree(); return; }
		if (elapsed < FormTime) return;
		foreach (Wizard wizard in Battle.Current.Wizards)
		{
			if (!wizard.Alive || !Combat.CanAffect(caster, casterTeam, wizard)) continue;
			Vector2 toEye = GlobalPosition - wizard.Feet;
			if (new Vector2(toEye.X, toEye.Y / Flatten).Length() > radius) continue;
			wizard.TakeDamage(data.Damage * dt);
			if (toEye.Length() > 8.0f) wizard.Shove(toEye.Normalized() * Units.U(data.Knockback), 0.12f);
		}
	}

	public override void _Process(double delta) => QueueRedraw();

	public override void _Draw()
	{
		float s = Strength, spin = elapsed * 9.0f;
		// The swirl on the ground marks the pull's reach.
		DrawSetTransform(Vector2.Zero, 0.0f, new Vector2(1.0f, Flatten));
		DrawCircle(Vector2.Zero, radius, new Color(Mint, 0.15f * s));
		for (int i = 0; i < 3; i++)
		{
			float start = -spin * 0.5f + i * Mathf.Tau / 3;
			DrawArc(Vector2.Zero, radius * (0.45f + 0.18f * i), start, start + 1.8f, 16, new Color(Ink, 0.6f * s), 7.0f, true);
			DrawArc(Vector2.Zero, radius * (0.45f + 0.18f * i), start, start + 1.8f, 16, new Color(Air, 0.85f * s), 3.5f, true);
		}
		DrawArc(Vector2.Zero, radius, 0.0f, Mathf.Tau, 48, new Color(Mint, 0.7f * s), 3.0f, true);
		DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);

		// The funnel: stacked spinning bands, narrow at the ground and wide at the top.
		const int Bands = 7;
		float height = Units.U(2.2f) * s;
		for (int b = 0; b < Bands; b++)
		{
			float t = b / (float)(Bands - 1);
			float y = -t * height, width = Mathf.Lerp(14.0f, Units.U(0.9f), t) * s;
			float start = spin + b * 0.7f;
			DrawSetTransform(new Vector2(Mathf.Sin(elapsed * 3.0f + t * 2.0f) * 10.0f * t, y), 0.0f, new Vector2(1.0f, 0.3f));
			DrawArc(Vector2.Zero, width, start, start + 3.6f, 20, new Color(Ink, 0.75f * s), 8.0f, true);
			DrawArc(Vector2.Zero, width, start, start + 3.6f, 20, b % 2 == 0 ? new Color(Air, s) : new Color(Mint, s), 4.0f, true);
		}
		DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);

		// Leaves and grit whirling round the funnel.
		foreach ((float angle, float h, float speed) in debris)
		{
			float a = angle + elapsed * speed;
			float y = -h * height, w = Mathf.Lerp(20.0f, Units.U(1.0f), h) * s;
			Vector2 at = new(Mathf.Cos(a) * w, y + Mathf.Sin(a) * w * 0.3f);
			DrawCircle(at, 5.0f, new Color(Ink, s));
			DrawCircle(at, 3.0f, new Color(0.55f, 0.75f, 0.3f, s));
		}
	}
}
