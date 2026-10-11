using Godot;

/// <summary>
/// Overcharge: for a few seconds the Electricity wizard fires an electricity ball in a random
/// direction and plants a short-lived lightning rod somewhere within range, three times a
/// second. Ported from ElectricityWizard.Overcharge.
/// </summary>
public partial class Overcharge : Node2D
{
	Wizard caster;
	AbilityData data, boltData, rodData;
	float elapsed, nextPulse;
	System.Action onFinished;
	Sprite2D icon;
	bool finished;

	public static Overcharge Create(Wizard caster, AbilityData data, System.Action onFinished)
	{
		Overcharge overcharge = new()
		{
			caster = caster,
			data = data,
			boltData = caster.Data.Basic,
			rodData = caster.Data.Second,
			onFinished = onFinished,
			ZIndex = 10,
		};
		overcharge.icon = new Sprite2D { Texture = Content.Texture("res://assets/art/ultimates/overcharge.png"), Position = new Vector2(0, -Units.U(1.4f)) };
		overcharge.AddChild(overcharge.icon);
		return overcharge;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (finished) return;
		if (!GodotObject.IsInstanceValid(caster) || !caster.Alive) { End(); return; }
		elapsed += (float)delta;
		GlobalPosition = caster.GlobalPosition;
		float wave = (Mathf.Sin(elapsed * 12.0f) + 1.0f) * 0.5f;
		icon.Scale = Vector2.One * Units.SpriteScale(Mathf.Lerp(0.5f, 0.7f, wave));

		if (elapsed >= nextPulse)
		{
			nextPulse = elapsed + data.Interval;
			Vector2 scatter = new Vector2((float)GD.RandRange(-1.0, 1.0), (float)GD.RandRange(-1.0, 1.0)).LimitLength(1.0f) * Units.U(data.Range);
			Battle.Current.AddEffect(LightningRod.Create(caster, caster.GlobalPosition + scatter, rodData, data.Duration2));
			Projectile.Fire(caster, boltData, ProjectileKind.ElectricityBall, Vector2.FromAngle((float)GD.RandRange(0.0, Mathf.Tau)));
		}

		if (elapsed >= data.Duration) End();
	}

	void End()
	{
		finished = true;
		onFinished?.Invoke();
		QueueFree();
	}
}
