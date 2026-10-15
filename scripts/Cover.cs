using Godot;

/// <summary>
/// One piece of a stage's cover: a tree stump, an ice crystal cluster, a saguaro cactus, a rune
/// obelisk, a basalt boulder or a toadstool. It is solid - wizards walk into it, projectiles stop
/// on it, flames inside it go out - and it is y-sorted with the wizards, so a wizard standing
/// behind it is drawn behind it.
///
/// Every piece stands on a hard, flat shadow. Nothing else in an arena casts one (the floors are
/// only ground), so a shadow means "solid": that is how a player tells cover from the map.
///
/// The node sits on the centre of the footprint, which is where the collider is. Sizes are in
/// Unity units.
/// </summary>
public partial class Cover : StaticBody2D
{
	static readonly Color ShadowColour = new(0.05f, 0.03f, 0.1f, 0.38f);

	public static Cover Create(string kind, Vector2 position, int variant = 0)
	{
		Cover cover = new() { Position = position, CollisionLayer = 1u << 0, CollisionMask = 0, ZIndex = 2, Name = "Cover" };
		(Shape2D shape, Vector2 shadow) = kind switch
		{
			"stump" => ((Shape2D)new CircleShape2D { Radius = Units.U(0.62f) }, new Vector2(0.8f, 0.3f)),
			"crystal" => (new CircleShape2D { Radius = Units.U(0.55f) }, new Vector2(0.72f, 0.28f)),
			"saguaro" => (new CircleShape2D { Radius = Units.U(0.36f) }, new Vector2(0.55f, 0.2f)),
			"obelisk" => (new RectangleShape2D { Size = new Vector2(Units.U(0.74f), Units.U(0.4f)) }, new Vector2(0.6f, 0.22f)),
			"boulder" => (new CircleShape2D { Radius = Units.U(0.62f) }, new Vector2(0.8f, 0.3f)),
			"toadstool" => (new CircleShape2D { Radius = Units.U(0.36f) }, new Vector2(0.72f, 0.26f)),
			_ => (new CircleShape2D { Radius = Units.U(0.6f) }, new Vector2(0.7f, 0.28f)),
		};
		cover.AddChild(new CollisionShape2D { Shape = shape });

		// The shadow lies on the ground, cast a little down and to the right, under wizards and
		// ground patches (ZIndex 2 - 4 = -2) but over the floor.
		Vector2[] ellipse = new Vector2[32];
		Vector2 radii = new(Units.U(shadow.X), Units.U(shadow.Y));
		for (int i = 0; i < ellipse.Length; i++)
		{
			float angle = i * Mathf.Tau / ellipse.Length;
			ellipse[i] = new Vector2(Mathf.Cos(angle) * radii.X, Mathf.Sin(angle) * radii.Y) + new Vector2(Units.U(0.14f), Units.U(0.04f));
		}
		cover.AddChild(new Polygon2D { Polygon = ellipse, Color = ShadowColour, Antialiased = true, ZIndex = -4 });

		CoverArt.Piece art = CoverArt.For(kind, variant);
		cover.AddChild(new Sprite2D
		{
			Texture = art.Texture,
			Centered = false,
			Offset = -art.Anchor,
			Scale = Vector2.One * art.Scale,
		});
		return cover;
	}
}
