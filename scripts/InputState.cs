using Godot;

/// <summary>
/// One frame of intent from whatever controls a wizard or a menu cursor: a keyboard, a
/// gamepad, or the computer. Wizard and the menus cannot tell which, so the AI can do
/// anything a player can (in Unity it could not hold the flamethrower).
///
/// Directions are in Godot screen space: +y is down.
/// </summary>
public struct InputState
{
	/// <summary>Left stick or WASD, magnitude 0..1 with the deadzone already removed.</summary>
	public Vector2 Move;

	/// <summary>Right stick or arrow keys, magnitude 0..1. Zero when the player is not aiming.</summary>
	public Vector2 Aim;

	/// <summary>Right trigger / 1. Held: the basic attack fires again whenever it is off cooldown.</summary>
	public bool BasicHeld;

	/// <summary>Left trigger / 2. Held, like the basic attack.</summary>
	public bool SecondHeld;

	/// <summary>RB / 3. The edge casts; the level is what keeps a flamethrower burning.</summary>
	public bool ThirdPressed;
	public bool ThirdHeld;

	/// <summary>LB / 4. The fourth ability: the one restored from the Unity original.</summary>
	public bool FourthPressed;

	/// <summary>X / 5.</summary>
	public bool UltimatePressed;

	// Menu edges.
	public bool StartPressed;     // Start / Enter
	public bool ConfirmPressed;   // A / Space
	public bool BackPressed;      // B / Backspace
	public bool ModePressed;      // Select (Back/View) / Tab
	public bool TeamPressed;      // Y / T
	public bool HelpHeld;         // RS click / H

	/// <summary>A menu step: one of the four directions on the frame it was pushed, else zero.</summary>
	public Vector2I NavPressed;

	public static InputState None => new() { Move = Vector2.Zero, Aim = Vector2.Zero };

	/// <summary>A source that never presses anything (tests, and a wizard with no controller).</summary>
	public static readonly IInputSource NoneSource = new NoInput();

	sealed class NoInput : IInputSource
	{
		public InputState Poll() => None;
	}
}

/// <summary>
/// Anything that can drive a wizard or a menu. Poll is called exactly once per frame per
/// source (by <see cref="Devices"/> for real devices, by the wizard for the computer), so
/// "Pressed" fields are true for exactly one frame.
/// </summary>
public interface IInputSource
{
	InputState Poll();
}
