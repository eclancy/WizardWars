using System.Collections.Generic;
using Godot;

/// <summary>
/// Tailwind Dash, Wind's RB: the caster shoots <c>Range</c> units along the aim (or the way they
/// were moving) in <c>Duration</c> seconds. Enemies they pass through take <c>Damage</c> once and
/// are knocked aside by <c>Knockback</c> units. Walls and cover stop the dash, as they stop any
/// movement. Streaks of wind and fading after-images trail behind.
/// </summary>
public partial class WindDash : Node2D
{
	const float TrailTime = 0.35f;
	static readonly Color Ink = new(0.04f, 0.03f, 0.05f);

	Wizard caster;
	int casterTeam;
	AbilityData data;
	Vector2 direction;
	float elapsed;
	readonly HashSet<Wizard> hit = new();
	readonly List<(Vector2 at, float time)> trail = new();

	public static WindDash Create(Wizard caster, AbilityData data, Vector2 direction)
	{
		WindDash dash = new() { caster = caster, casterTeam = caster.Team, data = data, direction = direction.Normalized(), ZIndex = 1, TopLevel = true };
		// The shove fades linearly, so starting at twice the average speed covers the range.
		caster.Shove(dash.direction * Units.U(data.Range) * 2.0f / data.Duration, data.Duration);
		return dash;
	}

	public override void _PhysicsProcess(double delta)
	{
		elapsed += (float)delta;
		bool moving = elapsed < data.Duration && GodotObject.IsInstanceValid(caster) && caster.Alive;
		if (moving)
		{
			trail.Add((caster.GlobalPosition, elapsed));
			foreach (Wizard wizard in Battle.Current.Wizards)
			{
				if (hit.Contains(wizard) || !wizard.Alive || !Combat.CanAffect(caster, casterTeam, wizard)) continue;
				if (wizard.GlobalPosition.DistanceTo(caster.GlobalPosition) > Units.U(0.8f)) continue;
				hit.Add(wizard);
				wizard.TakeDamage(data.Damage);
				// Knocked aside, away from the line of the dash.
				Vector2 off = wizard.GlobalPosition - caster.GlobalPosition;
				Vector2 side = direction.Orthogonal() * (off.Dot(direction.Orthogonal()) >= 0 ? 1.0f : -1.0f);
				wizard.Shove((side + direction * 0.5f).Normalized() * Units.U(data.Knockback) * 2.0f / 0.25f, 0.25f);
			}
		}
		if (elapsed >= data.Duration + TrailTime) QueueFree();
	}

	public override void _Process(double delta) => QueueRedraw();

	public override void _Draw()
	{
		// After-images: pale silhouettes left along the path, fading.
		foreach ((Vector2 at, float time) in trail)
		{
			float age = elapsed - time;
			if (age > TrailTime) continue;
			float a = 1.0f - age / TrailTime;
			DrawCircle(at, Units.U(0.42f), new Color(0.6f, 1.0f, 0.88f, 0.22f * a));
		}
		// Streaks of wind along the dash, sliding back.
		if (trail.Count < 2) return;
		Vector2 from = trail[0].at, to = trail[^1].at, side = direction.Orthogonal();
		float a2 = Mathf.Clamp(1.0f - (elapsed - data.Duration) / TrailTime, 0.0f, 1.0f);
		for (int i = -2; i <= 2; i++)
		{
			Vector2 offset = side * i * 14.0f;
			Vector2 start = from.Lerp(to, 0.1f * Mathf.Abs(i)) + offset, end = to - direction * 20.0f + offset;
			DrawLine(start, end, new Color(Ink, 0.5f * a2), 7.0f, true);
			DrawLine(start, end, new Color(i % 2 == 0 ? new Color(0.92f, 1.0f, 0.98f) : new Color(0.45f, 0.92f, 0.78f), a2), 3.5f, true);
		}
	}
}
