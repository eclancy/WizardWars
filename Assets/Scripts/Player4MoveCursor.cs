using UnityEngine;

public class Player4MoveCursor : MonoBehaviour
{
    public float baseSpeed = 12f;
    private void Update() { Move(Input.GetAxis("L_XAxis_4"), Input.GetAxis("L_YAxis_4")); }
    private void Move(float x, float y) { Vector3 target = transform.position + new Vector3(x, y, 0f); transform.position = Vector3.MoveTowards(transform.position, target, new Vector2(x, y).magnitude * baseSpeed * Time.deltaTime); }
}