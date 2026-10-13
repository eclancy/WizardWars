using Godot;

/// <summary>
/// Match-wide tuning shared by every wizard: health, energy, speed, and how the ultimate
/// meter fills. Lives in data/balance.tres so it can be tuned in the inspector like the
/// abilities. Speeds are in Unity units per second (see <see cref="Units"/>).
/// </summary>
[GlobalClass]
public partial class BalanceData : Resource
{
	[Export] public float MaxHealth { get; set; } = 100.0f;
	[Export] public float MaxEnergy { get; set; } = 100.0f;
	/// <summary>Energy regained per second while regen is not locked.</summary>
	[Export] public float EnergyRegen { get; set; } = 20.0f;
	[Export] public float MoveSpeed { get; set; } = 4.0f;

	// The ultimate is earned: a passive trickle while fighting, plus charge for spending
	// energy (fighting) and for taking damage (a way back in for whoever is losing).
	/// <summary>Charge per second while the battle is on. 100 fills the meter.</summary>
	[Export] public float UltimateChargeRate { get; set; } = 1.2f;
	[Export] public float UltimateChargePerEnergy { get; set; } = 0.3f;
	[Export] public float UltimateChargePerDamageTaken { get; set; } = 0.3f;
}
