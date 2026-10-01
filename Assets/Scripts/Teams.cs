using UnityEngine;

// Team identity and the friendly-fire rule for Teams mode.
//
// A player's team lives on PlayerStats.team and is set by ParkBattle at spawn. In
// Free-for-All every player is on NoTeam, which nothing counts as an ally, so all of
// these checks fall through to the old behaviour.
//
// Effects that outlive a moment (patches, rods, fire sources) capture the caster's
// team when cast rather than holding the caster and asking later, so a teammate stays
// protected even after the caster has died.
public static class Teams
{
    public const int NoTeam = -1;
    public const int Red = 0;
    public const int Blue = 1;
    public const int Count = 2;

    public static string Name(int team) => team == Red ? "Red" : team == Blue ? "Blue" : "";

    public static Color Colour(int team) =>
        team == Red ? new Color(0.95f, 0.25f, 0.2f) :
        team == Blue ? new Color(0.25f, 0.5f, 1f) :
        Color.white;

    // Players 1 and 3 start on Red, 2 and 4 on Blue. On the woodland stage that puts
    // teammates on the same side of the arena.
    public static int DefaultForSlot(int slotIndex) => slotIndex % 2 == 0 ? Red : Blue;

    public static int Of(GameObject player)
    {
        if (player == null) return NoTeam;
        PlayerStats stats = player.GetComponent<PlayerStats>();
        return stats == null ? NoTeam : stats.team;
    }

    // True when `target` is on `ownerTeam`. Never true in Free-for-All.
    public static bool IsAlly(int ownerTeam, GameObject target) => ownerTeam != NoTeam && Of(target) == ownerTeam;

    public static bool AreAllies(GameObject a, GameObject b) => a != null && b != null && a != b && IsAlly(Of(a), b);

    // A projectile passes through its caster and the caster's teammates. Every
    // projectile is a trigger that hits the first player it touches, so this has to
    // be done at the physics level; checking in the hit handler would still make the
    // shot vanish on the teammate.
    public static void IgnoreFriendlyCollisions(GameObject shot, GameObject caster)
    {
        if (shot == null || caster == null) return;
        Collider2D shotCollider = shot.GetComponent<Collider2D>();
        if (shotCollider == null) return;
        IgnoreAll(shotCollider, caster);
        int team = Of(caster);
        if (team == NoTeam) return;
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            if (player != caster && Of(player) == team) IgnoreAll(shotCollider, player);
    }

    private static void IgnoreAll(Collider2D shotCollider, GameObject player)
    {
        foreach (Collider2D collider in player.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(shotCollider, collider);
    }
}
