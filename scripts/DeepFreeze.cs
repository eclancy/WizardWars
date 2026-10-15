using System.Collections.Generic;
using Godot;

/// <summary>
/// Deep Freeze, Frost's fourth ability (restored from Unity). Frost gathers on the target point
/// - a pale disc grows to the full radius while shards spiral in - and then the ground bursts
/// into ice: everyone inside is frozen solid and takes a little damage.
///
/// Unity's version froze for four seconds instantly at any range for 85 energy; here the
/// telegraph gives a moment to step out, and the freeze is two seconds.
/// </summary>
public partial class DeepFreeze : Node2D
{
	const int GatherShards = 6;
	const int BurstShards = 10;
	const float BurstDuration = 0.45f;

	Wizard caster;
	int casterTeam;
	AbilityData data;
	float radius, elapsed;
	bool burst;
	Sprite2D disc;
	readonly List<Sprite2D> gather = new(), shards = new();
	readonly List<float> angles = new();

	public static DeepFreeze Create(Wizard caster, AbilityData data, Vector2 target)
	{
		DeepFreeze freeze = new() { caster = caster, casterTeam = caster.Team, data = data, Position = target, radius = Units.U(data.Radius), ZIndex = 1 };
		freeze.disc = ProceduralFx.AreaDisc.NewSprite();
		freeze.disc.Modulate = new Color(0.6f, 0.85f, 1.0f, 0.0f);
		freeze.AddChild(freeze.disc);
		for (int i = 0; i < GatherShards; i++)
		{
			Sprite2D shard = ProceduralFx.IceShard.NewSprite(0.35f);
			shard.ZIndex = 6;
			freeze.gather.Add(shard);
			freeze.AddChild(shard);
		}
		return freeze;
	}

	float DiscScale(float fraction) => radius * 2.0f * fraction / ProceduralFx.AreaDisc.Texture.GetSize().X;

	public override void _PhysicsProcess(double delta)
	{
		elapsed += (float)delta;
		if (!burst) Gather(); else Burst();
	}

	// The telegraph: the frost disc spreads to the full radius and shards spiral into the middle.
	void Gather()
	{
		float t = Mathf.Min(1.0f, elapsed / data.Duration);
		disc.Scale = Vector2.One * DiscScale(Mathf.Lerp(0.3f, 1.0f, Mathf.SmoothStep(0.0f, 1.0f, t)));
		disc.Modulate = new Color(0.6f, 0.85f, 1.0f, Mathf.Lerp(0.1f, 0.45f, t));
		for (int i = 0; i < gather.Count; i++)
		{
			float angle = i / (float)gather.Count * Mathf.Tau + t * Mathf.Pi * 1.5f;
			float distance = radius * (1.0f - t);
			gather[i].Position = Vector2.FromAngle(angle) * distance;
			gather[i].Rotation = angle - Mathf.Pi * 0.5f;
			gather[i].Modulate = new Color(1, 1, 1, Mathf.Min(1.0f, t * 3.0f));
		}
		if (t < 1.0f) return;

		burst = true;
		elapsed = 0.0f;
		foreach (Sprite2D shard in gather) shard.QueueFree();
		for (int i = 0; i < BurstShards; i++)
		{
			Sprite2D shard = ProceduralFx.IceShard.NewSprite(0.5f);
			shard.ZIndex = 6;
			angles.Add(i / (float)BurstShards * Mathf.Tau + (float)GD.RandRange(-0.15, 0.15));
			shards.Add(shard);
			AddChild(shard);
		}
		Sfx.Play("res://assets/sfx/icewave.wav", -2.0f);
		Freeze();
	}

	void Freeze()
	{
		foreach (Wizard wizard in Battle.Current.Wizards)
		{
			if (!Combat.CanAffect(caster, casterTeam, wizard)) continue;
			if (wizard.GlobalPosition.DistanceSquaredTo(GlobalPosition) > radius * radius) continue;
			wizard.Stun(data.Duration2);
			wizard.TakeDamage(data.Damage);
		}
	}

	// The burst: shards spring up out of the ground across the radius and melt away.
	void Burst()
	{
		float t = Mathf.Min(1.0f, elapsed / BurstDuration);
		float rise = Mathf.Sin(Mathf.Min(1.0f, t * 1.6f) * Mathf.Pi * 0.5f);
		for (int i = 0; i < shards.Count; i++)
		{
			Vector2 direction = Vector2.FromAngle(angles[i]);
			shards[i].Position = direction * radius * Mathf.Lerp(0.25f, 0.85f, rise);
			// The shard art points up; lean each one outward from the centre.
			shards[i].Rotation = angles[i] + Mathf.Pi * 0.5f;
			shards[i].Scale = Vector2.One * ProceduralFx.Scale(Mathf.Lerp(0.2f, 0.9f, rise));
			shards[i].Modulate = new Color(1, 1, 1, 1.0f - t * t);
		}
		disc.Modulate = new Color(0.75f, 0.92f, 1.0f, 0.45f * (1.0f - t));
		if (t >= 1.0f) QueueFree();
	}
}
