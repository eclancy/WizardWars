using Godot;

/// <summary>
/// The ring round a wizard's feet in Teams: a thick band in the team colour with a dark outline
/// and a light fill inside, flat on the ground. Bold enough to read on every floor, snow included.
/// </summary>
public partial class TeamMarker : Node2D
{
	static readonly Vector2 Radii = new(Units.U(0.58f), Units.U(0.24f));

	readonly Color colour;

	public TeamMarker() { }

	public TeamMarker(Color colour) => this.colour = colour;

	public override void _Draw()
	{
		// A real ellipse (not a squashed circle), so the band is equally thick all round.
		Vector2[] points = new Vector2[49];
		for (int i = 0; i < points.Length; i++)
		{
			float angle = i * Mathf.Tau / (points.Length - 1);
			points[i] = new Vector2(Mathf.Cos(angle) * Radii.X, Mathf.Sin(angle) * Radii.Y);
		}
		DrawColoredPolygon(points[..^1], new Color(colour, 0.3f));
		DrawPolyline(points, new Color(Ui.Ink, 0.85f), 12.0f, true);
		DrawPolyline(points, colour, 7.0f, true);
	}
}
