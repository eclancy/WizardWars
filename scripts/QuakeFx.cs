using Godot;

/// <summary>
/// How Earthquake looks: the ground itself trembling round the Earth wizard, not a coloured disc.
/// Every <c>interval</c> seconds the ground jolts:
/// - fresh cracks split out from where he stands (<see cref="GroundCracks"/>), reaching most of
///   the way to the edge, and fade as the next ones open
/// - the camera jumps
/// - every enemy he has hold of shakes
///
/// The reach is marked by a ring of broken, dusty dashes at the real radius, which tremble too.
/// Must be a child of the <see cref="Aura"/> it draws.
/// </summary>
public partial class QuakeFx : Node2D
{
	static readonly Color Edge = new(0.36f, 0.24f, 0.13f);

	readonly float interval;
	float nextJolt, sinceJolt = 10.0f;

	public QuakeFx() { }

	public QuakeFx(float interval) => this.interval = Mathf.Max(0.2f, interval);

	Aura Aura => GetParent<Aura>();

	public override void _PhysicsProcess(double delta)
	{
		Aura aura = Aura;
		if (aura == null || !aura.IsInsideTree()) return;
		sinceJolt += (float)delta;
		if (aura.Elapsed < nextJolt) return;
		nextJolt = aura.Elapsed + interval;
		sinceJolt = 0.0f;

		Battle battle = Battle.Current;
		battle.AddEffect(GroundCracks.Create(aura.GlobalPosition + new Vector2(0.0f, Units.U(0.55f)), aura.Radius * 0.85f, 7, 10.0f, 0.22f, interval * 0.9f, 0.5f));
		battle.Shake(7.0f, 0.3f);
		Sfx.Play("res://assets/sfx/ground-crack.wav", -5.0f);
		foreach (Wizard wizard in battle.Wizards) if (aura.Holds(wizard)) wizard.Jolt(0.3f);
	}

	public override void _Process(double delta) => QueueRedraw();

	public override void _Draw()
	{
		Aura aura = Aura;
		if (aura == null) return;
		// The edge comes up out of the ground in the first moment, then trembles on each jolt.
		float rise = Mathf.Min(1.0f, aura.Elapsed / 0.3f);
		float tremble = Mathf.Max(0.0f, 1.0f - sinceJolt / 0.3f) * 5.0f;
		const int Dashes = 44;
		for (int i = 0; i < Dashes; i++)
		{
			float a0 = i * Mathf.Tau / Dashes, a1 = a0 + Mathf.Tau / Dashes * 0.55f;
			float wobble = Mathf.Sin(i * 7.3f + aura.Elapsed * 40.0f) * tremble;
			float r = aura.Radius * rise + wobble;
			DrawLine(Vector2.FromAngle(a0) * r, Vector2.FromAngle(a1) * r, new Color(Edge, 0.6f), 5.0f, true);
		}
	}
}
