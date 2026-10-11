using System.Collections.Generic;
using Godot;

/// <summary>
/// Every real input device, polled exactly once per frame so press edges are never lost or
/// doubled. The keyboard is device <see cref="Keyboard"/>; gamepads use their Godot ids.
///
/// GameRoot calls <see cref="Update"/> at the top of each frame, before any screen or wizard
/// reads <see cref="Get"/>. A player is bound to a device id when they join, so a slot keeps
/// its controller for the whole session; a pad plugged in later just becomes another device.
/// </summary>
public static class Devices
{
	public const int Keyboard = -1;

	static readonly Dictionary<int, IInputSource> sources = new();
	static readonly Dictionary<int, InputState> latest = new();

	public static IEnumerable<int> All => latest.Keys;

	public static void Update()
	{
		if (!sources.ContainsKey(Keyboard)) sources[Keyboard] = new KeyboardInputSource();
		foreach (int pad in Input.GetConnectedJoypads())
			if (!sources.ContainsKey(pad)) sources[pad] = new GamepadInputSource(pad);

		foreach (KeyValuePair<int, IInputSource> pair in sources) latest[pair.Key] = pair.Value.Poll();
	}

	public static InputState Get(int device) => latest.TryGetValue(device, out InputState state) ? state : InputState.None;

	/// <summary>Adds a scripted device (the flow test). Real devices are found automatically.</summary>
	public static void Inject(int device, IInputSource source) => sources[device] = source;

	public static string Describe(int device) => device == Keyboard ? "Keyboard" : $"Pad {device + 1}";
}

/// <summary>A player's input source for a battle: reads whatever the bound device did this frame.</summary>
public class DeviceInputSource : IInputSource
{
	readonly int device;
	public DeviceInputSource(int device) => this.device = device;
	public InputState Poll() => Devices.Get(device);
}
