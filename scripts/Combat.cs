/// <summary>
/// The one place that decides who a hostile effect can touch, and applies damage.
///
/// Every effect records who cast it and that wizard's team at cast time, so a teammate stays
/// safe even after the caster has died. In Unity, damage arrived by SendMessage("ApplyDamage",
/// int) with no attacker at all, and the friendly-fire rule was re-implemented in fourteen
/// places; here it is <see cref="CanAffect"/>.
/// </summary>
public static class Combat
{
	/// <summary>
	/// True when an effect cast by <paramref name="caster"/> (on <paramref name="team"/>) may
	/// harm or hinder <paramref name="target"/>. Teammates are always spared. The caster is
	/// spared unless <paramref name="hitsCaster"/> - patches and lightning rods hit their own
	/// caster, exactly as in Unity.
	/// </summary>
	public static bool CanAffect(Wizard caster, int team, Wizard target, bool hitsCaster = false)
	{
		if (target == null || !target.Alive) return false;
		if (target == caster) return hitsCaster;
		return !Teams.Allied(team, target.Team);
	}

	/// <summary>Damages the target if the rule allows it. Returns whether it landed.</summary>
	public static bool Damage(Wizard caster, int team, Wizard target, float amount, bool hitsCaster = false)
	{
		if (!CanAffect(caster, team, target, hitsCaster)) return false;
		target.TakeDamage(amount);
		return true;
	}
}
