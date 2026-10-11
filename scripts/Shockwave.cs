using Godot;

/// <summary>
/// A ripple running out along the ground to the edge of an Earthquake, flattened vertically so
/// it reads as a wave across the ground. Uses the generated ring outline, whose band sits at
/// 0.85 of its half-width.
/// </summary>
public partial class Shockwave : Node2D
{
	const float Duration = 0.8f;
	float radius, elapsed;
	Sprite2D sprite;

	public static Shockwave Create(Vector2 position, float radiusPixels)
	{
		Shockwave wave = new() { Position = position, radius = radiusPixels, ZIndex = 1 };
		wave.sprite = ProceduralFx.RingOutline.NewSprite(1.0f);
		wave.AddChild(wave.sprite);
		return wave;
	}

	public override void _PhysicsProcess(double delta)
	{
		elapsed += (float)delta;
		float t = Mathf.Min(1.0f, elapsed / Duration);
		float halfWidth = ProceduralFx.RingOutline.Texture.GetSize().X * 0.5f;
		float scale = radius / (0.85f * halfWidth) * t;
		sprite.Scale = new Vector2(scale, scale * 0.55f);
		sprite.Modulate = new Color(0.62f, 0.45f, 0.24f, 1.0f - t);
		if (t >= 1.0f) QueueFree();
	}
}
