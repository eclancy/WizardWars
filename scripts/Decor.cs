using Godot;

/// <summary>
/// Soft scenery: a small cactus or a clump of toadstools that wizards and shots pass straight
/// through. It has no collider and casts no shadow (only cover does, see <see cref="Cover"/>), and
/// it sways when a wizard walks through it or a shot flies past, so it never reads as solid.
///
/// It is y-sorted with the wizards like cover is, and sways about its base.
/// </summary>
public partial class Decor : Node2D
{
	/// <summary>Plants are drawn at half the art's natural size: scenery, smaller than any cover.</summary>
	const float Shrink = 0.5f;
	const float MaxLean = 0.4f;

	float lean, swing;

	public static Decor Create(string kind, Vector2 position, int variant = 0)
	{
		Decor decor = new() { Position = position, ZIndex = 2, Name = "Decor" };
		bool flip = variant % 2 == 1;
		if (kind == "toadstool")
		{
			// A clump: a bigger toadstool and a smaller one beside it.
			decor.AddPlant("toadstool", new Vector2(flip ? 14 : -14, 0), 1.0f, flip);
			decor.AddPlant("toadstool", new Vector2(flip ? -22 : 22, 6), 0.62f, !flip);
		}
		else decor.AddPlant(kind, Vector2.Zero, 1.0f, flip);
		return decor;
	}

	void AddPlant(string kind, Vector2 offset, float size, bool flip)
	{
		CoverArt.Piece art = CoverArt.For(kind);
		AddChild(new Sprite2D
		{
			Texture = art.Texture,
			Centered = false,
			Offset = -art.Anchor,
			Position = offset,
			Scale = new Vector2(flip ? -1 : 1, 1) * art.Scale * Shrink * size,
		});
	}

	public override void _PhysicsProcess(double delta)
	{
		Battle battle = Battle.Current;
		if (battle == null) return;
		float dt = (float)delta;

		// Anything brushing past pushes the plant away from it.
		foreach (Wizard wizard in battle.Wizards)
		{
			if (!IsInstanceValid(wizard)) continue;
			Vector2 offset = GlobalPosition - wizard.Feet;
			if (Mathf.Abs(offset.X) < Units.U(0.7f) && Mathf.Abs(offset.Y) < Units.U(0.4f))
				swing += (offset.X >= 0 ? 1 : -1) * 16.0f * dt;
		}
		foreach (Projectile shot in battle.Projectiles)
		{
			if (!IsInstanceValid(shot)) continue;
			Vector2 offset = GlobalPosition - shot.GlobalPosition;
			if (offset.Length() < Units.U(0.9f))
				swing += Mathf.Sign(Vector2.FromAngle(shot.Rotation).X + 0.001f) * 10.0f * dt;
		}

		// A damped spring back to upright.
		swing += (-60.0f * lean - 5.0f * swing) * dt;
		lean = Mathf.Clamp(lean + swing * dt, -MaxLean, MaxLean);
		Rotation = lean;
	}
}
