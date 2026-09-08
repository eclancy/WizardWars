using System.Collections;
using UnityEngine;

public class LightningStaff : MonoBehaviour
{
    public Transform electricitySprite;
    public Animator anim;
    public float damageDelay = 0.5f;
    public float lastDamage = -10f;

    private void Start()
    {
        electricitySprite = transform.Find("rodShock");
        if (electricitySprite != null)
        {
            Renderer renderer = electricitySprite.GetComponent<Renderer>(); if (renderer != null) renderer.enabled = false;
            anim = electricitySprite.GetComponent<Animator>();
        }
        lastDamage = Time.time;
    }

    private void Update() { if (Time.time > damageDelay + lastDamage) { lastDamage = Time.time; StartCoroutine(Shock()); } }

    private IEnumerator Shock()
    {
        Renderer renderer = electricitySprite == null ? null : electricitySprite.GetComponent<Renderer>(); if (renderer != null) renderer.enabled = true;
        GetComponent<AudioSource>()?.Play();
        if (anim != null) { anim.Play("rodShock", -1, 0f); anim.speed = 7f; }
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, 1.8f))
            if (collider.transform.root.CompareTag("Player")) collider.transform.root.SendMessage("ApplyDamage", 3, SendMessageOptions.DontRequireReceiver);
        yield return new WaitForSeconds(.3f);
        if (renderer != null) renderer.enabled = false;
    }
}