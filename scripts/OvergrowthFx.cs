using System.Collections.Generic;
using Godot;

/// <summary>
/// Overgrowth, Nature's ultimate. Thorny vines race out across the ground from the Nature wizard
/// to the real radius, sprouting leaves and flowers as they go. Enemies inside take
/// <c>Damage</c> a second (the <see cref="Aura"/> slows them), and the caster heals
/// <c>Damage2</c> a second while it lasts. Must be a child of its Aura.
/// </summary>
public partial class OvergrowthFx : Node2D
{
	static readonly Color Ink = new(0.04f, 0.03f, 0.05f);

	readonly float damagePerSecond, healPerSecond;
	readonly Wizard caster;
	readonly List<(float angle, float wiggle, float length)> vines = new();

	public OvergrowthFx() { }

	public OvergrowthFx(Wizard caster, float damagePerSecond, float healPerSecond)
	{
		this.caster = caster;
		this.damagePerSecond = damagePerSecond;
		this.healPerSecond = healPerSecond;
		for (int i = 0; i < 14; i++) vines.Add((i * Mathf.Tau / 14 + (float)GD.RandRange(-0.15, 0.15), (float)GD.RandRange(0.0, Mathf.Tau), (float)GD.RandRange(0.75, 1.0)));
	}

	Aura Aura => GetParent<Aura>();

	public override void _PhysicsProcess(double delta)
	{
		Aura aura = Aura;
		if (aura == null) return;
		float dt = (float)delta;
		foreach (Wizard wizard in Battle.Current.Wizards) if (aura.Holds(wizard)) wizard.TakeDamage(damagePerSecond * dt);
		if (GodotObject.IsInstanceValid(caster)) caster.Heal(healPerSecond * dt);
	}

	public override void _Process(double delta) => QueueRedraw();

	public override void _Draw()
	{
		Aura aura = Aura;
		if (aura == null) return;
		float t = aura.Elapsed;
		float grow = 1.0f - Mathf.Pow(1.0f - Mathf.Min(1.0f, t / 0.6f), 2.0f);
		float ending = Mathf.Clamp((aura.Duration - t) / 0.4f, 0.0f, 1.0f);
		float r = aura.Radius;
		Color vine = new(0.3f, 0.6f, 0.2f, ending), dark = new(0.2f, 0.42f, 0.14f, ending), leaf = new(0.5f, 0.88f, 0.32f, ending), outline = new(Ink, ending);

		// The reach, a faint green floor with a vine-ring edge.
		DrawCircle(Vector2.Zero, r * grow, new Color(0.3f, 0.6f, 0.2f, 0.16f * ending));
		DrawArc(Vector2.Zero, r * grow, 0.0f, Mathf.Tau, 96, outline, 9.0f, true);
		DrawArc(Vector2.Zero, r * grow, 0.0f, Mathf.Tau, 96, vine, 5.0f, true);

		foreach ((float angle, float wiggle, float length) in vines)
		{
			Vector2 dir = Vector2.FromAngle(angle), side = dir.Orthogonal();
			Vector2[] line = new Vector2[16];
			for (int i = 0; i < line.Length; i++)
			{
				float f = i / (float)(line.Length - 1);
				line[i] = dir * r * length * grow * f + side * Mathf.Sin(f * 9.0f + wiggle + t * 1.5f) * 16.0f * f;
			}
			DrawPolyline(line, outline, 10.0f, true);
			DrawPolyline(line, vine, 5.5f, true);
			for (int i = 2; i < line.Length; i += 2)
			{
				Vector2 along = (line[i] - line[i - 1]).Normalized(), up = along.Orthogonal() * (i % 4 == 0 ? 10.0f : -10.0f);
				if (i % 6 == 0)
				{
					// A leaf.
					Vector2 tip = line[i] + up * 1.6f + along * 6.0f;
					Vector2[] blade = { line[i], line[i] + up * 0.8f - along * 6.0f, tip, line[i] + up * 0.3f + along * 8.0f };
					DrawColoredPolygon(blade, leaf);
					DrawPolyline(new[] { blade[0], blade[1], blade[2], blade[3], blade[0] }, outline, 2.0f, true);
				}
				else
				{
					Vector2[] spike = { line[i] - along * 4.0f, line[i] + up, line[i] + along * 4.0f };
					DrawColoredPolygon(spike, dark);
				}
			}
			// A flower at the tip once it has grown.
			if (grow > 0.9f)
			{
				Vector2 tip = line[^1];
				for (int p = 0; p < 5; p++) DrawCircle(tip + Vector2.FromAngle(p * Mathf.Tau / 5 + t) * 6.0f, 5.0f, new Color(1.0f, 0.55f, 0.7f, ending));
				DrawCircle(tip, 4.0f, new Color(1.0f, 0.9f, 0.4f, ending));
			}
		}
	}
}
