using System.Collections.Generic;
using System.IO;
using Godot;

/// <summary>
/// Builds the game's icon: a stylized "WW" above a fireball. Run once, with
/// `-- --make-icon`, whenever the source art changes; the outputs are committed.
///
/// The W's are cut from the logo's own lettering in title.png (the W of WIZARD), so the icon
/// matches the title art: the red letter is kept with its dark outline, and the book page it
/// is painted on is dropped. The fireball is the first frame of the hand-drawn fireball sheet,
/// turned to point up. Outputs icon.png (512 px) and icon.ico (256 down to 16 px, PNG-encoded
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

		// Fireball: the lower part of the icon, centred.
		Vector2I fireSize = Fit(fireball.GetSize(), new Vector2I(330, 350));
		fireball.Resize(fireSize.X, fireSize.Y, Image.Interpolation.Lanczos);
		icon.BlendRect(fireball, new Rect2I(Vector2I.Zero, fireSize), new Vector2I((Size - fireSize.X) / 2, Size - fireSize.Y - 6));

		// "WW": two logo W's side by side across the top, overlapping a touch like lettering.
		Vector2I wSize = Fit(letter.GetSize(), new Vector2I(224, 190));
		letter.Resize(wSize.X, wSize.Y, Image.Interpolation.Lanczos);
		int overlap = wSize.X / 7;
		int left = (Size - (wSize.X * 2 - overlap)) / 2;
		icon.BlendRect(letter, new Rect2I(Vector2I.Zero, wSize), new Vector2I(left, 18));
		icon.BlendRect(letter, new Rect2I(Vector2I.Zero, wSize), new Vector2I(left + wSize.X - overlap, 18));

		Error error = icon.SavePng(Global("res://icon.png"));
		GD.Print($"IconTool: wrote res://icon.png: {error}");
		SavePreview(icon, "icon-preview.png");
		WriteIco(icon, Global("res://icon.ico"), new[] { 256, 128, 64, 48, 32, 16 });
		GD.Print("IconTool: wrote res://icon.ico");
		return error == Error.Ok ? 0 : 1;
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
