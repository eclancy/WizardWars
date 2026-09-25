using System.Collections.Generic;
using UnityEngine;

// One shard of a generated ice wave. FrostWizard builds and animates the wave; this
// component only carries the hit reaction so each shard can connect on its own.
// Every shard in a wave shares one `alreadyHit` set, so a player caught by several
// shards of the same cast is still only stunned and damaged once.
public class IceWaveShard : MonoBehaviour
{
    public float stunDuration = 1.5f;
    public int damage = 8;
    public GameObject caster;
    public GameObject iceCubePrefab;
    public HashSet<Transform> alreadyHit;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Transform root = collision.transform.root;
        if (!root.CompareTag("Player") || root.gameObject == caster) return;
        if (alreadyHit != null && !alreadyHit.Add(root)) return;

        PlayerStats stats = root.GetComponent<PlayerStats>();
        if (stats == null) return;
        if (!stats.getStunned() && iceCubePrefab != null)
        {
            GameObject cube = Instantiate(iceCubePrefab, root.position, Quaternion.identity);
            cube.transform.SetParent(root);
        }
        stats.stun(stunDuration);
        stats.ApplyDamage(damage);
    }
}
