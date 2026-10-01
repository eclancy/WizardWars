using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EarthWizard : MonoBehaviour, IWizardAbilities
{
    public bool allow1 = true, allow2 = true, allow3 = true, allow4 = true;
    // The basic attack is free so a dry energy bar never leaves you with no action.
    public float basicCost = 0f;
    public float basicCooldown = 0.55f;
    public GameObject attack1, attack2, attack3Vertical, attack3Horizontal, attack4;
    public PlayerStats statScript;
    public ControllerScript controllerScript;
    public float mudPatchCost = 30f;
    public float rockWallCost = 20f;
    // Matches the scale the mudPatch prefab is authored at.
    public float mudPatchScale = 1.5f;
    // Earthquake ultimate.
    public float quakeDuration = 5f;
    public float quakeRadius = 6.5f;
    public float quakeSlow = 55f;
    private void Start() { attack1 = Resources.Load<GameObject>("mudball"); attack2 = Resources.Load<GameObject>("mudPatch"); attack3Vertical = Resources.Load<GameObject>("RockWallVertical"); attack3Horizontal = Resources.Load<GameObject>("RockWallHorizontal"); Sprite quake = Resources.Load<Sprite>("Earthquake"); attack4 = new GameObject("EarthquakeVisual"); attack4.AddComponent<SpriteRenderer>().sprite = quake; attack4.transform.SetParent(transform); attack4.transform.localPosition = Vector3.zero; attack4.SetActive(false); statScript = GetComponent<PlayerStats>(); controllerScript = GetComponent<ControllerScript>(); }
    public string getName() => "EarthWizard";
    public bool IsAbilityReady(int slot) => slot == 1 ? allow1 : slot == 2 ? allow2 : slot == 3 && allow3;

    public void basicAttack(object[] args) { if (allow1 && statScript != null && statScript.DepleteEnergy(basicCost)) StartCoroutine(Basic(args)); }
    private IEnumerator Basic(object[] args) { allow1 = false; GameObject shot = Instantiate(attack1, transform.position, Quaternion.identity); IgnoreSelf(shot); shot.transform.Rotate((Vector3)args[0]); Destroy(shot, 2f); yield return new WaitForSeconds(basicCooldown); allow1 = true; }
    public void secondAttack(object[] args) { if (allow2 && statScript != null && statScript.DepleteEnergy(mudPatchCost)) StartCoroutine(Second(args)); }
    private IEnumerator Second(object[] args)
    {
        allow2 = false;
        Vector3 center = args != null && args.Length > 1 ? (Vector3)args[1] : transform.position;
        yield return StartCoroutine(FormMudPatch(center));
        yield return new WaitForSeconds(4f);
        allow2 = true;
    }

    // Telegraph: clods of wet earth spurt up out of the ground and fall back around
    // the point, then the patch spreads out from where they landed. Nothing is
    // slowed until the spread finishes.
    private IEnumerator FormMudPatch(Vector3 center)
    {
        const int globCount = 8;
        const float spurtDuration = 0.45f;
        GameObject[] globs = new GameObject[globCount];
        Vector3[] landings = new Vector3[globCount];
        float[] heights = new float[globCount];
        for (int i = 0; i < globCount; i++)
        {
            float angle = i / (float)globCount * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
            float distance = Random.Range(0.3f, 0.9f) * mudPatchScale;
            landings[i] = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.55f, 0f) * distance;
            heights[i] = Random.Range(0.7f, 1.5f);
            GameObject glob = new GameObject("MudGlob");
            glob.transform.position = center;
            glob.transform.localScale = Vector3.one * Random.Range(0.25f, 0.45f);
            SpriteRenderer renderer = glob.AddComponent<SpriteRenderer>();
            renderer.sprite = ProceduralFx.MudGlob;
            renderer.sortingOrder = 7;
            globs[i] = glob;
        }

        float elapsed = 0f;
        while (elapsed < spurtDuration)
        {
            float t = elapsed / spurtDuration;
            for (int i = 0; i < globCount; i++)
            {
                if (globs[i] == null) continue;
                // Parabola that starts and ends on the ground.
                float arc = heights[i] * 4f * t * (1f - t);
                globs[i].transform.position = Vector3.Lerp(center, landings[i], t) + new Vector3(0f, arc, 0f);
                globs[i].transform.Rotate(0f, 0f, 220f * Time.deltaTime);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        for (int i = 0; i < globCount; i++) if (globs[i] != null) Destroy(globs[i]);

        if (attack2 == null) yield break;
        GameObject patch = Instantiate(attack2, center, Quaternion.identity);
        MudPatch mud = patch.GetComponent<MudPatch>();
        if (mud != null) { mud.ownerTeam = Teams.Of(gameObject); mud.caster = gameObject; }
        yield return StartCoroutine(SpreadMud(patch));
    }

    // The patch settles outward, wider before taller, as the mud finds its level.
    private IEnumerator SpreadMud(GameObject patch)
    {
        if (patch == null) yield break;
        PolygonCollider2D collider = patch.GetComponent<PolygonCollider2D>();
        if (collider != null) collider.enabled = false;
        float elapsed = 0f, duration = 0.3f;
        while (elapsed < duration && patch != null)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            patch.transform.localScale = new Vector3(mudPatchScale * Mathf.Lerp(0.3f, 1f, t), mudPatchScale * Mathf.Lerp(0.15f, 1f, t), 1f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (patch == null) yield break;
        patch.transform.localScale = new Vector3(mudPatchScale, mudPatchScale, 1f);
        if (collider != null) collider.enabled = true;
    }
    public void thirdAttack(object[] args) { if (allow3 && statScript != null && statScript.DepleteEnergy(rockWallCost)) StartCoroutine(Third(args)); }
    private IEnumerator Third(object[] args) { allow3 = false; Vector3 target = (Vector3)args[1]; bool horizontal = Mathf.Abs(Mathf.Abs(target.y) - Mathf.Abs(transform.position.y)) > Mathf.Abs(Mathf.Abs(target.x) - Mathf.Abs(transform.position.x)); GameObject shot = Instantiate(horizontal ? attack3Horizontal : attack3Vertical, target, Quaternion.identity); if (!horizontal) shot.transform.Rotate(0, 0, 270); Destroy(shot, 3f); yield return new WaitForSeconds(4f); allow3 = true; }
    public void ultimateAttack(object[] args)
    {
        if (!allow4 || statScript == null || controllerScript == null || controllerScript.ultimateActive || !statScript.TryUseUltimate()) return;
        allow4 = false;
        controllerScript.ultimateActive = true;
        StartCoroutine(Earthquake());
    }

    // Earthquake: the ground shakes around the caster and everyone caught in it is
    // slowed for as long as they stay inside. The caster is not slowed.
    //
    // slow()/removeSlow() are additive, so every slow applied has to be matched
    // exactly once. `slowed` tracks who is currently carrying this quake's slow, so
    // leaving the radius or the quake ending always removes it.
    private IEnumerator Earthquake()
    {
        if (attack4 != null) attack4.SetActive(true);
        StartCoroutine(PulseAreaVisual(attack4, 1.0f, 1.25f, quakeDuration));
        StartCoroutine(Shockwaves(quakeDuration));

        HashSet<ControllerScript> slowed = new HashSet<ControllerScript>();
        float elapsed = 0f;
        while (elapsed < quakeDuration)
        {
            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (player == gameObject || Teams.AreAllies(gameObject, player)) continue;
                ControllerScript c = player.GetComponent<ControllerScript>();
                if (c == null) continue;
                bool inside = (player.transform.position - transform.position).sqrMagnitude <= quakeRadius * quakeRadius;
                if (inside) { if (slowed.Add(c)) c.slow(quakeSlow); }
                else if (slowed.Remove(c)) c.removeSlow(quakeSlow);
            }
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        // Release anyone still caught, including players destroyed mid-quake.
        foreach (ControllerScript c in slowed) if (c != null) c.removeSlow(quakeSlow);

        if (controllerScript != null) controllerScript.ultimateActive = false;
        if (attack4 != null) attack4.SetActive(false);
        yield return new WaitForSeconds(2f);
        allow4 = true;
    }

    private IEnumerator Shockwaves(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            StartCoroutine(Shockwave());
            elapsed += 0.6f;
            yield return new WaitForSeconds(0.6f);
        }
    }

    // A ripple running out along the ground to the edge of the slow, so the area
    // that actually catches you is visible.
    private IEnumerator Shockwave()
    {
        GameObject ring = new GameObject("QuakeShockwave");
        ring.transform.position = transform.position;
        SpriteRenderer renderer = ring.AddComponent<SpriteRenderer>();
        renderer.sprite = ProceduralFx.RingOutline;
        renderer.sortingOrder = 3;
        // RingOutline draws its band at 0.85 of the sprite's half-width.
        float targetScale = quakeRadius / 0.85f;
        float elapsed = 0f, duration = 0.8f;
        while (elapsed < duration && ring != null)
        {
            float t = elapsed / duration;
            // Flattened vertically so it reads as a ripple across the ground.
            ring.transform.localScale = new Vector3(targetScale * t, targetScale * t * 0.55f, 1f);
            renderer.color = new Color(0.62f, 0.45f, 0.24f, 1f - t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (ring != null) Destroy(ring);
    }

    private IEnumerator PulseAreaVisual(GameObject visual, float minScale, float maxScale, float duration)
    {
        if (visual == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration && visual != null)
        {
            float wave = (Mathf.Sin(elapsed * 9f) + 1f) * 0.5f;
            visual.transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, wave);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (visual != null) visual.transform.localScale = Vector3.one;
    }
    // Passes through the caster and, in Teams mode, the caster's teammates.
    private void IgnoreSelf(GameObject shot) => Teams.IgnoreFriendlyCollisions(shot, gameObject);
}