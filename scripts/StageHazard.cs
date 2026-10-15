using System.Collections.Generic;
using Godot;

/// <summary>
/// A stage's hazard, drawn and applied (the geometry is <see cref="StageData.InHazard"/>):
/// - Lava (Volcano): a glowing river in flat bands, with flowing streaks and popping bubbles,
///   crossed by stone bridges. Feet in it take <see cref="StageData.HazardDamage"/> a second and
///   catch fire.
/// - Bog (Swamp): murky pools with a muddy rim, scum and rising bubbles. Feet in one are slowed
///   by <see cref="StageData.HazardSlow"/> percent and stop recharging energy. The modifier is
///   keyed to this node, so it lifts the moment you step out.
///
/// Drawn just over the floor, under everything else. The floor beyond the walls is dimmed over
/// it. The hazard stops acting once the battle is over.
/// </summary>
public partial class StageHazard : Node2D
{
	// Lava, from the bank inward.
	static readonly Color Crust = new(0.16f, 0.09f, 0.08f);
	static readonly Color Glow = new(0.86f, 0.2f, 0.05f);
	static readonly Color Lava = new(1.0f, 0.48f, 0.08f);
	static readonly Color Hot = new(1.0f, 0.82f, 0.25f);
	// Stone bridges.
	static readonly Color BridgeOutline = new(0.12f, 0.1f, 0.1f);
	static readonly Color Stone = new(0.55f, 0.5f, 0.47f);
	static readonly Color StoneDark = new(0.42f, 0.38f, 0.36f);
	// Bog.
	static readonly Color Mud = new(0.2f, 0.14f, 0.07f);
	static readonly Color Water = new(0.16f, 0.31f, 0.3f);
	static readonly Color Deep = new(0.11f, 0.23f, 0.24f);
	static readonly Color Scum = new(0.5f, 0.6f, 0.26f);
	static readonly Color Glint = new(0.62f, 0.82f, 0.78f);
	static readonly Color Bubble = new(0.6f, 0.78f, 0.7f);

	StageData stage;
	Battle battle;
	/// <summary>Total lava damage dealt this battle, for the soak test's report.</summary>
	public float DamageDealt { get; private set; }
	/// <summary>Times a wizard got from one bank of the lava to the other (the soak test reports it).</summary>
	public int Crossings { get; private set; }
	readonly Dictionary<Wizard, int> banks = new();
	float time;
	readonly List<(Vector2 at, float phase, float size)> bubbles = new();

	public static StageHazard Create(StageData stage, Battle battle)
	{
		StageHazard hazard = new() { stage = stage, battle = battle, ZIndex = -95, ZAsRelative = false, Name = "Hazard" };
		// Bubbles scattered over the hazard areas, each popping on its own beat.
		RandomNumberGenerator rng = new() { Seed = 7 };
		for (int i = 0; i + 1 < stage.Hazards.Length; i += 2)
		{
			Vector2 centre = stage.Hazards[i], half = stage.Hazards[i + 1];
			int count = Mathf.Max(3, (int)(half.X * half.Y * 2.0f));
			for (int n = 0; n < count; n++)
			{
				Vector2 at = centre + new Vector2(rng.RandfRange(-half.X, half.X), rng.RandfRange(-half.Y, half.Y)) * 0.75f;
				if (stage.HazardKind == "bog" && ((at - centre) / half).LengthSquared() > 0.55f) continue;
				if (!stage.InHazard(at)) continue;
				hazard.bubbles.Add((Units.U(at), rng.Randf(), rng.RandfRange(6.0f, 13.0f)));
			}
		}
		return hazard;
	}

	public StageData Stage => stage;

	/// <summary>Whether a point in the arena (pixels) is somewhere to keep out of.</summary>
	public bool Contains(Vector2 point) => stage.InHazard(Units.ToUnity(point));

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		bool active = !battle.Over;
		foreach (Wizard wizard in battle.Wizards)
		{
			if (!GodotObject.IsInstanceValid(wizard) || !wizard.Alive) continue;
			bool inside = active && Contains(wizard.Feet);
			if (stage.HazardKind == "lava")
			{
				if (!inside) CountCrossing(wizard);
				if (!inside) continue;
				wizard.TakeDamage(stage.HazardDamage * dt);
				DamageDealt += stage.HazardDamage * dt;
				wizard.SetFire(1.5f, 2.0f);
			}
			else if (inside) wizard.Status.Set(this, new Modifier { Slow = stage.HazardSlow, NoRegen = true });
			else wizard.Status.Remove(this);
		}
	}

	/// <summary>Which bank of the first lava area a wizard is on, across its narrow side.</summary>
	void CountCrossing(Wizard wizard)
	{
		Vector2 feet = Units.ToUnity(wizard.Feet), centre = stage.Hazards[0], half = stage.Hazards[1];
		int bank = half.X <= half.Y ? Mathf.Sign(feet.X - centre.X) : Mathf.Sign(feet.Y - centre.Y);
		if (bank == 0) return;
		if (banks.TryGetValue(wizard, out int was) && was != bank) Crossings++;
		banks[wizard] = bank;
	}

	public override void _Process(double delta)
	{
		time += (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (stage.HazardKind == "lava") DrawLava();
		else DrawBog();
	}

	// ---- Lava ----

	void DrawLava()
	{
		for (int i = 0; i + 1 < stage.Hazards.Length; i += 2)
		{
			Rect2 river = PixelRect(stage.Hazards[i], stage.Hazards[i + 1]);
			// Banks that wobble, so it reads as a flow and not a painted stripe. Each band is the
			// same wobbly outline, shrunk.
			DrawColoredPolygon(Bank(river, 10.0f), Crust);
			DrawColoredPolygon(Bank(river, 0.0f), Glow);
			DrawColoredPolygon(Bank(river, -12.0f), Lava);

			// Bright streaks drifting down the flow.
			for (int s = 0; s < 14; s++)
			{
				float x = river.Position.X + river.Size.X * (0.2f + 0.6f * ((s * 0.618f) % 1.0f));
				float y = river.Position.Y + (s * 137.0f + time * 70.0f) % river.Size.Y;
				float length = 30.0f + (s % 4) * 14.0f;
				DrawLine(new Vector2(x, y), new Vector2(x + Mathf.Sin(time + s) * 6.0f, y + length), new Color(Hot, 0.85f), 5.0f, true);
			}
		}
		// Bubbles that swell and pop.
		foreach ((Vector2 at, float phase, float size) in bubbles)
		{
			float f = (time * 0.6f + phase) % 1.0f;
			if (f > 0.7f) continue;
			float r = size * (0.3f + f);
			DrawCircle(at, r + 2.5f, Glow);
			DrawCircle(at, r, Hot);
		}

		// Bridges: stone slabs with a hard shadow on the lava, planks marked across them.
		for (int i = 0; i + 1 < stage.Bridges.Length; i += 2)
		{
			Rect2 bridge = PixelRect(stage.Bridges[i], stage.Bridges[i + 1]);
			DrawRect(new Rect2(bridge.Position + new Vector2(10, 12), bridge.Size), new Color(0.05f, 0.02f, 0.02f, 0.55f));
			DrawRect(bridge.Grow(5.0f), BridgeOutline);
			DrawRect(bridge, Stone);
			int planks = Mathf.Max(3, (int)(bridge.Size.X / 34.0f));
			for (int p = 1; p < planks; p++)
			{
				float x = bridge.Position.X + bridge.Size.X * p / planks;
				DrawLine(new Vector2(x, bridge.Position.Y), new Vector2(x, bridge.End.Y), StoneDark, 4.0f);
			}
			// Lit top edge, shaded bottom edge.
			DrawRect(new Rect2(bridge.Position, new Vector2(bridge.Size.X, 7)), Stone.Lightened(0.2f));
			DrawRect(new Rect2(bridge.Position.X, bridge.End.Y - 9, bridge.Size.X, 9), StoneDark);
		}
	}

	/// <summary>A rectangle whose long sides wobble, grown (or shrunk) by <paramref name="by"/> pixels.</summary>
	Vector2[] Bank(Rect2 river, float by)
	{
		List<Vector2> points = new();
		const int Steps = 40;
		for (int k = 0; k <= Steps; k++)
		{
			float y = river.Position.Y + river.Size.Y * k / Steps;
			points.Add(new Vector2(river.Position.X - by + Wobble(y, 0.0f), y));
		}
		for (int k = Steps; k >= 0; k--)
		{
			float y = river.Position.Y + river.Size.Y * k / Steps;
			points.Add(new Vector2(river.End.X + by + Wobble(y, 2.0f), y));
		}
		return points.ToArray();
	}

	static float Wobble(float y, float seed) => Mathf.Sin(y * 0.021f + seed) * 9.0f + Mathf.Sin(y * 0.057f + seed * 3.0f) * 4.0f;

	// ---- Bog ----

	void DrawBog()
	{
		for (int i = 0; i + 1 < stage.Hazards.Length; i += 2)
		{
			Vector2 centre = Units.U(stage.Hazards[i]), radii = stage.Hazards[i + 1] * Units.PixelsPerUnit;
			// A thick mud bank, murky water, and a darker deep middle.
			DrawColoredPolygon(Blob(centre, radii, 12.0f, i), Mud);
			DrawColoredPolygon(Blob(centre, radii, 0.0f, i), Water);
			DrawColoredPolygon(Blob(centre + new Vector2(radii.X * 0.08f, radii.Y * 0.1f), radii * 0.6f, 0.0f, i + 3), Deep);
			// Two small patches of scum drifting about.
			for (int s = 0; s < 2; s++)
			{
				Vector2 drift = new(Mathf.Sin(time * 0.35f + i + s * 2.0f) * radii.X * 0.45f, Mathf.Cos(time * 0.28f + i + s * 3.0f) * radii.Y * 0.4f);
				DrawColoredPolygon(Blob(centre + drift, radii * 0.16f, 0.0f, i + 5 + s), new Color(Scum, 0.9f));
			}
			// Glints of light on the surface, top left, so it reads as wet.
			DrawArc(centre + new Vector2(-radii.X * 0.35f, -radii.Y * 0.35f), radii.Y * 0.35f, Mathf.Pi * 1.05f, Mathf.Pi * 1.45f, 12, new Color(Glint, 0.85f), 4.0f, true);
			DrawArc(centre + new Vector2(-radii.X * 0.1f, -radii.Y * 0.55f), radii.Y * 0.18f, Mathf.Pi * 1.1f, Mathf.Pi * 1.5f, 8, new Color(Glint, 0.7f), 3.0f, true);
		}
		// Bubbles rising through the murk and popping.
		foreach ((Vector2 at, float phase, float size) in bubbles)
		{
			float f = (time * 0.45f + phase) % 1.0f;
			if (f > 0.8f) continue;
			float r = size * 0.6f * (0.4f + f);
			DrawArc(at, r, 0.0f, Mathf.Tau, 16, Bubble, 2.5f, true);
			DrawCircle(at + new Vector2(-r * 0.35f, -r * 0.35f), 1.8f, Bubble);
			// The ripple it leaves, spreading flat on the water.
			DrawSetTransform(at, 0.0f, new Vector2(1.0f, 0.5f));
			DrawArc(Vector2.Zero, size * (0.8f + f * 2.0f), 0.0f, Mathf.Tau, 20, new Color(Glint, 0.5f * (1.0f - f)), 2.0f, true);
			DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
		}
	}

	/// <summary>An ellipse with a gently irregular edge, grown by <paramref name="by"/> pixels.</summary>
	static Vector2[] Blob(Vector2 centre, Vector2 radii, float by, int seed)
	{
		const int Steps = 40;
		Vector2[] points = new Vector2[Steps];
		for (int k = 0; k < Steps; k++)
		{
			float angle = k * Mathf.Tau / Steps;
			float wobble = 1.0f + 0.06f * Mathf.Sin(angle * 3.0f + seed) + 0.04f * Mathf.Sin(angle * 5.0f + seed * 2.0f);
			points[k] = centre + new Vector2(Mathf.Cos(angle) * (radii.X * wobble + by), Mathf.Sin(angle) * (radii.Y * wobble + by));
		}
		return points;
	}

	/// <summary>
	/// A still, simplified drawing of a stage's hazard for its Stage Select tile: arena pixels
	/// mapped by <paramref name="scale"/> about <paramref name="centre"/>.
	/// </summary>
	public static void DrawPreview(CanvasItem canvas, StageData stage, Vector2 centre, float scale)
	{
		for (int i = 0; i + 1 < stage.Hazards.Length; i += 2)
		{
			Vector2 c = centre + Units.U(stage.Hazards[i]) * scale, h = stage.Hazards[i + 1] * Units.PixelsPerUnit * scale;
			if (stage.HazardKind == "lava")
			{
				canvas.DrawRect(new Rect2(c - h, h * 2.0f).Grow(3.0f), Crust);
				canvas.DrawRect(new Rect2(c - h, h * 2.0f), Glow);
				canvas.DrawRect(new Rect2(c - h, h * 2.0f).Grow(-3.0f), Lava);
			}
			else
			{
				canvas.DrawColoredPolygon(Blob(c, h, 3.0f, i), Mud);
				canvas.DrawColoredPolygon(Blob(c, h, 0.0f, i), Water);
			}
		}
		for (int i = 0; i + 1 < stage.Bridges.Length; i += 2)
		{
			Vector2 c = centre + Units.U(stage.Bridges[i]) * scale, h = stage.Bridges[i + 1] * Units.PixelsPerUnit * scale;
			canvas.DrawRect(new Rect2(c - h, h * 2.0f).Grow(2.0f), BridgeOutline);
			canvas.DrawRect(new Rect2(c - h, h * 2.0f), Stone);
		}
	}

	static Rect2 PixelRect(Vector2 centre, Vector2 half)
	{
		Vector2 c = Units.U(centre), h = half * Units.PixelsPerUnit;
		return new Rect2(c - h, h * 2.0f);
	}
}
