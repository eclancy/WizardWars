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
	/// <summary>One line for Stage Select on what makes the stage different.</summary>
	[Export] public string Caption { get; set; } = "";
	[Export] public Texture2D Background { get; set; }

	/// <summary>Player 1 to 4. In Teams, Gold takes spawns 1 and 3 and Pink takes 2 and 4.</summary>
	[Export] public Vector2[] Spawns { get; set; } = System.Array.Empty<Vector2>();
	/// <summary>Which spawn the solo computer opponent uses (0-based).</summary>
	[Export] public int ComputerSpawn { get; set; } = 3;

	/// <summary>
	/// This stage's cover: solid pieces that stop wizards and shots, and that a wizard can hide
	/// behind. One kind per stage (see <see cref="Cover"/>): stump, crystal, sandstone, obelisk, boulder
	/// or mossrock.
	/// </summary>
	[Export] public string CoverKind { get; set; } = "";
	[Export] public Vector2[] Cover { get; set; } = System.Array.Empty<Vector2>();

	/// <summary>
	/// Soft scenery that wizards and shots pass straight through (see <see cref="Decor"/>): small
	/// cacti or toadstools. It casts no shadow and sways when brushed, so it never reads as cover.
	/// </summary>
	[Export] public string DecorKind { get; set; } = "";
	[Export] public Vector2[] Decor { get; set; } = System.Array.Empty<Vector2>();

	/// <summary>
	/// The stage's hazard, if any (see <see cref="StageHazard"/>):
	/// - "lava": rectangles that burn and hurt, crossed on bridges
	/// - "bog": ellipses that slow you and stop your energy recharging
	/// </summary>
	[Export] public string HazardKind { get; set; } = "";
	/// <summary>Hazard areas as pairs: centre, then half-size (rectangle for lava, ellipse radii for bog).</summary>
	[Export] public Vector2[] Hazards { get; set; } = System.Array.Empty<Vector2>();
	/// <summary>Bridges over lava, as centre and half-size pairs. Standing on one is safe.</summary>
	[Export] public Vector2[] Bridges { get; set; } = System.Array.Empty<Vector2>();
	/// <summary>Damage per second while standing in lava (which also sets you burning).</summary>
	[Export] public float HazardDamage { get; set; }
	/// <summary>Slow, in percent, while standing in bog.</summary>
	[Export] public float HazardSlow { get; set; }

	/// <summary>
	/// Where King of the Rune's circle sits, in turn (Unity units). One spot keeps it still; with
	/// more it moves on every <see cref="RuneHill.MoveEvery"/> seconds. Empty means the centre.
	/// </summary>
	[Export] public Vector2[] RunePoints { get; set; } = System.Array.Empty<Vector2>();

	/// <summary>Whether a point (Unity units) is in the hazard and not on a bridge.</summary>
	public bool InHazard(Vector2 point)
	{
		for (int i = 0; i + 1 < Bridges.Length; i += 2)
			if (Mathf.Abs(point.X - Bridges[i].X) <= Bridges[i + 1].X + 0.15f && Mathf.Abs(point.Y - Bridges[i].Y) <= Bridges[i + 1].Y + 0.15f) return false;
		for (int i = 0; i + 1 < Hazards.Length; i += 2)
		{
			Vector2 offset = point - Hazards[i], half = Hazards[i + 1];
			bool inside = HazardKind == "bog"
				? (offset / half).LengthSquared() <= 1.0f
				: Mathf.Abs(offset.X) <= half.X && Mathf.Abs(offset.Y) <= half.Y;
			if (inside) return true;
		}
		return false;
	}
}
