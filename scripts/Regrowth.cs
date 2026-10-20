using Godot;

/// <summary>
/// Regrowth, Nature's RB: the caster heals <c>Damage</c> health over <c>Duration</c> seconds,
/// while leaves spiral up around them and a green glow pulses at their feet.
/// </summary>
public partial class Regrowth : Node2D
{
	const int Leaves = 8;
	static readonly Color Ink = new(0.04f, 0.03f, 0.05f);

	Wizard caster;
	float healPerSecond, duration, elapsed;

	public static Regrowth Create(Wizard caster, AbilityData data) =>
		new() { caster = caster, healPerSecond = data.Damage / Mathf.Max(0.1f, data.Duration), duration = data.Duration, ZIndex = 3, GlobalPosition = caster.GlobalPosition };

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		elapsed += dt;
		if (elapsed >= duration || !GodotObject.IsInstanceValid(caster) || !caster.Alive) { QueueFree(); return; }
		GlobalPosition = caster.GlobalPosition;
		caster.Heal(healPerSecond * dt);
	}

	public override void _Process(double delta) => QueueRedraw();

	public override void _Draw()
	{
		float fade = Mathf.Min(1.0f, (duration - elapsed) / 0.3f) * Mathf.Min(1.0f, elapsed / 0.2f);
		Vector2 feet = new(0.0f, Units.U(0.55f));
		// The glow at the feet, pulsing.
		float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * 6.0f);
		DrawSetTransform(feet, 0.0f, new Vector2(1.0f, 0.45f));
		DrawCircle(Vector2.Zero, Units.U(0.7f), new Color(0.45f, 0.9f, 0.3f, (0.18f + 0.12f * pulse) * fade));
		DrawArc(Vector2.Zero, Units.U(0.7f), 0.0f, Mathf.Tau, 40, new Color(0.6f, 1.0f, 0.4f, 0.8f * fade), 4.0f, true);
		DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
		// Leaves spiralling up round the caster, each one a little outlined lens.
		for (int i = 0; i < Leaves; i++)
		{
			float rise = (elapsed * 0.9f + i / (float)Leaves) % 1.0f;
			float angle = rise * Mathf.Tau * 1.3f + i * 0.8f;
			Vector2 at = feet + new Vector2(Mathf.Cos(angle) * Units.U(0.6f), Mathf.Sin(angle) * Units.U(0.22f) - rise * Units.U(1.5f));
			float alpha = fade * Mathf.Min(1.0f, (1.0f - rise) * 3.0f);
			Vector2 along = Vector2.FromAngle(angle * 1.7f) * 11.0f, across = along.Orthogonal() * 0.45f;
			Vector2[] leaf = { at - along, at - across, at + along, at + across };
			DrawColoredPolygon(new[] { leaf[0] - along * 0.25f, leaf[1] - across * 0.5f, leaf[2] + along * 0.25f, leaf[3] + across * 0.5f }, new Color(Ink, alpha));
			DrawColoredPolygon(leaf, new Color(0.5f, 0.88f, 0.32f, alpha));
		}
		// A soft plus over the head: healing.
		Vector2 top = new(0.0f, -Units.U(1.0f) - 6.0f * pulse);
		DrawLine(top - new Vector2(0, 10), top + new Vector2(0, 10), new Color(Ink, fade), 9.0f, true);
		DrawLine(top - new Vector2(10, 0), top + new Vector2(10, 0), new Color(Ink, fade), 9.0f, true);
		DrawLine(top - new Vector2(0, 9), top + new Vector2(0, 9), new Color(0.6f, 1.0f, 0.45f, fade), 5.0f, true);
		DrawLine(top - new Vector2(9, 0), top + new Vector2(9, 0), new Color(0.6f, 1.0f, 0.45f, fade), 5.0f, true);
	}
}
