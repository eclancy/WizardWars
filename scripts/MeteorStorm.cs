using Godot;

/// <summary>
/// Drops one meteor per interval, then hands control back once the last has landed. While it
/// lasts the sky glows red over the whole arena, so everyone knows the storm is on.
/// </summary>
public partial class MeteorStorm : Node2D
{
	Polygon2D sky;
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

	public override void _Ready()
	{
		Vector2 half = Battle.CoveredView;
		sky = new Polygon2D
		{
			Polygon = new[] { -half, new Vector2(half.X, -half.Y), half, new Vector2(-half.X, half.Y) },
			Color = new Color(0.55f, 0.08f, 0.0f, 0.0f),
			TopLevel = true,
			ZIndex = -80,
			ZAsRelative = false,
		};
		AddChild(sky);
	}

	public override void _PhysicsProcess(double delta)
	{
		elapsed += (float)delta;
		float total = (data.Count - 1) * data.Interval + data.Duration + 0.3f;
		sky.Color = new Color(sky.Color, 0.3f * Mathf.Min(1.0f, elapsed / 0.4f) * Mathf.Clamp((total - elapsed) / 0.5f, 0.0f, 1.0f));
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
