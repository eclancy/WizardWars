using System.Collections;
using UnityEngine;

// Fire kit: Fireball (basic), Ring of Fire (second), Flamethrower (third),
// Meteor Storm (ultimate). There is no fourth ability.
//
// The fire wizard is immune to his own fire. Projectiles use Physics2D.IgnoreCollision
// against the caster, and the effects that find targets by overlap query instead of
// collision — Ring of Fire's sources and the meteor blasts — skip the caster
// explicitly, because a query ignores IgnoreCollision.
public class FireWizard : MonoBehaviour, IWizardAbilities
{
    public bool allow1 = true, allow2 = true, allow3 = true, allow4 = true;
    // The basic attack is free so a dry energy bar never leaves you with no action.
    public float basicCost = 0f;
    public float basicCooldown = 0.55f;
    public GameObject attack1, attack2, attack3, attack4;
    public PlayerStats statScript;
    public bool flameThrower;
    public object[] flameThrowerArgs;
    public GameObject flameShot;
    public float lastTick;
    public ControllerScript controllerScript;

    public float ringCost = 35f;
    public float ringRadius = 2.6f;
    public int ringSourceCount = 12;
    public float ringLifetime = 4f;
    public float ringBurnDuration = 6f;
    public float ringDamagePerTick = 3f;

    // Flamethrower. The energy bar is the channel timer: it drains while you hold the
    // button and regen is off, so how long you can burn is visible on the bar.
    public float flameStartCost = 10f;
    public float flameDrainPerSecond = 10f;
    public float flameSelfSlow = 15f;
    public float flameRecastDelay = 1.5f;

    // Meteor Storm. Meteors rain on random points across the arena; the caster is
    // never hit by them.
    public int meteorStormCount = 10;
    public float meteorStormInterval = 0.45f;
    public Vector2 meteorStormArea = new Vector2(9f, 5f);
    public float meteorApproachDuration = 0.9f;
    public float meteorFallHeight = 12f;
    public float meteorBlastRadius = 3f;
    public float meteorCentreDamage = 28f;
    public float meteorEdgeDamage = 5f;

    private void Start()
    {
        // attack2 is no longer a prefab: Ring of Fire is generated at cast time.
        attack1 = Resources.Load<GameObject>("fireball");
        attack3 = Resources.Load<GameObject>("flameThrower");
        statScript = GetComponent<PlayerStats>();
        controllerScript = GetComponent<ControllerScript>();
    }

    public string getName() => "FireWizard";
    public bool IsAbilityReady(int slot) => slot == 1 ? allow1 : slot == 2 ? allow2 : slot == 3 && allow3 && !flameThrower;


    public void basicAttack(object[] args) { if (allow1 && statScript != null && statScript.DepleteEnergy(basicCost)) StartCoroutine(Basic(args)); }
    private IEnumerator Basic(object[] args) { allow1 = false; SpawnProjectile(attack1, transform.position, args, true, 2f); yield return new WaitForSeconds(basicCooldown); allow1 = true; }

    public void secondAttack(object[] args) { if (allow2 && statScript != null && statScript.DepleteEnergy(ringCost)) StartCoroutine(Second(args)); }
    private IEnumerator Second(object[] args)
    {
        allow2 = false;
        Vector3 center = args != null && args.Length > 1 ? (Vector3)args[1] : transform.position;
        SpawnRingOfFire(center);
        yield return new WaitForSeconds(4f);
        allow2 = true;
    }

    // Lay the ring as separate fire sources. Each one checks what it landed on, so
    // a source inside a wall or an obstacle goes out on its own and the rest of the
    // ring keeps burning.
    private void SpawnRingOfFire(Vector3 center)
    {
        for (int i = 0; i < ringSourceCount; i++)
        {
            float angle = i / (float)ringSourceCount * Mathf.PI * 2f;
            GameObject source = new GameObject("FireSource");
            source.tag = "destroyableAttack";
            source.transform.position = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ringRadius;
            source.transform.localScale = Vector3.one * 0.75f;
            SpriteRenderer renderer = source.AddComponent<SpriteRenderer>();
            renderer.sprite = ProceduralFx.Flame;
            renderer.sortingOrder = 6;
            FireSource fire = source.AddComponent<FireSource>();
            fire.burnDuration = ringBurnDuration;
            fire.damagePerTick = ringDamagePerTick;
            fire.lifetime = ringLifetime;
            fire.caster = gameObject;
        }
    }

    public void thirdAttack(object[] args)
    {
        if (!allow3 || flameThrower || statScript == null || controllerScript == null || controllerScript.ultimateActive) return;
        if (!statScript.DepleteEnergy(flameStartCost)) return;
        controllerScript.slow(flameSelfSlow);
        flameThrower = true;
        flameThrowerArgs = args;
        flameShot = Instantiate(attack3, transform.position, Quaternion.identity);
        IgnoreSelf(flameShot);
        FlameThrower flame = flameShot.GetComponent<FlameThrower>();
        if (flame != null) flame.caster = gameObject;
        allow3 = false;
        lastTick = Time.time;
    }

    public void ultimateAttack(object[] args)
    {
        if (!allow4 || statScript == null || controllerScript == null || controllerScript.ultimateActive || !statScript.TryUseUltimate()) return;
        allow4 = false;
        controllerScript.ultimateActive = true;
        StartCoroutine(MeteorStorm());
    }

    // Meteors rain on random points across the arena. Each one telegraphs, falls and
    // explodes on its own schedule, so several are in the air at once.
    private IEnumerator MeteorStorm()
    {
        for (int i = 0; i < meteorStormCount; i++)
        {
            Vector3 target = new Vector3(
                Random.Range(-meteorStormArea.x, meteorStormArea.x),
                Random.Range(-meteorStormArea.y, meteorStormArea.y),
                0f);
            StartCoroutine(SingleMeteor(target));
            yield return new WaitForSeconds(meteorStormInterval);
        }
        // Let the last meteor land before handing control back.
        yield return new WaitForSeconds(meteorApproachDuration + 0.3f);
        if (controllerScript != null) controllerScript.ultimateActive = false;
        allow4 = true;
    }

    private IEnumerator SingleMeteor(Vector3 target)
    {
        yield return StartCoroutine(MeteorApproach(target));
        StartCoroutine(Explode(target));
        // Damage falls off with distance, and never touches the caster.
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player == gameObject) continue;
            float distance = (player.transform.position - target).magnitude;
            if (distance > meteorBlastRadius) continue;
            int damage = Mathf.RoundToInt(Mathf.Lerp(meteorCentreDamage, meteorEdgeDamage, distance / meteorBlastRadius));
            player.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
    }

    // Telegraph: a shadow on the ground marks where the meteor will land. It starts
    // small, dark and soft, then tightens and catches fire as the meteor closes, so
    // how long you have left is readable from the shadow alone. The meteor itself
    // falls in from above and grows as it approaches.
    private IEnumerator MeteorApproach(Vector3 targetPoint)
    {
        GameObject shadow = new GameObject("MeteorShadow");
        shadow.transform.position = targetPoint;
        SpriteRenderer shadowRenderer = shadow.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = ProceduralFx.Shadow;
        shadowRenderer.sortingOrder = 4;

        GameObject rock = new GameObject("Meteor");
        SpriteRenderer rockRenderer = rock.AddComponent<SpriteRenderer>();
        rockRenderer.sprite = ProceduralFx.Meteor;
        rockRenderer.sortingOrder = 11;
        Vector3 origin = targetPoint + new Vector3(0f, meteorFallHeight, 0f);

        float elapsed = 0f;
        while (elapsed < meteorApproachDuration)
        {
            float t = elapsed / meteorApproachDuration;
            // Accelerate downward instead of drifting in at a constant rate.
            float fall = t * t;

            shadow.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.9f, fall);
            // Cold grey at range, glowing orange just before impact.
            shadowRenderer.color = Color.Lerp(new Color(0.1f, 0.09f, 0.09f, 0.55f), new Color(1f, 0.45f, 0.12f, 0.95f), fall * fall);

            rock.transform.position = Vector3.Lerp(origin, targetPoint, fall);
            rock.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.5f, fall);
            rock.transform.Rotate(0f, 0f, 90f * Time.deltaTime);

            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(shadow);
        Destroy(rock);
    }

    // The blast: a disc that snaps out to the damage radius and fades, so the area
    // that actually hurt is visible rather than implied.
    private IEnumerator Explode(Vector3 center)
    {
        GameObject blast = new GameObject("MeteorExplosion");
        blast.transform.position = center;
        SpriteRenderer blastRenderer = blast.AddComponent<SpriteRenderer>();
        blastRenderer.sprite = ProceduralFx.Burst;
        blastRenderer.sortingOrder = 9;
        // The Burst sprite is 2 world units wide, so this matches meteorBlastRadius.
        float targetScale = meteorBlastRadius;
        float elapsed = 0f, duration = 0.45f;
        while (elapsed < duration && blast != null)
        {
            float t = elapsed / duration;
            blast.transform.localScale = Vector3.one * targetScale * Mathf.Lerp(0.25f, 1.1f, Mathf.Sqrt(t));
            blastRenderer.color = new Color(1f, 1f, 1f, 1f - t * t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (blast != null) Destroy(blast);
    }

    private void Update()
    {
        if (!flameThrower) return;
        // The flame can be destroyed out from under us by a rock wall. Without this
        // the wizard would stay slowed with regen off for the rest of the match.
        if (flameShot == null || flameThrowerArgs == null || statScript == null) { StopFlame(); return; }

        string button = (string)flameThrowerArgs[2];
        if (Input.GetButtonUp(button)) { StopFlame(); return; }

        string xAxis = (string)flameThrowerArgs[3], yAxis = (string)flameThrowerArgs[4];
        float angle = Mathf.Atan2(-Input.GetAxis(yAxis), Input.GetAxis(xAxis)) * Mathf.Rad2Deg;
        flameShot.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, 0f, angle));
        flameShot.transform.Translate(Vector2.right * 2.5f);
        statScript.noRecharge();
        // Drain in small steps so the bar visibly empties while the button is held.
        if (Time.time > .2f + lastTick)
        {
            if (statScript.DepleteEnergy(flameDrainPerSecond * .2f)) lastTick = Time.time;
            else StopFlame();
        }
    }

    private void StopFlame()
    {
        if (flameShot != null) Destroy(flameShot);
        flameThrower = false;
        if (statScript != null) statScript.recharge();
        controllerScript?.removeSlow(flameSelfSlow);
        StartCoroutine(ResetFlame());
    }

    private IEnumerator ResetFlame() { yield return new WaitForSeconds(flameRecastDelay); allow3 = true; }

    private void SpawnProjectile(GameObject prefab, Vector3 position, object[] args, bool rotate, float lifetime) { if (prefab == null) return; GameObject shot = Instantiate(prefab, position, Quaternion.identity); IgnoreSelf(shot); if (rotate) shot.transform.Rotate((Vector3)args[0]); Destroy(shot, lifetime); }
    private void IgnoreSelf(GameObject shot) { Collider2D a = shot.GetComponent<Collider2D>(), b = GetComponent<Collider2D>(); if (a != null && b != null) Physics2D.IgnoreCollision(a, b); }
}
