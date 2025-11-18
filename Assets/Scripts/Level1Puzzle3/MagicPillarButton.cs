using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MagicPillarButton : MonoBehaviour
{
    [Header("Element Type (must match projectile tag)")]
    public string elementTag = "Fire"; // Fire, Water, Wind, Earth

    public MagicPillarPuzzleManager puzzleManager;
    public bool useTrigger = true;

    private Renderer rend;
    private Color defaultColor;

    void Start()
    {
        rend = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
        if (rend != null)
            defaultColor = rend.material.color;

        if (puzzleManager == null)
            puzzleManager = FindObjectOfType<MagicPillarPuzzleManager>();
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
        if (other.CompareTag(elementTag))     // 🔥 Only react to matching element!
            puzzleManager.RegisterInput(elementTag);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (useTrigger) return;
        if (collision.collider.CompareTag(elementTag))   // 🔥 Only react to matching element!
            puzzleManager.RegisterInput(elementTag);
    }
}
