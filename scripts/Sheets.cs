using System.Collections.Generic;
using Godot;

/// <summary>
/// Animations sliced from the Unity spritesheets. Frame rectangles come from each sheet's
/// Unity .meta (spriteMode: Multiple); Unity lists them bottom-up, these are top-down.
/// Built once per sheet and cached.
/// </summary>
public static class Sheets
{
	static readonly Dictionary<string, SpriteFrames> cache = new();

	/// <summary>A sheet of equal frames stacked in a column, played top to bottom.</summary>
	public static SpriteFrames Column(string path, int frameCount, float fps)
	{
		if (cache.TryGetValue(path, out SpriteFrames cached)) return cached;
		Texture2D texture = Content.Texture(path);
		Vector2 size = texture.GetSize();
		int frameHeight = (int)size.Y / frameCount;
		List<Rect2> rects = new();
		for (int i = 0; i < frameCount; i++) rects.Add(new Rect2(0, i * frameHeight, size.X, frameHeight));
		return cache[path] = Build(texture, rects, fps);
	}

	/// <summary>A grid sheet read left to right, top to bottom.</summary>
	public static SpriteFrames Grid(string path, int columns, int rows, int frameCount, float fps)
	{
		if (cache.TryGetValue(path, out SpriteFrames cached)) return cached;
		Texture2D texture = Content.Texture(path);
		Vector2 size = texture.GetSize();
		float width = size.X / columns, height = size.Y / rows;
		List<Rect2> rects = new();
		for (int i = 0; i < frameCount; i++) rects.Add(new Rect2(i % columns * width, i / columns * height, width, height));
		return cache[path] = Build(texture, rects, fps);
	}

	static SpriteFrames Build(Texture2D texture, List<Rect2> rects, float fps)
	{
		SpriteFrames frames = new();
		frames.SetAnimationSpeed("default", fps);
		frames.SetAnimationLoop("default", true);
		foreach (Rect2 rect in rects) frames.AddFrame("default", new AtlasTexture { Atlas = texture, Region = rect });
		return frames;
	}
}
