using System.Collections.Generic;
using Godot;

/// <summary>
/// How Absolute Zero looks. When it goes off:
/// - a sheet of frost races out from the Frost wizard to the real radius
/// - a jagged rim of ice crystals rings its edge
/// - snowflakes swirl round inside for as long as it lasts
///
/// Must be a child of the <see cref="Aura"/> it draws.
/// </summary>
public partial class FrostFx : Node2D
{
	static readonly Color Frost = new(0.5f, 0.8f, 1.0f);
	static readonly Color Rim = new(0.85f, 0.96f, 1.0f);
	static readonly Color RimOutline = new(0.18f, 0.36f, 0.6f);
	const int Flakes = 60, Shards = 36;

	readonly List<(float angle, float distance, float speed, float size)> flakes = new();

	readonly float damagePerSecond;

	public FrostFx() { }

	/// <summary>Enemies the frost has hold of lose this much health a second.</summary>
	public FrostFx(float damagePerSecond) => this.damagePerSecond = damagePerSecond;

	public override void _PhysicsProcess(double delta)
	{
		Aura aura = GetParent<Aura>();
		if (aura == null || damagePerSecond <= 0.0f) return;
		foreach (Wizard wizard in Battle.Current.Wizards) if (aura.Holds(wizard)) wizard.TakeDamage(damagePerSecond * (float)delta);
	}

	public override void _Ready()
	{
		for (int i = 0; i < Flakes; i++)
			flakes.Add(((float)GD.RandRange(0.0, Mathf.Tau), Mathf.Sqrt(GD.Randf()), (float)GD.RandRange(0.25, 0.7), (float)GD.RandRange(4.0, 8.0)));
	}

	public override void _Process(double delta) => QueueRedraw();

	public override void _Draw()
	{
		Aura aura = GetParent<Aura>();
		if (aura == null) return;
		float t = aura.Elapsed;
		// The frost spreads out fast, then the whole sheet breathes gently.
		float spread = 1.0f - Mathf.Pow(1.0f - Mathf.Min(1.0f, t / 0.35f), 3.0f);
		float radius = aura.Radius * spread;
		float ending = Mathf.Clamp((aura.Duration - t) / 0.4f, 0.0f, 1.0f);
		float breathe = 0.5f + 0.5f * Mathf.Sin(t * 5.0f);

		DrawCircle(Vector2.Zero, radius, new Color(Frost, (0.18f + 0.05f * breathe) * ending));
		// A hard icy edge at the real radius, under the crystals.
		DrawArc(Vector2.Zero, radius - 4.0f, 0.0f, Mathf.Tau, 96, new Color(RimOutline, 0.9f * ending), 10.0f, true);
		DrawArc(Vector2.Zero, radius - 4.0f, 0.0f, Mathf.Tau, 96, new Color(Rim, ending), 5.0f, true);
		DrawCircle(Vector2.Zero, radius * 0.6f, new Color(Frost, 0.06f * ending));

		// The rim: ice crystals pointing outward all round the edge, slowly turning.
		for (int i = 0; i < Shards; i++)
		{
			float angle = i * Mathf.Tau / Shards + t * 0.15f;
			float length = (i % 3 == 0 ? 46.0f : 28.0f) * spread;
			Vector2 outward = Vector2.FromAngle(angle), side = outward.Orthogonal() * (i % 3 == 0 ? 13.0f : 9.0f);
			Vector2 foot = outward * (radius - 6.0f);
			Vector2[] shard = { foot - side, foot + outward * length, foot + side };
			DrawColoredPolygon(new[] { shard[0] - outward * 2.0f - side * 0.3f, shard[1] + outward * 3.0f, shard[2] - outward * 2.0f + side * 0.3f }, new Color(RimOutline, ending));
			DrawColoredPolygon(shard, new Color(Rim, ending));
		}

		// Snowflakes swirling round inside: little six-armed stars.
		foreach ((float angle, float distance, float speed, float size) in flakes)
		{
			float a = angle + t * speed;
			Vector2 at = Vector2.FromAngle(a) * distance * radius * 0.95f + new Vector2(0.0f, Mathf.Sin(t * 2.0f + angle * 3.0f) * 6.0f);
			Color flake = new(1, 1, 1, 0.85f * ending);
			for (int arm = 0; arm < 3; arm++)
			{
				Vector2 d = Vector2.FromAngle(arm * Mathf.Pi / 3.0f + t) * size;
				DrawLine(at - d, at + d, flake, 2.4f, true);
			}
		}
	}
}
