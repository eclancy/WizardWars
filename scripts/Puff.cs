using Godot;

/// <summary>
/// The burst of shadow where a Shadow Step leaves and lands: dark puffs billowing out and
/// thinning away, with a ring of violet sparks.
/// </summary>
public partial class Puff : Node2D
{
	const float Life = 0.45f;
	static readonly Color Ink = new(0.04f, 0.03f, 0.05f);

	float elapsed;

	public static Puff Create(Vector2 at) => new() { Position = at, ZIndex = 4 };

	public override void _Process(double delta)
	{
		elapsed += (float)delta;
		if (elapsed >= Life) { QueueFree(); return; }
		QueueRedraw();
	}

	public override void _Draw()
	{
		float t = elapsed / Life, a = 1.0f - t;
		for (int i = 0; i < 7; i++)
		{
			Vector2 at = Vector2.FromAngle(i * Mathf.Tau / 7 + 0.3f) * Units.U(0.55f) * t + new Vector2(0, -Units.U(0.2f));
			float r = Units.U(0.32f) * (1.0f - 0.5f * t);
			DrawCircle(at, r + 4.0f, new Color(Ink, 0.7f * a));
			DrawCircle(at, r, new Color(0.25f, 0.2f, 0.35f, 0.9f * a));
		}
		for (int i = 0; i < 10; i++)
		{
			Vector2 spark = Vector2.FromAngle(i * Mathf.Tau / 10) * Units.U(0.9f) * t + new Vector2(0, -Units.U(0.2f));
			DrawCircle(spark, 3.5f * a + 1.0f, new Color(0.75f, 0.65f, 1.0f, a));
		}
	}
}
