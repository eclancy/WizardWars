using Godot;

/// <summary>
/// Speed Up: the caster runs faster for a few seconds, leaving fading afterimages behind him.
/// The speed bonus is a modifier keyed to this node, so it ends exactly when the node does.
/// (Unity used a particle prefab; the afterimages read better against busy stage art.)
/// </summary>
public partial class SpeedTrail : Node2D
{
	Wizard caster;
	float duration, elapsed, nextGhost;

	public static SpeedTrail Create(Wizard caster, float duration, float hastePercent)
	{
		SpeedTrail trail = new() { caster = caster, duration = duration, ZIndex = 1 };
		caster.Status.Set(trail, new Modifier { Haste = hastePercent });
		return trail;
	}

	public override void _PhysicsProcess(double delta)
	{
		elapsed += (float)delta;
		if (elapsed >= duration || !GodotObject.IsInstanceValid(caster) || !caster.Alive)
		{
			if (GodotObject.IsInstanceValid(caster)) caster.Status.Remove(this);
			QueueFree();
			return;
		}
		if (elapsed < nextGhost) return;
		nextGhost = elapsed + 0.06f;

		Sprite2D ghost = new()
		{
			Texture = caster.Data.Portrait,
			Scale = Vector2.One * Units.SpriteScale(0.15f),
			Position = caster.GlobalPosition, // the effects layer sits at the origin
			Modulate = new Color(1.0f, 0.95f, 0.4f, 0.5f),
			ZIndex = 1,
		};
		Battle.Current.AddEffect(ghost);
		Tween fade = ghost.CreateTween();
		fade.TweenProperty(ghost, "modulate:a", 0.0f, 0.3f);
		fade.TweenCallback(Callable.From(ghost.QueueFree));
	}
}
