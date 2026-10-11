using Godot;

/// <summary>Small helpers for building the menus in code.</summary>
public static class Ui
{
	/// <summary>Player colours, matching the Unity cursor art: red, blue, green, yellow.</summary>
	public static readonly Color[] PlayerColours =
	{
		new(0.95f, 0.3f, 0.25f), new(0.3f, 0.55f, 1.0f), new(0.35f, 0.85f, 0.35f), new(1.0f, 0.85f, 0.25f),
	};

	public static readonly Color Waiting = new(1.0f, 0.85f, 0.3f);
	public static readonly Color Done = new(0.45f, 1.0f, 0.5f);

	public static Label Label(string text, int size, Color colour, HorizontalAlignment align = HorizontalAlignment.Center)
	{
		Label label = new() { Text = text, HorizontalAlignment = align, VerticalAlignment = VerticalAlignment.Center };
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", colour);
		label.AddThemeConstantOverride("outline_size", Mathf.Max(2, size / 8));
		label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.8f));
		return label;
	}

	public static Label Label(string text, int size) => Label(text, size, Colors.White);

	/// <summary>A dark rounded panel at a fixed rectangle.</summary>
	public static Panel Panel(Rect2 rect, Color? border = null, float alpha = 0.78f)
	{
		StyleBoxFlat style = new()
		{
			BgColor = new Color(0.05f, 0.05f, 0.08f, alpha),
			CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
			BorderColor = border ?? new Color(1, 1, 1, 0.15f),
			BorderWidthLeft = 3, BorderWidthRight = 3, BorderWidthTop = 3, BorderWidthBottom = 3,
		};
		Panel panel = new() { Position = rect.Position, Size = rect.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
		panel.AddThemeStyleboxOverride("panel", style);
		return panel;
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

	/// <summary>A screen-sized root control on a black background.</summary>
	public static Control Screen(string name)
	{
		Control root = new() { Name = name };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		ColorRect black = new() { Color = new Color(0.02f, 0.02f, 0.04f) };
		black.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(black);
		return root;
	}
}
