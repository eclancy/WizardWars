using Godot;

/// <summary>
/// Eclipse, Shadow's ultimate. Night pours out from the Shadow wizard to the real radius: a dark
/// disc with a ragged edge of shadow flames, a black sun with a pale corona above the caster,
/// and motes of light being drawn into the dark. Enemies inside take <c>Damage</c> a second (the
/// <see cref="Aura"/> silences them). Must be a child of its Aura.
/// </summary>
public partial class EclipseFx : Node2D
{
	static readonly Color Ink = new(0.04f, 0.03f, 0.05f);
	static readonly Color Night = new(0.07f, 0.04f, 0.12f);
	static readonly Color Corona = new(0.82f, 0.74f, 1.0f);

	readonly float damagePerSecond;

	public EclipseFx() { }

	public EclipseFx(float damagePerSecond) => this.damagePerSecond = damagePerSecond;

	Aura Aura => GetParent<Aura>();

	public override void _PhysicsProcess(double delta)
	{
		Aura aura = Aura;
		if (aura == null) return;
		foreach (Wizard wizard in Battle.Current.Wizards) if (aura.Holds(wizard)) wizard.TakeDamage(damagePerSecond * (float)delta);
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

		DrawCircle(Vector2.Zero, r, new Color(Night, 0.5f * ending));
		DrawCircle(Vector2.Zero, r * 0.65f, new Color(Night, 0.2f * ending));
		// Shadow flames licking up all round the edge.
		const int Flames = 40;
		for (int i = 0; i < Flames; i++)
		{
			float angle = i * Mathf.Tau / Flames + t * 0.2f;
			float lick = 18.0f + 14.0f * Mathf.Sin(t * 7.0f + i * 1.9f);
			Vector2 outward = Vector2.FromAngle(angle), side = outward.Orthogonal() * 10.0f;
			Vector2 foot = outward * r;
			Vector2[] flame = { foot - side, foot + outward * lick + side * 0.4f, foot + side };
			DrawColoredPolygon(flame, new Color(0.24f, 0.16f, 0.38f, 0.95f * ending));
		}
		DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 96, new Color(Ink, 0.9f * ending), 9.0f, true);
		DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 96, new Color(Corona, 0.8f * ending), 3.5f, true);

		// Motes of light being drawn into the dark from its edge.
		for (int i = 0; i < 24; i++)
		{
			float f = (t * 0.5f + i / 24.0f) % 1.0f;
			Vector2 at = Vector2.FromAngle(i * 2.39996f) * r * (1.0f - f);
			DrawCircle(at, 3.5f * (1.0f - f) + 1.0f, new Color(Corona, ending * (1.0f - f)));
		}

		// The black sun over the caster, its corona flickering.
		Vector2 sun = new(0.0f, -Units.U(2.0f));
		for (int i = 0; i < 12; i++)
		{
			float angle = i * Mathf.Tau / 12 + t * 0.6f, len = 34.0f + 6.0f * Mathf.Sin(t * 9.0f + i);
			Vector2 dir = Vector2.FromAngle(angle), side = dir.Orthogonal() * 7.0f;
			Vector2[] ray = { sun + dir * 24.0f - side, sun + dir * len, sun + dir * 24.0f + side };
			DrawColoredPolygon(ray, new Color(Corona, ending));
		}
		DrawCircle(sun, 29.0f, new Color(Ink, ending));
		DrawCircle(sun, 25.0f, new Color(Corona, ending));
		DrawCircle(sun + new Vector2(4, -2), 22.0f, new Color(Night, ending));
	}
}
