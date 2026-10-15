using Godot;

/// <summary>
/// Sound effects and music. Effects play on short-lived players parented to the audio root
/// (GameRoot), so a sound keeps playing after the node that caused it is freed - a projectile
/// that pops still finishes its sound. Music has one dedicated player.
/// </summary>
public static class Sfx
{
	static Node root;
	static AudioStreamPlayer music;
	static AudioStream currentMusic;

	public static void Attach(Node audioRoot)
	{
		root = audioRoot;
		music = new AudioStreamPlayer { Name = "Music" };
		ApplyVolumes();
		root.AddChild(music);
		music.Finished += () => music.Play();
	}

	public static void Play(AudioStream stream, float volumeDb = -4.0f)
	{
		if (stream == null || root == null || !GodotObject.IsInstanceValid(root) || Settings.SoundVolume <= 0.0f) return;
		AudioStreamPlayer player = new() { Stream = stream, VolumeDb = volumeDb + Settings.Decibels(Settings.SoundVolume) };
		root.AddChild(player);
		player.Finished += player.QueueFree;
		player.Play();
	}

	public static void Play(string path, float volumeDb = -4.0f) => Play(Content.Sound(path), volumeDb);

	/// <summary>Starts a looping track, or leaves it alone if it is already the one playing.</summary>
	public static void Music(string path)
	{
		if (music == null) return;
		AudioStream stream = Content.Sound(path);
		if (stream == currentMusic && music.Playing) return;
		currentMusic = stream;
		music.Stream = stream;
		music.Play();
		ApplyVolumes();
	}

	const float MusicDb = -8.0f;

	/// <summary>Sets the music to the player's volume option (sound effects read theirs as they play).</summary>
	public static void ApplyVolumes()
	{
		if (music == null) return;
		music.VolumeDb = MusicDb + Settings.Decibels(Settings.MusicVolume);
		music.StreamPaused = Settings.MusicVolume <= 0.0f;
	}

	public static void StopMusic()
	{
		currentMusic = null;
		music?.Stop();
	}
}
