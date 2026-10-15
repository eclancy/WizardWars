using Godot;

/// <summary>
/// The tuning for one ability. This is the only place an ability's numbers live: scenes do
/// not duplicate them, so the Unity "prefab silently overrides the code default" trap cannot
/// happen here. Distances are in Unity units (converted with <see cref="Units.U"/>), times in
/// seconds. Each ability reads only the fields it needs; unused ones stay zero.
/// </summary>
[GlobalClass]
public partial class AbilityData : Resource
{
	[Export] public string Id { get; set; } = "";
	[Export] public string DisplayName { get; set; } = "";
	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";

	/// <summary>Energy spent to cast. For a channelled ability, the cost to start.</summary>
	[Export] public float Cost { get; set; }
	/// <summary>Seconds before the ability can be cast again, counted from the cast.</summary>
	[Export] public float Cooldown { get; set; }

	[Export] public float Damage { get; set; }
	/// <summary>A second damage figure: falloff edge damage, or damage per burn tick.</summary>
	[Export] public float Damage2 { get; set; }
	[Export] public float Radius { get; set; }
	[Export] public float Range { get; set; }
	[Export] public float Speed { get; set; }
	[Export] public float Duration { get; set; }
	/// <summary>A second duration: burn length, stun length, recast delay.</summary>
	[Export] public float Duration2 { get; set; }
	[Export] public int Count { get; set; }
	[Export] public float Interval { get; set; }
	/// <summary>Movement change in percent: a slow on enemies, or a speed-up on the caster.</summary>
	[Export] public float SlowPercent { get; set; }
	/// <summary>Burn damage per half-second tick that a hit sets (Meteor Strike). 0 = no burn.</summary>
	[Export] public float BurnPerTick { get; set; }
	/// <summary>Energy drained per second while channelled.</summary>
	[Export] public float Drain { get; set; }

	[Export] public AudioStream Sound { get; set; }
}
