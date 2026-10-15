using System.Collections.Generic;
using Godot;

/// <summary>
/// Tremor, Earth's fourth ability: the Unity fourth-slot "Earthquake" restored under a new name,
/// because Earthquake is now the Earth ultimate. The Earth wizard stamps and the ground splits
/// around him: cracks race out to the radius, clods of earth are thrown up, the view jumps, and
/// every enemy inside takes damage and is staggered (a short stun, Unity's knock-up
/// approximation; they shake rather than freeze).
///
/// Unity's version reached the whole arena, did no damage and staggered for 0.15 s, for 85
/// energy; here it is a close-range burst that actually hurts.
/// </summary>
public partial class Tremor : Node2D
{
	const int Clods = 14;
	const float ClodDuration = 0.55f;
	float radius, elapsed;
	readonly List<Sprite2D> clods = new();
	readonly List<Vector2> landings = new();
	readonly List<float> heights = new();

	public static Tremor Create(Wizard caster, AbilityData data)
	{
		Tremor tremor = new() { Position = caster.GlobalPosition, radius = Units.U(data.Radius), ZIndex = 7 };
		for (int i = 0; i < Clods; i++)
		{
			float angle = i / (float)Clods * Mathf.Tau + (float)GD.RandRange(-0.2, 0.2);
			tremor.landings.Add(Vector2.FromAngle(angle) * tremor.radius * (float)GD.RandRange(0.45, 0.95));
			tremor.heights.Add(Units.U((float)GD.RandRange(0.6, 1.4)));
			Sprite2D clod = ProceduralFx.MudGlob.NewSprite((float)GD.RandRange(0.3, 0.55));
			tremor.clods.Add(clod);
			tremor.AddChild(clod);
		}

		caster.Battle.AddEffect(GroundCracks.Create(caster.GlobalPosition + new Vector2(0.0f, Units.U(0.55f)), tremor.radius, 8, 8.0f, 0.18f, 0.5f, 0.45f));
		caster.Battle.Shake(8.0f, 0.3f);

		// The stagger lands on the stamp itself; the visuals follow it out.
		int team = caster.Team;
		foreach (Wizard wizard in caster.Battle.Wizards)
		{
			if (!Combat.CanAffect(caster, team, wizard)) continue;
			if (wizard.GlobalPosition.DistanceSquaredTo(caster.GlobalPosition) > tremor.radius * tremor.radius) continue;
			wizard.Stun(data.Duration2, StunKind.Staggered);
			wizard.TakeDamage(data.Damage);
		}
		return tremor;
	}

	public override void _PhysicsProcess(double delta)
	{
		elapsed += (float)delta;

		float t = Mathf.Min(1.0f, elapsed / ClodDuration);
		for (int i = 0; i < clods.Count; i++)
		{
			// A parabola that starts and ends on the ground.
			float arc = heights[i] * 4.0f * t * (1.0f - t);
			clods[i].Position = Vector2.Zero.Lerp(landings[i], t) + new Vector2(0.0f, -arc);
			clods[i].Rotation += Mathf.DegToRad(300.0f) * (float)delta;
			clods[i].Modulate = new Color(1, 1, 1, t < 0.8f ? 1.0f : (1.0f - t) * 5.0f);
		}
		if (t >= 1.0f) QueueFree();
	}
}
