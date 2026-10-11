using Godot;

/// <summary>
/// One of a wizard's four abilities. Plain C#, not a node: what it creates in the arena
/// (projectiles, patches, walls) is a node of its own, owned by the battle rather than the
/// wizard, so an effect outlives its caster the way it should and cleans up after itself.
///
/// The base handles the common rule: off cooldown, pay the energy, play the sound, cast.
/// </summary>
public abstract class Ability
{
	protected readonly Wizard Owner;
	public readonly AbilityData Data;
	protected double readyAt;

	protected Ability(Wizard owner, AbilityData data)
	{
		Owner = owner;
		Data = data;
	}

	protected Battle Battle => Owner.Battle;
	protected double Now => Owner.Battle.Time;
	protected int Team => Owner.Team;

	/// <summary>Castable now, ignoring energy. Drives the HUD pips.</summary>
	public virtual bool IsReady => Now >= readyAt;

	/// <summary>0..1 through the cooldown, for the HUD pip.</summary>
	public virtual float CooldownFill => Data.Cooldown <= 0.0f ? 1.0f : Mathf.Clamp(1.0f - (float)((readyAt - Now) / Data.Cooldown), 0.0f, 1.0f);

	public virtual bool TryCast()
	{
		if (!IsReady) return false;
		if (!Owner.SpendEnergy(Data.Cost)) return false;
		readyAt = Now + Data.Cooldown;
		Sfx.Play(Data.Sound);
		Cast();
		return true;
	}

	protected abstract void Cast();

	/// <summary>Called every physics frame, for channelled abilities.</summary>
	public virtual void Tick(float dt, InputState input) { }

	public virtual void OnOwnerDied() { }
}

/// <summary>
/// An ultimate: gated by the gold meter instead of energy, and while it runs the wizard's
/// other abilities are locked out (Unity's ultimateActive).
/// </summary>
public abstract class Ultimate : Ability
{
	protected Ultimate(Wizard owner, AbilityData data) : base(owner, data) { }

	public override bool IsReady => Owner.UltimateReady && !Owner.UltimateActive;

	public override bool TryCast()
	{
		if (Owner.UltimateActive || !Owner.TryUseUltimate()) return false;
		Owner.UltimateActive = true;
		Sfx.Play(Data.Sound);
		Cast();
		return true;
	}

	protected void Finish()
	{
		if (GodotObject.IsInstanceValid(Owner)) Owner.UltimateActive = false;
	}
}

public static class AbilityFactory
{
	public static Ability[] Create(Wizard owner, WizardData data) => new[]
	{
		Make(owner, data.Basic), Make(owner, data.Second), Make(owner, data.Third), Make(owner, data.Ultimate),
	};

	static Ability Make(Wizard owner, AbilityData data) => data.Id switch
	{
		"fireball" => new ProjectileAbility(owner, data, ProjectileKind.Fireball),
		"icicle" => new ProjectileAbility(owner, data, ProjectileKind.Icicle),
		"mudball" => new ProjectileAbility(owner, data, ProjectileKind.Mudball),
		"electricity-ball" => new ProjectileAbility(owner, data, ProjectileKind.ElectricityBall),
		"ring-of-fire" => new RingOfFireAbility(owner, data),
		"flamethrower" => new FlamethrowerAbility(owner, data),
		"meteor-storm" => new MeteorStormAbility(owner, data),
		"ice-patch" => new IcePatchAbility(owner, data),
		"ice-wave" => new IceWaveAbility(owner, data),
		"absolute-zero" => new AbsoluteZeroAbility(owner, data),
		"mud-patch" => new MudPatchAbility(owner, data),
		"rock-wall" => new RockWallAbility(owner, data),
		"earthquake" => new EarthquakeAbility(owner, data),
		"lightning-rod" => new LightningRodAbility(owner, data),
		"speed-up" => new SpeedUpAbility(owner, data),
		"overcharge" => new OverchargeAbility(owner, data),
		_ => throw new System.ArgumentException($"No ability behaviour for '{data.Id}'"),
	};

	/// <summary>Every id <see cref="Make"/> knows, for RegressionChecks.</summary>
	public static readonly string[] KnownIds =
	{
		"fireball", "icicle", "mudball", "electricity-ball", "ring-of-fire", "flamethrower", "meteor-storm",
		"ice-patch", "ice-wave", "absolute-zero", "mud-patch", "rock-wall", "earthquake",
		"lightning-rod", "speed-up", "overcharge",
	};
}
