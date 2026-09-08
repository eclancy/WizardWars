using System.Collections;
using UnityEngine;

public class EarthWizard : MonoBehaviour
{
    public bool allow1 = true, allow2 = true, allow3 = true, allow4 = true;
    public GameObject attack1, attack2, attack3Vertical, attack3Horizontal, attack4;
    public PlayerStats statScript;
    public ControllerScript controllerScript;
    private void Start() { attack1 = Resources.Load<GameObject>("mudball"); attack2 = Resources.Load<GameObject>("mudPatch"); attack3Vertical = Resources.Load<GameObject>("RockWallVertical"); attack3Horizontal = Resources.Load<GameObject>("RockWallHorizontal"); Sprite fort = Resources.Load<Sprite>("Fortress"); attack4 = new GameObject("FortressVisual"); attack4.AddComponent<SpriteRenderer>().sprite = fort; attack4.transform.SetParent(transform); attack4.transform.localPosition = Vector3.zero; attack4.SetActive(false); statScript = GetComponent<PlayerStats>(); controllerScript = GetComponent<ControllerScript>(); }
    public string getName() => "EarthWizard";
    public void basicAttack(object[] args) { if (allow1 && statScript != null && statScript.DepleteEnergy(33)) StartCoroutine(Basic(args)); }
    private IEnumerator Basic(object[] args) { allow1 = false; GameObject shot = Instantiate(attack1, transform.position, Quaternion.identity); IgnoreSelf(shot); shot.transform.Rotate((Vector3)args[0]); Destroy(shot, 2f); yield return new WaitForSeconds(.3f); allow1 = true; }
    public void secondAttack(object[] args) { if (allow2 && statScript != null && statScript.DepleteEnergy(50)) StartCoroutine(Second(args)); }
    private IEnumerator Second(object[] args) { allow2 = false; Instantiate(attack2, (Vector3)args[1], Quaternion.identity); yield return new WaitForSeconds(4f); allow2 = true; }
    public void thirdAttack(object[] args) { if (allow3 && statScript != null && statScript.DepleteEnergy(25)) StartCoroutine(Third(args)); }
    private IEnumerator Third(object[] args) { allow3 = false; Vector3 target = (Vector3)args[1]; bool horizontal = Mathf.Abs(Mathf.Abs(target.y) - Mathf.Abs(transform.position.y)) > Mathf.Abs(Mathf.Abs(target.x) - Mathf.Abs(transform.position.x)); GameObject shot = Instantiate(horizontal ? attack3Horizontal : attack3Vertical, target, Quaternion.identity); if (!horizontal) shot.transform.Rotate(0, 0, 270); Destroy(shot, 3f); yield return new WaitForSeconds(4f); allow3 = true; }
    public void ultimateAttack(object[] args)
    {
        if (!allow4 || statScript == null || controllerScript == null || controllerScript.ultimateActive || !statScript.TryUseUltimate()) return;
        allow4 = false;
        controllerScript.ultimateActive = true;
        StartCoroutine(Fortress());
    }
    private IEnumerator Fortress()
    {
        if (attack4 != null) attack4.SetActive(true);
        StartCoroutine(PulseAreaVisual(attack4, 1.0f, 1.2f, 4f));
        if (controllerScript != null) controllerScript.cantmove = true;
        System.Collections.Generic.List<GameObject> walls = new System.Collections.Generic.List<GameObject>();
        if (attack3Vertical != null)
        {
            walls.Add(Instantiate(attack3Vertical, transform.position + Vector3.left * 1.5f, Quaternion.identity));
            walls.Add(Instantiate(attack3Vertical, transform.position + Vector3.right * 1.5f, Quaternion.identity));
        }
        if (attack3Horizontal != null)
        {
            walls.Add(Instantiate(attack3Horizontal, transform.position + Vector3.up * 1.5f, Quaternion.identity));
            walls.Add(Instantiate(attack3Horizontal, transform.position + Vector3.down * 1.5f, Quaternion.identity));
        }
        yield return new WaitForSeconds(4f);
        foreach (GameObject wall in walls) if (wall != null) Destroy(wall);
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player == gameObject) continue;
            if ((player.transform.position - transform.position).sqrMagnitude <= 16f)
                player.SendMessage("ApplyDamage", 10, SendMessageOptions.DontRequireReceiver);
        }
        if (controllerScript != null) { controllerScript.cantmove = false; controllerScript.ultimateActive = false; }
        if (attack4 != null) attack4.SetActive(false);
        yield return new WaitForSeconds(2f);
        allow4 = true;
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
    public void fourthAttack(object[] args)
    {
        if (!allow3 || statScript == null || controllerScript == null || controllerScript.ultimateActive || !statScript.DepleteEnergy(85)) return;
        allow3 = false;
        StartCoroutine(Earthquake());
    }
    private IEnumerator Earthquake()
    {
        float radius = 12f;
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player == gameObject) continue;
            ControllerScript c = player.GetComponent<ControllerScript>();
            if (c != null && (player.transform.position - transform.position).sqrMagnitude <= radius * radius)
            {
                c.sliding();
                yield return new WaitForSeconds(0.15f);
                c.notSliding();
            }
        }
        yield return new WaitForSeconds(4f);
        allow3 = true;
    }
    private void IgnoreSelf(GameObject shot) { Collider2D a = shot.GetComponent<Collider2D>(), b = GetComponent<Collider2D>(); if (a != null && b != null) Physics2D.IgnoreCollision(a, b); }
}