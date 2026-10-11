using Godot;

/// <summary>
/// Mud: anyone in it is slowed, and fire goes out. Telegraph (EarthWizard.FormMudPatch): eight
/// clods of earth spurt up out of the ground on parabolic arcs over 0.45 s and land around the
/// point, then the patch spreads out from there over 0.3 s, wider before taller.
/// </summary>
public partial class MudPatch : GroundPatch
{
	const int GlobCount = 8;
	const float SpurtDuration = 0.45f;
	const float SpreadDuration = 0.3f;

	float slow;
	readonly Sprite2D[] globs = new Sprite2D[GlobCount];
	readonly Vector2[] landings = new Vector2[GlobCount];
	readonly float[] heights = new float[GlobCount];

	protected override string ArtPath => "res://assets/art/attacks/mud-patch.png";
	protected override Modifier EffectOn => new() { Slow = slow };

	public static MudPatch Create(Wizard caster, Vector2 position, float unityScale, float lifetime, float slowPercent)
	{
		MudPatch patch = new() { slow = slowPercent };
		// Unity's mud lifetime started when the patch began spreading.
		patch.Setup(caster, position, unityScale, Mathf.Max(0.1f, lifetime - SpreadDuration));
		for (int i = 0; i < GlobCount; i++)
		{
			float angle = i / (float)GlobCount * Mathf.Tau + (float)GD.RandRange(-0.25, 0.25);
			float distance = (float)GD.RandRange(0.3, 0.9) * unityScale;
			patch.landings[i] = Units.U(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * 0.55f * distance);
			patch.heights[i] = Units.U((float)GD.RandRange(0.7, 1.5));
			Sprite2D glob = ProceduralFx.MudGlob.NewSprite((float)GD.RandRange(0.25, 0.45));
			glob.ZIndex = 7;
			patch.globs[i] = glob;
			patch.AddChild(glob);
		}
		return patch;
	}

	protected override bool Form(float dt)
	{
		if (FormElapsed < SpurtDuration)
		{
			float t = FormElapsed / SpurtDuration;
			for (int i = 0; i < GlobCount; i++)
			{
				// A parabola that starts and ends on the ground.
				float arc = heights[i] * 4.0f * t * (1.0f - t);
				globs[i].Position = Vector2.Zero.Lerp(landings[i], t) + new Vector2(0.0f, -arc);
				globs[i].Rotation += Mathf.DegToRad(220.0f) * dt;
			}
			return false;
		}

		foreach (Sprite2D glob in globs) glob.Visible = false;
		Art.Visible = true;
		float s = Mathf.SmoothStep(0.0f, 1.0f, Mathf.Min(1.0f, (FormElapsed - SpurtDuration) / SpreadDuration));
		float baseScale = Units.SpriteScale(UnityScale);
		Art.Scale = new Vector2(baseScale * Mathf.Lerp(0.3f, 1.0f, s), baseScale * Mathf.Lerp(0.15f, 1.0f, s));
		return s >= 1.0f;
	}
}
