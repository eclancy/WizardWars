using Godot;

/// <summary>
/// A generated texture plus where its pivot is, in the Unity sense (0..1 from the bottom-left).
/// Unity sprites here are 64 px per unit.
/// </summary>
public readonly struct FxTexture
{
	public readonly Texture2D Texture;
	public readonly Vector2 Pivot;

	public FxTexture(Texture2D texture, Vector2 pivot)
	{
		Texture = texture;
		Pivot = pivot;
	}

	/// <summary>
	/// A Sprite2D showing this texture at <paramref name="unityScale"/>, pivoted where Unity
	/// pivoted it, so ported animation code that scales "outward from the pivot" still does.
	/// </summary>
	public Sprite2D NewSprite(float unityScale = 1.0f)
	{
		Vector2 size = Texture.GetSize();
		return new Sprite2D
		{
			Texture = Texture,
			Offset = new Vector2((0.5f - Pivot.X) * size.X, (Pivot.Y - 0.5f) * size.Y),
			Scale = Vector2.One * ProceduralFx.Scale(unityScale),
		};
	}
}

/// <summary>
/// Art for abilities that are animated in code, ported pixel for pixel from the Unity
/// ProceduralFx.cs. Each texture is built the first time it is asked for and cached for the
/// life of the process. Unity textures count rows from the bottom and Godot images from the
/// top, so every generator writes row (height - 1 - y).
/// </summary>
public static class ProceduralFx
{
	const float UnityPixelsPerUnit = 64.0f;
	const int FlameFrameCount = 6;

	// Palette sampled from the hand-drawn fireball sheet.
	static readonly Color FireCore = new(0.96f, 0.87f, 0.12f);
	static readonly Color FireAmber = new(0.91f, 0.64f, 0.23f);
	static readonly Color FireOrange = new(0.94f, 0.53f, 0.17f);
	static readonly Color FireAccent = new(0.72f, 0.36f, 0.12f);

	static FxTexture? iceShard, shadow, meteor, burst, ringOutline, puddle, mudGlob;
	static FxTexture[] flameFrames;

	/// <summary>Godot sprite scale for a generated texture shown at a Unity scale.</summary>
	public static float Scale(float unityScale) => unityScale * Units.PixelsPerUnit / UnityPixelsPerUnit;

	public static FxTexture IceShard => iceShard ??= BuildIceShard();
	public static FxTexture[] FlameFrames => flameFrames ??= BuildFlameFrames();
	public static FxTexture Flame => FlameFrames[0];
	public static FxTexture Shadow => shadow ??= BuildShadow();
	public static FxTexture Meteor => meteor ??= BuildMeteor();
	public static FxTexture Burst => burst ??= BuildBurst();
	public static FxTexture RingOutline => ringOutline ??= BuildRingOutline();
	public static FxTexture Puddle => puddle ??= BuildPuddle();
	public static FxTexture MudGlob => mudGlob ??= BuildMudGlob();

	/// <summary>
	/// New in the port: a white disc, faint in the middle and brighter at the rim, to tint and
	/// stretch over an area of effect so its real edge is visible.
	/// </summary>
	public static FxTexture AreaDisc => areaDisc ??= BuildAreaDisc();
	static FxTexture? areaDisc;

	static FxTexture BuildAreaDisc()
	{
		const int size = 128;
		Canvas canvas = new(size, size);
		float half = size * 0.5f;
		for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				float dx = (x + 0.5f - half) / half;
				float dy = (y + 0.5f - half) / half;
				float distance = Mathf.Sqrt(dx * dx + dy * dy);
				if (distance > 1.0f) { canvas.Set(x, y, new Color(1, 1, 1, 0)); continue; }
				float rim = SmoothStep(0.8f, 0.97f, distance) * (1.0f - SmoothStep(0.97f, 1.0f, distance));
				canvas.Set(x, y, new Color(1, 1, 1, 0.25f + 0.75f * rim));
			}
		return canvas.Finish(0.5f, 0.5f);
	}

	/// <summary>A blank image plus a setter that takes Unity's bottom-up row index.</summary>
	sealed class Canvas
	{
		public readonly Image Image;
		readonly int height;

		public Canvas(int width, int height)
		{
			this.height = height;
			Image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
		}

		public void Set(int x, int unityY, Color colour) => Image.SetPixel(x, height - 1 - unityY, colour);

		public FxTexture Finish(float pivotX, float pivotY) => new(ImageTexture.CreateFromImage(Image), new Vector2(pivotX, pivotY));
	}

	static float Clamp01(float v) => Mathf.Clamp(v, 0.0f, 1.0f);

	static float SmoothStep(float from, float to, float t)
	{
		t = Clamp01((t - from) / (to - from));
		return t * t * (3.0f - 2.0f * t);
	}

	static FxTexture BuildIceShard()
	{
		const int width = 32, height = 64;
		Canvas canvas = new(width, height);
		for (int y = 0; y < height; y++)
		{
			float along = (y + 0.5f) / height;
			float halfWidth = Mathf.Sin(along * Mathf.Pi) * 0.5f;
			for (int x = 0; x < width; x++)
			{
				float across = (x + 0.5f) / width - 0.5f;
				float edge = halfWidth <= 0.0001f ? 1.0f : Mathf.Abs(across) / halfWidth;
				float solid = Clamp01(1.0f - edge);
				solid *= solid;
				Color tint = new Color(0.35f, 0.68f, 1.0f).Lerp(Colors.White, solid);
				canvas.Set(x, y, new Color(tint.R, tint.G, tint.B, solid * Mathf.Lerp(0.55f, 1.0f, along)));
			}
		}
		return canvas.Finish(0.5f, 0.15f);
	}

	static FxTexture[] BuildFlameFrames()
	{
		FxTexture[] frames = new FxTexture[FlameFrameCount];
		for (int i = 0; i < FlameFrameCount; i++) frames[i] = BuildFlameFrame(i / (float)FlameFrameCount);
		return frames;
	}

	// A broad half-round body with two travelling waves across it, coloured in flat bands by
	// depth. Fully opaque body with at most a pixel of feathering; transparent pixels keep the
	// flame colour so filtering never fringes towards black.
	static FxTexture BuildFlameFrame(float phase)
	{
		const int width = 64, height = 96;
		const float tau = Mathf.Pi * 2.0f;
		Canvas canvas = new(width, height);
		for (int x = 0; x < width; x++)
		{
			float across = (x + 0.5f) / width * 2.0f - 1.0f;
			float envelope = Mathf.Pow(Mathf.Max(0.0f, 1.0f - across * across), 0.35f);
			float lick = 0.16f * Mathf.Sin(across * 5.0f + phase * tau) + 0.08f * Mathf.Sin(across * 9.0f - phase * tau * 1.6f);
			float top = envelope * (0.94f + lick);
			for (int y = 0; y < height; y++)
			{
				float up = (y + 0.5f) / height;
				float depth = Mathf.Min((top - up) / 0.45f, (1.0f - Mathf.Abs(across)) / 0.3f);
				float coreness = Clamp01(depth) * Mathf.Lerp(1.2f, 0.45f, up);
				Color tint = coreness > 0.6f ? FireCore : coreness > 0.34f ? FireAmber : coreness > 0.12f ? FireOrange : FireAccent;
				float alpha = 0.0f;
				if (up <= top)
				{
					float toEdge = (top - up) * height;
					alpha = toEdge < 1.5f ? Clamp01(toEdge / 1.5f) : 1.0f;
				}
				canvas.Set(x, y, new Color(tint.R, tint.G, tint.B, alpha));
			}
		}
		// Pivot at the base so a flame stands on the ground and licks upward.
		return canvas.Finish(0.5f, 0.02f);
	}

	static FxTexture BuildShadow()
	{
		const int width = 96, height = 48;
		Canvas canvas = new(width, height);
		float halfWidth = width * 0.5f, halfHeight = height * 0.5f;
		for (int y = 0; y < height; y++)
			for (int x = 0; x < width; x++)
			{
				float dx = (x + 0.5f - halfWidth) / halfWidth;
				float dy = (y + 0.5f - halfHeight) / halfHeight;
				float solid = Clamp01(1.0f - Mathf.Sqrt(dx * dx + dy * dy));
				canvas.Set(x, y, new Color(0.05f, 0.04f, 0.03f, solid * solid * 0.75f));
			}
		return canvas.Finish(0.5f, 0.5f);
	}

	static FxTexture BuildMeteor()
	{
		const int size = 96;
		Canvas canvas = new(size, size);
		float half = size * 0.5f;
		for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				float dx = (x + 0.5f - half) / half;
				float dy = (y + 0.5f - half) / half;
				// The head sits low; the tail streams upward behind it as it falls.
				float head = Mathf.Sqrt(dx * dx + (dy + 0.35f) * (dy + 0.35f)) / 0.55f;
				float tailWidth = Mathf.Max(0.06f, 0.34f * (1.0f - Clamp01((dy - 0.1f) / 0.9f)));
				float tail = dy > -0.35f ? Mathf.Abs(dx) / tailWidth : 999.0f;
				float shape = Mathf.Min(head, tail);
				if (shape > 1.0f) { canvas.Set(x, y, new Color(0, 0, 0, 0)); continue; }
				float coverage = 1.0f - SmoothStep(0.93f, 1.0f, shape);
				Color tint = shape < 0.35f ? FireCore : shape < 0.6f ? FireAmber : shape < 0.85f ? FireOrange : FireAccent;
				canvas.Set(x, y, new Color(tint.R, tint.G, tint.B, coverage));
			}
		return canvas.Finish(0.5f, 0.28f);
	}

	static FxTexture BuildBurst()
	{
		const int size = 128;
		Canvas canvas = new(size, size);
		float half = size * 0.5f;
		for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				float dx = (x + 0.5f - half) / half;
				float dy = (y + 0.5f - half) / half;
				float angle = Mathf.Atan2(dy, dx);
				// A ragged rim so the blast does not read as a plain circle.
				float ragged = 1.0f + 0.06f * Mathf.Sin(angle * 11.0f) + 0.04f * Mathf.Sin(angle * 19.0f);
				float distance = Mathf.Sqrt(dx * dx + dy * dy) / ragged;
				if (distance > 1.0f) { canvas.Set(x, y, new Color(0, 0, 0, 0)); continue; }
				float coverage = 1.0f - SmoothStep(0.94f, 1.0f, distance);
				Color tint = distance < 0.4f ? FireCore : distance < 0.68f ? FireAmber : distance < 0.88f ? FireOrange : FireAccent;
				canvas.Set(x, y, new Color(tint.R, tint.G, tint.B, coverage));
			}
		return canvas.Finish(0.5f, 0.5f);
	}

	static FxTexture BuildRingOutline()
	{
		const int size = 128;
		Canvas canvas = new(size, size);
		float half = size * 0.5f;
		for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				float dx = (x + 0.5f - half) / half;
				float dy = (y + 0.5f - half) / half;
				float band = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - 0.85f);
				float coverage = 1.0f - SmoothStep(0.1f, 0.125f, band);
				if (coverage <= 0.0f) { canvas.Set(x, y, new Color(0, 0, 0, 0)); continue; }
				Color tint = band < 0.045f ? FireCore : FireOrange;
				canvas.Set(x, y, new Color(tint.R, tint.G, tint.B, coverage));
			}
		return canvas.Finish(0.5f, 0.5f);
	}

	static FxTexture BuildPuddle()
	{
		const int width = 64, height = 32;
		Canvas canvas = new(width, height);
		float halfWidth = width * 0.5f, halfHeight = height * 0.5f;
		for (int y = 0; y < height; y++)
			for (int x = 0; x < width; x++)
			{
				float dx = (x + 0.5f - halfWidth) / halfWidth;
				float dy = (y + 0.5f - halfHeight) / halfHeight;
				float distance = Mathf.Sqrt(dx * dx + dy * dy);
				float solid = Clamp01(1.0f - distance);
				solid = solid * solid * (3.0f - 2.0f * solid);
				// A brighter rim reads as a wet edge catching the light.
				float rim = Clamp01(1.0f - Mathf.Abs(distance - 0.72f) / 0.22f);
				Color tint = new Color(0.55f, 0.78f, 0.95f).Lerp(new Color(0.88f, 0.96f, 1.0f), rim);
				canvas.Set(x, y, new Color(tint.R, tint.G, tint.B, solid * 0.85f));
			}
		return canvas.Finish(0.5f, 0.5f);
	}

	static FxTexture BuildMudGlob()
	{
		const int size = 32;
		Canvas canvas = new(size, size);
		float half = size * 0.5f;
		for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				float dx = (x + 0.5f - half) / half;
				float dy = (y + 0.5f - half) / half;
				float distance = Mathf.Sqrt(dx * dx + dy * dy);
				float solid = Clamp01(1.0f - distance);
				solid = solid * solid * (3.0f - 2.0f * solid);
				// Lighter towards the top so the clod reads as lit from above.
				float lift = Clamp01((dy + 1.0f) * 0.5f);
				Color tint = new Color(0.24f, 0.15f, 0.07f).Lerp(new Color(0.48f, 0.33f, 0.16f), lift);
				canvas.Set(x, y, new Color(tint.R, tint.G, tint.B, solid));
			}
		return canvas.Finish(0.5f, 0.5f);
	}
}
