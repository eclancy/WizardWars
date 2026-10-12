using Godot;

/// <summary>
/// The title: the Wizard Wars book art, a blinking prompt, and the Unity keyboard-controls box.
/// Start or A on any pad, or Enter or Space, goes to Character Select. Escape quits.
/// </summary>
public partial class TitleScreen : Control
{
	Label prompt;
	double elapsed;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		ColorRect black = new() { Color = Colors.Black };
		black.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(black);

		AddChild(Ui.Image(Content.Texture("res://assets/art/ui/title.png"), new Rect2(300, 10, 1320, 936)));

		prompt = Ui.Label("Press Start or Enter", 44, new Color(1.0f, 0.95f, 0.7f));
		Ui.Place(prompt, 0, 990, 1920, 70);
		AddChild(prompt);

		AddChild(Ui.Panel(new Rect2(30, 820, 520, 230)));
		string[] lines =
		{
			"Keyboard (player 1)",
			"Move: W A S D      Aim: arrow keys",
			"Abilities: 1  2  3   Ultimate: 5",
			"Join / start: Enter      Back: Backspace",
			"Pads: triggers, RB, X for the ultimate",
			"Character Select: Tab mode, T team, H help",
		};
		for (int i = 0; i < lines.Length; i++)
		{
			Label line = Ui.Label(lines[i], i == 0 ? 24 : 19, i == 0 ? Ui.Waiting : Colors.White, HorizontalAlignment.Left);
			Ui.Place(line, 50, 832 + i * 34, 490, 32);
			AddChild(line);
		}

		Label version = Ui.Label($"v{UpdateScreen.CurrentVersion}", 22, new Color(1, 1, 1, 0.55f), HorizontalAlignment.Right);
		Ui.Place(version, 1640, 1030, 250, 36);
		AddChild(version);

		Sfx.Music("res://assets/music/title.mp3");
	}

	public override void _PhysicsProcess(double delta)
	{
		elapsed += delta;
		prompt.Modulate = new Color(1, 1, 1, 0.55f + 0.45f * Mathf.Sin((float)elapsed * 4.0f));

		if (Input.IsPhysicalKeyPressed(Key.Escape)) { GetTree().Quit(); return; }
		foreach (int device in Devices.All)
		{
			InputState state = Devices.Get(device);
			if (state.StartPressed || state.ConfirmPressed) { GameRoot.Instance.ShowCharacterSelect(); return; }
		}
	}
}
