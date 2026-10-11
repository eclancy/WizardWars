using Godot;

/// <summary>
/// Frozen ground: anyone moving on it slides and cannot steer. Telegraph (FrostWizard.FormIcePatch):
/// six puddles seep out around the target and slide together over 0.55 s, then the sheet glosses
/// over from wet blue to plain ice over 0.28 s. It lasts its lifetime from the end of the gloss.
/// </summary>
public partial class IcePatch : GroundPatch
{
	const int PuddleCount = 6;
	const float GatherDuration = 0.55f;
	const float GlossDuration = 0.28f;

	readonly Sprite2D[] puddles = new Sprite2D[PuddleCount];
	readonly Vector2[] starts = new Vector2[PuddleCount];

	protected override string ArtPath => "res://assets/art/attacks/ice-patch.png";
	protected override Modifier EffectOn => new() { Slide = true };

	public static IcePatch Create(Wizard caster, Vector2 position, float unityScale, float lifetime)
	{
		IcePatch patch = new();
		patch.Setup(caster, position, unityScale, lifetime);
		for (int i = 0; i < PuddleCount; i++)
		{
			float angle = i / (float)PuddleCount * Mathf.Tau + (float)GD.RandRange(-0.3, 0.3);
			float distance = (float)GD.RandRange(0.45, 0.95) * unityScale;
			// Unity +y up, so the vertical squash (0.6) is applied before flipping.
			patch.starts[i] = Units.U(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * 0.6f * distance);
			Sprite2D puddle = ProceduralFx.Puddle.NewSprite(0.25f * unityScale);
			puddle.Position = patch.starts[i];
			puddle.Modulate = new Color(1, 1, 1, 0);
			patch.puddles[i] = puddle;
			patch.AddChild(puddle);
		}
		return patch;
	}

	protected override bool Form(float dt)
	{
		if (FormElapsed < GatherDuration)
		{
			float t = FormElapsed / GatherDuration;
			float eased = Mathf.SmoothStep(0.0f, 1.0f, t);
			for (int i = 0; i < PuddleCount; i++)
			{
				puddles[i].Position = starts[i].Lerp(Vector2.Zero, eased);
				puddles[i].Scale = Vector2.One * ProceduralFx.Scale(Mathf.Lerp(0.25f, 0.7f, eased) * UnityScale);
				puddles[i].Modulate = new Color(1, 1, 1, Mathf.Lerp(0.0f, 0.85f, Mathf.Min(1.0f, t * 3.0f)));
			}
			return false;
		}

		foreach (Sprite2D puddle in puddles) puddle.Visible = false;
		Art.Visible = true;
		float g = Mathf.Min(1.0f, (FormElapsed - GatherDuration) / GlossDuration);
		Art.Scale = Vector2.One * Units.SpriteScale(UnityScale * Mathf.Lerp(0.45f, 1.0f, Mathf.SmoothStep(0.0f, 1.0f, g)));
		Art.Modulate = new Color(0.75f, 0.9f, 1.0f, 0.55f).Lerp(Colors.White, g);
		return g >= 1.0f;
	}

	// Ice freezes over any mud it lands on, as Unity's IcePatch destroyed mud patches.
	protected override void Interact()
	{
		foreach (GroundPatch other in Battle.Current.Patches.ToArray())
			if (other is MudPatch mud && mud.Active && Overlaps(mud)) mud.Remove();
	}
}
