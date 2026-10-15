using System;
using Godot;

/// <summary>
/// The options, as an overlay over whatever opened it (Character Select or the pause menu),
/// driven by the one device that opened it:
/// - up/down picks a row, left/right changes it, A toggles
/// - B (or A on Back) closes it
///
/// The rows are music volume, sound volume, fullscreen and rumble; everything is saved at once
/// (<see cref="Settings"/>). It runs while the game is paused.
/// </summary>
public partial class OptionsPanel : Control
{
	static readonly string[] Rows = { "MUSIC", "SOUND", "FULLSCREEN", "RUMBLE", "BACK" };
	const float RowTop = 330.0f, RowHeight = 84.0f, Left = 560.0f, Width = 800.0f;

	int device, selected;
	Action onClose;
	Control rows;

	public static OptionsPanel Open(Node parent, int device, Action onClose)
	{
		OptionsPanel panel = new() { device = device, onClose = onClose, ProcessMode = ProcessModeEnum.Always, MouseFilter = MouseFilterEnum.Ignore };
		panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		parent.AddChild(panel);
		return panel;
	}

	public override void _Ready()
	{
		ColorRect dim = new() { Color = new Color(0, 0, 0, 0.6f), MouseFilter = MouseFilterEnum.Ignore };
		dim.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(dim);
		DesignFrame frame = DesignFrame.In(this);
		frame.AddChild(Ui.Panel(new Rect2(Left - 60, 200, Width + 120, 640), Ui.Gold, 0.97f));
		Label title = Ui.Heading("OPTIONS", 52, Ui.Gold);
		Ui.Place(title, Left - 60, 222, Width + 120, 70);
		frame.AddChild(title);
		rows = new Control { MouseFilter = MouseFilterEnum.Ignore };
		frame.AddChild(rows);
		Label hint = Ui.Label("Up / down: choose      Left / right: change      B: back", 20, new Color(1, 1, 1, 0.6f));
		Ui.Place(hint, Left - 60, 785, Width + 120, 36);
		frame.AddChild(hint);
		Rebuild();
	}

	public override void _PhysicsProcess(double delta)
	{
		InputState input = Devices.Get(device);
		bool changed = true;
		if (input.NavPressed.Y != 0) selected = (selected + input.NavPressed.Y + Rows.Length) % Rows.Length;
		else if (input.NavPressed.X != 0) Change(input.NavPressed.X);
		else if (input.ConfirmPressed && selected == Rows.Length - 1 || input.BackPressed) { Close(); return; }
		else if (input.ConfirmPressed) Change(1);
		else changed = false;
		if (changed)
		{
			Sfx.Play("res://assets/sfx/ice-chip.wav", -6.0f);
			Rebuild();
		}
	}

	void Change(int step)
	{
		switch (selected)
		{
			case 0: Settings.StepMusic(step); break;
			case 1: Settings.StepSound(step); break;
			case 2: Settings.SetFullscreen(!Settings.Fullscreen); break;
			case 3: Settings.SetRumble(!Settings.Rumble); break;
		}
	}

	void Close()
	{
		onClose?.Invoke();
		QueueFree();
	}

	void Rebuild()
	{
		foreach (Node child in rows.GetChildren()) child.QueueFree();
		for (int i = 0; i < Rows.Length; i++)
		{
			float y = RowTop + i * RowHeight;
			bool on = i == selected;
			Panel row = new() { Position = new Vector2(Left, y), Size = new Vector2(Width, RowHeight - 14), MouseFilter = MouseFilterEnum.Ignore };
			row.AddThemeStyleboxOverride("panel", Ui.Style(on ? new Color(0.25f, 0.2f, 0.4f) : new Color(0.12f, 0.1f, 0.2f), on ? Ui.Gold : new Color(1, 1, 1, 0.1f), on ? 4 : 2, 16));
			rows.AddChild(row);
			Label name = Ui.Heading(Rows[i], 28, on ? Ui.Gold : Colors.White, i == Rows.Length - 1 ? HorizontalAlignment.Center : HorizontalAlignment.Left);
			Ui.Place(name, i == Rows.Length - 1 ? Left : Left + 30, y + 6, i == Rows.Length - 1 ? Width : 300, RowHeight - 26);
			rows.AddChild(name);
			switch (i)
			{
				case 0: Volume(Settings.MusicVolume, y); break;
				case 1: Volume(Settings.SoundVolume, y); break;
				case 2: Toggle(Settings.Fullscreen, y); break;
				case 3: Toggle(Settings.Rumble, y); break;
			}
		}
	}

	/// <summary>Ten blocks, lit up to the volume, and the percentage.</summary>
	void Volume(float volume, float y)
	{
		int lit = Mathf.RoundToInt(volume * 10.0f);
		for (int b = 0; b < 10; b++)
			rows.AddChild(new ColorRect { Position = new Vector2(Left + 330 + b * 32, y + 20), Size = new Vector2(26, 30), Color = b < lit ? Ui.Gold : new Color(1, 1, 1, 0.15f), MouseFilter = MouseFilterEnum.Ignore });
		Label percent = Ui.Label(lit == 0 ? "OFF" : $"{lit * 10}%", 24, Colors.White, HorizontalAlignment.Right);
		Ui.Place(percent, Left + Width - 150, y + 10, 120, 50);
		rows.AddChild(percent);
	}

	void Toggle(bool on, float y)
	{
		rows.AddChild(Ui.Pill(new Rect2(Left + 330, y + 14, 110, 42), "ON", 20, on ? new Color(0.18f, 0.55f, 0.26f) : new Color(1, 1, 1, 0.08f), on ? Colors.White : new Color(1, 1, 1, 0.4f), heading: true));
		rows.AddChild(Ui.Pill(new Rect2(Left + 450, y + 14, 110, 42), "OFF", 20, !on ? new Color(0.55f, 0.2f, 0.2f) : new Color(1, 1, 1, 0.08f), !on ? Colors.White : new Color(1, 1, 1, 0.4f), heading: true));
	}
}
