using Godot;

/// <summary>
/// A stage's lava or bog, drawn over its Stage Select tile. The arena hazards are drawn live in
/// battle, so they are not in the floor image the tile shows. The mapping matches how the tile
/// covers its preview: the floor texture (<see cref="Battle.CoveredView"/>) scaled to cover the
/// tile, centred.
/// </summary>
public partial class HazardPreview : Control
{
	readonly StageData stage;

	public HazardPreview() { }

	public HazardPreview(StageData stage)
	{
		this.stage = stage;
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Draw()
	{
		if (stage == null) return;
		float scale = Mathf.Max(Size.X / Battle.CoveredView.X, Size.Y / Battle.CoveredView.Y);
		StageHazard.DrawPreview(this, stage, Size * 0.5f, scale);
	}
}
