using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class SunMuralQuad : MonoBehaviour
{
    private Renderer rend;

    public HiddenPathController hiddenPath; // asignar en inspector


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
        gameObject.SetActive(true); // hace visible el mural
        if (hiddenPath != null)
            hiddenPath.ShowPath(); // sincroniza el movimiento con todos
    }
}
