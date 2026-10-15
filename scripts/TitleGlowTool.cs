using Godot;

/// <summary>
/// Builds assets/art/ui/title-start-glow.png: just the red letters of the "START" painted into
/// the title art, as a white mask the title screen pulses on top of the art. Run once, with
/// `-- --make-title-glow`, whenever title.png changes; the output is committed.
///
/// It scans only the area round the START orb, so the red of "WIZARD WARS" and the fire wizard's
/// robe are left out, and crops the result to the letters. It prints the crop's position in the
/// full-size art, which TitleScreen needs (<see cref="TitleScreen.GlowOrigin"/>).
/// </summary>
public static class TitleGlowTool
{
	const string Source = "res://assets/art/ui/title.png";
	const string Output = "res://assets/art/ui/title-start-glow.png";
	// Search area in title.png pixels: the START orb and a margin.
	static readonly Rect2I Search = new(1900, 2700, 1800, 800);

	public static int Run()
	{
		Image art = Image.LoadFromFile(ProjectSettings.GlobalizePath(Source));
		if (art == null) { GD.PrintErr($"TitleGlowTool: cannot read {Source}"); return 1; }
		art.Convert(Image.Format.Rgba8);

		Image mask = Image.CreateEmpty(Search.Size.X, Search.Size.Y, false, Image.Format.Rgba8);
		int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
		for (int y = 0; y < Search.Size.Y; y++)
			for (int x = 0; x < Search.Size.X; x++)
			{
				Color c = art.GetPixel(Search.Position.X + x, Search.Position.Y + y);
				// The START letters are a saturated pinkish red; the orb is pale blue, the
				// outline near-black. How red a pixel is becomes how strongly it glows, which
				// keeps the letters' anti-aliased edges soft.
				float redness = Mathf.Clamp((c.R - Mathf.Max(c.G, c.B * 0.9f) - 0.3f) / 0.25f, 0.0f, 1.0f) * c.A;
				if (redness <= 0.0f) { mask.SetPixel(x, y, new Color(1, 1, 1, 0)); continue; }
				mask.SetPixel(x, y, new Color(1, 1, 1, redness));
				minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
				maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
			}
		if (maxX < 0) { GD.PrintErr("TitleGlowTool: found no red letters in the search area"); return 1; }

		Rect2I letters = new(minX, minY, maxX - minX + 1, maxY - minY + 1);
		Image cropped = mask.GetRegion(letters);

		// A preview on black, beside the system temp folder, for checking what was picked up.
		Image preview = Image.CreateEmpty(letters.Size.X, letters.Size.Y, false, Image.Format.Rgba8);
		preview.Fill(Colors.Black);
		preview.BlendRect(cropped, new Rect2I(Vector2I.Zero, letters.Size), Vector2I.Zero);
		string previewPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "title-start-glow-preview.png");
		preview.SavePng(previewPath);
		GD.Print($"TitleGlowTool: preview at {previewPath}");
		Error error = cropped.SavePng(ProjectSettings.GlobalizePath(Output));
		Vector2I origin = Search.Position + letters.Position;
		GD.Print($"TitleGlowTool: wrote {Output} ({letters.Size.X}x{letters.Size.Y}) at title.png pixel {origin.X},{origin.Y}: {error}");
		return error == Error.Ok ? 0 : 1;
	}
}
