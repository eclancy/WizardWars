using Godot;

/// <summary>
/// Stage Select: every arena in a grid three wide. Each tile shows the stage, its name, its
/// cover piece and a line on what makes it different (<see cref="StageData.Caption"/>). Under the
/// grid are the match settings:
/// - Y cycles the rounds: best of 1, 3 or 5
/// - RB switches the goal: knock everyone out, or King of the Rune (<see cref="RuneHill"/>)
/// - X cycles the computer's skill, when someone is playing alone
///
/// Any joined player moves the highlight and confirms with A (Space); B (Backspace) goes back to
/// Character Select. (Unity handed the cursor to one random joined player; letting anyone drive is
/// friendlier.)
/// </summary>
public partial class StageSelectScreen : Control
{
	const int Columns = 3;
	const float TileWidth = 540.0f, TileHeight = 250.0f, PlateHeight = 82.0f;
	const float GapX = 44.0f, GapY = 34.0f, Top = 128.0f, SettingsTop = 860.0f;

	Control[] tiles;
	Panel[] frames;
	Control settings;
	int selected;
	double time;

	static int Count => Content.Stages.Length;

	static Vector2 TileOrigin(int index) => new(
		(1920.0f - TileWidth * Columns - GapX * (Columns - 1)) * 0.5f + index % Columns * (TileWidth + GapX),
		Top + index / Columns * (TileHeight + PlateHeight + GapY));

	/// <summary>Someone is alone against the computer, so its skill matters.</summary>
	static bool ComputerPlays => Session.JoinedCount == 1;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(Ui.Screen("Background"));
		// The layout lives in a centred 1920 x 1080 frame; the backdrop fills the real screen.
		DesignFrame frame = DesignFrame.In(this);
		Label title = Ui.Heading("CHOOSE A STAGE", 60, Ui.Gold);
		Ui.Place(title, 0, 24, 1920, 84);
		frame.AddChild(title);

		tiles = new Control[Count];
		frames = new Panel[Count];
		selected = Mathf.Clamp(Session.StageIndex, 0, Count - 1);
		StageData[] stages = Content.Stages;
		for (int i = 0; i < Count; i++)
		{
			Vector2 origin = TileOrigin(i);
			Vector2 size = new(TileWidth, TileHeight + PlateHeight);
			// Each tile is one control so the selected one can lift as a whole.
			tiles[i] = new Control { Position = origin, Size = size, PivotOffset = size * 0.5f, MouseFilter = MouseFilterEnum.Ignore };
			frame.AddChild(tiles[i]);

			frames[i] = new Panel { Position = new Vector2(-8, -8), Size = size + new Vector2(16, 16), MouseFilter = MouseFilterEnum.Ignore };
			tiles[i].AddChild(frames[i]);

			TextureRect preview = Ui.Image(stages[i].Background, new Rect2(0, 0, TileWidth, TileHeight));
			preview.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
			preview.ClipContents = true;
			tiles[i].AddChild(preview);
			if (stages[i].HazardKind.Length > 0)
			{
				HazardPreview hazard = new(stages[i]);
				hazard.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
				preview.AddChild(hazard);
			}

			// The name plate: the stage's cover piece, its name, and what is special about it.
			Panel plate = new() { Position = new Vector2(0, TileHeight), Size = new Vector2(TileWidth, PlateHeight), MouseFilter = MouseFilterEnum.Ignore };
			plate.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(Ui.PanelColour, 0.96f), CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 });
			tiles[i].AddChild(plate);
			if (stages[i].CoverKind.Length > 0)
				tiles[i].AddChild(Ui.Image(CoverArt.For(stages[i].CoverKind).Texture, new Rect2(12, TileHeight + 6, 80, PlateHeight - 12)));
			Label name = Ui.Heading(stages[i].DisplayName.ToUpper(), 28, Colors.White, HorizontalAlignment.Left);
			Ui.Place(name, 104, TileHeight + 4, TileWidth - 116, 40);
			tiles[i].AddChild(name);
			Label caption = Ui.Label(stages[i].Caption, 18, new Color(1, 1, 1, 0.7f), HorizontalAlignment.Left);
			Ui.Place(caption, 104, TileHeight + 44, TileWidth - 116, 28);
			tiles[i].AddChild(caption);
		}

		settings = new Control { MouseFilter = MouseFilterEnum.Ignore };
		frame.AddChild(settings);
		Label footer = Ui.Label("A: fight here      B: back", 22, new Color(1, 1, 1, 0.6f));
		Ui.Place(footer, 0, 1030, 1920, 36);
		frame.AddChild(footer);
		Highlight();
		DrawSettings();
	}

	public override void _Process(double delta)
	{
		time += delta;
		float pulse = (Mathf.Sin((float)time * 4.0f) + 1.0f) * 0.5f;
		for (int i = 0; i < Count; i++)
		{
			float target = i == selected ? 1.03f : 0.97f;
			tiles[i].Scale = Vector2.One * Mathf.Lerp(tiles[i].Scale.X, target, 0.2f);
			tiles[i].Modulate = i == selected ? Colors.White : new Color(0.75f, 0.75f, 0.8f);
			frames[i].SelfModulate = i == selected ? Colors.White.Lerp(new Color(1.2f, 1.2f, 1.2f), pulse) : Colors.White;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		foreach (int device in Devices.All)
		{
			if (Session.SlotForDevice(device) == null) continue;
			InputState state = Devices.Get(device);
			if (state.NavPressed != Vector2I.Zero)
			{
				int rows = (Count + Columns - 1) / Columns;
				int column = Mathf.Clamp(selected % Columns + state.NavPressed.X, 0, Columns - 1);
				int row = Mathf.Clamp(selected / Columns + state.NavPressed.Y, 0, rows - 1);
				selected = Mathf.Min(row * Columns + column, Count - 1);
				Highlight();
			}
			if (state.TeamPressed)
			{
				Session.RoundsToWin = Session.RoundsToWin % 3 + 1;
				DrawSettings();
			}
			if (state.ThirdPressed)
			{
				Session.Goal = Session.Goal == Goal.Knockout ? Goal.KingOfTheRune : Goal.Knockout;
				DrawSettings();
			}
			if (state.UltimatePressed && ComputerPlays)
			{
				Session.CpuDifficulty = (Difficulty)(((int)Session.CpuDifficulty + 1) % 3);
				DrawSettings();
			}
			if (state.ConfirmPressed || state.StartPressed)
			{
				Session.StageIndex = selected;
				GameRoot.Instance.ShowBattle();
				return;
			}
			if (state.BackPressed) { GameRoot.Instance.ShowCharacterSelect(); return; }
		}
	}

	void Highlight()
	{
		for (int i = 0; i < Count; i++)
		{
			bool on = i == selected;
			frames[i].AddThemeStyleboxOverride("panel", Ui.Style(new Color(Ui.PanelColour, 0.95f), on ? Ui.Gold : new Color(1, 1, 1, 0.12f), on ? 8 : 3, 16));
			tiles[i].ZIndex = on ? 1 : 0;
		}
	}

	/// <summary>The match settings under the grid, each with the button that changes it.</summary>
	void DrawSettings()
	{
		foreach (Node child in settings.GetChildren()) child.QueueFree();
		bool computer = ComputerPlays;
		float width = computer ? SettingWidth * 3 + 40 : SettingWidth * 2 + 20, x = (1920.0f - width) * 0.5f;
		Setting(x, "Y", "ROUNDS", $"BEST OF {Session.RoundsToWin * 2 - 1}");
		Setting(x + SettingWidth + 20, "RB", "GOAL", Session.Goal == Goal.KingOfTheRune ? "KING OF THE RUNE" : "KNOCK OUT",
			Session.Goal == Goal.KingOfTheRune ? new Color(1.0f, 0.55f, 0.25f) : Ui.Gold);
		if (computer)
			Setting(x + (SettingWidth + 20) * 2, "X", "COMPUTER", Session.CpuDifficulty.ToString().ToUpper(),
				Session.CpuDifficulty switch { Difficulty.Easy => new Color(0.3f, 0.75f, 0.4f), Difficulty.Hard => new Color(0.9f, 0.3f, 0.25f), _ => Ui.Gold });
	}

	const float SettingWidth = 500.0f;

	void Setting(float x, string button, string name, string value, Color? colour = null)
	{
		settings.AddChild(Ui.Panel(new Rect2(x, SettingsTop, SettingWidth, 72), new Color(1, 1, 1, 0.15f), 0.9f));
		settings.AddChild(Ui.Pill(new Rect2(x + 14, SettingsTop + 18, 52, 36), button, 18, new Color(0.25f, 0.22f, 0.35f), Colors.White, heading: true));
		Label label = Ui.Label(name, 21, new Color(1, 1, 1, 0.75f), HorizontalAlignment.Left);
		Ui.Place(label, x + 76, SettingsTop + 14, 140, 44);
		settings.AddChild(label);
		settings.AddChild(Ui.Pill(new Rect2(x + 206, SettingsTop + 14, 280, 44), value, 21, colour ?? Ui.Gold, Ui.Ink, heading: true));
	}
}
