using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostWizard : MonoBehaviour, IWizardAbilities
{
    public bool allow1 = true, allow2 = true, allow3 = true, allow4 = true;
    // The basic attack is free so a dry energy bar never leaves you with no action.
    public float basicCost = 0f;
    public float basicCooldown = 0.55f;
    public GameObject attack1, attack2, attack3, attack4;
    public PlayerStats statScript;
    public ControllerScript controllerScript;
    private GameObject iceCubePrefab;
    public float icePatchCost = 30f;
    public float iceWaveCost = 35f;
    // The icePatch prefab is authored at 1.5; the patch reads better smaller.
    public float icePatchScale = 0.95f;
    private void Start() { attack1 = Resources.Load<GameObject>("icicle"); attack2 = Resources.Load<GameObject>("icePatch"); iceCubePrefab = Resources.Load<GameObject>("iceCube"); Sprite az = Resources.Load<Sprite>("AbsoluteZero"); attack4 = new GameObject("AbsoluteZeroVisual"); attack4.AddComponent<SpriteRenderer>().sprite = az; attack4.transform.SetParent(transform); attack4.transform.localPosition = Vector3.zero; attack4.SetActive(false); statScript = GetComponent<PlayerStats>(); controllerScript = GetComponent<ControllerScript>(); }
    public string getName() => "FrostWizard";
    public bool IsAbilityReady(int slot) => slot == 1 ? allow1 : slot == 2 ? allow2 : slot == 3 && allow3;

    public void basicAttack(object[] args) { if (allow1 && statScript != null && statScript.DepleteEnergy(basicCost)) StartCoroutine(Basic(args)); }
    private IEnumerator Basic(object[] args) { allow1 = false; GameObject shot = Instantiate(attack1, transform.position, Quaternion.identity); IgnoreSelf(shot); shot.transform.Rotate((Vector3)args[0]); Destroy(shot, 2f); yield return new WaitForSeconds(basicCooldown); allow1 = true; }
    public void secondAttack(object[] args) { if (allow2 && statScript != null && statScript.DepleteEnergy(icePatchCost)) StartCoroutine(Second(args)); }
    private IEnumerator Second(object[] args)
    {
        allow2 = false;
        Vector3 center = args != null && args.Length > 1 ? (Vector3)args[1] : transform.position;
        yield return StartCoroutine(FormIcePatch(center));
        yield return new WaitForSeconds(3f);
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player")) player.SendMessage("notSliding", SendMessageOptions.DontRequireReceiver);
        yield return new WaitForSeconds(1f);
        allow2 = true;
    }

    // Telegraph: a handful of small puddles seep out and slide together, then the
    // sheet glosses over into ice. Nothing slips until the gloss finishes.
    private IEnumerator FormIcePatch(Vector3 center)
    {
        const int puddleCount = 6;
        const float gatherDuration = 0.55f;
        GameObject[] puddles = new GameObject[puddleCount];
        Vector3[] starts = new Vector3[puddleCount];
        for (int i = 0; i < puddleCount; i++)
        {
            float angle = i / (float)puddleCount * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            float distance = Random.Range(0.45f, 0.95f) * icePatchScale;
            starts[i] = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.6f, 0f) * distance;
            GameObject puddle = new GameObject("IcePuddle");
            puddle.transform.position = starts[i];
            SpriteRenderer renderer = puddle.AddComponent<SpriteRenderer>();
            renderer.sprite = ProceduralFx.Puddle;
            renderer.sortingOrder = 3;
            renderer.color = new Color(1f, 1f, 1f, 0f);
            puddles[i] = puddle;
        }

        float elapsed = 0f;
        while (elapsed < gatherDuration)
        {
            float t = elapsed / gatherDuration;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            for (int i = 0; i < puddleCount; i++)
            {
                if (puddles[i] == null) continue;
                puddles[i].transform.position = Vector3.Lerp(starts[i], center, eased);
                puddles[i].transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 0.7f, eased) * icePatchScale;
                SpriteRenderer renderer = puddles[i].GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, 0.85f, Mathf.Min(1f, t * 3f)));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        for (int i = 0; i < puddleCount; i++) if (puddles[i] != null) Destroy(puddles[i]);

        if (attack2 == null) yield break;
        yield return StartCoroutine(GlossOver(Instantiate(attack2, center, Quaternion.identity)));
    }

    // The sheet snaps up to size with a wet blue sheen that clears to plain ice. The
    // collider stays off until it has fully formed.
    private IEnumerator GlossOver(GameObject patch)
    {
        if (patch == null) yield break;
        PolygonCollider2D collider = patch.GetComponent<PolygonCollider2D>();
        if (collider != null) collider.enabled = false;
        SpriteRenderer renderer = patch.GetComponent<SpriteRenderer>();
        float elapsed = 0f, duration = 0.28f;
        while (elapsed < duration && patch != null)
        {
            float t = elapsed / duration;
            patch.transform.localScale = Vector3.one * icePatchScale * Mathf.Lerp(0.45f, 1f, Mathf.SmoothStep(0f, 1f, t));
            if (renderer != null) renderer.color = Color.Lerp(new Color(0.75f, 0.9f, 1f, 0.55f), Color.white, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (patch == null) yield break;
        patch.transform.localScale = Vector3.one * icePatchScale;
        if (renderer != null) renderer.color = Color.white;
        if (collider != null) collider.enabled = true;
        // The patch only starts counting down once it is actually ice.
        patch.GetComponent<IcePatch>()?.beginLife();
    }
    public void thirdAttack(object[] args) { if (allow3 && statScript != null && statScript.DepleteEnergy(iceWaveCost)) StartCoroutine(Third(args)); }
    private IEnumerator Third(object[] args) { allow3 = false; SpawnIceWave(args); yield return new WaitForSeconds(4f); allow3 = true; }

    // The wave is assembled at cast time out of separate shards and animated in
    // code, rather than being one authored sprite that slides across the arena.
    private void SpawnIceWave(object[] args)
    {
        const int shardCount = 9;
        const float arcSpan = 70f;
        Vector3 origin = transform.position;
        Vector3 forward = AimDirection(args);
        GameObject wave = new GameObject("IceWave");
        wave.transform.position = origin;

        HashSet<Transform> hits = new HashSet<Transform>();
        GameObject[] shards = new GameObject[shardCount];
        float[] offsets = new float[shardCount];
        for (int i = 0; i < shardCount; i++)
        {
            offsets[i] = Mathf.Lerp(-arcSpan * 0.5f, arcSpan * 0.5f, i / (float)(shardCount - 1));
            shards[i] = CreateShard(wave.transform, hits);
        }
        StartCoroutine(AnimateIceWave(wave, shards, offsets, origin, forward));
    }

    private GameObject CreateShard(Transform parent, HashSet<Transform> hits)
    {
        GameObject shard = new GameObject("IceWaveShard");
        shard.transform.SetParent(parent);
        SpriteRenderer renderer = shard.AddComponent<SpriteRenderer>();
        renderer.sprite = ProceduralFx.IceShard;
        renderer.sortingOrder = 6;
        Rigidbody2D body = shard.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        CircleCollider2D collider = shard.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.4f;
        IceWaveShard behaviour = shard.AddComponent<IceWaveShard>();
        behaviour.caster = gameObject;
        behaviour.iceCubePrefab = iceCubePrefab;
        behaviour.alreadyHit = hits;
        return shard;
    }

    private IEnumerator AnimateIceWave(GameObject wave, GameObject[] shards, float[] offsets, Vector3 origin, Vector3 forward)
    {
        const float duration = 0.55f, nearRadius = 0.9f, farRadius = 7f;
        float baseAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float radius = Mathf.Lerp(nearRadius, farRadius, t);
            float spread = Mathf.Lerp(0.35f, 1f, t);
            for (int i = 0; i < shards.Length; i++)
            {
                if (shards[i] == null) continue;
                float angle = baseAngle + offsets[i] * spread;
                float radians = angle * Mathf.Deg2Rad;
                shards[i].transform.position = origin + new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * radius;
                shards[i].transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
                shards[i].transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.15f, Mathf.Sin(t * Mathf.PI));
                SpriteRenderer renderer = shards[i].GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - t * t));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (wave != null) Destroy(wave);
    }

    // Prefer the targeter position the controller passes in; fall back to the aim
    // rotation vector when an ability is triggered without one.
    private Vector3 AimDirection(object[] args)
    {
        if (args != null && args.Length > 1)
        {
            Vector3 toTarget = (Vector3)args[1] - transform.position;
            if (toTarget.sqrMagnitude > 0.0001f) return toTarget.normalized;
        }
        if (args != null && args.Length > 0)
        {
            float angle = ((Vector3)args[0]).z * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        }
        return Vector3.right;
    }
    public void ultimateAttack(object[] args)
    {
        if (!allow4 || statScript == null || controllerScript == null || controllerScript.ultimateActive || !statScript.TryUseUltimate()) return;
        allow4 = false;
        controllerScript.ultimateActive = true;
        StartCoroutine(AbsoluteZero());
    }
    private IEnumerator AbsoluteZero()
    {
        if (attack4 != null) attack4.SetActive(true);
        StartCoroutine(PulseAreaVisual(attack4, 1.0f, 1.25f, 5f));
        float duration = 5f, elapsed = 0f;
        while (elapsed < duration)
        {
            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (player == gameObject) continue;
                ControllerScript c = player.GetComponent<ControllerScript>();
                PlayerStats p = player.GetComponent<PlayerStats>();
                if (c != null && p != null && (player.transform.position - transform.position).sqrMagnitude <= 64f)
                {
                    c.slow(35);
                    p.noRecharge();
                }
            }
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            PlayerStats p = player.GetComponent<PlayerStats>();
            if (p != null) p.recharge();
        }
        controllerScript.ultimateActive = false;
        allow4 = true;
        if (attack4 != null) attack4.SetActive(false);
    }

    private IEnumerator PulseAreaVisual(GameObject visual, float minScale, float maxScale, float duration)
    {
        if (visual == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration && visual != null)
        {
            float wave = (Mathf.Sin(elapsed * 7f) + 1f) * 0.5f;
            visual.transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, wave);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (visual != null) visual.transform.localScale = Vector3.one;
    }
    private void IgnoreSelf(GameObject shot) { Collider2D a = shot.GetComponent<Collider2D>(), b = GetComponent<Collider2D>(); if (a != null && b != null) Physics2D.IgnoreCollision(a, b); }
}