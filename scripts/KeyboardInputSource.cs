using Godot;

/// <summary>
/// Player 1's keyboard, with the same keys as the Unity game: WASD move, arrows aim,
/// 1 / 2 / 3 / 5 basic, second, third and ultimate; Enter start, Space confirm,
/// Backspace back, Tab mode, T team, H how to play. (4 did nothing in Unity: the fourth
/// ability was cut. It is not bound here.)
/// </summary>
public class KeyboardInputSource : IInputSource
{
	bool thirdWas, ultimateWas, startWas, confirmWas, backWas, modeWas, teamWas;
	Vector2I navWas;

	static bool Down(Key key) => Input.IsPhysicalKeyPressed(key);

	static float Axis(Key negative, Key positive) => (Down(positive) ? 1.0f : 0.0f) - (Down(negative) ? 1.0f : 0.0f);

	public InputState Poll()
	{
		Vector2 move = new(Axis(Key.A, Key.D), Axis(Key.W, Key.S));
		Vector2 aim = new(Axis(Key.Left, Key.Right), Axis(Key.Up, Key.Down));
		bool third = Down(Key.Key3), ultimate = Down(Key.Key5);
		// Alt+Enter toggles fullscreen (GameRoot), so it is not also a menu Start.
		bool start = (Down(Key.Enter) || Down(Key.KpEnter)) && !Down(Key.Alt), confirm = Down(Key.Space), back = Down(Key.Backspace);
		bool mode = Down(Key.Tab), team = Down(Key.T);

		// Menus step on WASD or the arrows.
		Vector2 navAxis = move + aim;
		Vector2I nav = new(Mathf.Sign((int)Mathf.Round(navAxis.X)), Mathf.Sign((int)Mathf.Round(navAxis.Y)));

		InputState state = new()
		{
			Move = move.LimitLength(1.0f),
			Aim = aim.LimitLength(1.0f),
			BasicHeld = Down(Key.Key1),
			SecondHeld = Down(Key.Key2),
			ThirdPressed = third && !thirdWas,
			ThirdHeld = third,
			UltimatePressed = ultimate && !ultimateWas,
			StartPressed = start && !startWas,
			ConfirmPressed = confirm && !confirmWas,
			BackPressed = back && !backWas,
			ModePressed = mode && !modeWas,
			TeamPressed = team && !teamWas,
			HelpHeld = Down(Key.H),
			NavPressed = nav != navWas ? nav : Vector2I.Zero,
		};
		thirdWas = third; ultimateWas = ultimate; startWas = start; confirmWas = confirm;
		backWas = back; modeWas = mode; teamWas = team; navWas = nav;
		return state;
	}
}
