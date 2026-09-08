using System.Collections;
using UnityEngine;

public class ElectricityWizard : MonoBehaviour
{
    public bool allow1 = true, allow2 = true, allow3 = true, allow4 = true;
    public GameObject attack1, attack2, attack3, attack4;
    public PlayerStats statScript;
    public bool running;
    public ControllerScript controllerScript;
    private void Start() { attack1 = Resources.Load<GameObject>("electricityball"); attack2 = Resources.Load<GameObject>("lightningRod"); attack3 = Resources.Load<GameObject>("speedUpParticles"); Sprite oc = Resources.Load<Sprite>("Overcharge"); attack4 = new GameObject("OverchargeVisual"); attack4.AddComponent<SpriteRenderer>().sprite = oc; attack4.transform.SetParent(transform); attack4.transform.localPosition = Vector3.zero; attack4.SetActive(false); statScript = GetComponent<PlayerStats>(); controllerScript = GetComponent<ControllerScript>(); }
    public string getName() => "ElectricityWizard";
    public void basicAttack(object[] args) { if (allow1 && statScript != null && statScript.DepleteEnergy(33)) StartCoroutine(Basic(args)); }
    private IEnumerator Basic(object[] args) { allow1 = false; GameObject shot = Instantiate(attack1, transform.position, Quaternion.identity); IgnoreSelf(shot); shot.transform.Rotate((Vector3)args[0]); Destroy(shot, 2f); yield return new WaitForSeconds(.3f); allow1 = true; }
    public void secondAttack(object[] args) { if (allow2 && statScript != null && statScript.DepleteEnergy(30)) StartCoroutine(Second(args)); }
    private IEnumerator Second(object[] args) { allow2 = false; Instantiate(attack2, (Vector3)args[1], Quaternion.identity); yield return new WaitForSeconds(3.5f); allow2 = true; }
    public void thirdAttack(object[] args) { if (allow3 && statScript != null && statScript.DepleteEnergy(30)) StartCoroutine(Third()); }
    private IEnumerator Third() { allow3 = false; controllerScript?.speedUp(50); if (attack3 != null) { GameObject shot = Instantiate(attack3, transform.position, Quaternion.identity); shot.transform.SetParent(transform); Destroy(shot, 2.5f); } yield return new WaitForSeconds(2.5f); controllerScript?.removeSpeed(50); allow3 = true; }
    public void ultimateAttack(object[] args)
    {
        if (!allow4 || statScript == null || controllerScript == null || controllerScript.ultimateActive || !statScript.TryUseUltimate()) return;
        allow4 = false;
        controllerScript.ultimateActive = true;
        StartCoroutine(Overcharge());
    }
    private IEnumerator Overcharge()
    {
        if (attack4 != null) attack4.SetActive(true);
        StartCoroutine(PulseAreaVisual(attack4, 1.0f, 1.4f, 4f));
        float elapsed = 0f;
        while (elapsed < 4f)
        {
            SpawnOverchargeRod();
            if (attack3 != null) { GameObject shot = Instantiate(attack3, transform.position, Quaternion.identity); shot.transform.SetParent(transform); Destroy(shot, 0.5f); }
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(0f, 0f, angle * Mathf.Rad2Deg);
            Vector3 targetPoint = transform.position + new Vector3(Mathf.Cos(angle) * 6f, -Mathf.Sin(angle) * 6f, 0f);
            object[] fakeArgs = { direction, targetPoint };
            if (attack1 != null && statScript != null && statScript.DepleteEnergy(0))
            {
                GameObject bolt = Instantiate(attack1, transform.position, Quaternion.identity);
                IgnoreSelf(bolt);
                bolt.transform.Rotate(direction);
                Destroy(bolt, 2f);
            }
            elapsed += 0.3f;
            yield return new WaitForSeconds(0.3f);
        }
        controllerScript.ultimateActive = false;
        allow4 = true;
        if (attack4 != null) attack4.SetActive(false);
    }

    private void SpawnOverchargeRod()
    {
        if (attack2 == null) return;
        Vector2 offset = UnityEngine.Random.insideUnitCircle * 5f;
        GameObject rod = Instantiate(attack2, transform.position + new Vector3(offset.x, offset.y, 0f), Quaternion.identity);
        Destroy(rod, 2.5f);
    }

    private IEnumerator PulseAreaVisual(GameObject visual, float minScale, float maxScale, float duration)
    {
        if (visual == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration && visual != null)
        {
            float wave = (Mathf.Sin(elapsed * 12f) + 1f) * 0.5f;
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
        StartCoroutine(ChainLightning());
    }
    private IEnumerator ChainLightning()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        System.Collections.Generic.List<GameObject> targets = new System.Collections.Generic.List<GameObject>();
        foreach (GameObject player in players) if (player != gameObject) targets.Add(player);
        targets.Sort((a, b) => (a.transform.position - transform.position).sqrMagnitude.CompareTo((b.transform.position - transform.position).sqrMagnitude));
        int chained = 0, damage = 3;
        foreach (GameObject target in targets)
        {
            if (chained >= 3) break;
            target.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
            chained++;
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(4f);
        allow3 = true;
    }
    private void IgnoreSelf(GameObject shot) { Collider2D a = shot.GetComponent<Collider2D>(), b = GetComponent<Collider2D>(); if (a != null && b != null) Physics2D.IgnoreCollision(a, b); }
}