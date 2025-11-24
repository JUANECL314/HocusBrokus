using UnityEngine;

public class ScrollUV : MonoBehaviour
{
    public float scrollX = 0.2f;
    public float scrollY = 0.3f;

    Renderer rend;

    void Start() { rend = GetComponent<Renderer>(); }

    void Update()
    {
        float offsetX = Time.time * scrollX;
        float offsetY = Time.time * scrollY;
        rend.material.SetTextureOffset("_BaseMap", new Vector2(offsetX, offsetY));
    }
}
