using Godot;

/// <summary>
/// An arena: its background, where the four players start, and its obstacles. Positions are
/// in Unity units with Unity's +y up, copied straight from ParkBattle.ConfigureStageVariant.
/// </summary>
[GlobalClass]
public partial class StageData : Resource
{
	[Export] public string Id { get; set; } = "";
	[Export] public string DisplayName { get; set; } = "";
	[Export] public Texture2D Background { get; set; }

	/// <summary>Player 1 to 4. In Teams, Red takes spawns 1 and 3 and Blue takes 2 and 4.</summary>
	[Export] public Vector2[] Spawns { get; set; } = System.Array.Empty<Vector2>();
	/// <summary>Which spawn the solo computer opponent uses (0-based).</summary>
	[Export] public int ComputerSpawn { get; set; } = 3;

	[Export] public Vector2[] Rocks { get; set; } = System.Array.Empty<Vector2>();
	[Export] public Vector2[] Cacti { get; set; } = System.Array.Empty<Vector2>();
}
