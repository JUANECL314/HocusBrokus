// ...existing code...
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MirrorButton : MonoBehaviour
{
    public MirrorController mirrorController;  // Assign this in the Inspector
    public bool rotateLeft;

    [Header("Optional move-up action")]
    public bool moveUpMode = false;
    public float moveUpAmount = 0.2f;

    public string[] magicTags = new string[] { "Fire", "Earth", "Wind", "Water" };
    public bool useTrigger = true;

    public void Press()
    {
        if (mirrorController == null) return;

        if (moveUpMode)
        {
            mirrorController.MoveUp(moveUpAmount);
            // (SFX ahora lo dispara el MirrorController)
        }
        else
        {
            if (rotateLeft) mirrorController.RotateLeft();
            else mirrorController.RotateRight();
            // (SFX ahora lo dispara el MirrorController)
        }
    }

    private Renderer rend;
    private Color defaultColor;

    void Start()
    {
        // try to find a renderer on this object or its children
        rend = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
        if (rend != null)
            defaultColor = rend.material.color;
    }

    void OnMouseEnter()
    {
        if (rend != null)
            rend.material.color = Color.yellow;
    }

    void OnMouseExit()
    {
        if (rend != null)
            rend.material.color = defaultColor;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!useTrigger) return;
        if (IsMagic(other.gameObject))
            Press();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (useTrigger) return;
        if (IsMagic(collision.collider.gameObject))
            Press();
    }

    bool IsMagic(GameObject go)
    {
        if (go == null || magicTags == null) return false;
        foreach (var t in magicTags)
        {
            if (!string.IsNullOrEmpty(t) && go.CompareTag(t))
                return true;
        }
        return false;
    }
}
// ...existing code...