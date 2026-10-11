using Godot;

/// <summary>
/// Converts the Unity game's world units into Godot pixels.
///
/// Every tuning value in the Unity docs (CLAUDE.md, CLAUDE_HANDOFF.md) is in Unity units:
/// ranges, radii, speeds, spawn points. Converting them all through this one factor means a
/// number can be copied across unchanged and wrapped in U(), and the whole game rescales if
/// the factor ever moves.
///
/// The Unity Park camera is orthographic with size 5.95, so it shows 11.9 units top to bottom.
/// At 1080p that is ~90.8 px per unit; 90 keeps the arena just inside the screen.
/// </summary>
public static class Units
{
	public const float PixelsPerUnit = 90.0f;

	/// <summary>Visible arena in Unity units, from the Park camera (16:9 at size 5.95).</summary>
	public static readonly Vector2 ArenaSize = new(21.15f, 11.9f);

	/// <summary>A distance, radius or speed in Unity units, as pixels.</summary>
	public static float U(float unityUnits) => unityUnits * PixelsPerUnit;

	/// <summary>
	/// A Unity position as a Godot position. Unity's y axis points up and Godot's points down,
	/// so y is flipped; both use the screen centre as the origin here.
	/// </summary>
	public static Vector2 U(float x, float y) => new(x * PixelsPerUnit, -y * PixelsPerUnit);
}
