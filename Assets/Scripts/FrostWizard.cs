using System.Collections;
using UnityEngine;

public class FrostWizard : MonoBehaviour
{
    public bool allow1 = true, allow2 = true, allow3 = true, allow4 = true;
    public GameObject attack1, attack2, attack3, attack4;
    public PlayerStats statScript;
    public ControllerScript controllerScript;
    private Sprite deepFreezeSprite;
    private void Start() { attack1 = Resources.Load<GameObject>("icicle"); attack2 = Resources.Load<GameObject>("icePatch"); attack3 = Resources.Load<GameObject>("iceWave"); Sprite az = Resources.Load<Sprite>("AbsoluteZero"); deepFreezeSprite = Resources.Load<Sprite>("DeepFreeze"); attack4 = new GameObject("AbsoluteZeroVisual"); attack4.AddComponent<SpriteRenderer>().sprite = az; attack4.transform.SetParent(transform); attack4.transform.localPosition = Vector3.zero; attack4.SetActive(false); statScript = GetComponent<PlayerStats>(); controllerScript = GetComponent<ControllerScript>(); }
    public string getName() => "FrostWizard";
    public void basicAttack(object[] args) { if (allow1 && statScript != null && statScript.DepleteEnergy(33)) StartCoroutine(Basic(args)); }
    private IEnumerator Basic(object[] args) { allow1 = false; GameObject shot = Instantiate(attack1, transform.position, Quaternion.identity); IgnoreSelf(shot); shot.transform.Rotate((Vector3)args[0]); Destroy(shot, 2f); yield return new WaitForSeconds(.3f); allow1 = true; }
    public void secondAttack(object[] args) { if (allow2 && statScript != null && statScript.DepleteEnergy(50)) StartCoroutine(Second(args)); }
    private IEnumerator Second(object[] args) { allow2 = false; Instantiate(attack2, (Vector3)args[1], Quaternion.identity); yield return new WaitForSeconds(3f); foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player")) player.SendMessage("notSliding", SendMessageOptions.DontRequireReceiver); yield return new WaitForSeconds(1f); allow2 = true; }
    public void thirdAttack(object[] args) { if (allow3 && statScript != null && statScript.DepleteEnergy(66)) StartCoroutine(Third(args)); }
    private IEnumerator Third(object[] args) { allow3 = false; GameObject shot = Instantiate(attack3, transform.position, Quaternion.identity); IgnoreSelf(shot); shot.transform.Rotate((Vector3)args[0]); Destroy(shot, .5f); yield return new WaitForSeconds(4f); allow3 = true; }
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
    public void fourthAttack(object[] args)
    {
        if (!allow3 || statScript == null || controllerScript == null || controllerScript.ultimateActive || !statScript.DepleteEnergy(85)) return;
        allow3 = false;
        StartCoroutine(DeepFreeze(args));
    }
    private IEnumerator DeepFreeze(object[] args)
    {
        Vector3 targetPoint = args != null && args.Length > 1 ? (Vector3)args[1] : transform.position;
        GameObject spike = new GameObject("DeepFreezeSpike");
        spike.transform.position = targetPoint;
        SpriteRenderer spikeRenderer = spike.AddComponent<SpriteRenderer>();
        spikeRenderer.sprite = deepFreezeSprite;
        spikeRenderer.sortingOrder = 10;
        StartCoroutine(PulseAreaVisual(spike, 0.75f, 1.05f, 1.5f));
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player == gameObject) continue;
            if ((player.transform.position - targetPoint).sqrMagnitude <= 4f) player.SendMessage("stun", 4f, SendMessageOptions.DontRequireReceiver);
        }
        yield return new WaitForSeconds(1.5f);
        Destroy(spike);
        yield return new WaitForSeconds(4.5f);
        allow3 = true;
    }
    private void IgnoreSelf(GameObject shot) { Collider2D a = shot.GetComponent<Collider2D>(), b = GetComponent<Collider2D>(); if (a != null && b != null) Physics2D.IgnoreCollision(a, b); }
}