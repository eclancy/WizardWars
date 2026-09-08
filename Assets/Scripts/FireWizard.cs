using System.Collections;
using UnityEngine;

public class FireWizard : MonoBehaviour
{
    public bool allow1 = true, allow2 = true, allow3 = true, allowFlame = true, allow4 = true;
    public GameObject attack1, attack2, attack3, attack4;
    public PlayerStats statScript;
    public bool flameThrower, rechargeFlame;
    public object[] flameThrowerArgs;
    public GameObject flameShot;
    private Sprite meteorSprite;
    public float lastTick;
    public ControllerScript controllerScript;

    private void Start()
    {
        attack1 = Resources.Load<GameObject>("fireball"); attack2 = Resources.Load<GameObject>("ringOfFire"); attack3 = Resources.Load<GameObject>("flameThrower");
        Sprite phoenixSprite = Resources.Load<Sprite>("PhoenixRebirth");
        meteorSprite = Resources.Load<Sprite>("MeteorStrike");
        attack4 = new GameObject("PhoenixRebirthVisual");
        attack4.AddComponent<SpriteRenderer>().sprite = phoenixSprite;
        attack4.transform.SetParent(transform);
        attack4.transform.localPosition = Vector3.zero;
        attack4.SetActive(false);
        statScript = GetComponent<PlayerStats>(); controllerScript = GetComponent<ControllerScript>();
    }
    public string getName() => "FireWizard";
    public void basicAttack(object[] args) { if (allow1 && statScript != null && statScript.DepleteEnergy(33)) StartCoroutine(Basic(args)); }
    private IEnumerator Basic(object[] args) { allow1 = false; SpawnProjectile(attack1, transform.position, args, true, 2f); yield return new WaitForSeconds(.3f); allow1 = true; }
    public void secondAttack(object[] args) { if (allow2 && statScript != null && statScript.DepleteEnergy(50)) StartCoroutine(Second(args)); }
    private IEnumerator Second(object[] args) { allow2 = false; SpawnProjectile(attack2, (Vector3)args[1], args, false, 3f); yield return new WaitForSeconds(4f); allow2 = true; }
    public void thirdAttack(object[] args) { if (!allow3 || rechargeFlame || statScript == null) return; controllerScript?.slow(25); flameThrower = true; flameThrowerArgs = args; flameShot = Instantiate(attack3, transform.position, Quaternion.identity); IgnoreSelf(flameShot); statScript.DepleteEnergy(15); allow3 = false; }
    public void ultimateAttack(object[] args)
    {
        if (!allow4 || statScript == null || controllerScript == null || controllerScript.ultimateActive || !statScript.TryUseUltimate()) return;
        allow4 = false;
        controllerScript.ultimateActive = true;
        StartCoroutine(PhoenixRebirth());
    }
    private IEnumerator PhoenixRebirth()
    {
        if (attack4 != null) attack4.SetActive(true);
        StartCoroutine(PulseAreaVisual(attack4, 1.0f, 1.35f, 5f));
        float elapsed = 0f;
        while (elapsed < 5f)
        {
            statScript.health = Mathf.Min(statScript.health + 8f * 0.5f, 200f);
            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (player == gameObject) continue;
                if ((player.transform.position - transform.position).sqrMagnitude <= 16f)
                    player.SendMessage("ApplyDamage", 2, SendMessageOptions.DontRequireReceiver);
            }
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
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
            float wave = (Mathf.Sin(elapsed * 8f) + 1f) * 0.5f;
            visual.transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, wave);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (visual != null) visual.transform.localScale = Vector3.one;
    }
    public void fourthAttack(object[] args)
    {
        if (!allowFlame || statScript == null || controllerScript == null || controllerScript.ultimateActive || !statScript.DepleteEnergy(80)) return;
        allowFlame = false;
        StartCoroutine(MeteorStrike(args));
    }
    private IEnumerator MeteorStrike(object[] args)
    {
        Vector3 targetPoint = args != null && args.Length > 1 ? (Vector3)args[1] : transform.position;
        GameObject meteorMarker = new GameObject("MeteorStrikeMarker");
        meteorMarker.transform.position = targetPoint;
        SpriteRenderer markerRenderer = meteorMarker.AddComponent<SpriteRenderer>();
        markerRenderer.sprite = meteorSprite;
        markerRenderer.sortingOrder = 10;
        meteorMarker.transform.localScale = Vector3.one * 0.8f;
        StartCoroutine(PulseAreaVisual(meteorMarker, 0.6f, 1.0f, 1.2f));
        yield return new WaitForSeconds(1.2f);
        Destroy(meteorMarker);
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player == gameObject) continue;
            if ((player.transform.position - targetPoint).sqrMagnitude <= 9f)
            {
                player.SendMessage("ApplyDamage", 35, SendMessageOptions.DontRequireReceiver);
                player.SendMessage("setFire", new object[] { 4f, 2f }, SendMessageOptions.DontRequireReceiver);
            }
        }
        if (attack2 != null) Instantiate(attack2, targetPoint, Quaternion.identity);
        yield return new WaitForSeconds(5f);
        allowFlame = true;
    }
    private void Update()
    {
        if (rechargeFlame && statScript != null && statScript.getEnergy() > 50) { rechargeFlame = false; allowFlame = true; }
        if (!flameThrower || flameShot == null || flameThrowerArgs == null) return;
        string button = (string)flameThrowerArgs[2];
        if (!Input.GetButtonUp(button) && allowFlame)
        {
            string xAxis = (string)flameThrowerArgs[3], yAxis = (string)flameThrowerArgs[4];
            float angle = Mathf.Atan2(-Input.GetAxis(yAxis), Input.GetAxis(xAxis)) * Mathf.Rad2Deg;
            flameShot.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0, 0, angle)); flameShot.transform.Translate(Vector2.right * 2.5f);
            statScript.noRecharge();
            if (Time.time > .2f + lastTick) { if (statScript.DepleteEnergy(.5f)) lastTick = Time.time; else StopFlame(); }
        }
        else StopFlame();
    }
    private void StopFlame() { if (flameShot != null) Destroy(flameShot); flameThrower = false; rechargeFlame = true; statScript.recharge(); controllerScript?.removeSlow(25); StartCoroutine(ResetFlame()); }
    private IEnumerator ResetFlame() { yield return new WaitForSeconds(2f); allowFlame = true; allow3 = true; }
    private void SpawnProjectile(GameObject prefab, Vector3 position, object[] args, bool rotate, float lifetime) { if (prefab == null) return; GameObject shot = Instantiate(prefab, position, Quaternion.identity); IgnoreSelf(shot); if (rotate) shot.transform.Rotate((Vector3)args[0]); Destroy(shot, lifetime); }
    private void IgnoreSelf(GameObject shot) { Collider2D a = shot.GetComponent<Collider2D>(), b = GetComponent<Collider2D>(); if (a != null && b != null) Physics2D.IgnoreCollision(a, b); }
}