using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// The wind-up every ultimate plays before it goes off, so everyone sees it coming. For
/// <see cref="Duration"/> seconds:
/// - the floor darkens round the arena
/// - streaks of the caster's element colour pour in towards them
/// - rings tighten at their feet
/// - the ultimate's name pops up over their head
///
/// Then a flash and a jolt of the camera, and only then does the ultimate itself start: nothing
/// is hurt, slowed or stopped during the build-up. If the caster dies first, the ultimate never
/// happens.
/// </summary>
public partial class UltimateBuildUp : Node2D
{
	public const float Duration = 1.2f;
	const float Afterglow = 0.45f;
	const int Streaks = 30;

	Wizard caster;
	Color colour;
	Action release, cancel;
	float elapsed;
	bool released;
	Polygon2D dim;
	Label title;
	readonly List<(float angle, float distance, float phase)> streaks = new();

	public static UltimateBuildUp Create(Wizard caster, string name, Color colour, Action release, Action cancel)
	{
		UltimateBuildUp buildUp = new() { caster = caster, colour = colour, release = release, cancel = cancel, ZIndex = 8, GlobalPosition = caster.GlobalPosition };
		for (int i = 0; i < Streaks; i++)
			buildUp.streaks.Add(((float)GD.RandRange(0.0, Mathf.Tau), (float)GD.RandRange(2.4, 3.6), (float)GD.RandRange(0.0, 1.0)));

		// The floor darkens, so the caster and their glow stand out. It covers the whole view and
		// sits just over the floor, under the wizards and everything else.
		Vector2 half = Battle.CoveredView;
		buildUp.dim = new Polygon2D
		{
			Polygon = new[] { -half, new Vector2(half.X, -half.Y), half, new Vector2(-half.X, half.Y) },
			Color = new Color(0.02f, 0.0f, 0.06f, 0.0f),
			TopLevel = true,
			ZIndex = -80,
			ZAsRelative = false,
		};
		buildUp.AddChild(buildUp.dim);

		// The ultimate's name, in the caster's colour, popping up over their head.
		buildUp.title = Ui.Heading(name.ToUpper() + "!", 44, colour);
		buildUp.title.AddThemeConstantOverride("outline_size", 12);
		buildUp.title.Size = new Vector2(700, 64);
		buildUp.title.Position = new Vector2(-350, -Units.U(2.35f));
		buildUp.title.PivotOffset = buildUp.title.Size * 0.5f;
		buildUp.title.Scale = Vector2.One * 1.6f;
		buildUp.AddChild(buildUp.title);
		buildUp.title.CreateTween().TweenProperty(buildUp.title, "scale", Vector2.One, 0.22).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

		Sfx.Play("res://assets/sfx/ultimate-charge.wav", -3.0f);
		return buildUp;
	}

	public override void _PhysicsProcess(double delta)
	{
		elapsed += (float)delta;
		if (!released)
		{
			if (!GodotObject.IsInstanceValid(caster) || !caster.Alive)
			{
				cancel?.Invoke();
				QueueFree();
				return;
			}
			GlobalPosition = caster.GlobalPosition;
			if (elapsed >= Duration)
			{
				released = true;
				Battle.Current?.Shake(10.0f, 0.35f);
				Sfx.Play("res://assets/sfx/ultimate-release.wav", -1.0f);
				release?.Invoke();
			}
		}
		else if (elapsed >= Duration + Afterglow) QueueFree();
	}

	public override void _Process(double delta)
	{
		float t = Mathf.Min(1.0f, elapsed / Duration);
		float after = released ? Mathf.Clamp((elapsed - Duration) / Afterglow, 0.0f, 1.0f) : 0.0f;
		dim.Color = new Color(dim.Color, 0.42f * Mathf.SmoothStep(0.0f, 0.3f, t) * (1.0f - after));
		// The name shudders as the power builds, then rises and fades once it goes off.
		float shudder = released ? 0.0f : t * 3.0f;
		title.Position = new Vector2(-350 + (float)GD.RandRange(-shudder, shudder), -Units.U(2.35f) - after * 40.0f);
		title.Modulate = new Color(1, 1, 1, 1.0f - after);
		QueueRedraw();
	}

	public override void _Draw()
	{
		float t = Mathf.Min(1.0f, elapsed / Duration);
		Color bright = colour.Lightened(0.35f);
		Vector2 feet = new(0.0f, Units.U(0.6f));

		if (!released)
		{
			// A glow at the feet that swells as the power gathers.
			DrawSetTransform(feet, 0.0f, new Vector2(1.0f, 0.45f));
			DrawCircle(Vector2.Zero, Units.U(0.5f + 0.9f * t), new Color(colour, 0.18f + 0.25f * t));
			DrawCircle(Vector2.Zero, Units.U(0.25f + 0.5f * t), new Color(bright, 0.3f + 0.3f * t));
			// Rings that keep tightening onto the caster, faster as it nears.
			for (int i = 0; i < 2; i++)
			{
				float ring = 1.0f - (elapsed * (1.6f + 2.0f * t) + i * 0.5f) % 1.0f;
				DrawArc(Vector2.Zero, Units.U(0.5f + 2.2f * ring), 0.0f, Mathf.Tau, 48, new Color(bright, (1.0f - ring) * 0.9f), 5.0f, true);
			}
			DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);

			// Streaks pouring in from all round, each a short line with a bright head.
			foreach ((float angle, float distance, float phase) in streaks)
			{
				float f = (elapsed * (1.1f + 1.4f * t) + phase) % 1.0f;
				float r = Units.U(distance) * (1.0f - f) * (1.0f - f) + Units.U(0.3f);
				Vector2 head = Vector2.FromAngle(angle) * r, tail = Vector2.FromAngle(angle) * (r + Units.U(0.45f));
				float alpha = Mathf.Min(1.0f, f * 3.0f) * (0.5f + 0.5f * t);
				DrawLine(tail, head, new Color(colour, alpha * 0.8f), 4.0f, true);
				DrawCircle(head, 3.5f, new Color(bright, alpha));
			}
			return;
		}

		// Released: a white flash and a ring in the caster's colour bursting outward.
		float after = Mathf.Clamp((elapsed - Duration) / Afterglow, 0.0f, 1.0f);
		DrawCircle(Vector2.Zero, Units.U(0.6f + 1.4f * after), new Color(1, 1, 1, 0.55f * (1.0f - after)));
		DrawArc(Vector2.Zero, Units.U(0.8f + 4.0f * Mathf.Sqrt(after)), 0.0f, Mathf.Tau, 64, new Color(bright, 1.0f - after), 10.0f * (1.0f - after) + 2.0f, true);
	}
}
