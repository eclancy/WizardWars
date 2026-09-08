using System.Collections;
using UnityEngine;

public class RockWall : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.transform.root.CompareTag("destroyableAttack")) collision.SendMessage("disappear", SendMessageOptions.DontRequireReceiver);
        if (collision.transform.root.name == "flameThrower(Clone)") Destroy(collision.gameObject);
    }

    public void disappear() { StartCoroutine(Disappear()); }
    private IEnumerator Disappear()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>(); if (collider != null) collider.enabled = false;
        SpriteRenderer renderer = GetComponent<SpriteRenderer>(); if (renderer != null) renderer.enabled = false;
        AudioClip clip = GetComponent<AudioSource>()?.clip; if (clip != null) yield return new WaitForSeconds(clip.length);
        Destroy(gameObject);
    }
}