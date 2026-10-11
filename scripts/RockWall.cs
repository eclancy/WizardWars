using Godot;

/// <summary>
/// A rock wall raised by the Earth wizard. It is solid to everyone - wizards walk into it,
/// projectiles stop on it, ring flames inside it go out, and a flamethrower touching it is
/// snuffed. It sinks back after a few seconds.
///
/// Sizes from the Unity prefab: the art is 4.93 x 2.65 units and the collider 4.93 x 1.38,
/// sitting 0.51 below the art's centre (the base of the rocks).
/// </summary>
public partial class RockWall : StaticBody2D
{
	static readonly Vector2 ColliderSize = new(Units.U(4.93f), Units.U(1.38f));
	static readonly Vector2 ColliderOffset = new(0.0f, Units.U(0.51f));

	float lifetime, elapsed;
	Sprite2D sprite;

	public static RockWall Create(Vector2 position, bool vertical, float lifetime)
	{
		RockWall wall = new()
		{
			Position = position,
			Rotation = vertical ? Mathf.Pi * 0.5f : 0.0f,
			lifetime = lifetime,
			CollisionLayer = 1u << 0, // solids
			CollisionMask = 0,
			ZIndex = 4,
		};
		wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = ColliderSize }, Position = ColliderOffset });
		wall.sprite = new Sprite2D { Texture = Content.Texture("res://assets/art/attacks/rock-wall.png"), Scale = Vector2.One * Units.SpriteScale(0.1375f) };
		wall.AddChild(wall.sprite);
		return wall;
	}

	public override void _Ready() => Battle.Current.RockWalls.Add(this);
	public override void _ExitTree() => Battle.Current?.RockWalls.Remove(this);

	/// <summary>Whether a circle touches this wall's collider.</summary>
	public bool Blocks(Vector2 point, float radius)
	{
		Vector2 local = ToLocal(point) - ColliderOffset;
		Vector2 half = ColliderSize * 0.5f;
		Vector2 closest = new(Mathf.Clamp(local.X, -half.X, half.X), Mathf.Clamp(local.Y, -half.Y, half.Y));
		return closest.DistanceSquaredTo(local) <= radius * radius;
	}

	public override void _PhysicsProcess(double delta)
	{
		elapsed += (float)delta;
		// Rise out of the ground over a fifth of a second, sink back over the last fifth.
		float rise = Mathf.Min(1.0f, elapsed / 0.2f) * Mathf.Min(1.0f, (lifetime - elapsed) / 0.2f);
		sprite.Modulate = new Color(1, 1, 1, Mathf.Clamp(rise * 1.5f, 0.0f, 1.0f));
		sprite.Position = new Vector2(0.0f, (1.0f - rise) * Units.U(0.3f));
		if (elapsed >= lifetime) QueueFree();
	}
}
