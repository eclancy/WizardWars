using Godot;

/// <summary>Team identity for Teams mode. In Free-for-All every wizard is on <see cref="NoTeam"/>.</summary>
public static class Teams
{
	public const int NoTeam = -1;
	public const int Red = 0;
	public const int Blue = 1;

	public static string Name(int team) => team == Red ? "Red" : team == Blue ? "Blue" : "";

	public static Color Colour(int team) =>
		team == Red ? new Color(0.95f, 0.25f, 0.2f) :
		team == Blue ? new Color(0.25f, 0.5f, 1.0f) :
		Colors.White;

	/// <summary>Players 1 and 3 start on Red, 2 and 4 on Blue, as in Unity.</summary>
	public static int DefaultForSlot(int slotIndex) => slotIndex % 2 == 0 ? Red : Blue;

	/// <summary>True when both are on the same real team. Never true in Free-for-All.</summary>
	public static bool Allied(int a, int b) => a != NoTeam && a == b;
}
