using Godot;

/// <summary>
/// Eight Elder Futhark runes as line strokes in a unit box (x about -0.4..0.4, y -1..1, +y down),
/// for anything that draws runes from code: the game icon's ritual circle (<see cref="IconTool"/>)
/// and King of the Rune's circle (<see cref="RuneHill"/>). The Storm Plateau stones carve their
/// own, laid out for the stone's face (<see cref="CoverArt"/>).
/// </summary>
public static class RuneGlyphs
{
	public static readonly Vector2[][][] All =
	{
		new[] { new Vector2[] { new(-0.3f, -1), new(-0.3f, 1) }, new Vector2[] { new(-0.3f, -0.45f), new(0.35f, 0), new(-0.3f, 0.45f) } }, // Thurisaz
		new[] { new Vector2[] { new(0.35f, -1), new(-0.35f, -0.1f), new(0.35f, 0.15f), new(-0.35f, 1) } }, // Sowilo
		new[] { new Vector2[] { new(-0.35f, -1), new(-0.35f, 1) }, new Vector2[] { new(0.35f, -1), new(0.35f, 1) }, new Vector2[] { new(-0.35f, -0.3f), new(0.35f, 0.3f) } }, // Hagalaz
		new[] { new Vector2[] { new(0, -0.8f), new(0, 1) }, new Vector2[] { new(-0.4f, -1), new(0, -0.3f), new(0.4f, -1) } }, // Algiz
		new[] { new Vector2[] { new(0, -1), new(0, 1) }, new Vector2[] { new(-0.4f, -0.5f), new(0, -1), new(0.4f, -0.5f) } }, // Tiwaz
		new[] { new Vector2[] { new(-0.3f, -1), new(-0.3f, 1) }, new Vector2[] { new(-0.3f, -1), new(0.35f, -0.4f), new(-0.3f, 0) }, new Vector2[] { new(-0.3f, 0), new(0.35f, 1) } }, // Raidho
		new[] { new Vector2[] { new(-0.3f, -1), new(-0.3f, 1) }, new Vector2[] { new(-0.3f, -0.5f), new(0.35f, -0.9f) }, new Vector2[] { new(-0.3f, 0), new(0.35f, -0.4f) } }, // Fehu
		new[] { new Vector2[] { new(-0.35f, -1), new(-0.35f, 1) }, new Vector2[] { new(0.35f, -1), new(0.35f, 1) }, new Vector2[] { new(-0.35f, -1), new(0.35f, 0) }, new Vector2[] { new(0.35f, -1), new(-0.35f, 0) } }, // Mannaz
	};

	/// <summary>Draws a rune on a canvas: centred at a point, turned, scaled, as polylines.</summary>
	public static void Draw(CanvasItem canvas, Vector2[][] rune, Vector2 at, float angle, float scale, Color colour, float width)
	{
		foreach (Vector2[] line in rune)
		{
			Vector2[] points = new Vector2[line.Length];
			for (int i = 0; i < line.Length; i++) points[i] = at + (line[i] * scale).Rotated(angle);
			canvas.DrawPolyline(points, colour, width, true);
		}
	}
}
