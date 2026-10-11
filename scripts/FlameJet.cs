using Godot;

/// <summary>
/// The flamethrower's stream. It hangs in front of the caster along his aim and burns anyone
/// in the cone: direct damage once per interval plus a short burn. A rock wall it touches
/// snuffs it out (Unity's RockWall destroyed the flameThrower object).
///
/// Unity drew this with a particle system; here it is a fan of the generated flame frames,
/// laid along the cone, so it matches the Ring of Fire art.
/// </summary>
public partial class FlameJet : Node2D
{
	const int Puffs = 7;

	Wizard caster;
	int casterTeam;
	AbilityData data;
	readonly System.Collections.Generic.Dictionary<Wizard, double> nextHit = new();
	Sprite2D[] puffs;
	float animTime;
	public bool Snuffed { get; private set; }

	public static FlameJet Create(Wizard caster, AbilityData data)
	{
		FlameJet jet = new() { caster = caster, casterTeam = caster.Team, data = data, ZIndex = 6 };
		jet.puffs = new Sprite2D[Puffs];
		for (int i = 0; i < Puffs; i++)
		{
			Sprite2D puff = ProceduralFx.Flame.NewSprite(0.6f);
			// The flame art stands upward; turn it to point along the jet.
			puff.Rotation = Mathf.Pi * 0.5f;
			jet.puffs[i] = puff;
			jet.AddChild(puff);
		}
		return jet;
	}

	/// <summary>Length of the burning cone, in pixels: from just in front of the caster to past the Unity 2.5-unit offset.</summary>
	float Reach => Units.U(data.Range + 1.3f);

	public override void _PhysicsProcess(double delta)
	{
		if (!GodotObject.IsInstanceValid(caster) || !caster.Alive) { QueueFree(); return; }
		float dt = (float)delta;
		animTime += dt;
		GlobalPosition = caster.GlobalPosition;
		Rotation = caster.AimDirection.Angle();

		for (int i = 0; i < Puffs; i++)
		{
			float along = (i + 1.0f) / Puffs;
			float flicker = Mathf.Sin(animTime * 18.0f + i * 1.7f);
			Sprite2D puff = puffs[i];
			puff.Position = new Vector2(Units.U(0.4f) + along * Reach, flicker * Units.U(0.06f));
			puff.Scale = Vector2.One * ProceduralFx.Scale(Mathf.Lerp(0.35f, 0.85f, along) * (1.0f + 0.08f * flicker));
			puff.Texture = ProceduralFx.FlameFrames[(int)(animTime * 14.0f + i) % ProceduralFx.FlameFrames.Length].Texture;
		}

		Battle battle = Battle.Current;
		Vector2 forward = caster.AimDirection;
		Vector2 start = caster.GlobalPosition + forward * Units.U(0.4f);
		Vector2 end = caster.GlobalPosition + forward * (Units.U(0.4f) + Reach);

		foreach (RockWall wall in battle.RockWalls)
			if (wall.Blocks(start.Lerp(end, 0.5f), Units.U(0.5f)) || wall.Blocks(end, Units.U(0.4f))) { Snuffed = true; return; }

		foreach (Wizard wizard in battle.Wizards)
		{
			if (!Combat.CanAffect(caster, casterTeam, wizard)) continue;
			// Width of the cone grows from 0.3 to 0.8 units along its length.
			Vector2 closest = Geometry2D.GetClosestPointToSegment(wizard.GlobalPosition, start, end);
			float along = start.DistanceTo(closest) / Reach;
			if (!wizard.HitTest(closest, Units.U(Mathf.Lerp(0.3f, 0.8f, along)))) continue;
			if (nextHit.TryGetValue(wizard, out double due) && battle.Time < due) continue;
			nextHit[wizard] = battle.Time + data.Interval;
			wizard.TakeDamage(data.Damage);
			wizard.SetFire(data.Duration, data.Damage2);
		}
	}
}
