using UnityEngine;

public class FireFadeIn : MonoBehaviour
{
    private Color color;

    private void Start()
    {
        color = GetComponent<Renderer>().material.color;
        color.a = 0f;
        GetComponent<Renderer>().material.color = color;
    }

    private void Update()
    {
        if (Time.time > 0f && color.a < 1f)
        {
            color.a += 0.02f;
            GetComponent<Renderer>().material.color = color;
        }
    }
}