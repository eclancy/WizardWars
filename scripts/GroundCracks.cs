using System.Collections.Generic;
using Godot;

/// <summary>
/// Cracks splitting the ground out from a point, for Earthquake and Tremor. Jagged lines, some
/// branching, race outward, hold, and fade. Puffs of dust and chips of rock kick up along them.
/// They are drawn flat on the floor, under patches and wizards: a dark split with a lip of
/// lighter churned earth either side, in the game's flat style.
///
/// It is only a picture. What the quake does is decided by the ability that made it.
/// </summary>
public partial class GroundCracks : Node2D
{
	static readonly Color Split = new(0.13f, 0.07f, 0.03f);
	static readonly Color Lip = new(0.62f, 0.45f, 0.26f);
	static readonly Color Dust = new(0.82f, 0.7f, 0.52f);
	static readonly Color Chip = new(0.42f, 0.29f, 0.17f);

	float grow, hold, fade, elapsed;
	readonly List<Vector2[]> lines = new();
	readonly List<float> starts = new();   // how far out (along the cracks' growth) each line begins
	readonly List<(Vector2 at, float delay, float size)> puffs = new();

	/// <param name="radius">How far the longest cracks reach, in pixels.</param>
	/// <param name="count">How many main cracks.</param>
	/// <param name="width">Crack width at the root, in pixels.</param>
	public static GroundCracks Create(Vector2 position, float radius, int count, float width, float grow, float hold, float fade)
	{
		GroundCracks cracks = new() { Position = position, grow = grow, hold = hold, fade = fade, ZIndex = -6 };
		cracks.width = width;
		float step = Mathf.Max(8.0f, radius / 14.0f);
		for (int i = 0; i < count; i++)
		{
			float angle = i * Mathf.Tau / count + (float)GD.RandRange(-0.3, 0.3);
			cracks.Grow(Vector2.FromAngle(angle) * Units.U(0.25f), angle, radius * (float)GD.RandRange(0.6, 1.0), step, 0.0f, true);
		}
		return cracks;
	}

	float width;

	void Grow(Vector2 from, float angle, float length, float step, float startAt, bool main)
	{
		List<Vector2> points = new() { from };
		Vector2 at = from;
		float heading = angle, travelled = 0.0f;
		while (travelled < length)
		{
			heading = angle + (float)GD.RandRange(-0.6, 0.6);
			at += Vector2.FromAngle(heading) * step;
			travelled += step;
			points.Add(at);
			// Main cracks fork now and then into shorter side cracks.
			if (main && travelled > length * 0.3f && GD.Randf() < 0.07f)
				Grow(at, angle + (GD.Randf() < 0.5f ? 0.7f : -0.7f), (length - travelled) * 0.55f, step * 0.8f, startAt + travelled, false);
			if (GD.Randf() < 0.18f) puffs.Add((at, (startAt + travelled) / Mathf.Max(1.0f, length), (float)GD.RandRange(10.0, 20.0)));
		}
		lines.Add(points.ToArray());
		starts.Add(startAt);
		puffs.Add((at, 1.0f, (float)GD.RandRange(14.0, 24.0)));
	}

	public override void _Process(double delta)
	{
		elapsed += (float)delta;
		if (elapsed >= grow + hold + fade) QueueFree();
		else QueueRedraw();
	}

	public override void _Draw()
	{
		float reveal = grow <= 0.0f ? 1.0f : Mathf.Min(1.0f, elapsed / grow);
		float alpha = elapsed < grow + hold ? 1.0f : 1.0f - (elapsed - grow - hold) / fade;
		float longest = 1.0f;
		for (int i = 0; i < lines.Count; i++) longest = Mathf.Max(longest, starts[i] + Length(lines[i]));
		float shown = reveal * longest;

		// The lip first, then the dark split on top, each line tapering toward its end.
		foreach (bool lip in new[] { true, false })
			for (int i = 0; i < lines.Count; i++)
			{
				Vector2[] line = lines[i];
				float along = starts[i], total = starts[i] + Length(line);
				for (int k = 0; k + 1 < line.Length; k++)
				{
					float segment = line[k].DistanceTo(line[k + 1]);
					if (along >= shown) break;
					Vector2 end = along + segment <= shown ? line[k + 1] : line[k].Lerp(line[k + 1], (shown - along) / segment);
					float taper = 1.0f - 0.75f * (along / total);
					float w = width * taper * (starts[i] > 0.0f ? 0.7f : 1.0f);
					if (lip) DrawLine(line[k], end, new Color(Lip, 0.75f * alpha), w + 5.0f, true);
					else DrawLine(line[k], end, new Color(Split, alpha), w, true);
					along += segment;
				}
			}

		// Dust kicks up where the crack reaches, and chips of rock jump.
		foreach ((Vector2 at, float delay, float size) in puffs)
		{
			float since = elapsed - delay * grow;
			if (since < 0.0f || since > 0.7f) continue;
			float f = since / 0.7f;
			DrawCircle(at + new Vector2(0, -12.0f * f), size * (0.5f + f), new Color(Dust, 0.55f * (1.0f - f) * alpha));
			Vector2 hop = at + new Vector2(size * 0.4f, -26.0f * 4.0f * f * (1.0f - f));
			DrawCircle(hop, 3.5f, new Color(Chip, (1.0f - f) * alpha));
		}
	}

	static float Length(Vector2[] line)
	{
		float total = 0.0f;
		for (int k = 0; k + 1 < line.Length; k++) total += line[k].DistanceTo(line[k + 1]);
		return total;
	}
}
