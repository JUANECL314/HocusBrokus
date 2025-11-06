using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_Player : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    GameObject UI_Lobby;
    [SerializeField]
    GameObject UI_LobbyMultiPlayer;
    [SerializeField]
    GameObject UI_GamePlayer;


    [Header("Nombres de escenas con UI")]
    [SerializeField] string nombreEscenaLobbyMultiJugador = "TownRoom";
    [SerializeField] string nombreEscenaLobbyIndividual = "Lobby";

    void Start()
    {
        Desactivados();
        ActivarUI();
    }

    void Desactivados()
    {
        UI_Lobby.SetActive(false);
        UI_LobbyMultiPlayer.SetActive(false);
        UI_GamePlayer.SetActive(false);
    }

    void ActivarUI()
    {
        string nombreEscena = SceneManager.GetActiveScene().name;

        if (nombreEscena == nombreEscenaLobbyIndividual)
            UI_Lobby.SetActive(true);
        else if (nombreEscena == nombreEscenaLobbyMultiJugador)
            UI_LobbyMultiPlayer.SetActive(true);
        else
            UI_GamePlayer.SetActive(true);
    }

}
