using Godot;

/// <summary>
/// The player's options, kept between sessions in user://settings.cfg: music and sound volume,
/// fullscreen, and controller rumble. Changed on the options panel (<see cref="OptionsPanel"/>),
/// reachable from Character Select (X) and the pause menu.
///
/// Tests and screenshot runs never load or save it, so they always see the defaults.
/// </summary>
public static class Settings
{
	const string Path = "user://settings.cfg";

	/// <summary>0..1.</summary>
	public static float MusicVolume { get; private set; } = 0.8f;
	/// <summary>0..1.</summary>
	public static float SoundVolume { get; private set; } = 0.9f;
	public static bool Fullscreen { get; private set; } = true;
	public static bool Rumble { get; private set; } = true;

	/// <summary>False for tests and screenshots: nothing is read or written.</summary>
	public static bool Persist;

	public static void Load()
	{
		Persist = true;
		ConfigFile file = new();
		if (file.Load(Path) != Error.Ok) return;
		MusicVolume = Mathf.Clamp((float)file.GetValue("audio", "music", MusicVolume), 0.0f, 1.0f);
		SoundVolume = Mathf.Clamp((float)file.GetValue("audio", "sound", SoundVolume), 0.0f, 1.0f);
		Fullscreen = (bool)file.GetValue("display", "fullscreen", Fullscreen);
		Rumble = (bool)file.GetValue("controls", "rumble", Rumble);
	}

	static void Save()
	{
		if (!Persist) return;
		ConfigFile file = new();
		file.SetValue("audio", "music", MusicVolume);
		file.SetValue("audio", "sound", SoundVolume);
		file.SetValue("display", "fullscreen", Fullscreen);
		file.SetValue("controls", "rumble", Rumble);
		file.Save(Path);
	}

	/// <summary>Volume steps of a tenth.</summary>
	public static void StepMusic(int steps) { MusicVolume = Step(MusicVolume, steps); Sfx.ApplyVolumes(); Save(); }
	public static void StepSound(int steps) { SoundVolume = Step(SoundVolume, steps); Save(); }

	public static void SetFullscreen(bool on)
	{
		Fullscreen = on;
		ApplyWindow();
		Save();
	}

	public static void SetRumble(bool on) { Rumble = on; Save(); }

	/// <summary>Puts the window in the saved mode (a real run only; never while testing).</summary>
	public static void ApplyWindow()
	{
		if (!Persist || DisplayServer.GetName() == "headless") return;
		bool now = DisplayServer.WindowGetMode() is DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen;
		if (now != Fullscreen) DisplayServer.WindowSetMode(Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
	}

	/// <summary>Called when F11 or Alt+Enter flips the window, so the option follows.</summary>
	public static void NoteWindowMode(bool fullscreen) { Fullscreen = fullscreen; Save(); }

	/// <summary>Decibels to add for a 0..1 volume. Silence is handled by the caller.</summary>
	public static float Decibels(float volume) => Mathf.LinearToDb(Mathf.Max(volume, 0.0001f));

	static float Step(float value, int steps) => Mathf.Clamp(Mathf.Round(value * 10.0f + steps) / 10.0f, 0.0f, 1.0f);
}
