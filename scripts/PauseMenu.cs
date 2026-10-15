using Godot;

/// <summary>
/// Start during a battle pauses it and opens this menu. Whoever paused drives it:
/// - up/down picks an item, A chooses it
/// - Start or B resumes
///
/// The items:
/// - Resume
/// - Rematch: the whole match again from round 1, same fighters and stage
/// - Options (<see cref="OptionsPanel"/>)
/// - Quit to Character Select, with everyone still joined
///
/// The scene tree is paused underneath; this menu, GameRoot (which polls the devices) and the
/// options keep running.
/// </summary>
public partial class PauseMenu : Control
{
	static readonly string[] Items = { "RESUME", "REMATCH", "OPTIONS", "QUIT TO CHARACTER SELECT" };
	const float ItemTop = 400.0f, ItemHeight = 92.0f, Left = 610.0f, Width = 700.0f;

	int device, selected;
	string who;
	bool armed;
	Control items;
	OptionsPanel options;

	public static PauseMenu Open(Node parent, int device, string who)
	{
		PauseMenu menu = new() { device = device, who = who, ProcessMode = ProcessModeEnum.Always, MouseFilter = MouseFilterEnum.Ignore };
		menu.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		parent.AddChild(menu);
		menu.GetTree().Paused = true;
		return menu;
	}

	public override void _Ready()
	{
		ColorRect dim = new() { Color = new Color(0.02f, 0.0f, 0.06f, 0.65f), MouseFilter = MouseFilterEnum.Ignore };
		dim.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(dim);
		DesignFrame frame = DesignFrame.In(this);
		frame.AddChild(Ui.Panel(new Rect2(Left - 70, 200, Width + 140, 640), Ui.Gold, 0.97f));
		Label title = Ui.Heading("PAUSED", 64, Ui.Gold);
		Ui.Place(title, Left - 70, 222, Width + 140, 84);
		frame.AddChild(title);
		Label by = Ui.Label($"{who} paused the game", 22, new Color(1, 1, 1, 0.65f));
		Ui.Place(by, Left - 70, 305, Width + 140, 36);
		frame.AddChild(by);
		items = new Control { MouseFilter = MouseFilterEnum.Ignore };
		frame.AddChild(items);
		Label hint = Ui.Label("A: choose      Start or B: resume", 20, new Color(1, 1, 1, 0.6f));
		Ui.Place(hint, Left - 70, 785, Width + 140, 36);
		frame.AddChild(hint);
		Rebuild();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsInstanceValid(options)) return;
		InputState input = Devices.Get(device);
		// The Start that opened the menu must not also close it.
		if (!armed) { armed = true; return; }
		if (input.StartPressed || input.BackPressed) { Resume(); return; }
		if (input.NavPressed.Y != 0)
		{
			selected = (selected + input.NavPressed.Y + Items.Length) % Items.Length;
			Sfx.Play("res://assets/sfx/ice-chip.wav", -6.0f);
			Rebuild();
			return;
		}
		if (!input.ConfirmPressed) return;
		switch (selected)
		{
			case 0: Resume(); break;
			case 1: Resume(); GameRoot.Instance.Rematch(); break;
			case 2: options = OptionsPanel.Open(this, device, null); break;
			case 3: Resume(); GameRoot.Instance.ShowCharacterSelect(); break;
		}
	}

	void Resume()
	{
		GetTree().Paused = false;
		QueueFree();
	}

	void Rebuild()
	{
		foreach (Node child in items.GetChildren()) child.QueueFree();
		for (int i = 0; i < Items.Length; i++)
		{
			bool on = i == selected;
			float y = ItemTop + i * ItemHeight;
			Panel row = new() { Position = new Vector2(Left, y), Size = new Vector2(Width, ItemHeight - 16), MouseFilter = MouseFilterEnum.Ignore };
			row.AddThemeStyleboxOverride("panel", Ui.Style(on ? Ui.Gold : new Color(0.12f, 0.1f, 0.2f), on ? Colors.White : new Color(1, 1, 1, 0.1f), on ? 4 : 2, 18));
			items.AddChild(row);
			Label text = Ui.Heading(Items[i], 30, on ? Ui.Ink : Colors.White);
			if (on) text.AddThemeConstantOverride("outline_size", 0);
			Ui.Place(text, Left, y + 4, Width, ItemHeight - 24);
			items.AddChild(text);
		}
	}
}
