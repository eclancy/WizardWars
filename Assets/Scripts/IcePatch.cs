using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IcePatch : MonoBehaviour
{
    public float lifetime = 2f;
    // The frost wizard's team. Teammates still have fire put out by the ice but do
    // not slip on it. The caster himself still slips, as he always has.
    [System.NonSerialized] public int ownerTeam = Teams.NoTeam;
    [System.NonSerialized] public GameObject caster;
    private readonly List<GameObject> inRangePlayers = new List<GameObject>();
    private float expireTime;

    private void Start() { expireTime = Time.time + lifetime; }

    // FrostWizard restarts the clock once the patch has finished glossing over, so
    // the formation telegraph does not eat into the patch's two seconds.
    public void beginLife() { expireTime = Time.time + lifetime; }

    private void Update()
    {
        if (Time.time < expireTime) return;
        ReleasePlayers();
        Destroy(gameObject);
    }

    // Destroying the trigger does not run this object's own OnTriggerExit2D, so
    // anyone still standing on the patch has to be let go by hand or they stay stuck
    // sliding for the rest of the match.
    private void ReleasePlayers()
    {
        foreach (GameObject player in inRangePlayers) if (player != null) player.GetComponent<ControllerScript>()?.notSliding();
        inRangePlayers.Clear();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.transform.root.CompareTag("Player"))
        {
            ControllerScript controller = collision.transform.root.GetComponent<ControllerScript>();
            if (controller != null && IsTeammate(controller.gameObject))
                controller.SendMessage("putOutFire", SendMessageOptions.DontRequireReceiver);
            else if (controller != null)
            {
                controller.sliding();
                controller.SendMessage("putOutFire", SendMessageOptions.DontRequireReceiver);
                GameObject player = collision.transform.root.gameObject;
                if (!inRangePlayers.Contains(player)) inRangePlayers.Add(player);
            }
        }
        if (collision.gameObject.name == "mudPatch(Clone)") Destroy(collision.gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.transform.root.CompareTag("Player") || IsTeammate(collision.transform.root.gameObject)) return;
        ControllerScript controller = collision.transform.root.GetComponent<ControllerScript>();
        if (controller != null) controller.notSliding();
        inRangePlayers.Remove(collision.transform.root.gameObject);
    }

    private bool IsTeammate(GameObject player) => player != caster && Teams.IsAlly(ownerTeam, player);

    public void disappear() { StartCoroutine(Disappear()); }
    private IEnumerator Disappear()
    {
        ReleasePlayers();
        PolygonCollider2D collider = GetComponent<PolygonCollider2D>(); if (collider != null) collider.enabled = false;
        SpriteRenderer renderer = GetComponent<SpriteRenderer>(); if (renderer != null) renderer.enabled = false;
        AudioClip clip = GetComponent<AudioSource>()?.clip; if (clip != null) yield return new WaitForSeconds(clip.length);
        Destroy(gameObject);
    }
}
