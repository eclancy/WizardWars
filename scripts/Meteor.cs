using Godot;

/// <summary>
/// One meteor of a Meteor Storm. A shadow marks where it will land: small, dark and soft at
/// first, tightening and catching fire as the rock falls in from above and grows. On impact a
/// blast disc snaps out to the damage radius, and damage falls off from the centre to the rim.
/// It never hurts the caster or his team, and leaves no fire. Ported from FireWizard.cs.
/// </summary>
public partial class Meteor : Node2D
{
	const float FallHeight = 12.0f; // Unity units

	Wizard caster;
	int casterTeam;
	AbilityData data;
	Sprite2D shadow, rock, blast;
	float elapsed;
	bool landed;

	public static Meteor Create(Wizard caster, AbilityData data, Vector2 target)
	{
		Meteor meteor = new() { caster = caster, casterTeam = caster.Team, data = data, Position = target };
		meteor.shadow = ProceduralFx.Shadow.NewSprite(0.5f);
		meteor.shadow.ZIndex = 4;
		meteor.AddChild(meteor.shadow);
		meteor.rock = ProceduralFx.Meteor.NewSprite(0.35f);
		meteor.rock.ZIndex = 11;
		meteor.AddChild(meteor.rock);
		return meteor;
	}

	public override void _PhysicsProcess(double delta)
	{
		elapsed += (float)delta;
		if (!landed) Approach(); else Explode();
	}

	void Approach()
	{
		float t = Mathf.Min(1.0f, elapsed / data.Duration);
		// Accelerate downward instead of drifting in at a constant rate.
		float fall = t * t;
		shadow.Scale = Vector2.One * ProceduralFx.Scale(Mathf.Lerp(0.5f, 1.9f, fall));
		shadow.Modulate = new Color(0.1f, 0.09f, 0.09f, 0.55f).Lerp(new Color(1.0f, 0.45f, 0.12f, 0.95f), fall * fall) * new Color(20, 20, 20, 1);
		rock.Position = new Vector2(0.0f, -Units.U(FallHeight)).Lerp(Vector2.Zero, fall);
		rock.Scale = Vector2.One * ProceduralFx.Scale(Mathf.Lerp(0.35f, 1.5f, fall));
		rock.Rotation += Mathf.Pi * 0.5f * (float)GetPhysicsProcessDeltaTime();
		if (t < 1.0f) return;

		landed = true;
		elapsed = 0.0f;
		shadow.QueueFree();
		rock.QueueFree();
		blast = ProceduralFx.Burst.NewSprite(0.25f * data.Radius);
		blast.ZIndex = 9;
		AddChild(blast);
		Sfx.Play("res://assets/sfx/fireball.wav", -2.0f);
		DealDamage();
	}

	void DealDamage()
	{
		float radius = Units.U(data.Radius);
		foreach (Wizard wizard in Battle.Current.Wizards)
		{
			if (!Combat.CanAffect(caster, casterTeam, wizard)) continue;
			float distance = wizard.GlobalPosition.DistanceTo(GlobalPosition);
			if (distance > radius) continue;
			wizard.TakeDamage(Mathf.Round(Mathf.Lerp(data.Damage, data.Damage2, distance / radius)));
		}
	}

	// The blast disc snaps out to the damage radius and fades, so the area that hurt is shown.
	void Explode()
	{
		const float duration = 0.45f;
		float t = Mathf.Min(1.0f, elapsed / duration);
		// The Burst sprite is 2 Unity units wide at scale 1, so scale = radius.
		blast.Scale = Vector2.One * ProceduralFx.Scale(data.Radius * Mathf.Lerp(0.25f, 1.1f, Mathf.Sqrt(t)));
		blast.Modulate = new Color(1, 1, 1, 1.0f - t * t);
		if (t >= 1.0f) QueueFree();
	}
}
