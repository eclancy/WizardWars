using UnityEngine;

public class FadeIn : MonoBehaviour
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
        if (Time.time > 2f && color.a < 1f)
        {
            color.a += 0.01f;
            GetComponent<Renderer>().material.color = color;
        }
    }
}