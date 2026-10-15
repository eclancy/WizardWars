using Godot;

// The Frost, Earth and Electricity abilities that are not plain projectiles. Each one is a
// thin rule over an effect node: what it spawns, where, and with which numbers from its
// AbilityData. The effect nodes hold the behaviour.

/// <summary>Ice Patch: freezes the ground at the target after a short formation telegraph.</summary>
public sealed class IcePatchAbility : Ability
{
	public IcePatchAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(IcePatch.Create(Owner, Owner.TargetPoint, Data.Radius, Data.Duration));
}

/// <summary>Ice Wave: a fan of shards sweeps out along the aim, freezing whoever it touches.</summary>
public sealed class IceWaveAbility : Ability
{
	public IceWaveAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast()
	{
		// Toward the targeter when aiming, else the way the wizard last aimed.
		Vector2 toTarget = Owner.TargetPoint - Owner.GlobalPosition;
		Vector2 direction = toTarget.LengthSquared() > 1.0f ? toTarget.Normalized() : Owner.AimDirection;
		Battle.AddEffect(IceWave.Create(Owner, Data, direction));
	}
}

/// <summary>Absolute Zero: enemies near the caster are slowed and their energy stops recharging.</summary>
public sealed class AbsoluteZeroAbility : Ultimate
{
	public AbsoluteZeroAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(Aura.Create(
		Owner, Data.Radius, Data.Duration, new Modifier { Slow = Data.SlowPercent, NoRegen = true },
		"res://assets/art/ultimates/absolute-zero.png", new FrostFx(), Finish));
}

/// <summary>Mud Patch: turns the ground at the target to mud after clods spurt up and spread.</summary>
public sealed class MudPatchAbility : Ability
{
	public MudPatchAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(MudPatch.Create(Owner, Owner.TargetPoint, Data.Radius, Data.Duration, Data.SlowPercent));
}

/// <summary>
/// Rock Wall: raises a wall at the target, turned across the line from the caster - a target
/// mostly above or below gets a horizontal wall, one mostly to the side a vertical one.
/// </summary>
public sealed class RockWallAbility : Ability
{
	public RockWallAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast()
	{
		Vector2 target = Owner.TargetPoint;
		Vector2 offset = target - Owner.GlobalPosition;
		bool vertical = Mathf.Abs(offset.X) > Mathf.Abs(offset.Y);
		Battle.AddEffect(RockWall.Create(target, vertical, Data.Duration));
	}
}

/// <summary>Earthquake: the ground cracks and shakes around the caster, slowing enemies who stay close.</summary>
public sealed class EarthquakeAbility : Ultimate
{
	double reusableAt;

	public EarthquakeAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	// Unity held the ultimate back for a couple of seconds after a quake ended.
	public override bool IsReady => base.IsReady && Now >= reusableAt;

	public override bool TryCast() => Now >= reusableAt && base.TryCast();

	protected override void Cast() => Battle.AddEffect(Aura.Create(
		Owner, Data.Radius, Data.Duration, new Modifier { Slow = Data.SlowPercent },
		"res://assets/art/ultimates/earthquake.png", new QuakeFx(Data.Interval),
		() => { reusableAt = Now + Data.Duration2; Finish(); }));
}

/// <summary>Lightning Rod: plants a rod at the target that shocks everyone nearby.</summary>
public sealed class LightningRodAbility : Ability
{
	public LightningRodAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(LightningRod.Create(Owner, Owner.TargetPoint, Data, Data.Duration));
}

/// <summary>Speed Up: run faster for a few seconds.</summary>
public sealed class SpeedUpAbility : Ability
{
	public SpeedUpAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(SpeedTrail.Create(Owner, Data.Duration, Data.SlowPercent));
}

/// <summary>Overcharge: sprays electricity balls and scatters short-lived rods around the caster.</summary>
public sealed class OverchargeAbility : Ultimate
{
	public OverchargeAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(Overcharge.Create(Owner, Data, Finish));
}
