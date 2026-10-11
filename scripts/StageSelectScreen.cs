using Godot;

/// <summary>
/// Stage Select: the four arenas in a 2 x 2 grid. Any joined player moves the highlight and
/// confirms with A (Space); B (Backspace) goes back to Character Select. (Unity handed the
/// cursor to one random joined player; letting anyone drive is friendlier and changes nothing.)
/// </summary>
public partial class StageSelectScreen : Control
{
	const float TileWidth = 700.0f, TileHeight = 360.0f;
	readonly Panel[] frames = new Panel[4];
	int selected;

	static Vector2 TileOrigin(int index) => new(220 + index % 2 * (TileWidth + 80), 140 + index / 2 * (TileHeight + 90));

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(Ui.Screen("Background"));
		Label title = Ui.Label("Choose a stage", 48, Ui.Waiting);
		Ui.Place(title, 0, 40, 1920, 70);
		AddChild(title);

		selected = Mathf.Clamp(Session.StageIndex, 0, 3);
		StageData[] stages = Content.Stages;
		for (int i = 0; i < 4; i++)
		{
			Vector2 origin = TileOrigin(i);
			frames[i] = Ui.Panel(new Rect2(origin - new Vector2(10, 10), new Vector2(TileWidth + 20, TileHeight + 70)));
			AddChild(frames[i]);
			TextureRect preview = Ui.Image(stages[i].Background, new Rect2(origin, new Vector2(TileWidth, TileHeight)));
			preview.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
			preview.ClipContents = true;
			AddChild(preview);
			Label name = Ui.Label(stages[i].DisplayName, 30);
			Ui.Place(name, origin.X, origin.Y + TileHeight + 6, TileWidth, 44);
			AddChild(name);
		}
		Highlight();
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
			StyleBoxFlat style = (StyleBoxFlat)frames[i].GetThemeStylebox("panel").Duplicate();
			style.BorderColor = i == selected ? Ui.Waiting : new Color(1, 1, 1, 0.15f);
			style.BorderWidthLeft = style.BorderWidthRight = style.BorderWidthTop = style.BorderWidthBottom = i == selected ? 8 : 3;
			frames[i].AddThemeStyleboxOverride("panel", style);
		}
	}
}
