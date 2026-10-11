using System.Collections.Generic;
using Godot;

/// <summary>What one source is currently doing to a wizard's movement and energy.</summary>
public struct Modifier
{
	/// <summary>Percent off movement speed.</summary>
	public float Slow;
	/// <summary>Percent added to movement speed.</summary>
	public float Haste;
	/// <summary>Energy does not recharge.</summary>
	public bool NoRegen;
	/// <summary>On ice: a moving wizard keeps going the same way and cannot steer.</summary>
	public bool Slide;
}

/// <summary>
/// Slows, speed-ups, ice and regen locks, each held under the source that caused it.
///
/// Unity used additive slow()/removeSlow() calls, so every effect had to undo exactly what it
/// applied, and any effect that died early (its caster killed mid-ultimate, a patch destroyed)
/// left its slow on forever - that is how Absolute Zero and Earthquake leaked. Here a source
/// sets its modifier and removes it by key, and <see cref="Prune"/> drops any whose source
/// node has been freed, so a modifier can never outlive what caused it.
/// </summary>
public sealed class StatusEffects
{
	readonly Dictionary<object, Modifier> active = new();
	readonly List<object> stale = new();

	public void Set(object source, Modifier modifier) => active[source] = modifier;

	public void Remove(object source) => active.Remove(source);

	public bool Has(object source) => active.ContainsKey(source);

	public int Count => active.Count;

	/// <summary>Drops modifiers whose source node no longer exists.</summary>
	public void Prune()
	{
		stale.Clear();
		foreach (object source in active.Keys)
			if (source is GodotObject node && !GodotObject.IsInstanceValid(node)) stale.Add(source);
		foreach (object source in stale) active.Remove(source);
	}

	public void Clear() => active.Clear();

	/// <summary>Speed multiplier, never below zero (Unity let a big enough slow run you backwards).</summary>
	public float SpeedMultiplier
	{
		get
		{
			float percent = 100.0f;
			foreach (Modifier modifier in active.Values) percent += modifier.Haste - modifier.Slow;
			return Mathf.Max(0.0f, percent / 100.0f);
		}
	}

	public bool NoRegen
	{
		get
		{
			foreach (Modifier modifier in active.Values) if (modifier.NoRegen) return true;
			return false;
		}
	}

	public bool Sliding
	{
		get
		{
			foreach (Modifier modifier in active.Values) if (modifier.Slide) return true;
			return false;
		}
	}
}
