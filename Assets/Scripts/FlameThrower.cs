using System.Collections;
using UnityEngine;

public class FlameThrower : MonoBehaviour
{
    public GameObject fireSprite;
    public float burnDuration = 2f;
    public float damagePerTick = 1f;
    public float directDamage = 3f;
    public float damageInterval = 1f;
    // Set by FireWizard so the caster cannot be burned by his own flame.
    public GameObject caster;
    [System.NonSerialized] public int ownerTeam = Teams.NoTeam;
    private float lastDamage;

    private void Start()
    {
        fireSprite = Resources.Load<GameObject>("OnFire");
        Transform particles = transform.Find("Particles");
        Renderer renderer = particles == null ? null : particles.GetComponent<Renderer>();
        if (renderer != null) renderer.sortingOrder = 3;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.transform.root.gameObject == caster || Teams.IsAlly(ownerTeam, collision.transform.root.gameObject)) return;
        if (!collision.transform.root.CompareTag("Player") || Time.time <= damageInterval + lastDamage) return;
        collision.SendMessage("ApplyDamage", Mathf.RoundToInt(directDamage), SendMessageOptions.DontRequireReceiver);
        lastDamage = Time.time;
        collision.transform.root.SendMessage("setFire", new object[] { burnDuration, damagePerTick }, SendMessageOptions.DontRequireReceiver);
    }
}