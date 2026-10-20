using Godot;

// The Shadow, Nature and Wind wizards' abilities (the basics are ProjectileAbility). As with the
// others, each is a thin rule over an effect node that holds the behaviour.

/// <summary>Which way a movement ability goes: the aim, else the way the wizard is walking, else the last aim.</summary>
static class Heading
{
	public static Vector2 Of(Wizard wizard)
	{
		if (wizard.Aiming) return wizard.AimDirection;
		Vector2 move = wizard.CurrentInput.Move;
		return move.LengthSquared() > 0.04f ? move.Normalized() : wizard.AimDirection;
	}
}

// ---- Shadow ----

/// <summary>Smoke Cloud: a cloud at the target that hides everyone inside and slows enemies.</summary>
public sealed class SmokeCloudAbility : Ability
{
	public SmokeCloudAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(Zone.Create(Owner, Zone.Kind.Smoke, Owner.TargetPoint, Data));
}

/// <summary>
/// Shadow Step: vanish and reappear up to <c>Range</c> units away along the heading. It lands on
/// the furthest clear spot: inside the walls, not in cover, not in lava.
/// </summary>
public sealed class ShadowStepAbility : Ability
{
	public ShadowStepAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast()
	{
		Vector2 from = Owner.GlobalPosition, direction = Heading.Of(Owner);
		Vector2 limit = Units.U(Battle.ArenaHalf.X - 0.5f, -(Battle.ArenaHalf.Y - 0.7f));
		for (float distance = Data.Range; distance >= 0.5f; distance -= 0.25f)
		{
			Vector2 to = from + direction * Units.U(distance);
			if (Mathf.Abs(to.X) > limit.X || Mathf.Abs(to.Y) > limit.Y) continue;
			if (Battle.IsInsideSolid(to + new Vector2(0, Units.U(0.15f)), Units.U(0.45f))) continue;
			if (Battle.Hazard?.Stage.HazardKind == "lava" && Battle.Hazard.Contains(to + new Vector2(0, Units.U(0.55f)))) continue;
			Battle.AddEffect(Puff.Create(from));
			Owner.Blink(to);
			Battle.AddEffect(Puff.Create(to));
			return;
		}
	}
}

/// <summary>Night Grasp: shadowy hands claw up at the target, hurting and silencing enemies.</summary>
public sealed class NightGraspAbility : Ability
{
	public NightGraspAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(Eruption.Create(Owner, Eruption.Kind.Grasp, Owner.TargetPoint, Data));
}

/// <summary>Eclipse: night pours out round the caster, silencing and draining enemies inside.</summary>
public sealed class EclipseAbility : Ultimate
{
	public EclipseAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(Aura.Create(
		Owner, Data.Radius, Data.Duration, new Modifier { Silenced = true },
		"res://assets/art/ultimates/eclipse.png", new EclipseFx(Data.Damage), Finish));
}

// ---- Nature ----

/// <summary>Bramble Patch: thorny vines at the target that slow and scratch enemies.</summary>
public sealed class BrambleAbility : Ability
{
	public BrambleAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(Zone.Create(Owner, Zone.Kind.Bramble, Owner.TargetPoint, Data));
}

/// <summary>Regrowth: heal over a few seconds.</summary>
public sealed class RegrowthAbility : Ability
{
	public RegrowthAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(Regrowth.Create(Owner, Data));
}

/// <summary>Entangle: vines whip up at the target, hurting and rooting enemies.</summary>
public sealed class EntangleAbility : Ability
{
	public EntangleAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(Eruption.Create(Owner, Eruption.Kind.Vines, Owner.TargetPoint, Data));
}

/// <summary>Overgrowth: thorns overrun the ground round the caster, slowing and hurting enemies while the caster heals.</summary>
public sealed class OvergrowthAbility : Ultimate
{
	public OvergrowthAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(Aura.Create(
		Owner, Data.Radius, Data.Duration, new Modifier { Slow = Data.SlowPercent },
		"res://assets/art/ultimates/overgrowth.png", new OvergrowthFx(Owner, Data.Damage, Data.Damage2), Finish));
}

// ---- Wind ----

/// <summary>Updraft: rising air under the caster that speeds them and their teammates.</summary>
public sealed class UpdraftAbility : Ability
{
	public UpdraftAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(Zone.Create(Owner, Zone.Kind.Updraft, Owner.Feet, Data));
}

/// <summary>Tailwind Dash: a burst along the heading, knocking enemies aside on the way.</summary>
public sealed class TailwindDashAbility : Ability
{
	public TailwindDashAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(WindDash.Create(Owner, Data, Heading.Of(Owner)));
}

/// <summary>Cyclone: a whirlwind at the target that drags enemies in and batters them.</summary>
public sealed class CycloneAbility : Ability
{
	public CycloneAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(Cyclone.Create(Owner, Data, Owner.TargetPoint));
}

/// <summary>Tempest: a storm round the caster that hurls enemies outward and batters them.</summary>
public sealed class TempestAbility : Ultimate
{
	public TempestAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(Aura.Create(
		Owner, Data.Radius, Data.Duration, new Modifier(),
		"res://assets/art/ultimates/tempest.png", new TempestFx(Data.Damage, Data.Knockback), Finish));
}
