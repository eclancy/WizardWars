using Godot;

// The fourth abilities (LB / 4). Every wizard had one in Unity; they were cut on 2026-09-25
// because three of them were the weakest in the game, cost 80-85 energy, and shared the third
// ability's cooldown flag. They are restored here with their original ideas, numbers brought
// in line with the 20-35 energy abilities, and a cooldown of their own (the base Ability's).

/// <summary>Meteor Strike: one meteor on the targeter, with a warning shadow; it burns and leaves fire.</summary>
public sealed class MeteorStrikeAbility : Ability
{
	public MeteorStrikeAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(Meteor.Create(Owner, Data, Owner.TargetPoint, Data.Count));
}

/// <summary>Deep Freeze: frost gathers on the targeter, then everyone inside is frozen solid.</summary>
public sealed class DeepFreezeAbility : Ability
{
	public DeepFreezeAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(DeepFreeze.Create(Owner, Data, Owner.TargetPoint));
}

/// <summary>Tremor: the ground bucks around the caster, hurting and staggering enemies close by.</summary>
public sealed class TremorAbility : Ability
{
	public TremorAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(Tremor.Create(Owner, Data));
}

/// <summary>Chain Lightning: a bolt that leaps between up to three enemies.</summary>
public sealed class ChainLightningAbility : Ability
{
	public ChainLightningAbility(Wizard owner, AbilityData data) : base(owner, data) { }

	protected override void Cast() => Battle.AddEffect(ChainLightning.Create(Owner, Data));
}
