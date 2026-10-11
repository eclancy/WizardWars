using Godot;

/// <summary>Drops one meteor per interval, then hands control back once the last has landed.</summary>
public partial class MeteorStorm : Node
{
	readonly Wizard caster;
	readonly AbilityData data;
	readonly System.Action onFinished;
	int dropped;
	float elapsed, nextDrop;

	public MeteorStorm() { }

	public MeteorStorm(Wizard caster, AbilityData data, System.Action onFinished)
	{
		this.caster = caster;
		this.data = data;
		this.onFinished = onFinished;
	}

	public override void _PhysicsProcess(double delta)
	{
		elapsed += (float)delta;
		if (dropped < data.Count && elapsed >= nextDrop)
		{
			nextDrop = elapsed + data.Interval;
			dropped++;
			// The Unity area was 9 x 5 units either side of the centre.
			Vector2 target = Units.U((float)GD.RandRange(-data.Range, data.Range), (float)GD.RandRange(-data.Range * 5.0 / 9.0, data.Range * 5.0 / 9.0));
			if (GodotObject.IsInstanceValid(caster)) Battle.Current.AddEffect(Meteor.Create(caster, data, target));
		}
		// Let the last meteor land before handing control back.
		if (dropped >= data.Count && elapsed >= nextDrop - data.Interval + data.Duration + 0.3f)
		{
			onFinished?.Invoke();
			QueueFree();
		}
	}
}
