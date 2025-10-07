using UnityEngine;

public class PlayerMagicInput : MonoBehaviour
{
    // Single Magic reference (assign in Inspector)
    public Magic magic;

    private void Reset()
    {
        // editor convenience: autofill from same GameObject
        if (magic == null) magic = GetComponent<Magic>();
    }

    private void Awake()
    {
        // runtime autofill: same object -> children -> any Magic in scene
        if (magic == null) magic = GetComponent<Magic>();
        if (magic == null) magic = GetComponentInChildren<Magic>();
        if (magic == null) magic = FindObjectOfType<Magic>();
        if (magic == null)
            Debug.LogWarning("PlayerMagicInput: no Magic assigned. Assign it in the Inspector or add a Magic component to this GameObject or its children.");
    }

    void Update()
    {
        if (magic == null) return;

        // Left click to launch
        if (Input.GetMouseButtonDown(0)) magic.launchElement();

        // Press L to print element description
        if (Input.GetKeyDown(KeyCode.L)) magic.elementDescription();
    }
}
