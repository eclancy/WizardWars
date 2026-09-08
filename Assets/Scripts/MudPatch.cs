using System.Collections.Generic;
using UnityEngine;

public class MudPatch : MonoBehaviour
{
    private readonly List<GameObject> inRangePlayers = new List<GameObject>();
    private float spawnTime;

    private void Start() { spawnTime = Time.time; }

    private void Update()
    {
        if (Time.time <= spawnTime + 3f) return;
        foreach (GameObject player in inRangePlayers) if (player != null) player.GetComponent<ControllerScript>()?.removeSlow(50);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.root.CompareTag("Player"))
        {
            GameObject player = collision.transform.root.gameObject;
            player.GetComponent<ControllerScript>()?.slow(50);
            player.SendMessage("putOutFire", SendMessageOptions.DontRequireReceiver);
            if (!inRangePlayers.Contains(player)) inRangePlayers.Add(player);
        }
        if (collision.gameObject.name == "ringOfFire(Clone)") Destroy(collision.gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.transform.root.CompareTag("Player")) return;
        GameObject player = collision.transform.root.gameObject;
        player.GetComponent<ControllerScript>()?.removeSlow(50);
        inRangePlayers.Remove(player);
    }

    public void disappear() { Destroy(gameObject); }
}