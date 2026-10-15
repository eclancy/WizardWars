using Godot;

/// <summary>
/// Team identity for Teams mode. In Free-for-All every wizard is on <see cref="NoTeam"/>.
///
/// The teams are Green and Pink, colours no wizard wears: Fire is orange-red, Frost light blue,
/// Earth brown and Electricity yellow. Unity's Red and Blue teams looked like "the Fire side" and
/// "the Frost side", so a team colour said nothing certain about who was on it.
/// </summary>
public static class Teams
{
	public const int NoTeam = -1;
	public const int Green = 0;
	public const int Pink = 1;

	public static string Name(int team) => team == Green ? "Green" : team == Pink ? "Pink" : "";

	public static Color Colour(int team) =>
		team == Green ? new Color(0.3f, 0.9f, 0.45f) :
		team == Pink ? new Color(1.0f, 0.4f, 0.78f) :
		Colors.White;

	/// <summary>Players 1 and 3 start on Green, 2 and 4 on Pink, as Unity's Red and Blue did.</summary>
	public static int DefaultForSlot(int slotIndex) => slotIndex % 2 == 0 ? Green : Pink;

	/// <summary>True when both are on the same real team. Never true in Free-for-All.</summary>
	public static bool Allied(int a, int b) => a != NoTeam && a == b;
}
