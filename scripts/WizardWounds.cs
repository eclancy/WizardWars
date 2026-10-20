using System.Collections.Generic;
using Godot;

/// <summary>
/// Battle damage drawn over a wizard: as health falls past 80, 60, 40 and 20 percent, scuffs,
/// scratches and then rips appear on the robe and hat. Healing mends them, since they follow
/// health. Drawn in the flat cartoon style of the art: rips are dark holes with the same thick
/// black outline as the figure and a few frayed threads; scuffs are smudges of the cloth's own
/// colour, darkened, with lighter scratches.
///
/// It is a child of the wizard's body sprite, so it shakes, flashes and fades with it, and draws
/// in the portrait's texture pixels. All seven portraits share one 1200 x 1165 template (hat crown
/// about x 400-820, y 160-370, brim to y 520; robe below the beards from y 880 to the hem at
/// 1085, clear of the staff right of x 440), so one set of positions fits every wizard.
/// </summary>
public partial class WizardWounds : Node2D
{
	static readonly float[] Thresholds = { 0.8f, 0.6f, 0.4f, 0.2f };
	static readonly Color Outline = new(0.0f, 0.0f, 0.0f);
	static readonly Color Hole = new(0.06f, 0.04f, 0.08f);
	/// <summary>Marks are drawn larger than their outlines below, to read at battle scale.</summary>
	const float Grow = 1.6f;
	static readonly Dictionary<Texture2D, (Color robe, Color hat)> cloth = new();

	readonly Wizard wizard;
	readonly Color robe = Colors.Gray, hat = Colors.Gray;
	int stage;

	public WizardWounds() { }

	public WizardWounds(Wizard wizard, Texture2D portrait)
	{
		this.wizard = wizard;
		Position = -portrait.GetSize() * 0.5f;
		(robe, hat) = Cloth(portrait);
	}

	/// <summary>The robe and hat colours, read once from the portrait.</summary>
	static (Color, Color) Cloth(Texture2D portrait)
	{
		if (cloth.TryGetValue(portrait, out (Color, Color) found)) return found;
		Image image = portrait.GetImage();
		(Color, Color) colours = image == null ? (Colors.Gray, Colors.Gray) : (image.GetPixel(800, 1000), image.GetPixel(700, 300));
		cloth[portrait] = colours;
		return colours;
	}

	/// <summary>How many of the damage stages a wizard on this much health shows, 0 to 4.</summary>
	public static int StageFor(float healthFraction)
	{
		int stage = 0;
		foreach (float threshold in Thresholds) if (healthFraction < threshold) stage++;
		return stage;
	}

	public int Stage => stage;

	public override void _Process(double delta)
	{
		if (wizard == null) return;
		int now = StageFor(wizard.Health / Wizard.MaxHealth);
		if (now == stage) return;
		stage = now;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (stage >= 1)
		{
			Scuff(new Vector2(640, 990), new Vector2(58, 30), robe);
			Scratches(new Vector2(800, 925), robe, -0.5f);
			Scuff(new Vector2(615, 240), new Vector2(40, 24), hat);
		}
		if (stage >= 2)
		{
			// A tear up from the hem.
			Rip(new Vector2[] { new(698, 1078), new(716, 1030), new(731, 1050), new(750, 985), new(766, 1042), new(781, 1024), new(792, 1078) },
				new[] { (new Vector2(716, 1030), new Vector2(706, 1012)), (new Vector2(750, 985), new Vector2(752, 962)), (new Vector2(781, 1024), new Vector2(795, 1008)) }, new Vector2(745, 1078));
		}
		if (stage >= 3)
		{
			// A slash across the hat's crown.
			Rip(new Vector2[] { new(640, 248), new(667, 258), new(660, 276), new(694, 284), new(684, 300), new(722, 326), new(690, 321), new(664, 300), new(672, 287), new(634, 266) },
				new[] { (new Vector2(640, 248), new Vector2(626, 238)), (new Vector2(722, 326), new Vector2(738, 336)), (new Vector2(694, 284), new Vector2(708, 278)) });
			Scuff(new Vector2(520, 1035), new Vector2(46, 28), robe);
			Scratches(new Vector2(430, 470), hat, 0.35f);
		}
		if (stage >= 4)
		{
			// A gash in the robe's side and a bite out of the brim.
			Rip(new Vector2[] { new(818, 892), new(846, 906), new(838, 926), new(874, 936), new(858, 956), new(892, 978), new(860, 973), new(830, 952), new(842, 938), new(808, 914) },
				new[] { (new Vector2(818, 892), new Vector2(806, 878)), (new Vector2(892, 978), new Vector2(906, 990)), (new Vector2(874, 936), new Vector2(890, 930)) });
			Rip(new Vector2[] { new(948, 448), new(922, 458), new(936, 466), new(914, 476), new(944, 482) },
				new[] { (new Vector2(922, 458), new Vector2(908, 452)) }, new Vector2(950, 465), 1.25f);
			Scuff(new Vector2(765, 335), new Vector2(34, 20), hat);
		}
	}

	/// <summary>A smudge: three overlapping blots of the cloth darkened.</summary>
	void Scuff(Vector2 centre, Vector2 radii, Color under)
	{
		radii *= Grow;
		Color smudge = new(under.Darkened(0.45f), 0.55f);
		Blot(centre, radii, smudge);
		Blot(centre + new Vector2(radii.X * 0.45f, -radii.Y * 0.3f), radii * 0.6f, smudge);
		Blot(centre + new Vector2(-radii.X * 0.5f, radii.Y * 0.25f), radii * 0.5f, smudge);
	}

	void Blot(Vector2 centre, Vector2 radii, Color colour)
	{
		Vector2[] points = new Vector2[20];
		for (int i = 0; i < points.Length; i++)
		{
			float angle = i * Mathf.Tau / points.Length;
			// A slightly lumpy edge, so it reads as dirt rather than a painted oval.
			float wobble = 1.0f + 0.12f * Mathf.Sin(angle * 3.0f + centre.X);
			points[i] = centre + new Vector2(Mathf.Cos(angle) * radii.X, Mathf.Sin(angle) * radii.Y) * wobble;
		}
		DrawColoredPolygon(points, colour);
	}

	/// <summary>Three short claw-like scratches, lighter than the cloth.</summary>
	void Scratches(Vector2 centre, Color under, float slant)
	{
		Color scratch = new(under.Lightened(0.45f), 0.85f);
		Vector2 along = new Vector2(1.0f, slant).Normalized() * 34.0f * Grow, across = new Vector2(-along.Y, along.X).Normalized() * 14.0f * Grow;
		for (int i = -1; i <= 1; i++)
		{
			Vector2 start = centre + across * i - along * (1.0f - 0.2f * Mathf.Abs(i));
			DrawLine(start, start + along * (2.0f - 0.4f * Mathf.Abs(i)), scratch, 8.0f, true);
		}
	}

	/// <summary>
	/// A hole torn in the cloth: dark inside, outlined like the art, with loose threads. It grows
	/// about <paramref name="anchor"/> (its middle unless given: a tear from the hem grows from the hem).
	/// </summary>
	void Rip(Vector2[] edge, (Vector2 from, Vector2 to)[] threads, Vector2? anchor = null, float grow = Grow)
	{
		Vector2 middle = Vector2.Zero;
		foreach (Vector2 point in edge) middle += point / edge.Length;
		Vector2 origin = anchor ?? middle;
		Vector2 Scaled(Vector2 point) => origin + (point - origin) * grow;
		edge = System.Array.ConvertAll(edge, Scaled);
		threads = System.Array.ConvertAll(threads, t => (Scaled(t.from), Scaled(t.to)));
		DrawColoredPolygon(edge, Hole);
		Vector2[] loop = new Vector2[edge.Length + 1];
		edge.CopyTo(loop, 0);
		loop[edge.Length] = edge[0];
		DrawPolyline(loop, Outline, 9.0f, true);
		foreach ((Vector2 from, Vector2 to) in threads) DrawLine(from, to, Outline, 7.0f, true);
	}
}
