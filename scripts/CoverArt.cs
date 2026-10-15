using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Generated art for the stages' cover pieces, in the game's flat cartoon style: each piece is a
/// stack of shapes, every shape a few solid colour bands inside a thick dark outline, with no
/// soft gradients (matching the woodland art and the wizards).
///
/// Shapes are signed-distance functions in texture pixels (negative inside). Layers are painted
/// back to front, so a front shape's outline crosses the shapes behind it, as in the hand-drawn
/// art. Each texture is built once and cached.
/// </summary>
public static class CoverArt
{
	public readonly struct Piece
	{
		public readonly Texture2D Texture;
		/// <summary>The texture pixel that sits on the cover's footprint centre (the node origin).</summary>
		public readonly Vector2 Anchor;
		/// <summary>Sprite scale, so the art is the intended size in the arena.</summary>
		public readonly float Scale;

		public Piece(Texture2D texture, Vector2 anchor, float scale)
		{
			Texture = texture;
			Anchor = anchor;
			Scale = scale;
		}
	}

	delegate float Sdf(Vector2 p);
	delegate Color Fill(Vector2 p);

	static readonly Dictionary<string, Piece> cache = new();

	/// <summary>The art for one piece. <paramref name="variant"/> picks between versions of a kind (each obelisk its own rune).</summary>
	public static Piece For(string kind, int variant = 0)
	{
		string key = kind + variant;
		if (cache.TryGetValue(key, out Piece piece)) return piece;
		piece = kind switch
		{
			"stump" => Stump(),
			"crystal" => Crystal(),
			"saguaro" => Saguaro(),
			"obelisk" => Obelisk(variant),
			_ => throw new ArgumentException($"No cover art for '{kind}'"),
		};
		cache[key] = piece;
		return piece;
	}

	// ---- Painting ----

	const float OutlinePx = 4.0f;

	sealed class Painter
	{
		readonly Image image;
		readonly int width, height;

		public Painter(int width, int height)
		{
			this.width = width;
			this.height = height;
			image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
		}

		/// <summary>Paints one shape over what is already there: fill inside, outline at the edge.</summary>
		public void Layer(Sdf shape, Fill fill, Color outline, float outlinePx = OutlinePx)
		{
			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					Vector2 p = new(x + 0.5f, y + 0.5f);
					float d = shape(p);
					if (d > 0.5f) continue;
					// One pixel of anti-aliasing on the outside edge only.
					float coverage = Mathf.Clamp(0.5f - d, 0.0f, 1.0f);
					Color colour = d > -outlinePx ? outline : fill(p);
					Color under = image.GetPixel(x, y);
					image.SetPixel(x, y, under.A <= 0.0f ? new Color(colour, coverage) : under.Lerp(new Color(colour, 1.0f), coverage));
				}
		}

		/// <summary>
		/// Cuts a groove into what is already painted, lit from the top left: the stone inside is
		/// darkened, the inner wall nearest the light falls into deep shadow, the far wall catches
		/// the light. Colour comes from the stone itself, so the carving reads as cut, not painted.
		/// </summary>
		public void Carve(Sdf groove, Color shadow)
		{
			Vector2 towardLight = new Vector2(-1, -1).Normalized() * 2.6f;
			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					Color under = image.GetPixel(x, y);
					if (under.A <= 0.0f) continue;
					Vector2 p = new(x + 0.5f, y + 0.5f);
					float d = groove(p);
					if (d > 0.8f) continue;
					Color cut;
					if (d > 0.0f) cut = under.Darkened(0.2f);                                // the crisp edge of the cut
					else if (groove(p + towardLight) > 0.0f) cut = shadow;                  // inner wall in shadow
					else if (groove(p - towardLight) > 0.0f) cut = under.Lightened(0.32f);   // inner wall in the light
					else cut = under.Darkened(0.55f);                                       // the floor of the groove
					image.SetPixel(x, y, new Color(cut, under.A));
				}
		}

		public Texture2D Finish() => ImageTexture.CreateFromImage(image);
	}

	static float Ellipse(Vector2 p, Vector2 centre, Vector2 radii)
	{
		Vector2 q = (p - centre) / radii;
		return (q.Length() - 1.0f) * Mathf.Min(radii.X, radii.Y);
	}

	static float RoundedBox(Vector2 p, Vector2 min, Vector2 max, float radius)
	{
		Vector2 centre = (min + max) * 0.5f, half = (max - min) * 0.5f - new Vector2(radius, radius);
		Vector2 q = (p - centre).Abs() - half;
		return new Vector2(Mathf.Max(q.X, 0.0f), Mathf.Max(q.Y, 0.0f)).Length() + Mathf.Min(Mathf.Max(q.X, q.Y), 0.0f) - radius;
	}

	static float Polygon(Vector2 p, Vector2[] points)
	{
		float distance = float.MaxValue;
		bool inside = false;
		for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
		{
			Vector2 a = points[j], b = points[i];
			distance = Mathf.Min(distance, p.DistanceTo(Geometry2D.GetClosestPointToSegment(p, a, b)));
			if ((b.Y > p.Y) != (a.Y > p.Y) && p.X < (a.X - b.X) * (p.Y - b.Y) / (a.Y - b.Y) + b.X) inside = !inside;
		}
		return inside ? -distance : distance;
	}

	/// <summary>Distance to the nearest of a set of line strokes, minus their half-width: a carved line drawing.</summary>
	static float Strokes(Vector2 p, Vector2[][] strokes, float halfWidth)
	{
		float distance = float.MaxValue;
		foreach (Vector2[] line in strokes)
			for (int i = 0; i + 1 < line.Length; i++)
				distance = Mathf.Min(distance, p.DistanceTo(Geometry2D.GetClosestPointToSegment(p, line[i], line[i + 1])));
		return distance - halfWidth;
	}

	static bool Odd(float value, float band) => ((int)Mathf.Floor(value / band) & 1) == 1;

	// ---- Pieces ----

	/// <summary>Woodland: a sawn tree stump, the cut face showing its rings above a bark side.</summary>
	static Piece Stump()
	{
		const int w = 150, h = 132;
		Color outline = new(0.17f, 0.1f, 0.05f);
		Color bark = new(0.45f, 0.29f, 0.16f), barkDark = new(0.36f, 0.22f, 0.11f);
		Color wood = new(0.89f, 0.72f, 0.46f), woodRing = new(0.76f, 0.56f, 0.33f);
		Vector2 top = new(75, 46), topRadii = new(62, 30);
		Painter painter = new(w, h);

		// The bark side: a column with a rounded foot, darker in vertical stripes and on the right.
		painter.Layer(p => Mathf.Min(RoundedBox(p, new Vector2(13, 46), new Vector2(137, 104), 4), Ellipse(p, new Vector2(75, 104), new Vector2(62, 22))),
			p => Odd(p.X + 6.0f * Mathf.Sin(p.Y * 0.12f), 15.0f) || p.X > 112 ? barkDark : bark, outline);
		// The cut face: alternating ring bands, a rim of bark round the edge.
		painter.Layer(p => Ellipse(p, top, topRadii), p =>
		{
			float r = ((p - top) / topRadii).Length();
			return r > 0.86f ? barkDark : Odd(r, 0.2f) ? woodRing : wood;
		}, outline);
		return new Piece(painter.Finish(), new Vector2(75, 92), Units.U(1.5f) / w);
	}

	/// <summary>Frost Tundra: a cluster of three ice crystals, lit from the left.</summary>
	static Piece Crystal()
	{
		const int w = 144, h = 176;
		Color outline = new(0.11f, 0.2f, 0.38f);
		Color light = new(0.84f, 0.95f, 1.0f), mid = new(0.56f, 0.8f, 0.98f), deep = new(0.4f, 0.64f, 0.9f), shine = new(1.0f, 1.0f, 1.0f);
		Painter painter = new(w, h);

		void Prism(Vector2[] points, float centreX, float shoulderY)
		{
			painter.Layer(p => Polygon(p, points), p =>
			{
				if (p.Y < shoulderY) return p.X < centreX ? shine : light;  // the faceted tip
				if (Mathf.Abs(p.X - (centreX - 9)) < 3.5f) return shine;     // a highlight down the lit face
				return p.X < centreX ? light : p.X < centreX + 10 ? mid : deep;
			}, outline);
		}

		// Back two first, then the tall one in front.
		Prism(new Vector2[] { new(30, 62), new(54, 88), new(56, 158), new(40, 166), new(18, 154), new(13, 92) }, 34, 88);
		Prism(new Vector2[] { new(114, 56), new(131, 88), new(127, 154), new(110, 166), new(90, 158), new(92, 88) }, 111, 88);
		Prism(new Vector2[] { new(72, 8), new(99, 48), new(99, 152), new(72, 170), new(45, 152), new(45, 48) }, 72, 48);
		return new Piece(painter.Finish(), new Vector2(72, 152), Units.U(1.4f) / w);
	}

	/// <summary>Scorched Canyon: a saguaro cactus, ribbed trunk and two raised arms, on a little sand mound.</summary>
	static Piece Saguaro()
	{
		const int w = 130, h = 200;
		Color outline = new(0.07f, 0.19f, 0.09f);
		Color green = new(0.36f, 0.66f, 0.3f), rib = new(0.27f, 0.53f, 0.23f), shade = new(0.2f, 0.42f, 0.18f), bloom = new(1.0f, 0.5f, 0.62f);
		Color sand = new(0.8f, 0.52f, 0.29f), sandOutline = new(0.45f, 0.24f, 0.11f);
		Painter painter = new(w, h);

		// Ribs run down every part; the right-hand edge of each part is in shade.
		Fill Ribbed(float right) => p => p.X > right - 9 ? shade : Odd(p.X + 2, 10.0f) ? rib : green;
		painter.Layer(p => Ellipse(p, new Vector2(65, 188), new Vector2(42, 10)), p => sand, sandOutline);
		painter.Layer(p => Mathf.Min(RoundedBox(p, new Vector2(12, 104), new Vector2(52, 128), 12), RoundedBox(p, new Vector2(12, 56), new Vector2(38, 128), 13)), Ribbed(38), outline);
		painter.Layer(p => Mathf.Min(RoundedBox(p, new Vector2(78, 86), new Vector2(118, 108), 11), RoundedBox(p, new Vector2(92, 40), new Vector2(118, 108), 13)), Ribbed(118), outline);
		painter.Layer(p => RoundedBox(p, new Vector2(41, 12), new Vector2(89, 192), 24), Ribbed(89), outline);
		painter.Layer(p => Ellipse(p, new Vector2(65, 15), new Vector2(9, 6)), p => bloom, outline, 3.0f);
		return new Piece(painter.Finish(), new Vector2(65, 188), Units.U(2.0f) / h);
	}

	/// <summary>
	/// Four Elder Futhark runes for the four standing stones, in face coordinates (the face runs
	/// about x 27-70 from y 60 to 160): Thurisaz (Thor, the thunderer), Sowilo (the sun, shaped
	/// like a lightning stroke), Hagalaz (hail) and Algiz (protection).
	/// </summary>
	static readonly Vector2[][][] Runes =
	{
		new[] { new Vector2[] { new(38, 66), new(38, 158) }, new Vector2[] { new(38, 88), new(60, 108), new(38, 128) } },
		new[] { new Vector2[] { new(60, 68), new(37, 106), new(59, 120), new(36, 158) } },
		new[] { new Vector2[] { new(35, 66), new(35, 158) }, new Vector2[] { new(61, 66), new(61, 158) }, new Vector2[] { new(35, 96), new(61, 124) } },
		new[] { new Vector2[] { new(48, 74), new(48, 158) }, new Vector2[] { new(34, 70), new(48, 102), new(62, 70) } },
	};

	public static int RuneCount => Runes.Length;

	/// <summary>Storm Plateau: a basalt standing stone with a rune cut into its face, one rune per stone.</summary>
	static Piece Obelisk(int variant)
	{
		const int w = 96, h = 210;
		Color outline = new(0.1f, 0.09f, 0.17f);
		// Dark basalt, so it stands out from the pale slate floor.
		Color lit = new(0.3f, 0.33f, 0.47f), dark = new(0.2f, 0.21f, 0.32f), cap = new(0.42f, 0.46f, 0.62f), slab = new(0.16f, 0.15f, 0.22f);
		Painter painter = new(w, h);
		painter.Layer(p => RoundedBox(p, new Vector2(7, 176), new Vector2(89, 204), 6), p => slab, outline);
		painter.Layer(p => Polygon(p, new Vector2[] { new(16, 186), new(80, 186), new(68, 44), new(48, 6), new(28, 44) }),
			p => p.Y < 46 ? cap : p.X < 48 ? lit : dark, outline);
		Vector2[][] rune = Runes[((variant % Runes.Length) + Runes.Length) % Runes.Length];
		painter.Carve(p => Strokes(p, rune, 4.2f), new Color(0.06f, 0.05f, 0.1f));
		return new Piece(painter.Finish(), new Vector2(48, 190), Units.U(2.0f) / h);
	}
}
