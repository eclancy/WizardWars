using Godot;

/// <summary>
/// Small helpers for building the menus in code, and the look they share: a chunky heading
/// font that echoes the logo's lettering, a cleaner body font, rounded panels with a coloured
/// accent, and pill-shaped badges.
///
/// Fonts are the system's own (SystemFont), so there is no font file to license; each lists
/// fallbacks, and Godot uses its built-in font if none is installed.
/// </summary>
public static class Ui
{
	public static readonly Color Waiting = new(1.0f, 0.85f, 0.3f);
	public static readonly Color Done = new(0.45f, 1.0f, 0.5f);
	public static readonly Color Gold = new(1.0f, 0.82f, 0.3f);
	public static readonly Color Ink = new(0.05f, 0.03f, 0.1f);
	public static readonly Color PanelColour = new(0.07f, 0.05f, 0.13f);

	static SystemFont heading, body;

	public static Font HeadingFont => heading ??= new SystemFont { FontNames = new[] { "Segoe UI Black", "Arial Black", "Impact" }, FontWeight = 900 };
	public static Font BodyFont => body ??= new SystemFont { FontNames = new[] { "Segoe UI Semibold", "Segoe UI", "Arial" }, FontWeight = 600 };

	public static Label Label(string text, int size, Color colour, HorizontalAlignment align = HorizontalAlignment.Center)
	{
		Label label = new() { Text = text, HorizontalAlignment = align, VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
		label.AddThemeFontOverride("font", BodyFont);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", colour);
		label.AddThemeConstantOverride("outline_size", Mathf.Max(2, size / 8));
		label.AddThemeColorOverride("font_outline_color", new Color(Ink, 0.85f));
		return label;
	}

	public static Label Label(string text, int size) => Label(text, size, Colors.White);

	/// <summary>A heading in the chunky font with a thick dark outline, like the logo's letters.</summary>
	public static Label Heading(string text, int size, Color colour, HorizontalAlignment align = HorizontalAlignment.Center)
	{
		Label label = Label(text, size, colour, align);
		label.AddThemeFontOverride("font", HeadingFont);
		label.AddThemeConstantOverride("outline_size", Mathf.Clamp(size / 5, 3, 14));
		label.AddThemeColorOverride("font_outline_color", Ink);
		return label;
	}

	public static StyleBoxFlat Style(Color background, Color border, int borderWidth = 3, int radius = 14) => new()
	{
		BgColor = background,
		CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius, CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
		BorderColor = border,
		BorderWidthLeft = borderWidth, BorderWidthRight = borderWidth, BorderWidthTop = borderWidth, BorderWidthBottom = borderWidth,
		ShadowColor = new Color(0, 0, 0, 0.35f),
		ShadowSize = 6,
		ShadowOffset = new Vector2(0, 4),
	};

	/// <summary>A dark rounded panel at a fixed rectangle.</summary>
	public static Panel Panel(Rect2 rect, Color? border = null, float alpha = 0.85f)
	{
		Panel panel = new() { Position = rect.Position, Size = rect.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
		panel.AddThemeStyleboxOverride("panel", Style(new Color(PanelColour, alpha), border ?? new Color(1, 1, 1, 0.12f)));
		return panel;
	}

	/// <summary>A filled pill with centred text: badges, button chips, mode tags.</summary>
	public static Panel Pill(Rect2 rect, string text, int size, Color fill, Color textColour, bool heading = false)
	{
		Panel pill = new() { Position = rect.Position, Size = rect.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
		pill.AddThemeStyleboxOverride("panel", Style(fill, new Color(Ink, 0.9f), 3, (int)(rect.Size.Y * 0.5f)));
		Label label = heading ? Heading(text, size, textColour) : Label(text, size, textColour);
		// The fill gives the text its contrast; an outline only clogs small letters.
		label.AddThemeConstantOverride("outline_size", 0);
		Place(label, 0, 0, rect.Size.X, rect.Size.Y);
		pill.AddChild(label);
		return pill;
	}

	public static void Place(Control control, float x, float y, float width, float height)
	{
		control.Position = new Vector2(x, y);
		control.Size = new Vector2(width, height);
	}

	public static TextureRect Image(Texture2D texture, Rect2 rect)
	{
		TextureRect image = new()
		{
			Texture = texture,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			Position = rect.Position,
			Size = rect.Size,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		return image;
	}

	/// <summary>A screen-sized root control on the magic backdrop.</summary>
	public static Control Screen(string name)
	{
		Control root = new() { Name = name, MouseFilter = Control.MouseFilterEnum.Ignore };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(new MagicBackdrop());
		return root;
	}
}
