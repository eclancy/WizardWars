using System;
using Godot;

/// <summary>
/// Synthesises the game's newer sound effects and writes them to assets/sfx. Run once, with
/// `-- --make-sounds`; the WAVs are committed. Making them in code means there is no licence to
/// track for the public repo, and they are easy to retune.
///
/// - ultimate-charge: rises through an ultimate's build-up (<see cref="UltimateBuildUp.Duration"/>)
///   as the pitch climbs, a whoosh swells and a tremolo quickens.
/// - ultimate-release: the boom when it goes off. A falling sub thump, a burst of noise and a
///   bright shimmer.
/// - ground-crack: Earthquake and Tremor. Sharp cracks over a low rumble.
/// - ice-break: shattering out of a freeze. A crack, then tinkling shards.
/// - ice-chip: one wiggle landing on the ice. A tiny click.
///
/// All are 44.1 kHz mono, 16-bit, normalised to the same peak as the Unity effects.
/// </summary>
public static class SoundTool
{
	const int Rate = 44100;
	const float Peak = 0.85f;

	public static int Run()
	{
		Report("existing doorslam", "res://assets/sfx/doorslam.wav");
		Report("existing rockwall", "res://assets/sfx/rockwall.wav");
		Report("existing icepatch", "res://assets/sfx/icepatch.wav");
		Report("existing fireball", "res://assets/sfx/fireball.wav");
		Report("existing speedup", "res://assets/sfx/speedup.wav");
		int failures = 0;
		failures += Save("ultimate-charge", Charge());
		failures += Save("ultimate-release", Release());
		failures += Save("ground-crack", Crack());
		failures += Save("ice-break", IceBreak());
		failures += Save("ice-chip", IceChip());
		return failures;
	}

	// ---- Output ----

	static int Save(string name, float[] samples)
	{
		Normalise(samples);
		byte[] data = new byte[samples.Length * 2];
		for (int i = 0; i < samples.Length; i++)
		{
			short s = (short)Mathf.Clamp(samples[i] * 32767.0f, -32768.0f, 32767.0f);
			data[i * 2] = (byte)(s & 0xff);
			data[i * 2 + 1] = (byte)((s >> 8) & 0xff);
		}
		AudioStreamWav wav = new() { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = Rate, Stereo = false, Data = data };
		string path = $"res://assets/sfx/{name}.wav";
		Error error = wav.SaveToWav(ProjectSettings.GlobalizePath(path));
		(float peak, float rms) = Levels(samples);
		GD.Print($"SoundTool: wrote {path} ({samples.Length / (float)Rate:0.00}s, peak {peak:0.00}, rms {rms:0.000}): {error}");
		return error == Error.Ok ? 0 : 1;
	}

	static void Report(string label, string path)
	{
		// Read the file itself: Godot imports WAVs compressed, so the imported stream has no samples to measure.
		byte[] file = FileAccess.GetFileAsBytes(path);
		if (file.Length < 44) return;
		int channels = BitConverter.ToInt16(file, 22), rate = BitConverter.ToInt32(file, 24), bits = BitConverter.ToInt16(file, 34);
		int at = 12;
		while (at + 8 <= file.Length && System.Text.Encoding.ASCII.GetString(file, at, 4) != "data") at += 8 + BitConverter.ToInt32(file, at + 4);
		if (bits != 16 || at + 8 > file.Length) { GD.Print($"SoundTool: {label}: {bits}-bit, not measured"); return; }
		int length = Math.Min(BitConverter.ToInt32(file, at + 4), file.Length - at - 8);
		float[] samples = new float[length / 2 / channels];
		for (int i = 0; i < samples.Length; i++) samples[i] = BitConverter.ToInt16(file, at + 8 + i * 2 * channels) / 32768.0f;
		(float peak, float rms) = Levels(samples);
		GD.Print($"SoundTool: {label}: {samples.Length / (float)rate:0.00}s, peak {peak:0.00}, rms {rms:0.000}");
	}

	static (float peak, float rms) Levels(float[] samples)
	{
		float peak = 0.0f;
		double sum = 0.0;
		foreach (float s in samples)
		{
			peak = Mathf.Max(peak, Mathf.Abs(s));
			sum += s * s;
		}
		return (peak, (float)Math.Sqrt(sum / Math.Max(1, samples.Length)));
	}

	static void Normalise(float[] samples)
	{
		float peak = 0.0f;
		foreach (float s in samples) peak = Mathf.Max(peak, Mathf.Abs(s));
		if (peak <= 0.0f) return;
		for (int i = 0; i < samples.Length; i++) samples[i] *= Peak / peak;
	}

	// ---- Building blocks ----

	static float[] Buffer(float seconds) => new float[(int)(seconds * Rate)];

	/// <summary>A one-pole low-pass filter coefficient for a cutoff frequency.</summary>
	static float LowPass(float cutoff) => 1.0f - Mathf.Exp(-Mathf.Tau * cutoff / Rate);

	/// <summary>A short click-free fade at both ends.</summary>
	static void Fade(float[] samples, float inSeconds, float outSeconds)
	{
		int fadeIn = (int)(inSeconds * Rate), fadeOut = (int)(outSeconds * Rate);
		for (int i = 0; i < fadeIn && i < samples.Length; i++) samples[i] *= i / (float)fadeIn;
		for (int i = 0; i < fadeOut && i < samples.Length; i++) samples[samples.Length - 1 - i] *= i / (float)fadeOut;
	}

	/// <summary>A sine ping: a quick strike that rings and dies away.</summary>
	static void Ping(float[] samples, float start, float frequency, float decay, float level)
	{
		int from = (int)(start * Rate);
		for (int i = from; i < samples.Length; i++)
		{
			float t = (i - from) / (float)Rate;
			float envelope = Mathf.Exp(-t / decay) * Mathf.Min(1.0f, t * 2000.0f);
			if (envelope < 0.0005f) break;
			samples[i] += Mathf.Sin(Mathf.Tau * frequency * t) * envelope * level;
		}
	}

	/// <summary>A burst of bright noise (white noise minus its low end): a crack or a snap.</summary>
	static void Snap(float[] samples, Random rng, float start, float decay, float level, float darkness = 1200.0f)
	{
		int from = (int)(start * Rate);
		float low = 0.0f, k = LowPass(darkness);
		for (int i = from; i < samples.Length; i++)
		{
			float t = (i - from) / (float)Rate;
			float envelope = Mathf.Exp(-t / decay);
			if (envelope < 0.0005f) break;
			float white = (float)(rng.NextDouble() * 2.0 - 1.0);
			low += (white - low) * k;
			samples[i] += (white - low) * envelope * level;
		}
	}

	// ---- The sounds ----

	/// <summary>
	/// The build-up. A buzzy chord climbs about two octaves (90 to 420 Hz) as it grows louder,
	/// a tremolo speeds up from 6 to 26 Hz, and a whoosh of noise opens up underneath. It stops
	/// on the beat, where the release takes over.
	/// </summary>
	static float[] Charge()
	{
		float length = UltimateBuildUp.Duration;
		float[] samples = Buffer(length + 0.02f);
		Random rng = new(1);
		double phase = 0.0, phase2 = 0.0, tremoloPhase = 0.0;
		float noise = 0.0f;
		for (int i = 0; i < samples.Length; i++)
		{
			float t = i / (float)Rate, f = Mathf.Min(1.0f, t / length);
			float pitch = 90.0f * Mathf.Pow(2.0f, f * 2.2f);
			phase += pitch / Rate;
			phase2 += pitch * 1.008f / Rate;
			// A softened saw: the first six harmonics, falling off.
			float tone = 0.0f;
			for (int h = 1; h <= 6; h++) tone += (Mathf.Sin((float)(Mathf.Tau * h * phase)) + Mathf.Sin((float)(Mathf.Tau * h * phase2))) / (h * 1.4f);
			tone += Mathf.Sin((float)(Mathf.Pi * phase)) * 0.8f; // a sub an octave down
			tremoloPhase += Mathf.Lerp(6.0f, 26.0f, f * f) / Rate;
			float tremolo = 1.0f - 0.35f * (0.5f + 0.5f * Mathf.Sin((float)(Mathf.Tau * tremoloPhase)));
			// The whoosh: noise through a filter that opens as the power gathers.
			noise += ((float)(rng.NextDouble() * 2.0 - 1.0) - noise) * LowPass(Mathf.Lerp(300.0f, 5000.0f, f * f));
			float swell = Mathf.Pow(f, 1.5f);
			samples[i] = (tone * 0.22f * tremolo + noise * 1.6f) * (0.12f + 0.88f * swell);
		}
		Fade(samples, 0.02f, 0.02f);
		return samples;
	}

	/// <summary>The release: a sub thump falling from 110 to 38 Hz, a burst of noise, and a shimmer of bright partials.</summary>
	static float[] Release()
	{
		float[] samples = Buffer(1.1f);
		Random rng = new(2);
		double phase = 0.0;
		float noise = 0.0f;
		for (int i = 0; i < samples.Length; i++)
		{
			float t = i / (float)Rate;
			float pitch = 38.0f + 72.0f * Mathf.Exp(-t / 0.12f);
			phase += pitch / Rate;
			float thump = Mathf.Sin((float)(Mathf.Tau * phase)) * Mathf.Exp(-t / 0.35f) * Mathf.Min(1.0f, t * 400.0f);
			noise += ((float)(rng.NextDouble() * 2.0 - 1.0) - noise) * LowPass(300.0f + 3700.0f * Mathf.Exp(-t / 0.08f));
			samples[i] = thump * 1.0f + noise * 2.2f * Mathf.Exp(-t / 0.18f);
		}
		foreach (float partial in new[] { 1760.0f, 2350.0f, 3140.0f, 4190.0f }) Ping(samples, 0.0f, partial, 0.35f, 0.05f);
		Fade(samples, 0.0f, 0.05f);
		return samples;
	}

	/// <summary>Ground cracking: a handful of sharp snaps over a deep rumble that swells and settles.</summary>
	static float[] Crack()
	{
		float[] samples = Buffer(0.9f);
		Random rng = new(3);
		// The rumble: brown noise (integrated white noise) kept below about 160 Hz.
		float brown = 0.0f, low = 0.0f;
		for (int i = 0; i < samples.Length; i++)
		{
			float t = i / (float)Rate;
			brown = Mathf.Clamp(brown * 0.995f + (float)(rng.NextDouble() * 2.0 - 1.0) * 0.06f, -1.0f, 1.0f);
			low += (brown - low) * LowPass(160.0f);
			samples[i] = low * 2.2f * Mathf.Min(1.0f, t / 0.03f) * Mathf.Exp(-t / 0.3f);
		}
		// The cracks: sharp at first, then smaller ones as it settles.
		for (int n = 0; n < 9; n++)
		{
			float start = n == 0 ? 0.0f : (float)(rng.NextDouble() * 0.4);
			Snap(samples, rng, start, (float)(0.006 + rng.NextDouble() * 0.014), n == 0 ? 0.9f : (float)(0.25 + rng.NextDouble() * 0.4), 900.0f);
		}
		Fade(samples, 0.0f, 0.05f);
		return samples;
	}

	/// <summary>Breaking out of ice: one hard crack, then shards tinkling down.</summary>
	static float[] IceBreak()
	{
		float[] samples = Buffer(0.6f);
		Random rng = new(4);
		Snap(samples, rng, 0.0f, 0.035f, 0.9f, 2500.0f);
		for (int n = 0; n < 14; n++)
			Ping(samples, (float)(rng.NextDouble() * 0.35), (float)(2600 + rng.NextDouble() * 4400), (float)(0.03 + rng.NextDouble() * 0.07), (float)(0.08 + rng.NextDouble() * 0.12));
		Fade(samples, 0.0f, 0.04f);
		return samples;
	}

	/// <summary>A wiggle landing: a tiny click with a glassy edge.</summary>
	static float[] IceChip()
	{
		float[] samples = Buffer(0.12f);
		Random rng = new(5);
		Snap(samples, rng, 0.0f, 0.008f, 0.8f, 3000.0f);
		Ping(samples, 0.002f, 3900.0f, 0.03f, 0.25f);
		Ping(samples, 0.004f, 5300.0f, 0.02f, 0.15f);
		Fade(samples, 0.0f, 0.02f);
		return samples;
	}
}
