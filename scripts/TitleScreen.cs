using Godot;

/// <summary>
/// The title: the Wizard Wars book art over the magic backdrop, with the red START painted into
/// the art flashing to invite a press, and the version in the corner. No text prompt: the art
/// already says START.
///
/// The flash is an additive white copy of just the START letters (title-start-glow.png, built
/// from title.png by <see cref="TitleGlowTool"/>), laid exactly over them and pulsed.
/// Start or A on any pad, or Enter or Space, goes to Character Select. Escape quits.
/// </summary>
public partial class TitleScreen : Control
{
	/// <summary>title.png's size, and where the START letters' mask sits in it (from TitleGlowTool).</summary>
	static readonly Vector2 ArtSize = new(5528, 3921);
	public static readonly Vector2 GlowOrigin = new(1960, 2773);

	static readonly Rect2 ArtRect = new(250, 25, 1420, 1007);

	Control art;
	TextureRect glow;
	double elapsed;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(new MagicBackdrop());

		// The art and its START glow share a parent so they breathe together.
		art = new Control { Position = ArtRect.Position, Size = ArtRect.Size, PivotOffset = ArtRect.Size * 0.5f, MouseFilter = MouseFilterEnum.Ignore };
		DesignFrame.In(this).AddChild(art);
		art.AddChild(Ui.Image(Content.Texture("res://assets/art/ui/title.png"), new Rect2(Vector2.Zero, ArtRect.Size)));

		float scale = ArtRect.Size.X / ArtSize.X;
		Texture2D glowTexture = Content.Texture("res://assets/art/ui/title-start-glow.png");
		glow = Ui.Image(glowTexture, new Rect2(GlowOrigin * scale, glowTexture.GetSize() * scale));
		glow.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
		glow.Modulate = new Color(1, 1, 1, 0);
		art.AddChild(glow);

		// The version sits in the real bottom-right corner, whatever the screen's shape.
		Label version = Ui.Label($"v{UpdateScreen.CurrentVersion}", 22, new Color(1, 1, 1, 0.55f), HorizontalAlignment.Right);
		AddChild(version);
		DesignFrame.BottomRight(version, 14);

		Sfx.Music("res://assets/music/title.mp3");
	}

	public override void _Process(double delta)
	{
		elapsed += delta;
		float t = (float)elapsed;
		// START flashes: a quick bright pulse, then a rest.
		float pulse = Mathf.Pow((Mathf.Sin(t * 3.6f) + 1.0f) * 0.5f, 3.0f);
		glow.Modulate = new Color(1, 1, 1, 0.9f * pulse);
		art.Scale = Vector2.One * (1.0f + 0.006f * Mathf.Sin(t * 1.3f));
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Input.IsPhysicalKeyPressed(Key.Escape)) { GetTree().Quit(); return; }
		foreach (int device in Devices.All)
		{
			InputState state = Devices.Get(device);
			if (state.StartPressed || state.ConfirmPressed) { GameRoot.Instance.ShowCharacterSelect(); return; }
		}
	}
}
