using System;
using Godot;
using Painter = CoverArt.Painter;

/// <summary>
/// Makes the art for the Shadow, Nature and Wind wizards, in the style of the hand-drawn four.
/// Run once, with `-- --make-wizards`; the PNGs are committed.
///
/// - Portraits: the Electricity wizard is the template (the four share one figure). Its violet
///   robe and hat are recoloured, keeping their shading, and its lightning eyebrows and beard are
///   painted out. Then each new wizard gets its own face and staff orb in flat, outlined shapes:
///   - Shadow: a face lost in shadow with two glowing eyes, wisps of smoke for a beard, and a
///     crescent moon in a night-dark orb
///   - Nature: leaf eyebrows, a beard of leaves, and a leaf in a green orb
///   - Wind: curling white brows, a cloud of a beard, and a white spiral in a mint orb
/// - Targeters: the yellow one recoloured.
/// - Projectile sheets (four frames, like the hand-drawn mudball and fireball sheets): a smoky
///   shadow bolt, a spinning thorn with a leaf in its wake, and a crescent gust with wind streaks.
/// - Ultimate icons (220 px, like the other three): Eclipse, Overgrowth and Tempest.
/// </summary>
public static class WizardArtTool
{
	static readonly Color Ink = new(0.04f, 0.03f, 0.05f);
	static readonly Color Skin = new(1.0f, 0.737f, 0.576f);
	/// <summary>The template's robe violet, whose shading the recolour keeps.</summary>
	static readonly Color TemplateRobe = new(0.333f, 0.043f, 0.569f);

	// Where things are in the 1200 x 1165 template.
	static readonly Vector2 OrbCentre = new(215, 250);
	const float OrbRadius = 104.0f;
	static readonly Rect2I FaceMarks = new(480, 555, 350, 260);
	const float ChinY = 686.0f;

	public static int Run()
	{
		int failures = 0;
		failures += Portrait("shadow", new Color(0.24f, 0.23f, 0.3f), ShadowFace, ShadowOrb);
		failures += Portrait("nature", new Color(0.2f, 0.55f, 0.18f), NatureFace, NatureOrb);
		failures += Portrait("wind", new Color(0.3f, 0.72f, 0.64f), WindFace, WindOrb);
		failures += Targeter("ash", new Color(0.72f, 0.7f, 0.82f));
		failures += Targeter("leaf", new Color(0.45f, 0.85f, 0.3f));
		failures += Targeter("mint", new Color(0.45f, 0.95f, 0.8f));
		failures += Save(ShadowBoltSheet(), "res://assets/art/attacks/shadow-bolt-sheet.png");
		failures += Save(ThornSheet(), "res://assets/art/attacks/thorn-sheet.png");
		failures += Save(GustSheet(), "res://assets/art/attacks/gust-sheet.png");
		failures += Save(EclipseIcon(), "res://assets/art/ultimates/eclipse.png");
		failures += Save(OvergrowthIcon(), "res://assets/art/ultimates/overgrowth.png");
		failures += Save(TempestIcon(), "res://assets/art/ultimates/tempest.png");
		return failures;
	}

	static int Save(Image image, string path)
	{
		Error error = image.SavePng(ProjectSettings.GlobalizePath(path));
		GD.Print($"WizardArtTool: wrote {path}: {error}");
		return error == Error.Ok ? 0 : 1;
	}

	// ---- Portraits ----

	static int Portrait(string name, Color robe, Action<Painter> face, Action<Painter> orb)
	{
		Image image = Image.LoadFromFile(ProjectSettings.GlobalizePath("res://assets/art/wizards/electricity.png"));
		image.Convert(Image.Format.Rgba8);
		int w = image.GetWidth(), h = image.GetHeight();

		// The face marks (yellow, with their dark outline): painted out, as skin on the face and
		// robe below the chin. The new face covers the seam.
		bool[,] marks = new bool[w, h];
		for (int y = FaceMarks.Position.Y; y < FaceMarks.End.Y; y++)
			for (int x = FaceMarks.Position.X; x < FaceMarks.End.X; x++)
			{
				Color c = image.GetPixel(x, y);
				if (c.A > 0.5f && c.R > 0.7f && c.G > 0.7f && c.B < 0.65f) marks[x, y] = true;
			}
		const int Grow = 8;
		bool[,] grown = new bool[w, h];
		for (int y = FaceMarks.Position.Y; y < FaceMarks.End.Y; y++)
			for (int x = FaceMarks.Position.X; x < FaceMarks.End.X; x++)
			{
				if (!marks[x, y]) continue;
				for (int dy = -Grow; dy <= Grow; dy++)
					for (int dx = -Grow; dx <= Grow; dx++)
						if (dx * dx + dy * dy <= Grow * Grow) grown[x + dx, y + dy] = true;
			}

		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				Color c = image.GetPixel(x, y);
				if (grown[x, y] && c.A > 0.5f) { image.SetPixel(x, y, y < ChinY ? Skin : robe); continue; }
				if (c.A <= 0.0f || new Vector2(x, y).DistanceTo(OrbCentre) < OrbRadius + 6) continue;
				// Violet robe and hat: recoloured, keeping each pixel's shading against the template violet.
				if (c.B > c.G + 0.25f && c.R < c.B)
				{
					float shade = (c.R + c.G + c.B) / (TemplateRobe.R + TemplateRobe.G + TemplateRobe.B);
					image.SetPixel(x, y, new Color(Mathf.Min(1, robe.R * shade), Mathf.Min(1, robe.G * shade), Mathf.Min(1, robe.B * shade), c.A));
				}
			}

		Painter painter = new(image);
		face(painter);
		// The orb: cleared, then the new one in its place.
		for (int y = (int)(OrbCentre.Y - OrbRadius - 26); y <= OrbCentre.Y + OrbRadius + 26; y++)
			for (int x = (int)(OrbCentre.X - OrbRadius - 26); x <= OrbCentre.X + OrbRadius + 26; x++)
				if (x >= 0 && y >= 0 && new Vector2(x, y).DistanceTo(OrbCentre) <= OrbRadius + 24 && y < OrbCentre.Y + OrbRadius - 10) image.SetPixel(x, y, new Color(0, 0, 0, 0));
		orb(painter);
		return Save(image, $"res://assets/art/wizards/{name}.png");
	}

	static Rect2I Around(Vector2 centre, float reach) => new((int)(centre.X - reach), (int)(centre.Y - reach), (int)(reach * 2), (int)(reach * 2));

	static void Disc(Painter painter, Vector2 centre, Vector2 radii, Color fill, float outline = 7.0f) =>
		painter.Layer(p => CoverArt.Ellipse(p, centre, radii), _ => fill, Ink, outline, Around(centre, Mathf.Max(radii.X, radii.Y) + 4));

	/// <summary>A pointed leaf, tip one way and stem the other, with a centre vein.</summary>
	static void Leaf(Painter painter, Vector2 centre, float length, float width, float angle, Color fill, Color vein)
	{
		Vector2 along = Vector2.FromAngle(angle), across = along.Orthogonal();
		Vector2[] lens = LensPoints(centre, along, across, length, width);
		painter.Layer(p => CoverArt.Polygon(p, lens), p =>
		{
			float side = (p - centre).Dot(across);
			return Mathf.Abs(side) < 2.5f ? vein : fill;
		}, Ink, 5.0f, Around(centre, length));
	}

	static Vector2[] LensPoints(Vector2 centre, Vector2 along, Vector2 across, float length, float width)
	{
		Vector2[] points = new Vector2[24];
		for (int i = 0; i < 12; i++)
		{
			float t = i / 11.0f;
			float x = Mathf.Lerp(-length * 0.5f, length * 0.5f, t), y = Mathf.Sin(t * Mathf.Pi) * width;
			points[i] = centre + along * x + across * y;
			points[23 - i] = centre + along * x - across * y;
		}
		return points;
	}

	/// <summary>A spiral curl as a stroke: white with an outline.</summary>
	static void Curl(Painter painter, Vector2 centre, float radius, float turns, float startAngle, float width, Color colour, bool clockwise = true)
	{
		Vector2[] line = new Vector2[40];
		for (int i = 0; i < line.Length; i++)
		{
			float t = i / (float)(line.Length - 1);
			float angle = startAngle + (clockwise ? 1 : -1) * t * turns * Mathf.Tau;
			line[i] = centre + Vector2.FromAngle(angle) * radius * (1.0f - t * 0.8f);
		}
		Vector2[][] strokes = { line };
		painter.Layer(p => CoverArt.Strokes(p, strokes, width), _ => colour, Ink, 4.0f, Around(centre, radius + width + 4));
	}

	// ---- Faces ----

	static void ShadowFace(Painter painter)
	{
		Image image = painter.Image;
		Color dusk = new(0.16f, 0.14f, 0.2f);
		// The face itself falls into shadow under the hat.
		for (int y = 480; y < 720; y++)
			for (int x = 430; x < 870; x++)
			{
				Color c = image.GetPixel(x, y);
				if (c.A > 0.5f && c.R > 0.85f && c.G > 0.6f && c.G < 0.85f && c.B > 0.45f && c.B < 0.7f) image.SetPixel(x, y, dusk);
			}
		// Two eyes glowing out of the dark.
		Color glow = new(0.86f, 0.78f, 1.0f), core = new(1.0f, 1.0f, 1.0f);
		foreach ((Vector2 at, float tilt) in new[] { (new Vector2(582, 612), 0.22f), (new Vector2(718, 612), -0.22f) })
		{
			Vector2[] eye = LensPoints(at, Vector2.FromAngle(tilt), Vector2.FromAngle(tilt).Orthogonal(), 92, 17);
			painter.Layer(p => CoverArt.Polygon(p, eye), p => (p - at).Length() < 11 ? core : glow, Ink, 4.0f, Around(at, 60));
		}
		// A beard of smoke: dark wisps curling down from the chin, covering where the old beard was.
		Color smoke = new(0.2f, 0.19f, 0.26f), smokeLight = new(0.3f, 0.29f, 0.37f);
		// One tapering tendril drifting down and to the side: a run of shrinking ellipses joined
		// into a single outlined shape, lighter on its upper edge.
		(Vector2 at, Vector2 r)[] wisp = new (Vector2, Vector2)[9];
		for (int i = 0; i < wisp.Length; i++)
		{
			float t = i / (float)(wisp.Length - 1);
			wisp[i] = (new Vector2(650 + Mathf.Sin(t * Mathf.Pi * 1.3f) * 34, 694 + t * 130), new Vector2(Mathf.Lerp(62, 12, t), Mathf.Lerp(32, 10, t)));
		}
		painter.Layer(p => Union(p, wisp), p => Union(p + new Vector2(-6, -9), wisp) > 0.0f ? smokeLight : smoke, Ink, 6.0f, new Rect2I(560, 640, 180, 210));
		Curl(painter, new Vector2(654, 832), 18, 0.9f, Mathf.Pi, 5.0f, smokeLight, false);
	}

	static void NatureFace(Painter painter)
	{
		Color leaf = new(0.38f, 0.78f, 0.27f), vein = new(0.2f, 0.5f, 0.15f), dark = new(0.27f, 0.62f, 0.2f);
		// Leaf eyebrows, angled up and out.
		Leaf(painter, new Vector2(578, 605), 132, 26, 0.25f + Mathf.Pi, leaf, vein);
		Leaf(painter, new Vector2(722, 605), 132, 26, -0.25f, leaf, vein);
		// A beard of overlapping leaves hanging from the chin.
		Leaf(painter, new Vector2(612, 728), 118, 32, Mathf.Pi * 0.62f, dark, vein);
		Leaf(painter, new Vector2(688, 728), 118, 32, Mathf.Pi * 0.38f, dark, vein);
		Leaf(painter, new Vector2(650, 748), 130, 36, Mathf.Pi * 0.5f, leaf, vein);
		Leaf(painter, new Vector2(650, 695), 90, 30, 0.0f, leaf, vein);
	}

	static void WindFace(Painter painter)
	{
		Color white = new(0.97f, 0.99f, 1.0f), cloudShade = new(0.8f, 0.9f, 0.92f);
		// Curling brows.
		Curl(painter, new Vector2(565, 606), 34, 0.85f, Mathf.Pi, 9.0f, white, false);
		Curl(painter, new Vector2(735, 606), 34, 0.85f, 0.0f, 9.0f, white);
		// A cloud of a beard: puffs, the lower ones shaded.
		// One billowing silhouette: a broad puff with a scalloped lower edge and a tuft at the tip.
		(Vector2 at, Vector2 r)[] cloud =
		{
			(new Vector2(650, 710), new Vector2(86, 44)), (new Vector2(586, 740), new Vector2(30, 28)), (new Vector2(628, 758), new Vector2(31, 29)),
			(new Vector2(672, 758), new Vector2(31, 29)), (new Vector2(714, 740), new Vector2(30, 28)), (new Vector2(650, 792), new Vector2(24, 22)),
		};
		painter.Layer(p => Union(p, cloud), p => p.Y > 752 ? cloudShade : white, Ink, 6.0f, new Rect2I(540, 650, 220, 180));
		Curl(painter, new Vector2(662, 822), 20, 1.1f, Mathf.Pi * 0.5f, 6.0f, white);
	}

	/// <summary>Several ellipses as one shape: the distance to the nearest.</summary>
	static float Union(Vector2 p, (Vector2 at, Vector2 r)[] parts)
	{
		float d = float.MaxValue;
		foreach ((Vector2 at, Vector2 r) in parts) d = Mathf.Min(d, CoverArt.Ellipse(p, at, r));
		return d;
	}

	// ---- Orbs ----

	static void OrbBase(Painter painter, Color fill, Color rim) =>
		painter.Layer(p => CoverArt.Ellipse(p, OrbCentre, new Vector2(OrbRadius, OrbRadius)), p => (p - OrbCentre).Length() > OrbRadius - 16 ? rim : fill, Ink, 8.0f, Around(OrbCentre, OrbRadius + 6));

	static void ShadowOrb(Painter painter)
	{
		OrbBase(painter, new Color(0.13f, 0.1f, 0.2f), new Color(0.24f, 0.18f, 0.36f));
		// A pale crescent moon and two stars.
		Vector2 moon = OrbCentre + new Vector2(-6, 0);
		painter.Layer(p => Mathf.Max(CoverArt.Ellipse(p, moon, new Vector2(56, 56)), -CoverArt.Ellipse(p, moon + new Vector2(26, -14), new Vector2(50, 50))),
			_ => new Color(0.9f, 0.86f, 1.0f), Ink, 4.0f, Around(OrbCentre, OrbRadius));
		foreach (Vector2 star in new[] { OrbCentre + new Vector2(46, 36), OrbCentre + new Vector2(30, -50) })
			Disc(painter, star, new Vector2(9, 9), new Color(0.95f, 0.9f, 1.0f), 3.0f);
	}

	static void NatureOrb(Painter painter)
	{
		OrbBase(painter, new Color(0.2f, 0.45f, 0.16f), new Color(0.27f, 0.58f, 0.2f));
		Leaf(painter, OrbCentre + new Vector2(4, -4), 150, 40, -0.8f, new Color(0.55f, 0.9f, 0.36f), new Color(0.25f, 0.55f, 0.18f));
	}

	static void WindOrb(Painter painter)
	{
		OrbBase(painter, new Color(0.3f, 0.72f, 0.64f), new Color(0.42f, 0.85f, 0.76f));
		Curl(painter, OrbCentre, 78, 1.6f, 0.0f, 9.0f, new Color(0.97f, 1.0f, 1.0f));
	}

	// ---- Targeters ----

	static int Targeter(string name, Color colour)
	{
		Image image = Image.LoadFromFile(ProjectSettings.GlobalizePath("res://assets/art/ui/targeter-yellow.png"));
		image.Convert(Image.Format.Rgba8);
		Color pale = colour.Lerp(Colors.White, 0.65f);
		for (int y = 0; y < image.GetHeight(); y++)
			for (int x = 0; x < image.GetWidth(); x++)
			{
				Color c = image.GetPixel(x, y);
				if (c.A <= 0.0f) continue;
				// Strong yellow is the line; pale yellow the fill.
				Color to = c.B < 0.45f ? colour : pale;
				image.SetPixel(x, y, new Color(to, c.A));
			}
		return Save(image, $"res://assets/art/ui/targeter-{name}.png");
	}

	// ---- Projectile sheets: four frames stacked, each 600 x 240, flying right ----

	const int FrameW = 600, FrameH = 240;

	static Image Sheet(Action<Painter, int, Vector2> frame)
	{
		Image sheet = Image.CreateEmpty(FrameW, FrameH * 4, false, Image.Format.Rgba8);
		Painter painter = new(sheet);
		for (int f = 0; f < 4; f++) frame(painter, f, new Vector2(0, f * FrameH));
		return sheet;
	}

	/// <summary>A dark ball trailing puffs of smoke that billow differently each frame.</summary>
	static Image ShadowBoltSheet() => Sheet((painter, f, at) =>
	{
		Color puff = new(0.24f, 0.2f, 0.32f), puffLight = new(0.36f, 0.3f, 0.46f), ball = new(0.13f, 0.09f, 0.2f), swirl = new(0.62f, 0.5f, 0.9f);
		Random rng = new(10 + f);
		for (int i = 0; i < 7; i++)
		{
			float x = 430 - i * 58 - rng.Next(18), y = 120 + (float)Math.Sin(i * 1.3 + f * 1.6) * 26;
			float r = 46 - i * 5 + rng.Next(8);
			Vector2 c = at + new Vector2(x, y);
			painter.Layer(p => CoverArt.Ellipse(p, c, new Vector2(r, r * 0.85f)), p => p.Y < c.Y - r * 0.2f ? puffLight : puff, Ink, 5.0f, Around(c, r + 6));
		}
		Vector2 centre = at + new Vector2(495, 120);
		painter.Layer(p => CoverArt.Ellipse(p, centre, new Vector2(84, 84)), _ => ball, Ink, 7.0f, Around(centre, 92));
		Vector2[][] spiral = { SpiralPoints(centre, 58, 1.3f, f * 0.8f) };
		painter.Layer(p => CoverArt.Strokes(p, spiral, 7.0f), _ => swirl, Ink, 0.0f, Around(centre, 80));
	});

	static Vector2[] SpiralPoints(Vector2 centre, float radius, float turns, float start)
	{
		Vector2[] line = new Vector2[30];
		for (int i = 0; i < line.Length; i++)
		{
			float t = i / (float)(line.Length - 1);
			line[i] = centre + Vector2.FromAngle(start + t * turns * Mathf.Tau) * radius * (1.0f - t * 0.85f);
		}
		return line;
	}

	/// <summary>A green thorn, point first, with a leaf tumbling in its wake.</summary>
	static Image ThornSheet() => Sheet((painter, f, at) =>
	{
		Color thorn = new(0.42f, 0.7f, 0.22f), thornDark = new(0.28f, 0.5f, 0.15f), leaf = new(0.5f, 0.86f, 0.32f);
		// Speed streaks.
		for (int i = 0; i < 3; i++)
		{
			float y = 82 + i * 38, x0 = 60 + ((i * 70 + f * 40) % 120);
			Vector2[][] streak = { new[] { at + new Vector2(x0, y), at + new Vector2(x0 + 150, y) } };
			painter.Layer(p => CoverArt.Strokes(p, streak, 5.0f), _ => new Color(0.75f, 0.92f, 0.6f), Ink, 3.0f, new Rect2I((int)(at.X + x0 - 12), (int)(at.Y + y - 14), 180, 28));
		}
		Vector2 leafAt = at + new Vector2(250, 120 + (f % 2 == 0 ? -18 : 18));
		Leaf(painter, leafAt, 96, 22, f * Mathf.Pi * 0.5f + 0.4f, leaf, thornDark);
		Vector2[] body = { at + new Vector2(590, 120), at + new Vector2(430, 78), at + new Vector2(330, 96), at + new Vector2(310, 120), at + new Vector2(330, 144), at + new Vector2(430, 162) };
		painter.Layer(p => CoverArt.Polygon(p, body), p => p.Y > at.Y + 120 ? thornDark : thorn, Ink, 6.0f, new Rect2I((int)at.X + 290, (int)at.Y + 60, 310, 120));
	});

	/// <summary>A pale crescent of air with streaks flowing round it, shifting each frame.</summary>
	static Image GustSheet() => Sheet((painter, f, at) =>
	{
		Color air = new(0.88f, 1.0f, 0.97f), mint = new(0.45f, 0.9f, 0.78f);
		for (int i = 0; i < 4; i++)
		{
			float y = 60 + i * 40 + (f % 2) * 8, x0 = 40 + ((i * 53 + f * 47) % 110);
			Vector2[][] streak = { WavyLine(at + new Vector2(x0, y), 230, 8, f + i) };
			painter.Layer(p => CoverArt.Strokes(p, streak, 6.0f), _ => i % 2 == 0 ? air : mint, Ink, 4.0f, new Rect2I((int)(at.X + x0 - 14), (int)(at.Y + y - 26), 260, 52));
		}
		Vector2 c = at + new Vector2(470, 120);
		// The crescent: a big disc with a smaller one cut out of its back.
		painter.Layer(p => Mathf.Max(CoverArt.Ellipse(p, c, new Vector2(110, 108)), -CoverArt.Ellipse(p, c + new Vector2(-62, 0), new Vector2(100, 112))),
			p => p.X > c.X + 60 ? air : mint, Ink, 6.0f, Around(c, 120));
	});

	static Vector2[] WavyLine(Vector2 from, float length, float wave, int phase)
	{
		Vector2[] line = new Vector2[20];
		for (int i = 0; i < line.Length; i++)
		{
			float t = i / (float)(line.Length - 1);
			line[i] = from + new Vector2(t * length, Mathf.Sin(t * Mathf.Tau * 1.5f + phase) * wave);
		}
		return line;
	}

	// ---- Ultimate icons, 220 px ----

	static Image Icon(Action<Painter, Vector2> draw)
	{
		Image icon = Image.CreateEmpty(220, 220, false, Image.Format.Rgba8);
		draw(new Painter(icon), new Vector2(110, 110));
		return icon;
	}

	/// <summary>A black sun with a pale corona.</summary>
	static Image EclipseIcon() => Icon((painter, c) =>
	{
		for (int i = 0; i < 12; i++)
		{
			float angle = i * Mathf.Tau / 12;
			Vector2 tip = c + Vector2.FromAngle(angle) * 102, side = Vector2.FromAngle(angle).Orthogonal() * 16;
			Vector2[] ray = { c + Vector2.FromAngle(angle) * 60 - side, tip, c + Vector2.FromAngle(angle) * 60 + side };
			painter.Layer(p => CoverArt.Polygon(p, ray), _ => new Color(0.82f, 0.74f, 1.0f), Ink, 4.0f);
		}
		painter.Layer(p => CoverArt.Ellipse(p, c, new Vector2(70, 70)), _ => new Color(0.86f, 0.8f, 1.0f), Ink, 5.0f);
		painter.Layer(p => CoverArt.Ellipse(p, c + new Vector2(8, -4), new Vector2(60, 60)), _ => new Color(0.08f, 0.06f, 0.12f), Ink, 4.0f);
	});

	/// <summary>A ring of thorny vine round a big leaf.</summary>
	static Image OvergrowthIcon() => Icon((painter, c) =>
	{
		Color vine = new(0.3f, 0.6f, 0.2f);
		Vector2[] ring = new Vector2[41];
		for (int i = 0; i < ring.Length; i++) ring[i] = c + Vector2.FromAngle(i * Mathf.Tau / 40) * (84 + Mathf.Sin(i * 1.7f) * 6);
		Vector2[][] strokes = { ring };
		painter.Layer(p => CoverArt.Strokes(p, strokes, 9.0f), _ => vine, Ink, 4.0f);
		for (int i = 0; i < 10; i++)
		{
			float angle = i * Mathf.Tau / 10 + 0.2f;
			Vector2 b = c + Vector2.FromAngle(angle) * 86, side = Vector2.FromAngle(angle + Mathf.Pi * 0.5f) * 8;
			Vector2[] spike = { b - side, b + Vector2.FromAngle(angle) * 24, b + side };
			painter.Layer(p => CoverArt.Polygon(p, spike), _ => new Color(0.2f, 0.42f, 0.14f), Ink, 3.0f);
		}
		Leaf(painter, c, 120, 34, -0.7f, new Color(0.5f, 0.86f, 0.32f), new Color(0.24f, 0.52f, 0.16f));
	});

	/// <summary>A whirl of wind lines.</summary>
	static Image TempestIcon() => Icon((painter, c) =>
	{
		for (int i = 0; i < 3; i++)
		{
			Vector2[][] arm = { SpiralPoints(c, 96 - i * 6, 0.75f, i * Mathf.Tau / 3) };
			painter.Layer(p => CoverArt.Strokes(p, arm, 11.0f), _ => i == 0 ? new Color(0.92f, 1.0f, 0.98f) : new Color(0.45f, 0.92f, 0.78f), Ink, 4.0f);
		}
		painter.Layer(p => CoverArt.Ellipse(p, c, new Vector2(22, 22)), _ => new Color(0.92f, 1.0f, 0.98f), Ink, 4.0f);
	});
}
