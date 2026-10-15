using Godot;

/// <summary>
/// One jagged lightning bolt between two points, drawn in the game's flat style: a dark
/// outline, a yellow body and a white core, re-jagged a few times as it flickers out.
/// </summary>
public partial class Bolt : Node2D
{
	static readonly Color Outline = new(0.15f, 0.12f, 0.3f);
	static readonly Color Body = new(1.0f, 0.9f, 0.3f);
	static readonly Color Core = new(1.0f, 1.0f, 0.95f);

	Vector2 start, end;
	float life, elapsed, nextJag;
	Vector2[] points;

	public static Bolt Create(Vector2 start, Vector2 end, float life) => new() { start = start, end = end, life = life };

	public override void _Process(double delta)
	{
		elapsed += (float)delta;
		if (elapsed >= life) { QueueFree(); return; }
		if (points == null || elapsed >= nextJag)
		{
			nextJag = elapsed + 0.06f;
			Jag();
		}
		QueueRedraw();
	}

	// Zig-zag points along the line, displaced sideways, fixed at both ends.
	void Jag()
	{
		float length = start.DistanceTo(end);
		int segments = Mathf.Max(4, (int)(length / 28.0f));
		Vector2 side = (end - start).Normalized().Orthogonal();
		points = new Vector2[segments + 1];
		for (int i = 0; i <= segments; i++)
		{
			float along = i / (float)segments;
			float sway = i == 0 || i == segments ? 0.0f : (float)GD.RandRange(-1.0, 1.0) * Mathf.Min(22.0f, length * 0.08f);
			points[i] = start.Lerp(end, along) + side * sway;
		}
	}

	public override void _Draw()
	{
		if (points == null) return;
		float fade = 1.0f - elapsed / life;
		DrawPolyline(points, new Color(Outline, fade), 11.0f);
		DrawPolyline(points, new Color(Body, fade), 7.0f);
		DrawPolyline(points, new Color(Core, fade), 2.5f);
	}
}
