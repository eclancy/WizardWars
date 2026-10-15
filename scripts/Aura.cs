using Godot;

/// <summary>
/// An ultimate centred on its caster that hinders enemies for as long as they stay inside its
/// radius: Absolute Zero (slow and no energy regen, drawn by <see cref="FrostFx"/>) and
/// Earthquake (a heavier slow, drawn by <see cref="QuakeFx"/>). The look is a child node; this
/// class is only the rule.
///
/// The modifier is keyed to this node, so leaving the radius, the ultimate ending, and the
/// caster dying mid-ultimate all release it - Unity leaked the slow in the first and last cases.
/// </summary>
public partial class Aura : Node2D
{
	Wizard caster;
	int casterTeam;
	float duration, elapsed;
	Modifier effect;
	Sprite2D icon;
	System.Action onFinished;
	bool finished;

	/// <summary>The radius in pixels.</summary>
	public float Radius { get; private set; }
	public float Elapsed => elapsed;
	public float Duration => duration;

	public static Aura Create(Wizard caster, float radiusUnits, float duration, Modifier effect, string iconPath, Node2D look, System.Action onFinished)
	{
		Aura aura = new()
		{
			caster = caster,
			casterTeam = caster.Team,
			Radius = Units.U(radiusUnits),
			duration = duration,
			effect = effect,
			onFinished = onFinished,
			ZIndex = 1,
		};
		aura.AddChild(look);
		aura.icon = new Sprite2D { Texture = Content.Texture(iconPath), ZIndex = 10, Position = new Vector2(0, -Units.U(1.4f)) };
		aura.AddChild(aura.icon);
		return aura;
	}

	/// <summary>Whether the aura has hold of this wizard: an enemy inside the radius.</summary>
	public bool Holds(Wizard wizard) =>
		GodotObject.IsInstanceValid(wizard) && wizard.Alive && Combat.CanAffect(caster, casterTeam, wizard) &&
		wizard.GlobalPosition.DistanceSquaredTo(GlobalPosition) <= Radius * Radius;

	public override void _PhysicsProcess(double delta)
	{
		if (finished) return;
		if (!GodotObject.IsInstanceValid(caster) || !caster.Alive) { End(); return; }
		elapsed += (float)delta;
		GlobalPosition = caster.GlobalPosition;

		// Pulse the icon, as Unity's PulseAreaVisual did.
		float wave = (Mathf.Sin(elapsed * 8.0f) + 1.0f) * 0.5f;
		icon.Scale = Vector2.One * Units.SpriteScale(Mathf.Lerp(0.5f, 0.62f, wave));

		foreach (Wizard wizard in Battle.Current.Wizards)
		{
			if (Holds(wizard)) wizard.Status.Set(this, effect);
			else wizard.Status.Remove(this);
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
