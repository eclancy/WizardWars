using System.Collections.Generic;
using UnityEngine;

public class MudPatch : MonoBehaviour
{
    // The earth wizard's team. Teammates still have fire put out by the mud but are
    // not slowed by it. The caster himself is still slowed, as he always has been.
    [System.NonSerialized] public int ownerTeam = Teams.NoTeam;
    [System.NonSerialized] public GameObject caster;
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
            player.SendMessage("putOutFire", SendMessageOptions.DontRequireReceiver);
            if (!IsTeammate(player))
            {
                player.GetComponent<ControllerScript>()?.slow(50);
                if (!inRangePlayers.Contains(player)) inRangePlayers.Add(player);
            }
        }
        if (collision.gameObject.name == "ringOfFire(Clone)") Destroy(collision.gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.transform.root.CompareTag("Player")) return;
        GameObject player = collision.transform.root.gameObject;
        if (IsTeammate(player)) return;
        player.GetComponent<ControllerScript>()?.removeSlow(50);
        inRangePlayers.Remove(player);
    }

    private bool IsTeammate(GameObject player) => player != caster && Teams.IsAlly(ownerTeam, player);

    public void disappear() { Destroy(gameObject); }
}