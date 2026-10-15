using Godot;

/// <summary>
/// The menus' background: a night sky in flat bands (no smooth gradient, to match the game's
/// cartoon art) with four-pointed sparkles in the four elements' colours drifting up and
/// twinkling. Drawn in code; fills its parent.
/// </summary>
public partial class MagicBackdrop : Control
{
	const int Sparkles = 70;

	static readonly Color[] Bands =
	{
		new(0.2f, 0.11f, 0.34f), new(0.17f, 0.09f, 0.3f), new(0.14f, 0.08f, 0.26f),
		new(0.11f, 0.06f, 0.21f), new(0.08f, 0.05f, 0.16f), new(0.06f, 0.04f, 0.12f),
	};

	static readonly Color[] Elements =
	{
		new(1.0f, 0.5f, 0.2f), new(0.5f, 0.8f, 1.0f), new(0.55f, 0.9f, 0.4f), new(1.0f, 0.9f, 0.35f), new(0.85f, 0.7f, 1.0f),
	};

	readonly Vector2[] positions = new Vector2[Sparkles];
	readonly float[] sizes = new float[Sparkles], speeds = new float[Sparkles], phases = new float[Sparkles];
	readonly int[] colours = new int[Sparkles];
	readonly RandomNumberGenerator rng = new();
	float time;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Ignore;
		rng.Seed = 7;
		for (int i = 0; i < Sparkles; i++)
		{
			positions[i] = new Vector2(rng.RandfRange(0, 1920), rng.RandfRange(0, 1080));
			sizes[i] = rng.RandfRange(3.0f, 9.0f);
			speeds[i] = rng.RandfRange(8.0f, 26.0f);
			phases[i] = rng.RandfRange(0.0f, Mathf.Tau);
			colours[i] = rng.RandiRange(0, Elements.Length - 1);
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		time += dt;
		Vector2 size = GetViewportRect().Size;
		for (int i = 0; i < Sparkles; i++)
		{
			positions[i].Y -= speeds[i] * dt;
			positions[i].X += Mathf.Sin(time * 0.6f + phases[i]) * 6.0f * dt;
			if (positions[i].Y < -20.0f) positions[i] = new Vector2(rng.RandfRange(0, size.X), size.Y + 20.0f);
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		// The backdrop always covers the whole screen, so it sizes itself from the viewport
		// rather than its own rect (which can still be zero on the first frames).
		Vector2 size = GetViewportRect().Size;
		float band = size.Y / Bands.Length;
		for (int i = 0; i < Bands.Length; i++) DrawRect(new Rect2(0, i * band, size.X, band + 1), Bands[i]);

		for (int i = 0; i < Sparkles; i++)
		{
			float twinkle = (Mathf.Sin(time * 2.2f + phases[i]) + 1.0f) * 0.5f;
			float r = sizes[i] * Mathf.Lerp(0.55f, 1.0f, twinkle);
			Color colour = new(Elements[colours[i]], Mathf.Lerp(0.25f, 0.85f, twinkle));
			Vector2 p = positions[i];
			// A four-pointed star: long points on the axes, pinched waist between.
			float w = r * 0.28f;
			DrawColoredPolygon(new[]
			{
				p + new Vector2(0, -r), p + new Vector2(w, -w), p + new Vector2(r, 0), p + new Vector2(w, w),
				p + new Vector2(0, r), p + new Vector2(-w, w), p + new Vector2(-r, 0), p + new Vector2(-w, -w),
			}, colour);
		}
	}
}
