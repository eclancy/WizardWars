using Godot;

/// <summary>
/// The root of the game. Screens (title, character select, stage select, battle, result) will
/// be swapped in and out as children of this node, so no state has to survive a scene change.
///
/// Command-line flags, passed after "--":
///   --checks   run RegressionChecks and quit with the number of failures as the exit code.
/// </summary>
public partial class GameRoot : Node2D
{
	public override void _Ready()
	{
		if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--checks") >= 0)
		{
			int failures = RegressionChecks.RunAll();
			GetTree().Quit(failures == 0 ? 0 : 1);
			return;
		}

		// Placeholder until the title screen exists (Phase 3).
		Label label = new()
		{
			Text = "Wizard Wars - Godot port in progress",
			Position = new Vector2(-400.0f, -40.0f),
			Size = new Vector2(800.0f, 80.0f),
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		label.AddThemeFontSizeOverride("font_size", 40);
		AddChild(label);
		AddChild(new Camera2D());
	}
}
