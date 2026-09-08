using UnityEngine;

public class IceCubeIgnoreCollision : MonoBehaviour
{
    private void Start()
    {
        Collider2D ownCollider = GetComponent<Collider2D>();
        Collider2D rootCollider = transform.root.GetComponent<Collider2D>();
        if (ownCollider != null && rootCollider != null) Physics2D.IgnoreCollision(ownCollider, rootCollider);
    }
}