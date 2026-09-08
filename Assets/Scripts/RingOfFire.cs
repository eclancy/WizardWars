using System.Collections;
using UnityEngine;

public class RingOfFire : MonoBehaviour
{
    public float burnDuration = 6f;
    public float damagePerTick = 3f;
    public GameObject fireSprite;

    private void Start()
    {
        fireSprite = Resources.Load<GameObject>("OnFire");
        StartCoroutine(RotateObject());
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.transform.root.CompareTag("Player"))
            collision.transform.root.SendMessage("setFire", new object[] { burnDuration, damagePerTick }, SendMessageOptions.DontRequireReceiver);
        if (collision.gameObject.name == "icePatch(Clone)") Destroy(collision.gameObject);
    }

    public void disappear() { StartCoroutine(Disappear()); }
    private IEnumerator Disappear()
    {
        PolygonCollider2D collider = GetComponent<PolygonCollider2D>(); if (collider != null) collider.enabled = false;
        SpriteRenderer renderer = GetComponent<SpriteRenderer>(); if (renderer != null) renderer.enabled = false;
        AudioClip clip = GetComponent<AudioSource>()?.clip; if (clip != null) yield return new WaitForSeconds(clip.length);
        Destroy(gameObject);
    }

    private IEnumerator RotateObject()
    {
        while (this != null) { transform.Rotate(0f, 180f, 0f); yield return new WaitForSeconds(.3f); }
    }
}