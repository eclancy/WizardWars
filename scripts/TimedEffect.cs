using Godot;

/// <summary>
/// A modifier held on one wizard for a few seconds, and drawn on them while it lasts:
/// - Entangle's root: thorny vines coiled round their feet
/// - Night Grasp's silence: shadowy wisps circling their head
///
/// The modifier is keyed to this node, so it lifts when the time is up, and also if the effect
/// is freed early (a battle ending, say).
/// </summary>
public partial class TimedEffect : Node2D
{
	public enum Look { Vines, Shadow }

	static readonly Color Ink = new(0.04f, 0.03f, 0.05f);

	Wizard target;
	Look look;
	float left, total, time;

	public static TimedEffect Apply(Wizard target, Modifier effect, float seconds, Look look)
	{
		TimedEffect timed = new() { target = target, look = look, left = seconds, total = seconds, ZIndex = 3, GlobalPosition = target.GlobalPosition };
		target.Battle.AddEffect(timed);
		target.Status.Set(timed, effect);
		return timed;
	}

	public override void _PhysicsProcess(double delta)
	{
		left -= (float)delta;
		if (left <= 0.0f || !GodotObject.IsInstanceValid(target) || !target.Alive)
		{
			if (GodotObject.IsInstanceValid(target)) target.Status.Remove(this);
			QueueFree();
			return;
		}
		GlobalPosition = target.GlobalPosition;
	}

	public override void _Process(double delta)
	{
		time += (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		// Grows in quickly and withers away at the end.
		float grow = Mathf.Min(1.0f, (total - left) / 0.15f) * Mathf.Min(1.0f, left / 0.3f);
		if (look == Look.Vines) DrawVines(grow);
		else DrawWisps(grow);
	}

	void DrawVines(float grow)
	{
		Color vine = new(0.3f, 0.6f, 0.2f), thorn = new(0.2f, 0.42f, 0.14f);
		Vector2 feet = new(0.0f, Units.U(0.55f));
		// Three vines coiling up round the feet, each an ellipse arc rising a little.
		for (int v = 0; v < 3; v++)
		{
			float start = v * Mathf.Tau / 3 + 0.4f;
			Vector2[] coil = new Vector2[18];
			for (int i = 0; i < coil.Length; i++)
			{
				float t = i / (float)(coil.Length - 1) * grow;
				float angle = start + t * Mathf.Pi * 1.6f;
				coil[i] = feet + new Vector2(Mathf.Cos(angle) * Units.U(0.48f), Mathf.Sin(angle) * Units.U(0.2f) - t * Units.U(0.55f));
			}
			DrawPolyline(coil, Ink, 9.0f, true);
			DrawPolyline(coil, vine, 5.0f, true);
			for (int i = 3; i < coil.Length; i += 4)
			{
				Vector2 at = coil[i], up = (coil[i] - coil[i - 1]).Normalized().Orthogonal() * 7.0f;
				DrawColoredPolygon(new[] { at - up * 0.4f + (coil[i] - coil[i - 1]).Normalized() * 3.0f, at + up, at - up * 0.4f - (coil[i] - coil[i - 1]).Normalized() * 3.0f }, thorn);
			}
		}
	}

	void DrawWisps(float grow)
	{
		Color wisp = new(0.3f, 0.24f, 0.42f, 0.9f), light = new(0.62f, 0.52f, 0.86f);
		Vector2 head = new(0.0f, -Units.U(0.75f));
		for (int w = 0; w < 3; w++)
		{
			float angle = time * 2.6f + w * Mathf.Tau / 3;
			Vector2[] tail = new Vector2[10];
			for (int i = 0; i < tail.Length; i++)
			{
				float a = angle - i * 0.18f;
				tail[i] = head + new Vector2(Mathf.Cos(a) * Units.U(0.55f), Mathf.Sin(a) * Units.U(0.22f)) * grow;
			}
			DrawPolyline(tail, Ink, 8.0f, true);
			DrawPolyline(tail, wisp, 5.0f, true);
			DrawCircle(tail[0], 5.0f * grow, light);
		}
	}
}
