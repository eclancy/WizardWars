using System.Collections;
using UnityEngine;

public class ElectricityWizard : MonoBehaviour, IWizardAbilities
{
    public bool allow1 = true, allow2 = true, allow3 = true, allow4 = true;
    // The basic attack is free so a dry energy bar never leaves you with no action.
    public float basicCost = 0f;
    public float basicCooldown = 0.55f;
    public float rodCost = 30f;
    public float speedCost = 25f;
    public GameObject attack1, attack2, attack3, attack4;
    public PlayerStats statScript;
    public bool running;
    public ControllerScript controllerScript;
    private void Start() { attack1 = Resources.Load<GameObject>("electricityball"); attack2 = Resources.Load<GameObject>("lightningRod"); attack3 = Resources.Load<GameObject>("speedUpParticles"); Sprite oc = Resources.Load<Sprite>("Overcharge"); attack4 = new GameObject("OverchargeVisual"); attack4.AddComponent<SpriteRenderer>().sprite = oc; attack4.transform.SetParent(transform); attack4.transform.localPosition = Vector3.zero; attack4.SetActive(false); statScript = GetComponent<PlayerStats>(); controllerScript = GetComponent<ControllerScript>(); }
    public string getName() => "ElectricityWizard";
    public bool IsAbilityReady(int slot) => slot == 1 ? allow1 : slot == 2 ? allow2 : slot == 3 && allow3;

    public void basicAttack(object[] args) { if (allow1 && statScript != null && statScript.DepleteEnergy(basicCost)) StartCoroutine(Basic(args)); }
    private IEnumerator Basic(object[] args) { allow1 = false; GameObject shot = Instantiate(attack1, transform.position, Quaternion.identity); IgnoreSelf(shot); shot.transform.Rotate((Vector3)args[0]); Destroy(shot, 2f); yield return new WaitForSeconds(basicCooldown); allow1 = true; }
    public void secondAttack(object[] args) { if (allow2 && statScript != null && statScript.DepleteEnergy(rodCost)) StartCoroutine(Second(args)); }
    private IEnumerator Second(object[] args) { allow2 = false; MarkRod(Instantiate(attack2, (Vector3)args[1], Quaternion.identity)); yield return new WaitForSeconds(3.5f); allow2 = true; }
    public void thirdAttack(object[] args) { if (allow3 && statScript != null && statScript.DepleteEnergy(speedCost)) StartCoroutine(Third()); }
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
        MarkRod(rod);
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
    // Rods shock everyone in range, the caster included, but spare his teammates.
    private void MarkRod(GameObject rod)
    {
        LightningStaff staff = rod == null ? null : rod.GetComponent<LightningStaff>();
        if (staff != null) { staff.ownerTeam = Teams.Of(gameObject); staff.caster = gameObject; }
    }

    // Passes through the caster and, in Teams mode, the caster's teammates.
    private void IgnoreSelf(GameObject shot) => Teams.IgnoreFriendlyCollisions(shot, gameObject);
}