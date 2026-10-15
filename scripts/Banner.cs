using Godot;

/// <summary>A big centred result line with a drop shadow, for results that have no authored art.</summary>
public static class Banner
{
	public static Control Create(string text, Color colour, int size = 120)
	{
		Control root = new();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.MouseFilter = Control.MouseFilterEnum.Ignore;
		root.AddChild(MakeLabel(text, new Color(0, 0, 0, 0.75f), new Vector2(5, 5), size));
		root.AddChild(MakeLabel(text, colour, Vector2.Zero, size));
		return root;
	}

	static Label MakeLabel(string text, Color colour, Vector2 offset, int size)
	{
		Label label = new()
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		label.Position = offset;
		label.AddThemeFontOverride("font", Ui.HeadingFont);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", colour);
		label.AddThemeConstantOverride("outline_size", 10);
		label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.6f));
		return label;
	}
}
