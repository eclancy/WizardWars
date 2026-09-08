using System.Collections;
using UnityEngine;

public class Fireball : MonoBehaviour
{
    private void FixedUpdate()
    {
        transform.Translate(Vector3.right * 0.9f * Time.deltaTime * 10f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.gameObject.SendMessage("ApplyDamage", 20);
        }
        else if (collision.gameObject.name == "electricityball(Clone)" ||
                 collision.gameObject.name == "mudball(Clone)" ||
                 collision.gameObject.name == "icicle(Clone)")
        {
            collision.SendMessage("disappear");
        }

        StartCoroutine(Disappear());
    }

    private IEnumerator Disappear()
    {
        GetComponent<CircleCollider2D>().enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;

        AudioClip clip = GetComponent<AudioSource>().clip;
        if (clip != null)
        {
            yield return new WaitForSeconds(clip.length);
        }

        Destroy(gameObject);
    }
}