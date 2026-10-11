using System.Collections.Generic;
using Godot;

/// <summary>
/// Ice Wave: an arc of shards that sweeps outward from the caster, widening and fading. A
/// wizard caught by any shard is frozen and damaged once per cast - every shard shares one hit
/// set. Ported from FrostWizard.SpawnIceWave / IceWaveShard.
/// </summary>
public partial class IceWave : Node2D
{
	const float ArcSpan = 70.0f;     // degrees
	const float NearRadius = 0.9f;   // Unity units

	Wizard caster;
	int casterTeam;
	AbilityData data;
	Vector2 origin;
	float baseAngle, elapsed;
	readonly List<Sprite2D> shards = new();
	readonly List<float> offsets = new();
	readonly HashSet<Wizard> hit = new();

	public static IceWave Create(Wizard caster, AbilityData data, Vector2 direction)
	{
		IceWave wave = new()
		{
			caster = caster,
			casterTeam = caster.Team,
			data = data,
			origin = caster.GlobalPosition,
			baseAngle = Mathf.RadToDeg(direction.Angle()),
			ZIndex = 6,
		};
		for (int i = 0; i < data.Count; i++)
		{
			wave.offsets.Add(Mathf.Lerp(-ArcSpan * 0.5f, ArcSpan * 0.5f, i / (float)(data.Count - 1)));
			Sprite2D shard = ProceduralFx.IceShard.NewSprite(0.4f);
			wave.shards.Add(shard);
			wave.AddChild(shard);
		}
		return wave;
	}

	public override void _PhysicsProcess(double delta)
	{
		elapsed += (float)delta;
		float t = Mathf.Min(1.0f, elapsed / data.Duration);
		float radius = Units.U(Mathf.Lerp(NearRadius, data.Range, t));
		float spread = Mathf.Lerp(0.35f, 1.0f, t);
		float hitRadius = Units.U(data.Radius);
		Battle battle = Battle.Current;

		for (int i = 0; i < shards.Count; i++)
		{
			float angle = Mathf.DegToRad(baseAngle + offsets[i] * spread);
			Vector2 position = origin + Vector2.FromAngle(angle) * radius;
			Sprite2D shard = shards[i];
			shard.GlobalPosition = position;
			// The shard art points up (Unity: rotation = angle - 90); Godot's up is -y.
			shard.Rotation = angle + Mathf.Pi * 0.5f;
			shard.Scale = Vector2.One * ProceduralFx.Scale(Mathf.Lerp(0.4f, 1.15f, Mathf.Sin(t * Mathf.Pi)));
			shard.Modulate = new Color(1, 1, 1, Mathf.Clamp(1.0f - t * t, 0.0f, 1.0f));

			foreach (Wizard wizard in battle.Wizards)
			{
				if (hit.Contains(wizard) || !Combat.CanAffect(caster, casterTeam, wizard) || !wizard.HitTest(position, hitRadius)) continue;
				hit.Add(wizard);
				wizard.Stun(data.Duration2);
				wizard.TakeDamage(data.Damage);
			}
		}

		if (t >= 1.0f) QueueFree();
	}
}
