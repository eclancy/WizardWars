using Godot;

/// <summary>
/// One gamepad, read directly by device id (the DrawFight pattern) rather than through the
/// InputMap, so four pads never fight over shared actions.
///
/// Layout, as in the Unity game: left stick move, right stick aim, RT basic, LT second,
/// RB third, X ultimate; Start, A confirm, B back, Back/View mode, Y team, RS help.
/// Unlike Unity, Godot reports the triggers as two separate axes.
/// </summary>
public class GamepadInputSource : IInputSource
{
	/// <summary>Radial deadzone, rescaled from its edge so the stick ramps up from zero.</summary>
	const float Deadzone = 0.2f;
	const float TriggerThreshold = 0.45f;
	/// <summary>Menus need a firmer push than movement before a stick counts as a step.</summary>
	const float NavThreshold = 0.6f;

	public readonly int Device;

	bool thirdWas, ultimateWas, startWas, confirmWas, backWas, modeWas, teamWas;
	Vector2I navWas;

	public GamepadInputSource(int device) => Device = device;

	public bool IsConnected => Input.GetConnectedJoypads().Contains(Device);

	bool Button(JoyButton button) => Input.IsJoyButtonPressed(Device, button);

	Vector2 Stick(JoyAxis x, JoyAxis y)
	{
		Vector2 raw = new(Input.GetJoyAxis(Device, x), Input.GetJoyAxis(Device, y));
		float magnitude = raw.Length();
		if (magnitude <= Deadzone) return Vector2.Zero;
		return raw / magnitude * Mathf.Min(1.0f, (magnitude - Deadzone) / (1.0f - Deadzone));
	}

	public InputState Poll()
	{
		// An unplugged pad reports nothing rather than freezing its last direction.
		if (!IsConnected)
		{
			thirdWas = ultimateWas = startWas = confirmWas = backWas = modeWas = teamWas = false;
			navWas = Vector2I.Zero;
			return InputState.None;
		}

		Vector2 move = Stick(JoyAxis.LeftX, JoyAxis.LeftY);
		Vector2 aim = Stick(JoyAxis.RightX, JoyAxis.RightY);
		bool third = Button(JoyButton.RightShoulder), ultimate = Button(JoyButton.X);
		bool start = Button(JoyButton.Start), confirm = Button(JoyButton.A), back = Button(JoyButton.B);
		bool mode = Button(JoyButton.Back), team = Button(JoyButton.Y);

		Vector2 dpad = new(
			(Button(JoyButton.DpadRight) ? 1.0f : 0.0f) - (Button(JoyButton.DpadLeft) ? 1.0f : 0.0f),
			(Button(JoyButton.DpadDown) ? 1.0f : 0.0f) - (Button(JoyButton.DpadUp) ? 1.0f : 0.0f));
		Vector2 navAxis = dpad != Vector2.Zero ? dpad : move;
		Vector2I nav = Vector2I.Zero;
		if (Mathf.Abs(navAxis.X) >= NavThreshold || Mathf.Abs(navAxis.Y) >= NavThreshold)
			nav = Mathf.Abs(navAxis.X) >= Mathf.Abs(navAxis.Y) ? new Vector2I(Mathf.Sign(navAxis.X), 0) : new Vector2I(0, Mathf.Sign(navAxis.Y));

		InputState state = new()
		{
			Move = dpad != Vector2.Zero ? dpad.Normalized() : move,
			Aim = aim,
			BasicHeld = Input.GetJoyAxis(Device, JoyAxis.TriggerRight) > TriggerThreshold,
			SecondHeld = Input.GetJoyAxis(Device, JoyAxis.TriggerLeft) > TriggerThreshold,
			ThirdPressed = third && !thirdWas,
			ThirdHeld = third,
			UltimatePressed = ultimate && !ultimateWas,
			StartPressed = start && !startWas,
			ConfirmPressed = confirm && !confirmWas,
			BackPressed = back && !backWas,
			ModePressed = mode && !modeWas,
			TeamPressed = team && !teamWas,
			HelpHeld = Button(JoyButton.RightStick),
			NavPressed = nav != navWas ? nav : Vector2I.Zero,
		};
		thirdWas = third; ultimateWas = ultimate; startWas = start; confirmWas = confirm;
		backWas = back; modeWas = mode; teamWas = team; navWas = nav;
		return state;
	}
}
