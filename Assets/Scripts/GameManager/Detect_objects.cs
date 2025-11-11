using System.Runtime.CompilerServices;
using Unity.VisualScripting;
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
    public int longitudArreglo = 6;

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
        
        if(ui_oculto != null) seleccionAccion(nombreEscena);
    }

    void seleccionAccion(string nombreEscena)
    {
        switch (gameObject.tag)
        {
            // UI Lobby libro
            case "Libro":
                LibroLobby(nombreEscena);
                break;
            // UI Lobby armario - Tienda
            case "Armario":
                ArmarioLobby(nombreEscena);
                break;
            default:
                break;
        }
        
    }

    void LibroLobby(string nombreEscena)
    {
        if (nombreEscena == escenaDeterminada && ui_oculto.Length == longitudArreglo)
        {

            DetectarObjetos(ui_oculto[0]);
            if (Input.GetKeyDown(teclaAbrir) && !ui_oculto[1].activeSelf && !ui_oculto[2].activeSelf && !ui_oculto[3].activeSelf && !ui_oculto[4].activeSelf && ui_oculto[0].activeSelf)
            {
                if (!jugador) return;
                jugador.GetComponent<FreeFlyDebug>().enabled = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                EjecutarAccion(ui_oculto[1], true);
                EjecutarAccion(ui_oculto[2], true);

            }
            else if (Input.GetKeyDown(teclaAbrir) && ui_oculto[1].activeSelf && ui_oculto[2].activeSelf && !ui_oculto[3].activeSelf && !ui_oculto[4].activeSelf && ui_oculto[0].activeSelf)
            {


                for (int i = 1; i < ui_oculto.Length; i++)
                {
                    EjecutarAccion(ui_oculto[i], false);
                }
                jugador.GetComponent<FreeFlyDebug>().enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

            }

        }
    }


    void ArmarioLobby(string nombreEscena)
    {
        if (nombreEscena == escenaDeterminada && ui_oculto.Length == longitudArreglo)
        {

            DetectarObjetos(ui_oculto[0]);
            if (Input.GetKeyDown(teclaAbrir) && ui_oculto[0].activeSelf && !ui_oculto[1].activeSelf)
            {
                if (!jugador) return;
                jugador.GetComponent<FreeFlyDebug>().enabled = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                EjecutarAccion(ui_oculto[1], true);
                


            }
            else if (Input.GetKeyDown(teclaAbrir) && ui_oculto[1].activeSelf && ui_oculto[0].activeSelf)
            {


                for (int i = 1; i < ui_oculto.Length; i++)
                {
                    EjecutarAccion(ui_oculto[i], false);
                }
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

    void EjecutarAccion(GameObject ui,bool activar)
    {
        ui.SetActive(activar);
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        // Dibuja el gizmo en el mismo centro que el usado en la detección
        Vector3 centroDeteccion = transform.position + offset;
        Gizmos.DrawWireSphere(centroDeteccion, radioDeteccion);
    }
}
