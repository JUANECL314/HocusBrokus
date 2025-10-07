using UnityEngine;

public class MirrorButton : MonoBehaviour
{
    public MirrorController mirrorController;  // Assign this in the Inspector
    public bool rotateLeft;                    // true = left, false = right

    public void Press()
    {
        if (rotateLeft)
            mirrorController.RotateLeft();
        else
            mirrorController.RotateRight();
    }
    private Renderer rend;
    private Color defaultColor;

    void Start()
    {
        rend = GetComponent<Renderer>();
        defaultColor = rend.material.color;
    }

    void OnMouseEnter()
    {
        rend.material.color = Color.yellow;
    }

    void OnMouseExit()
    {
        rend.material.color = defaultColor;
    }

}
