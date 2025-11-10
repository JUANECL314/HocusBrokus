using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Detect_objects : MonoBehaviour
{
    [Header("Rango y objetivo a detectar")]
    public float radioDeteccion = 5f;
    public string tagObjetivo = "Player";
    public string escenaDeterminada;
    public Vector3 offset = new Vector3(0, 4f, 0);
    [Header("UI")]
    public GameObject[] ui_oculto;
    public KeyCode teclaAbrir = KeyCode.E;
    private bool jugadorCerca = false;

    public GameObject jugador;
    void Awake()
    {
        // Busca el script incluso si el objeto hijo está desactivado
        foreach (GameObject ui in ui_oculto)
        {
            if (ui != null)
                ui.SetActive(false);
        }
        escenaDeterminada = SceneManager.GetActiveScene().name;
    }

    void Update()
    {
        string nombreEscena = escenaDeterminada;
        if (string.IsNullOrEmpty(escenaDeterminada))
            return;
        if (jugador != null || !jugadorCerca) return;
        seleccionAccion(nombreEscena);
    }

    void seleccionAccion(string nombreEscena)
    {
        if (ui_oculto != null && nombreEscena == escenaDeterminada && ui_oculto.Length == 2)
        {

            DetectarObjetos(ui_oculto[0]);
            if (Input.GetKeyDown(teclaAbrir))
            {
                if (!jugador) return;
                jugador.GetComponent<FreeFlyDebug>().enabled = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                EjecutarAccion(ui_oculto[1]);
            }
            else if (Input.GetKeyUp(teclaAbrir))
            {

                EjecutarAccion(ui_oculto[1]);
                jugador.GetComponent<FreeFlyDebug>().enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

            }

        }
    }
    void DetectarObjetos(GameObject ui)
    {
        bool encontrado = false;

        // Centro del radio desplazado
        Vector3 centroDeteccion = transform.position + offset;
        Collider[] objetos = Physics.OverlapSphere(centroDeteccion, radioDeteccion);

        foreach (Collider col in objetos)
        {
            if (col.CompareTag(tagObjetivo))
            {
                encontrado = true;
                jugador = col.gameObject;
                break;
            }
        }


        jugadorCerca = encontrado;

        if (ui != null)
            ui.SetActive(encontrado);
    }

    void EjecutarAccion(GameObject ui)
    {
        ui.SetActive(jugadorCerca);
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        // Dibuja el gizmo en el mismo centro que el usado en la detección
        Vector3 centroDeteccion = transform.position + offset;
        Gizmos.DrawWireSphere(centroDeteccion, radioDeteccion);
    }
}
