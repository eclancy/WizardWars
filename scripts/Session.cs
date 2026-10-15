/// <summary>One of the four player slots on Character Select.</summary>
public sealed class PlayerSlot
{
	public readonly int Index;
	public bool Joined;
	public bool Locked;
	/// <summary>The device that joined this slot (<see cref="Devices"/> id).</summary>
	public int Device;
	/// <summary>The wizard the slot's cursor is on, and once locked, its pick.</summary>
	public int WizardIndex;
	public int Team;

	public PlayerSlot(int index)
	{
		Index = index;
		Leave();
	}

	public void Leave()
	{
		Joined = false;
		Locked = false;
		Device = int.MinValue;
		WizardIndex = Index % 4;
		Team = Teams.DefaultForSlot(Index);
	}

	public string Label => "P" + (Index + 1);
}

/// <summary>
/// The single source of truth for who is playing, how, and where. In Unity this lived in two
/// systems (GlobalHandler and the LocalMultiplayer runtime) kept in sync by reflection; here
/// there is only this.
/// </summary>
public static class Session
{
	public static readonly PlayerSlot[] Slots = { new(0), new(1), new(2), new(3) };

	/// <summary>The mode chosen on Character Select. It carries over between matches.</summary>
	public static bool TeamsMode;

	public static int StageIndex;

	public static int JoinedCount
	{
		get
		{
			int count = 0;
			foreach (PlayerSlot slot in Slots) if (slot.Joined) count++;
			return count;
		}
	}

	public static PlayerSlot SlotForDevice(int device)
	{
		foreach (PlayerSlot slot in Slots) if (slot.Joined && slot.Device == device) return slot;
		return null;
	}

	/// <summary>
	/// The player locked in on a wizard, other than <paramref name="except"/>, or null. Each
	/// wizard can be played by one player at a time, so a wizard's colour is its player's colour.
	/// </summary>
	public static PlayerSlot LockedOn(int wizardIndex, PlayerSlot except = null)
	{
		foreach (PlayerSlot slot in Slots)
			if (slot != except && slot.Joined && slot.Locked && slot.WizardIndex == wizardIndex) return slot;
		return null;
	}

	/// <summary>The next wizard in a direction (+1 or -1) nobody else has locked in, or <paramref name="from"/> if none.</summary>
	public static int NextFree(int from, int step, PlayerSlot slot)
	{
		int count = Content.Wizards.Length;
		for (int k = 1; k <= count; k++)
		{
			int index = ((from + step * k) % count + count) % count;
			if (LockedOn(index, slot) == null) return index;
		}
		return from;
	}

	/// <summary>The first wizard from <paramref name="from"/> onward nobody else has locked in.</summary>
	public static int FirstFree(int from, PlayerSlot slot) => LockedOn(from, slot) == null ? from : NextFree(from, 1, slot);

	/// <summary>
	/// Teams needs two or more players with somebody on each side. A lone player in Teams
	/// mode plays the computer in Free-for-All, as in Unity.
	/// </summary>
	public static bool TeamsPlayable
	{
		get
		{
			if (!TeamsMode) return false;
			bool green = false, pink = false;
			foreach (PlayerSlot slot in Slots)
			{
				if (!slot.Joined) continue;
				if (slot.Team == Teams.Green) green = true; else pink = true;
			}
			return green && pink;
		}
	}

	public static bool TeamsUnbalanced => TeamsMode && JoinedCount > 1 && !TeamsPlayable;

	public static void Reset()
	{
		foreach (PlayerSlot slot in Slots) slot.Leave();
		TeamsMode = false;
		StageIndex = 0;
	}
}
