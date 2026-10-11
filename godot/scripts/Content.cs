using System.Collections.Generic;
using Godot;

/// <summary>
/// Loads the game's data resources. The lists are explicit rather than scanned so the order
/// is fixed (it is the order on Character Select and Stage Select); RegressionChecks verifies
/// every entry loads and is complete, so a missing or misnamed file fails the checks instead
/// of failing silently in a menu.
/// </summary>
public static class Content
{
	public static readonly string[] WizardPaths =
	{
		"res://data/wizards/fire.tres",
		"res://data/wizards/frost.tres",
		"res://data/wizards/earth.tres",
		"res://data/wizards/electricity.tres",
	};

	public static readonly string[] StagePaths =
	{
		"res://data/stages/woodland.tres",
		"res://data/stages/frost-tundra.tres",
		"res://data/stages/scorched-canyon.tres",
		"res://data/stages/storm-plateau.tres",
	};

	static WizardData[] wizards;
	static StageData[] stages;

	// Default cache mode on purpose: Wizard Survivors found CacheMode.Replace returned blank
	// instances on first load.
	public static WizardData[] Wizards => wizards ??= Load<WizardData>(WizardPaths);
	public static StageData[] Stages => stages ??= Load<StageData>(StagePaths);

	static T[] Load<T>(string[] paths) where T : Resource
	{
		List<T> loaded = new();
		foreach (string path in paths)
		{
			T resource = ResourceLoader.Load<T>(path);
			if (resource == null) GD.PrintErr($"Content: could not load {path}");
			loaded.Add(resource);
		}
		return loaded.ToArray();
	}

	public static WizardData WizardById(string id)
	{
		foreach (WizardData wizard in Wizards) if (wizard != null && wizard.Id == id) return wizard;
		return null;
	}

	public static Texture2D Texture(string path) => ResourceLoader.Load<Texture2D>(path);
	public static AudioStream Sound(string path) => ResourceLoader.Load<AudioStream>(path);
}
