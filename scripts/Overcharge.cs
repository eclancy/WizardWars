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

	// Lightning crackles round the caster the whole time, and a charged ring flickers at their feet.
	float nextArc;

	public override void _Process(double delta)
	{
		if (finished) return;
		if (elapsed >= nextArc)
		{
			nextArc = elapsed + 0.07f;
			Vector2 reach = Vector2.FromAngle((float)GD.RandRange(0.0, Mathf.Tau)) * Units.U((float)GD.RandRange(0.9, 1.7));
			AddChild(Bolt.Create(new Vector2(0.0f, -Units.U(0.2f)), reach, 0.12f));
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		float flicker = 0.5f + 0.5f * Mathf.Sin(elapsed * 40.0f);
		DrawSetTransform(new Vector2(0.0f, Units.U(0.6f)), 0.0f, new Vector2(1.0f, 0.45f));
		DrawArc(Vector2.Zero, Units.U(1.1f), 0.0f, Mathf.Tau, 48, new Color(1.0f, 0.9f, 0.3f, 0.45f + 0.4f * flicker), 6.0f, true);
		DrawCircle(Vector2.Zero, Units.U(1.1f), new Color(1.0f, 0.95f, 0.5f, 0.12f + 0.1f * flicker));
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
