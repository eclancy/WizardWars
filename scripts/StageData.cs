using Godot;

/// <summary>
/// An arena: its floor art, where the four players start, and its cover. Positions are in Unity
/// units with Unity's +y up; the spawns are copied straight from ParkBattle.ConfigureStageVariant.
/// </summary>
[GlobalClass]
public partial class StageData : Resource
{
	[Export] public string Id { get; set; } = "";
	[Export] public string DisplayName { get; set; } = "";
	[Export] public Texture2D Background { get; set; }

	/// <summary>Player 1 to 4. In Teams, Green takes spawns 1 and 3 and Pink takes 2 and 4.</summary>
	[Export] public Vector2[] Spawns { get; set; } = System.Array.Empty<Vector2>();
	/// <summary>Which spawn the solo computer opponent uses (0-based).</summary>
	[Export] public int ComputerSpawn { get; set; } = 3;

	/// <summary>
	/// This stage's cover: solid pieces that stop wizards and shots, and that a wizard can hide
	/// behind. One kind per stage (see <see cref="Cover"/>): stump, crystal, saguaro or obelisk.
	/// </summary>
	[Export] public string CoverKind { get; set; } = "";
	[Export] public Vector2[] Cover { get; set; } = System.Array.Empty<Vector2>();
}
