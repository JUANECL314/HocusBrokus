using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class SunMuralQuad : MonoBehaviour
{
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
            rend.enabled = false; // start invisible
    }

    public void Show()
    {
        if (rend != null)
            rend.enabled = true;  // make visible
    }
}
