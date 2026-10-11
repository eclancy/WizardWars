using Godot;

/// <summary>
/// The checks dotnet build cannot do: project settings, data wiring, and the game's number
/// rules. Run headless by tools/check.sh (`-- --checks`), which reads the exit code.
///
/// Every line is printed as "RegressionChecks: PASS|FAIL name - detail" so a failure is
/// greppable. Add a check whenever a rule is easy to break silently - a tuning invariant, a
/// resource path, a physics layer.
/// </summary>
public static class RegressionChecks
{
	static int passed;
	static int failed;

	/// <summary>Runs every check and returns the number that failed.</summary>
	public static int RunAll()
	{
		passed = 0;
		failed = 0;

		CheckPhysicsLayers();
		CheckViewport();
		CheckUnits();

		GD.Print($"RegressionChecks: {passed} passed, {failed} failed");
		return failed;
	}

	static void Expect(bool condition, string name, string detail = "")
	{
		if (condition) passed++; else failed++;
		string suffix = detail.Length > 0 ? " - " + detail : "";
		if (condition) GD.Print($"RegressionChecks: PASS {name}{suffix}");
		else GD.PrintErr($"RegressionChecks: FAIL {name}{suffix}");
	}

	/// <summary>Collision masks are set by layer number; renaming or reordering breaks them silently.</summary>
	static void CheckPhysicsLayers()
	{
		string[] expected = { "solids", "wizards", "projectiles", "zones" };
		for (int i = 0; i < expected.Length; i++)
		{
			string actual = (string)ProjectSettings.GetSetting($"layer_names/2d_physics/layer_{i + 1}", "");
			Expect(actual == expected[i], $"physics layer {i + 1}", $"expected '{expected[i]}', found '{actual}'");
		}
	}

	static void CheckViewport()
	{
		int width = (int)ProjectSettings.GetSetting("display/window/size/viewport_width", 0);
		int height = (int)ProjectSettings.GetSetting("display/window/size/viewport_height", 0);
		Expect(width == 1920 && height == 1080, "viewport is 1920x1080", $"{width}x{height}");
	}

	/// <summary>The Unity arena has to fit the 1080p viewport at the chosen scale.</summary>
	static void CheckUnits()
	{
		Vector2 arena = Units.ArenaSize * Units.PixelsPerUnit;
		Expect(arena.Y <= 1080.0f, "arena height fits 1080", $"{arena.Y:0} px");
		Expect(Units.U(1.0f, 1.0f).Y < 0.0f, "Unity +y maps to Godot up");
	}
}
