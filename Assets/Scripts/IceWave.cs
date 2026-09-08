using System.Collections;
using UnityEngine;

public class IceWave : MonoBehaviour
{
    public GameObject iceCubeSprite;
    private void Start() { iceCubeSprite = Resources.Load<GameObject>("iceCube"); transform.Translate(Vector2.right * 3f); }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.transform.root.CompareTag("Player")) return;
        PlayerStats stats = collision.transform.root.GetComponent<PlayerStats>();
        if (stats == null) return;
        if (!stats.getStunned() && iceCubeSprite != null)
        {
            GameObject sprite = Instantiate(iceCubeSprite, collision.transform.position, Quaternion.identity);
            sprite.transform.SetParent(collision.transform.root);
        }
        stats.stun(1.5f);
        stats.ApplyDamage(8);
    }

    public void disappear() { StartCoroutine(Disappear()); }
    private IEnumerator Disappear()
    {
        CircleCollider2D collider = GetComponent<CircleCollider2D>(); if (collider != null) collider.enabled = false;
        SpriteRenderer renderer = GetComponent<SpriteRenderer>(); if (renderer != null) renderer.enabled = false;
        AudioClip clip = GetComponent<AudioSource>()?.clip; if (clip != null) yield return new WaitForSeconds(clip.length);
        Destroy(gameObject);
    }
}