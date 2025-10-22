using UnityEngine;

public class PlayerMagicInput1 : MonoBehaviour
{
    // Asigna este componente en el Inspector o se autodescubre en Awake
    public Magic1 magic;

    // Opcional: desactiva entradas (útil para menús, cinemáticas, etc.)
    [Header("Mock Options")]
    public bool inputEnabled = true;

    private void Reset()
    {
        // Conveniencia en editor: intenta autollenar desde el mismo GO
        if (magic == null) magic = GetComponent<Magic1>();
    }

    private void Awake()
    {
        // Autodescubrimiento en runtime
        if (magic == null) magic = GetComponent<Magic1>();
        if (magic == null) magic = GetComponentInChildren<Magic1>();
        if (magic == null) magic = FindObjectOfType<Magic1>();

        if (magic == null)
            Debug.LogWarning("PlayerMagicInput (MOCK): no se encontró Magic. Asigna uno en el Inspector.");
    }

    private void Update()
    {
        if (!inputEnabled || magic == null) return;

        // Click izquierdo: lanzar hechizo
        if (Input.GetMouseButtonDown(0))
            magic.launchElement();

        // Tecla L: descripción del elemento
        if (Input.GetKeyDown(KeyCode.L))
            magic.elementDescription();
    }
}
