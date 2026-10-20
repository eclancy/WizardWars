using Godot;

/// <summary>
/// Team identity for Teams mode. In Free-for-All every wizard is on <see cref="NoTeam"/>.
///
/// The teams are Gold and Pink, colours no wizard wears: Fire is orange-red, Frost light blue,
/// Earth brown, Electricity violet, Shadow ash grey, Nature leaf green and Wind mint. Unity's Red
/// and Blue teams looked like "the Fire side" and "the Frost side", so a team colour said nothing
/// certain about who was on it; Green gave way to Gold when the Nature wizard arrived.
/// </summary>
public static class Teams
{
	public const int NoTeam = -1;
	public const int Gold = 0;
	public const int Pink = 1;

	public static string Name(int team) => team == Gold ? "Gold" : team == Pink ? "Pink" : "";

	public static Color Colour(int team) =>
		team == Gold ? new Color(1.0f, 0.78f, 0.2f) :
		team == Pink ? new Color(1.0f, 0.4f, 0.78f) :
		Colors.White;

	/// <summary>Players 1 and 3 start on Gold, 2 and 4 on Pink, as Unity's Red and Blue did.</summary>
	public static int DefaultForSlot(int slotIndex) => slotIndex % 2 == 0 ? Gold : Pink;

	/// <summary>True when both are on the same real team. Never true in Free-for-All.</summary>
	public static bool Allied(int a, int b) => a != NoTeam && a == b;
}
