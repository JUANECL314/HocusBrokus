using UnityEngine;

public class InteractRaycast : MonoBehaviour
{
    public float maxDistance = 100f;
    public LayerMask interactLayer;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactLayer))
            {
                MirrorButton button = hit.collider.GetComponent<MirrorButton>();
                if (button != null)
                {
                    button.Press();
                }
            }
        }
    }
}
