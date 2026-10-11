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
		music = new AudioStreamPlayer { Name = "Music", VolumeDb = -8.0f };
		root.AddChild(music);
		music.Finished += () => music.Play();
	}

	public static void Play(AudioStream stream, float volumeDb = -4.0f)
	{
		if (stream == null || root == null || !GodotObject.IsInstanceValid(root)) return;
		AudioStreamPlayer player = new() { Stream = stream, VolumeDb = volumeDb };
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
	}

	public static void StopMusic()
	{
		currentMusic = null;
		music?.Stop();
	}
}
