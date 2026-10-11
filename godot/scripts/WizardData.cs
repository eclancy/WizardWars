using Godot;

/// <summary>A wizard: its art and its four abilities in button order.</summary>
[GlobalClass]
public partial class WizardData : Resource
{
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
	[Export] public AbilityData Ultimate { get; set; }

	public AbilityData[] Abilities => new[] { Basic, Second, Third, Ultimate };
}
