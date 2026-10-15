using Godot;

/// <summary>
/// One meteor, for Meteor Storm (the ultimate) and Meteor Strike (the fourth ability). A shadow
/// marks where it will land: small, dark and soft at first, tightening and catching fire as the
/// rock falls straight down from above and grows. On impact a blast disc snaps out to the damage
/// radius, and damage falls off from the centre (Damage) to the rim (Damage2). It never hurts
/// the caster or his team. A storm meteor leaves no fire; a strike also sets its targets
/// burning (BurnPerTick for Duration2) and leaves a ring of flames. Ported from FireWizard.cs.
/// </summary>
public partial class Meteor : Node2D
{
	const float FallHeight = 12.0f; // Unity units
	const float FlameLifetime = 3.0f;

	Wizard caster;
	int casterTeam;
	AbilityData data;
	Sprite2D shadow, rock, blast;
	float elapsed;
	bool landed;
	int flames;

	/// <param name="flames">Flames left burning in a ring at the impact (Meteor Strike), or 0.</param>
	public static Meteor Create(Wizard caster, AbilityData data, Vector2 target, int flames = 0)
	{
		Meteor meteor = new() { caster = caster, casterTeam = caster.Team, data = data, Position = target, flames = flames };
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
		// No spin: the art is a head with its tail streaming up behind, so it reads as falling
		// straight down only while it stays upright. (Unity spun it, which tilted the tail.)
		rock.Scale = Vector2.One * ProceduralFx.Scale(Mathf.Lerp(0.35f, 1.5f, fall));
		if (t < 1.0f) return;

		landed = true;
		elapsed = 0.0f;
		shadow.QueueFree();
		rock.QueueFree();
		blast = ProceduralFx.Burst.NewSprite(0.25f * data.Radius);
		blast.ZIndex = 9;
		AddChild(blast);
		Sfx.Play("res://assets/sfx/fireball.wav", -2.0f);
		Battle.Current?.Shake(6.0f, 0.25f);
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
			if (data.BurnPerTick > 0.0f) wizard.SetFire(data.Duration2, data.BurnPerTick);
		}

		// Meteor Strike leaves the impact burning: a ring of flames halfway out to the rim.
		for (int i = 0; i < flames; i++)
		{
			Vector2 offset = Vector2.FromAngle(i / (float)flames * Mathf.Tau) * radius * 0.5f;
			Battle.Current.AddEffect(FireSource.Create(caster, GlobalPosition + offset, data.Duration2, data.BurnPerTick, FlameLifetime));
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
