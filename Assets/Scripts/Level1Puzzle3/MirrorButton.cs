// ...existing code...
using UnityEngine;
using System.Collections;

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

    // NUEVO: versión con actor (para KPI)
    public void Press(GameObject actor)
    {
        Debug.Log("Pilar activándose (con actor)");
        KPITracker.Instance?.RegisterButtonPress(gameObject, actor);
        KPITracker.Instance?.RegisterRotationOrMoveUp(actor); // suma participación/cooperación

        if (mirrorController == null) return;

        if (moveUpMode)
        {
            mirrorController.MoveUp(moveUpAmount);
            // SFX lo dispara el MirrorController
        }
        else
        {
            if (rotateLeft) mirrorController.RotateLeft();
            else mirrorController.RotateRight();
            // SFX lo dispara el MirrorController
        }
    }

    // Retrocompatibilidad: sigue existiendo Press() sin actor
    public void Press()
    {
        Debug.Log("Pilar activándose");
        KPITracker.Instance?.RegisterButtonPress(gameObject, null);

        if (mirrorController == null) return;

        if (moveUpMode)
        {
            mirrorController.MoveUp(moveUpAmount);
        }
        else
        {
            if (rotateLeft) mirrorController.RotateLeft();
            else mirrorController.RotateRight();
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
            Press(other.gameObject); // << ahora pasamos actor para KPI
    }

    void OnCollisionEnter(Collision collision)
    {
        if (useTrigger) return;
        if (IsMagic(collision.collider.gameObject))
            Press(collision.collider.gameObject); // << ahora pasamos actor para KPI
    }

    bool IsMagic(GameObject go)
    {
        Debug.Log("Colisión");
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
