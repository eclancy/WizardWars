using UnityEngine;

public class Player4MoveCursor : MonoBehaviour
{
    public float baseSpeed = 12f;
    public float deadzone = 0.2f;
    private void Update() { Move(Input.GetAxisRaw("L_XAxis_4"), Input.GetAxisRaw("L_YAxis_4")); }

    // Raw input so the cursor starts and stops with the stick instead of easing in
    // through the InputManager's smoothing, clamped to 1 so a diagonal is not faster.
    private void Move(float x, float y)
    {
        Vector2 move = new Vector2(x, y);
        if (move.sqrMagnitude < deadzone * deadzone) return;
        move = Vector2.ClampMagnitude(move, 1f);
        Vector3 target = transform.position + new Vector3(move.x, move.y, 0f);
        transform.position = Vector3.MoveTowards(transform.position, target, move.magnitude * baseSpeed * Time.deltaTime);
    }
}
