using System.Collections;
using UnityEngine;

public class IcePatch : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.transform.root.CompareTag("Player"))
        {
            ControllerScript controller = collision.transform.root.GetComponent<ControllerScript>();
            if (controller != null) { controller.sliding(); controller.SendMessage("putOutFire", SendMessageOptions.DontRequireReceiver); }
        }
        if (collision.gameObject.name == "mudPatch(Clone)") Destroy(collision.gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.transform.root.CompareTag("Player")) return;
        ControllerScript controller = collision.transform.root.GetComponent<ControllerScript>();
        if (controller != null) controller.notSliding();
    }

    public void disappear() { StartCoroutine(Disappear()); }
    private IEnumerator Disappear()
    {
        PolygonCollider2D collider = GetComponent<PolygonCollider2D>(); if (collider != null) collider.enabled = false;
        SpriteRenderer renderer = GetComponent<SpriteRenderer>(); if (renderer != null) renderer.enabled = false;
        AudioClip clip = GetComponent<AudioSource>()?.clip; if (clip != null) yield return new WaitForSeconds(clip.length);
        Destroy(gameObject);
    }
}