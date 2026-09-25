using System.Collections;
using UnityEngine;

// A single flame placed by Ring of Fire. Sources burn and die independently: one
// that lands against a wall, an obstacle, or a raised rock wall is put out on its
// own and the rest of the ring keeps burning.
//
// Players are found by an overlap test on a slow tick rather than by trigger
// callbacks, which keeps a source to one GameObject with no Rigidbody2D and makes
// the wall check and the burn check the same piece of code.
public class FireSource : MonoBehaviour
{
    public float burnDuration = 6f;
    public float damagePerTick = 3f;
    public float lifetime = 4f;
    public float radius = 0.55f;
    public float tickInterval = 0.35f;
    // The wizard who lit this fire. Overlap queries ignore Physics2D.IgnoreCollision,
    // so the caster has to be skipped by hand or he burns in his own ring.
    public GameObject caster;

    private SpriteRenderer flameRenderer;
    private bool extinguished;
    private bool erupted;
    // The scale the flame settles at, captured before any coroutine animates it.
    private Vector3 fullScale;

    private void Start()
    {
        flameRenderer = GetComponent<SpriteRenderer>();
        fullScale = transform.localScale;
        // A source that spawns inside solid geometry never lights at all.
        if (ProceduralFx.IsInsideSolid(transform.position, radius * 0.6f))
        {
            Destroy(gameObject);
            return;
        }
        StartCoroutine(Erupt());
        StartCoroutine(Animate());
        StartCoroutine(Burn());
    }

    // The flame grows up out of the ground and flickers as it takes, the way a fire
    // catches: it climbs, stutters back, and settles at full height. The sprite is
    // pivoted at its base, so scaling y is the flame rising from the ground.
    private IEnumerator Erupt()
    {
        float elapsed = 0f, duration = 0.4f;
        float stutterSeed = Random.value * 10f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float take = Mathf.SmoothStep(0f, 1f, t);
            // Stutters are strongest early and die away as the fire establishes.
            float stutter = 1f + 0.3f * Mathf.Sin(t * 22f + stutterSeed) * (1f - t) * (1f - t);
            transform.localScale = new Vector3(fullScale.x * Mathf.Lerp(0.5f, 1f, take), fullScale.y * take * stutter, fullScale.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = fullScale;
        erupted = true;
    }

    // Kept for the existing RockWall contract, which puts out anything tagged
    // destroyableAttack by sending this message.
    public void disappear() => Extinguish();

    private IEnumerator Burn()
    {
        float elapsed = 0f;
        while (!extinguished && elapsed < lifetime)
        {
            // A wall raised after the ring was placed puts this source out too.
            if (ProceduralFx.IsInsideSolid(transform.position, radius * 0.6f)) { Extinguish(); yield break; }

            foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, radius))
            {
                if (hit == null) continue;
                Transform root = hit.transform.root;
                if (root.gameObject == caster) continue;
                if (root.CompareTag("Player"))
                    root.SendMessage("setFire", new object[] { burnDuration, damagePerTick }, SendMessageOptions.DontRequireReceiver);
                else if (hit.gameObject.name == "icePatch(Clone)")
                    Destroy(hit.gameObject);
                else if (hit.gameObject.name == "mudPatch(Clone)")
                {
                    // Mud still puts fire out, but now only the source standing in it.
                    Extinguish();
                    yield break;
                }
            }

            elapsed += tickInterval;
            yield return new WaitForSeconds(tickInterval);
        }
        if (!extinguished) Extinguish();
    }

    private void Extinguish()
    {
        if (extinguished) return;
        extinguished = true;
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f, duration = 0.35f;
        Vector3 startScale = transform.localScale;
        while (elapsed < duration && flameRenderer != null)
        {
            float t = elapsed / duration;
            Color color = flameRenderer.color;
            flameRenderer.color = new Color(color.r, color.g, color.b, 1f - t);
            transform.localScale = startScale * Mathf.Lerp(1f, 0.3f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    // Cycle the generated flame frames. Each source starts on its own frame and runs
    // at its own rate, so a ring of them does not pulse in lockstep. Scale wobble is
    // kept small now that the silhouette itself animates, and alpha stays at 1 so the
    // flat art keeps its contrast against the stage.
    private IEnumerator Animate()
    {
        Sprite[] frames = ProceduralFx.FlameFrames;
        int frame = Random.Range(0, frames.Length);
        float seed = Random.value * 10f;
        float rate = Random.Range(11f, 15f);
        while (!extinguished)
        {
            if (flameRenderer != null) flameRenderer.sprite = frames[frame % frames.Length];
            frame++;
            // Wobble around the settled scale, never around the current one, so the
            // flame cannot drift larger or smaller over its lifetime.
            if (erupted)
            {
                float wave = Mathf.PerlinNoise(seed, Time.time * 2.5f);
                transform.localScale = new Vector3(fullScale.x * Mathf.Lerp(0.94f, 1.04f, wave), fullScale.y * Mathf.Lerp(0.92f, 1.12f, wave), fullScale.z);
            }
            yield return new WaitForSeconds(1f / rate);
        }
    }
}
