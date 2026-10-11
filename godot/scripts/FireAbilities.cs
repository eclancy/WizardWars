using Godot;

/// <summary>Ring of Fire: flames placed straight onto a circle at the target, no telegraph.</summary>
public sealed class RingOfFireAbility : Ability
{
	public RingOfFireAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast()
	{
		Vector2 centre = Owner.TargetPoint;
		for (int i = 0; i < Data.Count; i++)
		{
			float angle = i / (float)Data.Count * Mathf.Tau;
			Vector2 position = centre + Vector2.FromAngle(angle) * Units.U(Data.Radius);
			Battle.AddEffect(FireSource.Create(Owner, position, Data.Duration2, Data.Damage2, Data.Duration));
		}
	}
}

/// <summary>
/// Flamethrower: hold to breathe fire. The energy bar is the channel timer - it drains while the
/// button is held and regen is off - and the caster is slowed a little while burning. Releasing,
/// running dry, or a rock wall in the way ends it, and it can be lit again after a short delay.
/// </summary>
public sealed class FlamethrowerAbility : Ability
{
	FlameJet jet;
	float drainTimer;

	public FlamethrowerAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	bool Burning => jet != null && GodotObject.IsInstanceValid(jet);

	public override bool IsReady => !Burning && base.IsReady;

	public override float CooldownFill => Burning ? 0.0f : Data.Duration2 <= 0.0f ? 1.0f : Mathf.Clamp(1.0f - (float)((readyAt - Now) / Data.Duration2), 0.0f, 1.0f);

	public override bool TryCast()
	{
		if (Burning || !IsReady || !Owner.SpendEnergy(Data.Cost)) return false;
		Cast();
		return true;
	}

	protected override void Cast()
	{
		jet = FlameJet.Create(Owner, Data);
		Battle.AddEffect(jet);
		Owner.Status.Set(jet, new Modifier { Slow = Data.SlowPercent, NoRegen = true });
		drainTimer = 0.2f;
		Sfx.Play(Data.Sound);
	}

	public override void Tick(float dt, InputState input)
	{
		if (!Burning) return;
		if (!input.ThirdHeld || jet.Snuffed || Owner.UltimateActive) { Stop(); return; }
		// Drain in small steps so the bar visibly empties while the button is held.
		drainTimer -= dt;
		if (drainTimer > 0.0f) return;
		drainTimer += 0.2f;
		if (!Owner.SpendEnergy(Data.Drain * 0.2f)) Stop();
	}

	void Stop()
	{
		if (Burning)
		{
			Owner.Status.Remove(jet);
			jet.QueueFree();
		}
		jet = null;
		readyAt = Now + Data.Duration2;
	}

	public override void OnOwnerDied() { if (Burning) jet.QueueFree(); }
}

/// <summary>Meteor Storm: meteors land on random points across the arena, several in the air at once.</summary>
public sealed class MeteorStormAbility : Ultimate
{
	public MeteorStormAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(new MeteorStorm(Owner, Data, Finish));
}
