using System.Collections.Generic;
using System.IO;
using Godot;

/// <summary>
/// Builds the game's icon: a stylized "WW" behind a fireball, lit by it. Run once, with
/// `-- --make-icon`, whenever the source art changes; the outputs are committed.
///
/// The W's are cut from the logo's own lettering in title.png (the W of WIZARD), so the icon
/// matches the title art: the red letter is kept with its dark outline, and the book page it
/// is painted on is dropped. The fireball is the first frame of the hand-drawn fireball sheet,
/// turned to point up, in front of the W's. The W's red is warmed toward orange near the fire and
/// dimmed a little away from it, a subtle gradient as if the fireball lights them. Outputs icon.png (512 px) and icon.ico (256 down to 16 px, PNG-encoded
/// entries, which Windows Vista and later read).
/// </summary>
public static class IconTool
{
	const string Title = "res://assets/art/ui/title.png";
	const string FireballSheet = "res://assets/art/attacks/fireball-sheet.png";
	const int Size = 512;

	/// <summary>The W of WIZARD in title.png, with a margin for its outline.</summary>
	static readonly Rect2I LetterW = new(160, 1960, 620, 560);

	static string Global(string path) => ProjectSettings.GlobalizePath(path);

	public static int Run()
	{
		Image title = Image.LoadFromFile(Global(Title));
		Image sheet = Image.LoadFromFile(Global(FireballSheet));
		if (title == null || sheet == null) { GD.PrintErr("IconTool: cannot read the source art"); return 1; }
		title.Convert(Image.Format.Rgba8);
		sheet.Convert(Image.Format.Rgba8);

		Image letter = CutLetter(title.GetRegion(LetterW));
		SavePreview(letter, "icon-letter-preview.png");

		// The top frame of the sheet: the fireball flies right, so turning it clockwise puts
		// the ball at the bottom and the flames streaming up.
		Image fireball = sheet.GetRegion(new Rect2I(0, 0, sheet.GetWidth(), sheet.GetHeight() / 4));
		fireball.Rotate90(ClockDirection.Clockwise);
		fireball = Trim(fireball);

		Image icon = Image.CreateEmpty(Size, Size, false, Image.Format.Rgba8);
		icon.Fill(new Color(0, 0, 0, 0));

		// The fireball's size and place come first: it lights the W's and the backdrop.
		Vector2I fireSize = Fit(fireball.GetSize(), new Vector2I(290, 330));
		fireball.Resize(fireSize.X, fireSize.Y, Image.Interpolation.Lanczos);
		Vector2I firePlace = new((Size - fireSize.X) / 2, Size - fireSize.Y - 18);
		// The light sits in the flames just above the ball.
		Vector2 light = new(Size * 0.5f, firePlace.Y + fireSize.Y * 0.45f);
		Backdrop(icon, light);

		// "WW" behind: two big logo W's side by side in the middle of the icon, overlapping like
		// lettering; the fire rises in front of where they meet.
		Vector2I wSize = Fit(letter.GetSize(), new Vector2I(262, 250));
		letter.Resize(wSize.X, wSize.Y, Image.Interpolation.Lanczos);
		int overlap = wSize.X / 5;
		int left = (Size - (wSize.X * 2 - overlap)) / 2;
		int top = (Size - wSize.Y) / 2 - 34;
		// Each W is turned a little toward the fire, as if they stand round it: the left one's
		// right side and the right one's left side lean away.
		icon.BlendRect(Lit(TurnToward(letter, innerOnRight: true), new Vector2I(left, top), light), new Rect2I(Vector2I.Zero, wSize), new Vector2I(left, top));
		int right = left + wSize.X - overlap;
		icon.BlendRect(Lit(TurnToward(letter, innerOnRight: false), new Vector2I(right, top), light), new Rect2I(Vector2I.Zero, wSize), new Vector2I(right, top));

		// The fireball in front, and a few embers rising from it.
		icon.BlendRect(fireball, new Rect2I(Vector2I.Zero, fireSize), firePlace);
		Embers(icon, light);

		Error error = icon.SavePng(Global("res://icon.png"));
		GD.Print($"IconTool: wrote res://icon.png: {error}");
		SavePreview(icon, "icon-preview.png");
		WriteIco(icon, Global("res://icon.ico"), new[] { 256, 128, 64, 48, 32, 16 });
		GD.Print("IconTool: wrote res://icon.ico");
		return error == Error.Ok ? 0 : 1;
	}

	// ---- The backdrop ----

	static readonly Color Void = new(0.035f, 0.012f, 0.02f);
	static readonly Color Blood = new(0.32f, 0.04f, 0.03f);
	static readonly Color Rune = new(0.75f, 0.14f, 0.07f);

	/// <summary>
	/// A dark rounded square: near-black at the edges, a dim blood-red glow round the fire, a
	/// hard black rim, and a ritual circle of faintly burning Elder Futhark runes between two thin
	/// rings.
	/// </summary>
	static void Backdrop(Image icon, Vector2 light)
	{
		Vector2 centre = new(Size * 0.5f, Size * 0.5f);
		const float Corner = 84.0f, Inset = 6.0f;
		for (int y = 0; y < Size; y++)
			for (int x = 0; x < Size; x++)
			{
				Vector2 p = new(x + 0.5f, y + 0.5f);
				// Rounded square: how far outside its edge (negative inside).
				Vector2 q = (p - centre).Abs() - new Vector2(Size * 0.5f - Inset - Corner, Size * 0.5f - Inset - Corner);
				float outside = new Vector2(Mathf.Max(q.X, 0), Mathf.Max(q.Y, 0)).Length() + Mathf.Min(Mathf.Max(q.X, q.Y), 0) - Corner;
				if (outside > 1.0f) continue;
				float glow = Mathf.Clamp(1.0f - p.DistanceTo(light) / 300.0f, 0.0f, 1.0f);
				Color c = Void.Lerp(Blood, glow * glow);
				if (outside > -7.0f) c = Colors.Black;
				float ring = Mathf.Abs(p.DistanceTo(centre) - 214.0f), inner = Mathf.Abs(p.DistanceTo(centre) - 168.0f);
				if (ring < 2.2f || inner < 1.6f) c = c.Lerp(Rune, 0.55f);
				icon.SetPixel(x, y, new Color(c, Mathf.Clamp(1.0f - outside, 0.0f, 1.0f)));
			}

		// Runes round the circle, each upright toward the centre, faintly lit: brighter near the fire.
		Vector2[][][] runes = RuneGlyphs.All;
		const int Count = 16;
		for (int i = 0; i < Count; i++)
		{
			float angle = i * Mathf.Tau / Count + Mathf.Pi / Count;
			Vector2 at = centre + Vector2.FromAngle(angle) * 191.0f;
			float lit = 0.45f + 0.55f * Mathf.Clamp(1.0f - at.DistanceTo(light) / 330.0f, 0.0f, 1.0f);
			DrawStrokes(icon, runes[i % runes.Length], at, angle + Mathf.Pi * 0.5f, 13.0f, 2.6f, new Color(Rune * lit, 1.0f));
		}
	}

	/// <summary>Line strokes (in a unit box) drawn at a point, turned and scaled, blended over the image.</summary>
	static void DrawStrokes(Image image, Vector2[][] strokes, Vector2 at, float angle, float scale, float halfWidth, Color colour)
	{
		int reach = (int)(scale * 1.6f + halfWidth + 2);
		for (int y = (int)at.Y - reach; y <= at.Y + reach; y++)
			for (int x = (int)at.X - reach; x <= at.X + reach; x++)
			{
				if (x < 0 || y < 0 || x >= Size || y >= Size) continue;
				Vector2 local = (new Vector2(x + 0.5f, y + 0.5f) - at).Rotated(-angle) / scale;
				float distance = float.MaxValue;
				foreach (Vector2[] line in strokes)
					for (int k = 0; k + 1 < line.Length; k++)
						distance = Mathf.Min(distance, local.DistanceTo(Geometry2D.GetClosestPointToSegment(local, line[k], line[k + 1])));
				float cover = Mathf.Clamp(halfWidth + 0.5f - distance * scale, 0.0f, 1.0f);
				if (cover <= 0.0f) continue;
				Color under = image.GetPixel(x, y);
				image.SetPixel(x, y, new Color(under.Lerp(colour, cover), under.A));
			}
	}

	/// <summary>A few embers drifting up from the fire, over everything.</summary>
	static void Embers(Image icon, Vector2 light)
	{
		RandomNumberGenerator rng = new() { Seed = 9 };
		for (int n = 0; n < 14; n++)
		{
			Vector2 at = light + new Vector2(rng.RandfRange(-150, 150), rng.RandfRange(-230, -60));
			float r = rng.RandfRange(2.0f, 4.5f);
			for (int y = (int)(at.Y - r - 2); y <= at.Y + r + 2; y++)
				for (int x = (int)(at.X - r - 2); x <= at.X + r + 2; x++)
				{
					if (x < 0 || y < 0 || x >= Size || y >= Size) continue;
					float d = new Vector2(x + 0.5f, y + 0.5f).DistanceTo(at);
					float cover = Mathf.Clamp(r + 0.5f - d, 0.0f, 1.0f);
					if (cover <= 0.0f) continue;
					Color under = icon.GetPixel(x, y);
					if (under.A <= 0.0f) continue;
					Color ember = d < r * 0.5f ? new Color(1.0f, 0.85f, 0.4f) : new Color(1.0f, 0.45f, 0.1f);
					icon.SetPixel(x, y, new Color(under.Lerp(ember, cover), under.A));
				}
		}
	}

	/// <summary>
	/// The letter turned slightly about its upright, toward the middle: the side nearer the fire
	/// is foreshortened (up to <see cref="Turn"/> shorter, a light touch) and the whole letter a
	/// touch narrower. Its feet stay on the same baseline, as letters standing on the ground would.
	/// </summary>
	static Image TurnToward(Image letter, bool innerOnRight)
	{
		int w = letter.GetWidth(), h = letter.GetHeight();
		Image turned = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		turned.Fill(new Color(0, 0, 0, 0));
		const float Narrow = 0.06f;
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				// 0 at the outer edge, 1 at the edge nearer the fire.
				float inner = innerOnRight ? x / (float)(w - 1) : 1.0f - x / (float)(w - 1);
				float height = 1.0f - Turn * inner;
				// Squeeze toward the outer edge, then shorten from the top down to the baseline.
				float outerX = innerOnRight ? 0.0f : w - 1;
				float sx = outerX + (x - outerX) / (1.0f - Narrow);
				float sy = h - 1 - (h - 1 - y) / height;
				if (sx < 0 || sx > w - 1 || sy < 0 || sy > h - 1) continue;
				turned.SetPixel(x, y, Sample(letter, sx, sy));
			}
		return turned;
	}

	const float Turn = 0.14f;

	/// <summary>Bilinear sample, so the turned letter keeps a smooth edge.</summary>
	static Color Sample(Image image, float x, float y)
	{
		int x0 = (int)x, y0 = (int)y, x1 = Mathf.Min(x0 + 1, image.GetWidth() - 1), y1 = Mathf.Min(y0 + 1, image.GetHeight() - 1);
		float fx = x - x0, fy = y - y0;
		Color top = Premultiplied(image.GetPixel(x0, y0)).Lerp(Premultiplied(image.GetPixel(x1, y0)), fx);
		Color bottom = Premultiplied(image.GetPixel(x0, y1)).Lerp(Premultiplied(image.GetPixel(x1, y1)), fx);
		Color c = top.Lerp(bottom, fy);
		return c.A > 0.0f ? new Color(c.R / c.A, c.G / c.A, c.B / c.A, c.A) : new Color(0, 0, 0, 0);
	}

	static Color Premultiplied(Color c) => new(c.R * c.A, c.G * c.A, c.B * c.A, c.A);

	/// <summary>
	/// A copy of the letter lit by the fire at <paramref name="light"/> (icon pixels): its red fill
	/// warms toward orange the nearer it is, and the far parts dim slightly. The dark outline is
	/// left as it is, so the lettering keeps its shape.
	/// </summary>
	static Image Lit(Image letter, Vector2I place, Vector2 light)
	{
		Color warm = new(1.0f, 0.6f, 0.22f);
		Image lit = (Image)letter.Duplicate();
		for (int y = 0; y < lit.GetHeight(); y++)
			for (int x = 0; x < lit.GetWidth(); x++)
			{
				Color c = lit.GetPixel(x, y);
				if (c.A <= 0.0f || c.R < 0.35f || c.R - c.G < 0.2f) continue; // only the red fill
				float near = Mathf.Clamp(1.0f - (place + new Vector2(x, y)).DistanceTo(light) / 300.0f, 0.0f, 1.0f);
				Color shaded = c.Lerp(warm, 0.7f * near * near).Darkened(0.22f * (1.0f - near));
				lit.SetPixel(x, y, new Color(shaded, c.A));
			}
		return lit;
	}

	/// <summary>
	/// Keeps the red letter and the dark outline round it; drops the beige page. A dark pixel
	/// only counts as outline if it is close to red, so the page's ink writing is left out.
	/// </summary>
	static Image CutLetter(Image region)
	{
		int w = region.GetWidth(), h = region.GetHeight();
		bool[,] red = new bool[w, h];
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				Color c = region.GetPixel(x, y);
				red[x, y] = c.R > 0.45f && c.R - c.G > 0.3f && c.R - c.B > 0.25f;
			}

		// Keep only the largest red blob: the W itself, not a neighbouring letter's edge.
		red = LargestBlob(red, w, h);

		const int reach = 16; // outline thickness to allow, in title.png pixels
		Image letter = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				Color c = region.GetPixel(x, y);
				if (red[x, y]) { letter.SetPixel(x, y, c); continue; }
				float darkness = 1.0f - Mathf.Max(c.R, Mathf.Max(c.G, c.B));
				if (darkness > 0.45f && NearRed(red, x, y, w, h, reach))
					letter.SetPixel(x, y, new Color(c.R, c.G, c.B, Mathf.Clamp((darkness - 0.45f) / 0.3f, 0.0f, 1.0f)));
				else letter.SetPixel(x, y, new Color(0, 0, 0, 0));
			}
		return Trim(letter);
	}

	static bool[,] LargestBlob(bool[,] mask, int w, int h)
	{
		int[,] label = new int[w, h];
		int best = 0, bestSize = 0, next = 0;
		Queue<Vector2I> queue = new();
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				if (!mask[x, y] || label[x, y] != 0) continue;
				next++;
				int size = 0;
				label[x, y] = next;
				queue.Enqueue(new Vector2I(x, y));
				while (queue.Count > 0)
				{
					Vector2I p = queue.Dequeue();
					size++;
					foreach (Vector2I d in new[] { Vector2I.Left, Vector2I.Right, Vector2I.Up, Vector2I.Down })
					{
						Vector2I q = p + d;
						if (q.X < 0 || q.Y < 0 || q.X >= w || q.Y >= h || !mask[q.X, q.Y] || label[q.X, q.Y] != 0) continue;
						label[q.X, q.Y] = next;
						queue.Enqueue(q);
					}
				}
				if (size > bestSize) { bestSize = size; best = next; }
			}
		bool[,] result = new bool[w, h];
		for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) result[x, y] = label[x, y] == best;
		return result;
	}

	static bool NearRed(bool[,] red, int x, int y, int w, int h, int reach)
	{
		for (int dy = -reach; dy <= reach; dy += 2)
			for (int dx = -reach; dx <= reach; dx += 2)
			{
				int nx = x + dx, ny = y + dy;
				if (nx >= 0 && ny >= 0 && nx < w && ny < h && red[nx, ny] && dx * dx + dy * dy <= reach * reach) return true;
			}
		return false;
	}

	/// <summary>Crops away fully transparent borders.</summary>
	static Image Trim(Image image)
	{
		Rect2I used = image.GetUsedRect();
		return used.Size.X > 0 ? image.GetRegion(used) : image;
	}

	static Vector2I Fit(Vector2I size, Vector2I box)
	{
		float scale = Mathf.Min(box.X / (float)size.X, box.Y / (float)size.Y);
		return new Vector2I(Mathf.Max(1, (int)(size.X * scale)), Mathf.Max(1, (int)(size.Y * scale)));
	}

	static void SavePreview(Image image, string name)
	{
		Image preview = Image.CreateEmpty(image.GetWidth(), image.GetHeight(), false, Image.Format.Rgba8);
		preview.Fill(new Color(0.15f, 0.12f, 0.25f));
		preview.BlendRect(image, new Rect2I(Vector2I.Zero, image.GetSize()), Vector2I.Zero);
		string path = Path.Combine(Path.GetTempPath(), name);
		preview.SavePng(path);
		GD.Print($"IconTool: preview at {path}");
	}

	/// <summary>An .ico holding PNG-encoded images, one per size.</summary>
	static void WriteIco(Image source, string path, int[] sizes)
	{
		List<byte[]> pngs = new();
		foreach (int size in sizes)
		{
			Image scaled = (Image)source.Duplicate();
			scaled.Resize(size, size, Image.Interpolation.Lanczos);
			pngs.Add(scaled.SavePngToBuffer());
		}

		using FileStream file = File.Create(path);
		using BinaryWriter writer = new(file);
		writer.Write((ushort)0);            // reserved
		writer.Write((ushort)1);            // type: icon
		writer.Write((ushort)sizes.Length); // image count
		int offset = 6 + 16 * sizes.Length;
		for (int i = 0; i < sizes.Length; i++)
		{
			writer.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i])); // 0 means 256
			writer.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
			writer.Write((byte)0);   // palette colours
			writer.Write((byte)0);   // reserved
			writer.Write((ushort)1); // colour planes
			writer.Write((ushort)32); // bits per pixel
			writer.Write(pngs[i].Length);
			writer.Write(offset);
			offset += pngs[i].Length;
		}
		foreach (byte[] png in pngs) writer.Write(png);
	}
}
