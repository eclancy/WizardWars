using Godot;

/// <summary>
/// A lightning rod. Every half second it shocks everyone within its radius - the caster
/// included, as in Unity, but never the caster's teammates. The rod itself is solid: it blocks
/// movement and stops projectiles, like the non-trigger collider on the Unity prefab.
/// </summary>
public partial class LightningRod : StaticBody2D
{
	Wizard caster;
	int casterTeam;
	float radius, damage, interval, lifetime, elapsed, nextShock, shockShownUntil;
	AnimatedSprite2D shock;

	public static LightningRod Create(Wizard caster, Vector2 position, AbilityData data, float lifetime)
	{
		LightningRod rod = new()
		{
			caster = caster,
			casterTeam = caster.Team,
			radius = Units.U(data.Radius),
			damage = data.Damage,
			interval = data.Interval,
			lifetime = lifetime,
			nextShock = data.Interval,
			Position = position,
			CollisionLayer = 1u << 0, // solids
			CollisionMask = 0,
			ZIndex = 3,
		};
		// The rod stands on the ground with its base at the placed point.
		rod.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(Units.U(0.25f), Units.U(0.3f)) } });
		Texture2D art = Content.Texture("res://assets/art/attacks/lightning-rod.png");
		float scale = Units.U(1.6f) / art.GetSize().Y;
		rod.AddChild(new Sprite2D { Texture = art, Scale = Vector2.One * scale, Position = new Vector2(0.0f, -Units.U(0.65f)) });

		rod.shock = new AnimatedSprite2D
		{
			SpriteFrames = Sheets.Grid("res://assets/art/attacks/rod-shock-sheet.png", 6, 5, 30, 70.0f),
			Scale = Vector2.One * (rod.radius * 2.0f / 994.0f),
			Position = new Vector2(0.0f, -Units.U(0.6f)),
			Visible = false,
			ZIndex = 2,
		};
		rod.AddChild(rod.shock);
		return rod;
	}

	public override void _PhysicsProcess(double delta)
	{
		elapsed += (float)delta;
		if (elapsed >= lifetime) { QueueFree(); return; }
		if (elapsed >= shockShownUntil) shock.Visible = false;
		if (elapsed < nextShock) return;
		nextShock = elapsed + interval;

		shock.Visible = true;
		shock.Play("default");
		shockShownUntil = elapsed + 0.3f;
		Sfx.Play("res://assets/sfx/rodlightning.wav", -10.0f);

		foreach (Wizard wizard in Battle.Current.Wizards)
			if (wizard.GlobalPosition.DistanceSquaredTo(GlobalPosition) <= radius * radius)
				Combat.Damage(caster, casterTeam, wizard, damage, hitsCaster: true);
	}
}
