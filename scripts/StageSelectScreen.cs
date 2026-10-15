using Godot;

/// <summary>
/// Stage Select: the four arenas in a 2 x 2 grid, each tile showing the stage, its name, and the
/// cover it is known for (drawn with that stage's own cover art). Any joined player moves the
/// highlight and confirms with A (Space); B (Backspace) goes back to Character Select. (Unity
/// handed the cursor to one random joined player; letting anyone drive is friendlier.)
/// </summary>
public partial class StageSelectScreen : Control
{
	const float TileWidth = 760.0f, TileHeight = 330.0f, PlateHeight = 84.0f;
	const float GapX = 60.0f, GapY = 44.0f, Top = 150.0f;

	readonly Control[] tiles = new Control[4];
	readonly Panel[] frames = new Panel[4];
	int selected;
	double time;

	static Vector2 TileOrigin(int index) => new(
		(1920.0f - TileWidth * 2 - GapX) * 0.5f + index % 2 * (TileWidth + GapX),
		Top + index / 2 * (TileHeight + PlateHeight + GapY));

	/// <summary>What each stage's cover is, for the tile's caption.</summary>
	static string CoverCaption(string kind) => kind switch
	{
		"stump" => "Tree stumps to hide behind",
		"crystal" => "Ice crystals block the way",
		"saguaro" => "Saguaro cacti for cover",
		"obelisk" => "Rune stones stand in the storm",
		_ => "",
	};

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(Ui.Screen("Background"));
		// The layout lives in a centred 1920 x 1080 frame; the backdrop fills the real screen.
		DesignFrame frame = DesignFrame.In(this);
		Label title = Ui.Heading("CHOOSE A STAGE", 64, Ui.Gold);
		Ui.Place(title, 0, 30, 1920, 90);
		frame.AddChild(title);

		selected = Mathf.Clamp(Session.StageIndex, 0, 3);
		StageData[] stages = Content.Stages;
		for (int i = 0; i < 4; i++)
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

			// The name plate: the stage's cover piece, its name, and what the cover is.
			Panel plate = new() { Position = new Vector2(0, TileHeight), Size = new Vector2(TileWidth, PlateHeight), MouseFilter = MouseFilterEnum.Ignore };
			plate.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(Ui.PanelColour, 0.96f), CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 });
			tiles[i].AddChild(plate);
			if (stages[i].CoverKind.Length > 0)
				tiles[i].AddChild(Ui.Image(CoverArt.For(stages[i].CoverKind).Texture, new Rect2(16, TileHeight + 6, 90, PlateHeight - 12)));
			Label name = Ui.Heading(stages[i].DisplayName.ToUpper(), 32, Colors.White, HorizontalAlignment.Left);
			Ui.Place(name, 122, TileHeight + 4, TileWidth - 140, 44);
			tiles[i].AddChild(name);
			Label caption = Ui.Label(CoverCaption(stages[i].CoverKind), 20, new Color(1, 1, 1, 0.7f), HorizontalAlignment.Left);
			Ui.Place(caption, 122, TileHeight + 46, TileWidth - 140, 30);
			tiles[i].AddChild(caption);
		}

		Label footer = Ui.Label("A: fight here      B: back", 22, new Color(1, 1, 1, 0.6f));
		Ui.Place(footer, 0, 1030, 1920, 36);
		frame.AddChild(footer);
		Highlight();
	}

	public override void _Process(double delta)
	{
		time += delta;
		float pulse = (Mathf.Sin((float)time * 4.0f) + 1.0f) * 0.5f;
		for (int i = 0; i < 4; i++)
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
				int column = Mathf.Clamp(selected % 2 + state.NavPressed.X, 0, 1);
				int row = Mathf.Clamp(selected / 2 + state.NavPressed.Y, 0, 1);
				selected = row * 2 + column;
				Highlight();
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
		for (int i = 0; i < 4; i++)
		{
			bool on = i == selected;
			frames[i].AddThemeStyleboxOverride("panel", Ui.Style(new Color(Ui.PanelColour, 0.95f), on ? Ui.Gold : new Color(1, 1, 1, 0.12f), on ? 8 : 3, 16));
			tiles[i].ZIndex = on ? 1 : 0;
		}
	}
}
