using Godot;

/// <summary>
/// A little joystick beside a frozen wizard, rocking from side to side between two arrows: wiggle
/// the stick to break out of the ice. Drawn in the game's flat style (dark outline, flat fills).
/// Each wiggle that lands makes it jump, so the player can see it working.
/// </summary>
public partial class WiggleHint : Node2D
{
	static readonly Color Outline = new(0.05f, 0.07f, 0.16f);
	static readonly Color Base = new(0.32f, 0.36f, 0.48f);
	static readonly Color Stick = new(0.62f, 0.66f, 0.76f);
	static readonly Color Knob = new(0.86f, 0.95f, 1.0f);
	static readonly Color Arrow = new(1.0f, 1.0f, 1.0f);

	float time, bump;

	public void Nudge() => bump = 1.0f;

	public override void _Process(double delta)
	{
		time += (float)delta;
		bump = Mathf.Max(0.0f, bump - (float)delta * 6.0f);
		Scale = Vector2.One * 1.35f * (1.0f + 0.25f * bump);
		QueueRedraw();
	}

	public override void _Draw()
	{
		// The base: a flat ellipse, drawn as a squashed circle.
		DrawSetTransform(new Vector2(0, 14), 0.0f, new Vector2(1.0f, 0.42f));
		DrawCircle(Vector2.Zero, 20.0f, Outline);
		DrawCircle(Vector2.Zero, 16.5f, Base);
		DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);

		// The stick rocks quickly from side to side.
		float tilt = Mathf.Sin(time * 15.0f) * 0.6f;
		Vector2 foot = new(0, 14), top = foot + Vector2.Up.Rotated(tilt) * 30.0f;
		DrawLine(foot, top, Outline, 10.0f, true);
		DrawLine(foot, top, Stick, 5.0f, true);
		DrawCircle(top, 11.0f, Outline);
		DrawCircle(top, 8.0f, Knob);
		DrawCircle(top + new Vector2(-3, -3), 2.5f, Colors.White);

		// Arrows either side, flashing in time with the rock.
		float flash = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(time * 15.0f));
		foreach (float side in new[] { -1.0f, 1.0f })
		{
			Vector2 tip = new(side * 38.0f, 0.0f);
			Vector2[] points = { tip, tip + new Vector2(-side * 11.0f, -9.0f), tip + new Vector2(-side * 11.0f, 9.0f) };
			DrawColoredPolygon(Grow(points, 2.5f), Outline);
			DrawColoredPolygon(points, new Color(Arrow, flash));
		}
	}

	/// <summary>A triangle pushed out from its centre, for an outline behind it.</summary>
	static Vector2[] Grow(Vector2[] points, float by)
	{
		Vector2 centre = (points[0] + points[1] + points[2]) / 3.0f;
		Vector2[] grown = new Vector2[points.Length];
		for (int i = 0; i < points.Length; i++) grown[i] = points[i] + (points[i] - centre).Normalized() * by * 1.8f;
		return grown;
	}
}
