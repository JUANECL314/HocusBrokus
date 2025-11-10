using UnityEngine;
using Photon.Pun;
using Photon.Voice.PUN;
using UnityEngine.SceneManagement;

public class SpawnPlayers : MonoBehaviour
{
    public GameObject jugadorPrefab;
    [SerializeField] string _nombreEscenaLobbyMultiJugador = "TownRoom";
    [SerializeField] string _nombreEscenaLobbyIndividual = "Lobby";

    private void Start()
    {
        SeleccionEscenas();
    }

    void SeleccionEscenas()
    {
        string nombreEscena = SceneManager.GetActiveScene().name;

        if (nombreEscena == _nombreEscenaLobbyIndividual)
            AparicionLobbyIndividual();
        else if (nombreEscena == _nombreEscenaLobbyMultiJugador)
            AparicionLobbyMultijugador();
    }

    void AparicionLobbyIndividual()
    {
        Vector3 spawnPoint = transform.position;

        // Instanciar el jugador offline
        GameObject jugador = Instantiate(jugadorPrefab, spawnPoint, Quaternion.identity);

        // Activar componentes locales
        // jugador.GetComponent<FreeFlyCamera>().enableFlying = false;
      
        jugador.GetComponent<PlayerMagicInput>().enabled = false;
        jugador.GetComponent<Magic>().enabled = false;
        jugador.GetComponent<Elements>().enabled = false;

        // Activar cámara y audio
        PlayerCamera pc = jugador.GetComponent<PlayerCamera>();
        if (pc != null) pc.EnableLocalCamera(); // Método que activa camera y audio en jugador local
    }

    void AparicionLobbyMultijugador()
    {
        Vector3 spawnPoint = new Vector3(
            Random.Range(transform.position.x - 5, transform.position.x + 5),
            transform.position.y,
            transform.position.z
        );

        // Instanciar jugador en red
        GameObject jugador = PhotonNetwork.Instantiate(jugadorPrefab.name, spawnPoint, Quaternion.identity);

        // Activar componentes locales
        PhotonView pv = jugador.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            
            jugador.GetComponent<PlayerMagicInput>().enabled = true;
            jugador.GetComponent<Magic>().enabled = true;
            jugador.GetComponent<Elements>().enabled = true;

            PlayerCamera pc = jugador.GetComponent<PlayerCamera>();
            if (pc != null) pc.EnableLocalCamera();
        }
    }
}
