using System.Collections;
using UnityEngine;

public class FlameThrower : MonoBehaviour
{
    public GameObject fireSprite;
    public float burnDuration = 2f;
    public float damagePerTick = 1f;
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
        if (!collision.transform.root.CompareTag("Player") || Time.time <= 1f + lastDamage) return;
        collision.SendMessage("ApplyDamage", 3, SendMessageOptions.DontRequireReceiver);
        lastDamage = Time.time;
        collision.transform.root.SendMessage("setFire", new object[] { burnDuration, damagePerTick }, SendMessageOptions.DontRequireReceiver);
    }
}