using Godot;

/// <summary>
/// One flame of a Ring of Fire. Flames burn and die independently: one that lands in a wall,
/// an obstacle, a rock wall or a mud patch goes out by itself and the rest of the ring keeps
/// burning. It grows up out of the ground and stutters as it catches, then cycles the six
/// generated flame frames at its own rate. Ported from Unity's FireSource.cs.
/// </summary>
public partial class FireSource : Node2D
{
	const float Radius = 0.55f;          // Unity units
	const float TickInterval = 0.35f;

	Wizard caster;
	int casterTeam;
	float burnDuration, damagePerTick, lifetime;
	float elapsed, tickTimer, frameTimer, frameRate, stutterSeed, noiseSeed;
	int frame;
	bool extinguished, erupted;
	float fadeElapsed;
	Sprite2D sprite;
	Vector2 fullScale;

	public static FireSource Create(Wizard caster, Vector2 position, float burnDuration, float damagePerTick, float lifetime)
	{
		FireSource source = new()
		{
			caster = caster,
			casterTeam = caster.Team,
			burnDuration = burnDuration,
			damagePerTick = damagePerTick,
			lifetime = lifetime,
			Position = position,
			ZIndex = 6,
		};
		source.sprite = ProceduralFx.Flame.NewSprite(0.75f);
		source.AddChild(source.sprite);
		return source;
	}

	float PixelRadius => Units.U(Radius);

	public override void _Ready()
	{
		fullScale = sprite.Scale;
		// A source that lands inside solid geometry never lights at all.
		if (Battle.Current.IsInsideSolid(GlobalPosition, PixelRadius * 0.6f)) { QueueFree(); return; }
		frame = GD.RandRange(0, ProceduralFx.FlameFrames.Length - 1);
		frameRate = (float)GD.RandRange(11.0, 15.0);
		stutterSeed = GD.Randf() * 10.0f;
		noiseSeed = GD.Randf() * 10.0f;
		sprite.Scale = new Vector2(fullScale.X * 0.5f, 0.0f);
		Battle.Current.FireSources.Add(this);
	}

	public override void _ExitTree() => Battle.Current?.FireSources.Remove(this);

	/// <summary>Puts this flame out (mud, a wall, the end of its life).</summary>
	public void Extinguish() => extinguished = true;

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if (extinguished) { FadeOut(dt); return; }
		elapsed += dt;
		Animate(dt);
		if (elapsed >= lifetime) { Extinguish(); return; }

		tickTimer -= dt;
		if (tickTimer > 0.0f) return;
		tickTimer = TickInterval;

		Battle battle = Battle.Current;
		// A wall raised after the ring was placed puts this source out too.
		if (battle.IsInsideSolid(GlobalPosition, PixelRadius * 0.6f)) { Extinguish(); return; }

		foreach (GroundPatch patch in battle.Patches)
		{
			if (!patch.Active || !patch.Contains(GlobalPosition)) continue;
			if (patch is IcePatch) patch.Remove();          // fire melts ice
			else if (patch is MudPatch) { Extinguish(); return; } // mud puts the flame out
		}

		foreach (Wizard wizard in battle.Wizards)
			if (Combat.CanAffect(caster, casterTeam, wizard) && wizard.HitTest(GlobalPosition, PixelRadius))
				wizard.SetFire(burnDuration, damagePerTick);
	}

	// Grows up out of the ground and stutters as it catches, then cycles frames with a small
	// wobble around the settled scale.
	void Animate(float dt)
	{
		if (!erupted)
		{
			float t = Mathf.Min(1.0f, elapsed / 0.4f);
			float take = Mathf.SmoothStep(0.0f, 1.0f, t);
			float stutter = 1.0f + 0.3f * Mathf.Sin(t * 22.0f + stutterSeed) * (1.0f - t) * (1.0f - t);
			sprite.Scale = new Vector2(fullScale.X * Mathf.Lerp(0.5f, 1.0f, take), fullScale.Y * take * stutter);
			if (t >= 1.0f) { sprite.Scale = fullScale; erupted = true; }
		}

		frameTimer -= dt;
		if (frameTimer <= 0.0f)
		{
			frameTimer = 1.0f / frameRate;
			FxTexture[] frames = ProceduralFx.FlameFrames;
			sprite.Texture = frames[frame++ % frames.Length].Texture;
			if (erupted)
			{
				float wave = (Mathf.Sin(noiseSeed + elapsed * 2.5f * 6.28f) + 1.0f) * 0.5f;
				sprite.Scale = new Vector2(fullScale.X * Mathf.Lerp(0.94f, 1.04f, wave), fullScale.Y * Mathf.Lerp(0.92f, 1.12f, wave));
			}
		}
	}

	void FadeOut(float dt)
	{
		fadeElapsed += dt;
		float t = Mathf.Min(1.0f, fadeElapsed / 0.35f);
		sprite.Modulate = new Color(1, 1, 1, 1.0f - t);
		sprite.Scale = fullScale * Mathf.Lerp(1.0f, 0.3f, t);
		if (t >= 1.0f) QueueFree();
	}
}
