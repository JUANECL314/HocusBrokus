using UnityEngine;

public class EndorsementsBootstrap : MonoBehaviour
{
    private void Awake()
    {
        // Liberar cursor para poder usar el mouse en el menú.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Asegura que el tiempo esté normal por si alguna escena anterior lo cambió.
        Time.timeScale = 1f;
    }
}
