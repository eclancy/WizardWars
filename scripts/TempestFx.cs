using Godot;

/// <summary>
/// Tempest, Wind's ultimate. A storm whirls round the Wind wizard out to the real radius: bands
/// of wind spinning at different speeds, streaks of rain driven sideways, and a ring of gale at
/// the edge. Enemies inside are hurled outward at <c>Knockback</c> units a second and take
/// <c>Damage</c> a second. Must be a child of its <see cref="Aura"/>.
/// </summary>
public partial class TempestFx : Node2D
{
	static readonly Color Ink = new(0.04f, 0.03f, 0.05f);
	static readonly Color Air = new(0.92f, 1.0f, 0.98f);
	static readonly Color Mint = new(0.45f, 0.92f, 0.78f);

	readonly float damagePerSecond, pushUnitsPerSecond;

	public TempestFx() { }

	public TempestFx(float damagePerSecond, float pushUnitsPerSecond)
	{
		this.damagePerSecond = damagePerSecond;
		this.pushUnitsPerSecond = pushUnitsPerSecond;
	}

	Aura Aura => GetParent<Aura>();

	public override void _PhysicsProcess(double delta)
	{
		Aura aura = Aura;
		if (aura == null) return;
		foreach (Wizard wizard in Battle.Current.Wizards)
		{
			if (!aura.Holds(wizard)) continue;
			wizard.TakeDamage(damagePerSecond * (float)delta);
			Vector2 away = wizard.GlobalPosition - aura.GlobalPosition;
			if (away.LengthSquared() < 1.0f) away = Vector2.Right;
			// Outward, with a twist round the eye.
			Vector2 push = (away.Normalized() + away.Normalized().Orthogonal() * 0.4f).Normalized();
			wizard.Shove(push * Units.U(pushUnitsPerSecond), 0.12f);
		}
	}

	public override void _Process(double delta) => QueueRedraw();

	public override void _Draw()
	{
		Aura aura = Aura;
		if (aura == null) return;
		float t = aura.Elapsed;
		float spread = 1.0f - Mathf.Pow(1.0f - Mathf.Min(1.0f, t / 0.4f), 3.0f);
		float ending = Mathf.Clamp((aura.Duration - t) / 0.4f, 0.0f, 1.0f);
		float r = aura.Radius * spread;

		DrawCircle(Vector2.Zero, r, new Color(0.25f, 0.45f, 0.45f, 0.14f * ending));
		// Wind bands, inner ones spinning faster.
		for (int band = 0; band < 5; band++)
		{
			float br = r * (0.3f + band * 0.16f), speed = 4.0f - band * 0.5f;
			for (int arc = 0; arc < 3; arc++)
			{
				float start = t * speed + arc * Mathf.Tau / 3 + band;
				DrawArc(Vector2.Zero, br, start, start + 1.1f, 18, new Color(Ink, 0.6f * ending), 8.0f, true);
				DrawArc(Vector2.Zero, br, start, start + 1.1f, 18, band % 2 == 0 ? new Color(Air, ending) : new Color(Mint, ending), 4.0f, true);
			}
		}
		// Rain driven sideways round the storm.
		for (int i = 0; i < 40; i++)
		{
			float angle = i * 2.39996f + t * 2.5f, dist = r * ((i * 0.618f) % 1.0f);
			Vector2 at = Vector2.FromAngle(angle) * dist, along = Vector2.FromAngle(angle).Orthogonal() * 14.0f;
			DrawLine(at - along, at + along, new Color(0.75f, 0.9f, 1.0f, 0.8f * ending), 2.5f, true);
		}
		// The gale at the edge.
		DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 96, new Color(Ink, 0.85f * ending), 10.0f, true);
		DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 96, new Color(Mint, ending), 5.0f, true);
		for (int i = 0; i < 16; i++)
		{
			float start = -t * 3.0f + i * Mathf.Tau / 16;
			DrawArc(Vector2.Zero, r + 10.0f, start, start + 0.25f, 6, new Color(Air, ending), 4.0f, true);
		}
	}
}
