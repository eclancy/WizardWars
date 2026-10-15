using System.Collections.Generic;
using Godot;

/// <summary>
/// Chain Lightning, Electricity's fourth ability (restored from Unity). A bolt leaps from the
/// caster to the nearest enemy within range, then on to the nearest enemy not yet struck within
/// jumping distance of the last one, up to <c>Count</c> targets, one hop every <c>Interval</c>.
/// Each bolt is a jagged generated line - a yellow body round a white core - that flickers out.
///
/// Unity's version hit the three nearest players anywhere on the map for 3 damage each at a
/// cost of 85; here it has a real range, real damage, and needs no line of sight. With nobody
/// in range the bolt cracks out along the aim and fizzles.
/// </summary>
public partial class ChainLightning : Node2D
{
	const float BoltLife = 0.3f;

	Wizard caster;
	int casterTeam;
	AbilityData data;
	readonly HashSet<Wizard> struck = new();
	Vector2 from;
	float elapsed, nextHop;
	int hops;
	bool done;

	public static ChainLightning Create(Wizard caster, AbilityData data)
	{
		return new ChainLightning { caster = caster, casterTeam = caster.Team, data = data, from = caster.GlobalPosition, ZIndex = 9 };
	}

	public override void _PhysicsProcess(double delta)
	{
		elapsed += (float)delta;
		if (!done && elapsed >= nextHop)
		{
			nextHop = elapsed + data.Interval;
			Hop();
		}
		if (done && GetChildCount() == 0) QueueFree();
	}

	void Hop()
	{
		// The first jump is measured from the caster, the rest from the last wizard struck.
		float reach = Units.U(hops == 0 ? data.Range : data.Radius);
		Wizard next = null;
		float best = reach * reach;
		foreach (Wizard wizard in Battle.Current.Wizards)
		{
			if (struck.Contains(wizard) || !Combat.CanAffect(caster, casterTeam, wizard)) continue;
			float distance = wizard.GlobalPosition.DistanceSquaredTo(from);
			if (distance <= best) { best = distance; next = wizard; }
		}

		if (next == null)
		{
			// Nobody to jump to. The very first bolt still cracks out along the aim so the cast
			// reads; later hops just end the chain.
			if (hops == 0 && GodotObject.IsInstanceValid(caster)) AddBolt(from, from + caster.AimDirection * Units.U(data.Range * 0.6f));
			done = true;
			return;
		}

		struck.Add(next);
		AddBolt(from, next.GlobalPosition);
		Sfx.Play("res://assets/sfx/electricityball.wav", -6.0f);
		next.TakeDamage(data.Damage);
		from = next.GlobalPosition;
		hops++;
		if (hops >= data.Count) done = true;
	}

	void AddBolt(Vector2 start, Vector2 end) => AddChild(Bolt.Create(start, end, BoltLife));
}
