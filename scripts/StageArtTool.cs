using System;
using Godot;

/// <summary>
/// Paints the floors of Frost Tundra, Scorched Canyon and Storm Plateau. Run once, with
/// `-- --make-stages`; the PNGs are committed.
///
/// The old generated backgrounds were scattered with painted boulders, trees and blobs that
/// read as obstacles (or as nothing at all). A floor here is only ground: flat colour bands
/// with fine detail - drift curls, ripples, flagstones, scorch marks, tiny pebbles - and no objects,
/// so the only things that look solid in the arena are the cover pieces, which are solid.
///
/// Floors are 2560 x 1440, the view Battle's background must cover (Battle.CoveredView), so
/// they draw at scale 1. Woodland keeps its hand-drawn art, with its bush-like tufts painted out.
/// </summary>
public static class StageArtTool
{
	const int W = 2560, H = 1440;

	public static int Run()
	{
		int failures = 0;
		failures += Save(Frost(), "frost-tundra");
		failures += Save(Canyon(), "scorched-canyon");
		failures += Save(Storm(), "storm-plateau");
		failures += CleanWoodland();
		return failures;
	}

	/// <summary>
	/// Woodland keeps its hand-drawn floor, minus five dark spiky grass tufts that read like bushes
	/// you might hide behind. Each is painted over with the grass colour round it (the commonest
	/// colour in a ring just outside it); only pixels darker than that grass change, so the path
	/// edging and lighter grass nearby are left alone. Running it again changes nothing.
	/// </summary>
	static int CleanWoodland()
	{
		string path = "res://assets/art/stages/woodland.png";
		Image image = Image.LoadFromFile(ProjectSettings.GlobalizePath(path));
		image.Convert(Image.Format.Rgba8);
		// Centre and radii of each tuft, in the image's own pixels.
		(Vector2 centre, Vector2 radii)[] tufts =
		{
			(new(1691, 110), new(72, 50)), (new(1301, 424), new(60, 40)), (new(1145, 675), new(66, 46)),
			(new(989, 925), new(125, 62)), (new(1194, 864), new(36, 24)),
		};
		foreach ((Vector2 centre, Vector2 radii) in tufts)
		{
			System.Collections.Generic.Dictionary<Color, int> ring = new();
			for (int i = 0; i < 360; i++)
			{
				float angle = Mathf.DegToRad(i);
				Vector2 p = centre + new Vector2(Mathf.Cos(angle) * (radii.X + 14), Mathf.Sin(angle) * (radii.Y + 14));
				Color c = image.GetPixel((int)p.X, (int)p.Y);
				ring[c] = ring.TryGetValue(c, out int seen) ? seen + 1 : 1;
			}
			Color grass = default;
			int most = 0;
			foreach ((Color c, int count) in ring) if (count > most) { most = count; grass = c; }
			for (int y = (int)(centre.Y - radii.Y); y <= centre.Y + radii.Y; y++)
				for (int x = (int)(centre.X - radii.X); x <= centre.X + radii.X; x++)
				{
					Vector2 q = (new Vector2(x, y) - centre) / radii;
					if (q.LengthSquared() > 1.0f) continue;
					if (image.GetPixel(x, y).Luminance < grass.Luminance - 0.01f) image.SetPixel(x, y, grass);
				}
		}
		Error error = image.SavePng(ProjectSettings.GlobalizePath(path));
		GD.Print($"StageArtTool: cleaned {path}: {error}");
		return error == Error.Ok ? 0 : 1;
	}

	static int Save(byte[] pixels, string name)
	{
		Image image = Image.CreateFromData(W, H, false, Image.Format.Rgba8, pixels);
		string path = $"res://assets/art/stages/{name}.png";
		Error error = image.SavePng(ProjectSettings.GlobalizePath(path));
		GD.Print($"StageArtTool: wrote {path}: {error}");
		return error == Error.Ok ? 0 : 1;
	}

	// ---- Helpers ----

	/// <summary>Noise in 0..1 for every pixel, from Godot's FastNoiseLite (built natively, so fast).</summary>
	static float[] Noise(int seed, float frequency, FastNoiseLite.NoiseTypeEnum type = FastNoiseLite.NoiseTypeEnum.Simplex, int octaves = 3)
	{
		FastNoiseLite noise = new() { Seed = seed, Frequency = frequency, NoiseType = type, FractalOctaves = octaves };
		byte[] data = noise.GetImage(W, H).GetData();
		float[] values = new float[W * H];
		for (int i = 0; i < values.Length; i++) values[i] = data[i] / 255.0f;
		return values;
	}

	static void Set(byte[] pixels, int i, Color c)
	{
		pixels[i * 4] = (byte)(Mathf.Clamp(c.R, 0, 1) * 255);
		pixels[i * 4 + 1] = (byte)(Mathf.Clamp(c.G, 0, 1) * 255);
		pixels[i * 4 + 2] = (byte)(Mathf.Clamp(c.B, 0, 1) * 255);
		pixels[i * 4 + 3] = 255;
	}

	static Color Get(byte[] pixels, int i) => new(pixels[i * 4] / 255.0f, pixels[i * 4 + 1] / 255.0f, pixels[i * 4 + 2] / 255.0f);

	/// <summary>Picks one of a few flat colours by thresholds: banded, not a smooth gradient.</summary>
	static Color Band(float value, Color[] bands, float[] steps)
	{
		for (int i = 0; i < steps.Length; i++) if (value < steps[i]) return bands[i];
		return bands[^1];
	}

	/// <summary>A small filled ellipse with a darker rim, like the art's pebbles.</summary>
	static void Pebble(byte[] pixels, float cx, float cy, float rx, float ry, Color fill, Color rim)
	{
		for (int y = (int)(cy - ry - 2); y <= cy + ry + 2; y++)
			for (int x = (int)(cx - rx - 2); x <= cx + rx + 2; x++)
			{
				if (x < 0 || y < 0 || x >= W || y >= H) continue;
				float d = Mathf.Sqrt((x - cx) * (x - cx) / (rx * rx) + (y - cy) * (y - cy) / (ry * ry));
				if (d > 1.0f) continue;
				Set(pixels, y * W + x, d > 0.7f ? rim : fill);
			}
	}

	/// <summary>The bottom of a circle (a smile), centred on x, spanning `span` radians each side, tapering at the ends.</summary>
	static void Arc(byte[] pixels, float x, float y, float radius, float span, float width, Color colour)
	{
		int steps = (int)(radius * span * 2);
		for (int s = 0; s <= steps; s++)
		{
			float t = s / (float)steps, angle = Mathf.Pi * 0.5f + (t * 2 - 1) * span;
			float px = x + Mathf.Cos(angle) * radius, py = y - radius + Mathf.Sin(angle) * radius;
			float w = width * Mathf.Sin(t * Mathf.Pi);
			for (int dy = (int)-w - 1; dy <= w + 1; dy++)
				for (int dx = (int)-w - 1; dx <= w + 1; dx++)
				{
					int ix = (int)px + dx, iy = (int)py + dy;
					if (ix < 0 || iy < 0 || ix >= W || iy >= H || dx * dx + dy * dy > w * w) continue;
					Set(pixels, iy * W + ix, colour);
				}
		}
	}

	// ---- Floors ----

	/// <summary>Snow in three close bands, little wind-drift curls, sparkle flecks, a few tiny pebbles.</summary>
	static byte[] Frost()
	{
		byte[] pixels = new byte[W * H * 4];
		float[] body = Noise(11, 0.0016f), fleck = Noise(13, 0.35f, FastNoiseLite.NoiseTypeEnum.Value, 1);
		Color[] snow = { new(0.79f, 0.88f, 0.95f), new(0.84f, 0.92f, 0.97f), new(0.89f, 0.95f, 0.99f) };
		Color line = new(0.7f, 0.81f, 0.91f), sparkle = new(1, 1, 1);
		for (int i = 0; i < W * H; i++)
		{
			Color c = Band(body[i], snow, new[] { 0.42f, 0.6f });
			if (fleck[i] > 0.985f) c = sparkle;
			Set(pixels, i, c);
		}
		Random rng = new(14);
		// Drift curls: a shallow arc, thick in the middle and thin at the ends, often with a smaller one
		// tucked under it, the way wind leaves snow.
		for (int n = 0; n < 110; n++)
		{
			float x = rng.Next(W), y = rng.Next(H), radius = 50 + rng.Next(60);
			Arc(pixels, x, y, radius, 0.55f, 3.0f, line);
			if (rng.NextDouble() < 0.6) Arc(pixels, x + radius * 0.25f, y + 14, radius * 0.6f, 0.5f, 2.4f, line);
		}
		for (int n = 0; n < 60; n++) Pebble(pixels, rng.Next(W), rng.Next(H), 5 + rng.Next(5), 3 + rng.Next(3), new Color(0.66f, 0.7f, 0.76f), new Color(0.42f, 0.47f, 0.55f));
		return pixels;
	}

	/// <summary>Sand in three bands, wind ripples and small pebbles.</summary>
	static byte[] Canyon()
	{
		byte[] pixels = new byte[W * H * 4];
		float[] body = Noise(21, 0.0014f), ripple = Noise(22, 0.004f, octaves: 1);
		Color[] sand = { new(0.85f, 0.58f, 0.33f), new(0.89f, 0.63f, 0.37f), new(0.93f, 0.68f, 0.42f) };
		Color line = new(0.79f, 0.51f, 0.28f);
		for (int i = 0; i < W * H; i++)
		{
			int x = i % W, y = i / W;
			Color c = Band(body[i], sand, new[] { 0.42f, 0.6f });
			// Ripples run mostly sideways: stretch the contour across x.
			float r = ripple[i] * 0.8f + y * 0.014f + Mathf.Sin(x * 0.004f) * 0.6f;
			if (Mathf.Abs(r % 1.0f - 0.5f) < 0.04f) c = line;
			Set(pixels, i, c);
		}
		Random rng = new(23);
		for (int n = 0; n < 70; n++) Pebble(pixels, rng.Next(W), rng.Next(H), 4 + rng.Next(5), 3 + rng.Next(3), new Color(0.72f, 0.47f, 0.28f), new Color(0.5f, 0.29f, 0.15f));
		return pixels;
	}

	/// <summary>Slate flagstones with dark grout, and a few lightning scorch marks.</summary>
	static byte[] Storm()
	{
		byte[] pixels = new byte[W * H * 4];
		const int Cell = 150;
		int cols = W / Cell + 3, rows = H / Cell + 3;
		Random rng = new(31);
		Vector2[] seeds = new Vector2[cols * rows];
		int[] shade = new int[cols * rows];
		for (int j = 0; j < rows; j++)
			for (int i = 0; i < cols; i++)
			{
				seeds[j * cols + i] = new Vector2((i - 0.5f + (float)rng.NextDouble()) * Cell, (j - 0.5f + (float)rng.NextDouble()) * Cell);
				shade[j * cols + i] = rng.Next(3);
			}
		Color[] slate = { new(0.42f, 0.4f, 0.53f), new(0.46f, 0.44f, 0.57f), new(0.5f, 0.48f, 0.61f) };
		Color grout = new(0.25f, 0.23f, 0.33f);
		float[] grain = Noise(32, 0.02f, octaves: 1);

		for (int y = 0; y < H; y++)
			for (int x = 0; x < W; x++)
			{
				// Nearest and second-nearest stone centre. Grout is a band either side of the line
				// halfway between them: the distance to that line is (d2^2 - d1^2) / (2 |s2 - s1|).
				int ci = Mathf.RoundToInt(x / (float)Cell), cj = Mathf.RoundToInt(y / (float)Cell);
				Vector2 here = new(x, y);
				float best = float.MaxValue, second = float.MaxValue;
				int bestIndex = 0, secondIndex = 0;
				for (int j = cj - 2; j <= cj + 2; j++)
					for (int i = ci - 2; i <= ci + 2; i++)
					{
						if (i < 0 || j < 0 || i >= cols || j >= rows) continue;
						float d = seeds[j * cols + i].DistanceSquaredTo(here);
						if (d < best) { second = best; secondIndex = bestIndex; best = d; bestIndex = j * cols + i; }
						else if (d < second) { second = d; secondIndex = j * cols + i; }
					}
				int p = y * W + x;
				float toEdge = (second - best) / (2.0f * seeds[bestIndex].DistanceTo(seeds[secondIndex]));
				Color c = toEdge < 2.5f ? grout : slate[shade[bestIndex]];
				// A faint speckle in the stone so it reads as rock, not flat paint.
				if (c != grout && grain[p] > 0.84f) c = c.Darkened(0.06f);
				Set(pixels, p, c);
			}

		// Scorch marks where lightning has struck: a ragged burnt splotch that darkens the stone
		// under it (so the flagstones still show through), in two flat steps.
		for (int n = 0; n < 7; n++)
		{
			float sx = rng.Next(200, W - 200), sy = rng.Next(200, H - 200), radius = rng.Next(38, 60);
			float phase = (float)rng.NextDouble() * Mathf.Tau;
			for (int y = (int)(sy - radius * 1.4f); y <= sy + radius * 1.4f; y++)
				for (int x = (int)(sx - radius * 1.4f); x <= sx + radius * 1.4f; x++)
				{
					float angle = Mathf.Atan2(y - sy, x - sx);
					float edge = radius * (1.0f + 0.25f * Mathf.Sin(angle * 5 + phase) + 0.12f * Mathf.Sin(angle * 11 + phase * 2));
					float d = new Vector2(x - sx, y - sy).Length();
					if (d > edge) continue;
					int p = y * W + x;
					Set(pixels, p, Get(pixels, p).Darkened(d < edge * 0.6f ? 0.42f : 0.24f));
				}
		}
		return pixels;
	}
}
