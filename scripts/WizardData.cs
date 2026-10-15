using Godot;

/// <summary>A wizard: its art and its five abilities in button order.</summary>
[GlobalClass]
public partial class WizardData : Resource
{
	// Ability slots, in button order. Use these rather than bare indices: the ultimate used to
	// be slot 3 before the fourth ability was restored.
	public const int BasicSlot = 0;
	public const int SecondSlot = 1;
	public const int ThirdSlot = 2;
	public const int FourthSlot = 3;
	public const int UltimateSlot = 4;
	public const int SlotCount = 5;

	/// <summary>The pad button for each slot, for menus and help text.</summary>
	public static readonly string[] PadButtons = { "RT", "LT", "RB", "LB", "X" };

	/// <summary>Matches the Unity component name: FireWizard, FrostWizard, EarthWizard, ElectricityWizard.</summary>
	[Export] public string Id { get; set; } = "";
	[Export] public string DisplayName { get; set; } = "";
	[Export] public Texture2D Portrait { get; set; }
	[Export] public Texture2D Targeter { get; set; }
	/// <summary>The colour of the wizard's magic, for menus and the HUD.</summary>
	[Export] public Color Colour { get; set; } = Colors.White;

	[Export] public AbilityData Basic { get; set; }
	[Export] public AbilityData Second { get; set; }
	[Export] public AbilityData Third { get; set; }
	/// <summary>LB: the fourth ability, cut in Unity and restored in the Godot version.</summary>
	[Export] public AbilityData Fourth { get; set; }
	[Export] public AbilityData Ultimate { get; set; }

	public AbilityData[] Abilities => new[] { Basic, Second, Third, Fourth, Ultimate };
}
