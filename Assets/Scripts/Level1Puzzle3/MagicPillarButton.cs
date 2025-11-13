using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MagicPillarButton : MonoBehaviour
{
    [Header("Element Type")]
    public string elementTag = "Fire"; // Example: Fire, Water, Wind, Earth
    public MagicPillarPuzzleManager puzzleManager;
    public bool useTrigger = true;

    private Renderer rend;
    private Color defaultColor;

    void Start()
    {
        rend = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
        if (rend != null)
            defaultColor = rend.material.color;

        // Auto-find puzzle manager if not assigned
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

        // Only activate for player or spell objects
        if (IsMagicActivator(other.gameObject))
        {
            if (puzzleManager != null)
            {
                puzzleManager.RegisterInput(elementTag);
            }
            else
            {
                Debug.LogWarning($"PuzzleManager not assigned on {name}");
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (useTrigger) return;

        if (IsMagicActivator(collision.collider.gameObject))
        {
            if (puzzleManager != null)
            {
                puzzleManager.RegisterInput(elementTag);
            }
            else
            {
                Debug.LogWarning($"PuzzleManager not assigned on {name}");
            }
        }
    }

    private bool IsMagicActivator(GameObject go)
    {
        // Accept only player or any magic projectile
        return go.CompareTag("Player") || go.CompareTag("Fire") || go.CompareTag("Water") 
               || go.CompareTag("Wind") || go.CompareTag("Earth");
    }
}
