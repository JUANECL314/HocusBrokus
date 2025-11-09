using UnityEngine;
using UnityEngine.SceneManagement;

public class Detect_objects : MonoBehaviour
{
    public float radioDeteccion = 5f;
    public string tagObjetivoLobby = "Libro";
    public string tagObjetivoMultijugador = "Portal";
    [SerializeField] string _nombreEscenaLobbyMultiJugador = "TownRoom";
    [SerializeField] string _nombreEscenaLobbyIndividual = "Lobby";
    public Vector3 offset = new Vector3(0, 4f, 0);
    public CreateAndJoinRooms roomsScript;


    void Start()
    {
        // Busca el script incluso si el objeto hijo está desactivado
        roomsScript = GetComponentInChildren<CreateAndJoinRooms>(true);

        if (roomsScript == null)
            Debug.LogWarning("No se encontró el script CreateAndJoinRooms en los hijos.");
    }

    void Update()
    {
        string nombreEscena = SceneManager.GetActiveScene().name;
        if (roomsScript != null && nombreEscena == _nombreEscenaLobbyIndividual)
        {
            DetectarObjetos();
        }
        else if (nombreEscena == _nombreEscenaLobbyMultiJugador)
        {

        }
        
    }

    void DetectarObjetos()
    {
        bool encontrado = false;

        // Centro del radio desplazado
        Vector3 centroDeteccion = transform.position + offset;
        Collider[] objetos = Physics.OverlapSphere(centroDeteccion, radioDeteccion);

        foreach (Collider col in objetos)
        {
            if (col.CompareTag(tagObjetivoLobby))
            {
                encontrado = true;
                break;
            }
        }

        if (roomsScript != null)
        {
            if (encontrado)
            {
                Debug.Log("Detectado objeto con tag " + tagObjetivoLobby);
                roomsScript.ActivarBoton();
            }
            else
            {
                roomsScript.DesactivarBoton();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        // Dibuja el gizmo en el mismo centro que el usado en la detección
        Vector3 centroDeteccion = transform.position + offset;
        Gizmos.DrawWireSphere(centroDeteccion, radioDeteccion);
    }
}
