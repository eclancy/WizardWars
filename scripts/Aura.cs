using Godot;

/// <summary>
/// An ultimate centred on its caster that hinders enemies for as long as they stay inside its
/// radius: Absolute Zero (slow and no energy regen) and Earthquake (a heavier slow, with
/// shockwave rings rippling out to the edge).
///
/// The modifier is keyed to this node, so leaving the radius, the ultimate ending, and the
/// caster dying mid-ultimate all release it - Unity leaked the slow in the first and last cases.
/// </summary>
public partial class Aura : Node2D
{
	Wizard caster;
	int casterTeam;
	float radius, duration, elapsed, shockwaveInterval, nextShockwave;
	Modifier effect;
	Sprite2D icon, ring;
	System.Action onFinished;
	bool finished;

	public static Aura Create(Wizard caster, float radiusUnits, float duration, Modifier effect, string iconPath, Color tint, float shockwaveInterval, System.Action onFinished)
	{
		Aura aura = new()
		{
			caster = caster,
			casterTeam = caster.Team,
			radius = Units.U(radiusUnits),
			duration = duration,
			effect = effect,
			shockwaveInterval = shockwaveInterval,
			onFinished = onFinished,
			ZIndex = 1,
		};

		// A faint disc showing the area that actually catches you. (Unity's overlay image
		// ended up a third of a unit wide, so the radius was never visible.)
		aura.ring = ProceduralFx.AreaDisc.NewSprite();
		aura.ring.Scale = Vector2.One * (aura.radius * 2.0f / ProceduralFx.AreaDisc.Texture.GetSize().X);
		aura.ring.Modulate = new Color(tint, 0.22f);
		aura.AddChild(aura.ring);

		aura.icon = new Sprite2D { Texture = Content.Texture(iconPath), ZIndex = 10, Position = new Vector2(0, -Units.U(1.4f)) };
		aura.AddChild(aura.icon);
		return aura;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (finished) return;
		if (!GodotObject.IsInstanceValid(caster) || !caster.Alive) { End(); return; }
		float dt = (float)delta;
		elapsed += dt;
		GlobalPosition = caster.GlobalPosition;

		// Pulse the icon, as Unity's PulseAreaVisual did.
		float wave = (Mathf.Sin(elapsed * 8.0f) + 1.0f) * 0.5f;
		icon.Scale = Vector2.One * Units.SpriteScale(Mathf.Lerp(0.5f, 0.62f, wave));
		ring.Modulate = new Color(ring.Modulate, Mathf.Lerp(0.16f, 0.26f, wave));

		foreach (Wizard wizard in Battle.Current.Wizards)
		{
			bool inside = Combat.CanAffect(caster, casterTeam, wizard) && wizard.GlobalPosition.DistanceSquaredTo(GlobalPosition) <= radius * radius;
			if (inside) wizard.Status.Set(this, effect);
			else wizard.Status.Remove(this);
		}

		if (shockwaveInterval > 0.0f && elapsed >= nextShockwave)
		{
			nextShockwave = elapsed + shockwaveInterval;
			Battle.Current.AddEffect(Shockwave.Create(GlobalPosition, radius));
		}

		if (elapsed >= duration) End();
	}

	void End()
	{
		finished = true;
		foreach (Wizard wizard in Battle.Current.Wizards) wizard.Status.Remove(this);
		onFinished?.Invoke();
		QueueFree();
	}
}
