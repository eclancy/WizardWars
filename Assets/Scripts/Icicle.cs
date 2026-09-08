using System.Collections;
using UnityEngine;

public class Icicle : MonoBehaviour
{
    private void FixedUpdate() { transform.Translate(Vector3.right * 0.9f * Time.deltaTime * 10f); }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) collision.SendMessage("ApplyDamage", 20, SendMessageOptions.DontRequireReceiver);
        else if (collision.gameObject.name == "electricityball(Clone)" || collision.gameObject.name == "mudball(Clone)" || collision.gameObject.name == "fireball(Clone)") collision.SendMessage("disappear", SendMessageOptions.DontRequireReceiver);
        StartCoroutine(Disappear());
    }

    public void disappear() { StartCoroutine(Disappear()); }

    private IEnumerator Disappear()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null) collider.enabled = false;
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.enabled = false;
        AudioClip clip = GetComponent<AudioSource>()?.clip;
        if (clip != null) yield return new WaitForSeconds(clip.length);
        Destroy(gameObject);
    }
}