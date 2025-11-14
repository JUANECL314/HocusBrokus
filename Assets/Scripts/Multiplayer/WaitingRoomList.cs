using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaitingRoomUI : MonoBehaviourPunCallbacks
{
    [Header("Referencias UI")]
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;
    public Transform playerListContainer;
    public GameObject playerListItemPrefab;     // Prefab con un TMP_Text
    public GameObject panel;
    public Button startGameButton;

    /*private void Start()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("WaitingRoomUI: No estás en una sala todavía.");
            return;
        }

        ActualizarUI();

        // El botón solo debe mostrarse si eres MasterClient
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        // Asignar evento del botón
        startGameButton.onClick.AddListener(() =>
        {
            NetworkManager.Instance.MasterIniciarPartida();
        });
    }

    // ----------------------- Actualización visual -----------------------

    public void ActualizarUI()
    {
        if (roomNameText != null)
            roomNameText.text = $"Sala: {PhotonNetwork.CurrentRoom.Name}";

        if (playerCountText != null)
            playerCountText.text = $"{PhotonNetwork.CurrentRoom.PlayerCount} / {PhotonNetwork.CurrentRoom.MaxPlayers} jugadores";

        // Limpiar lista anterior
        foreach (Transform t in playerListContainer)
            Destroy(t.gameObject);

        // Crear listado de jugadores actual
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListContainer);
            TMP_Text txt = item.GetComponentInChildren<TMP_Text>();
            txt.text = p.NickName + (p.IsMasterClient ? " (Master)" : "");
        }
    }

    // ----------------------- Callbacks de Photon -----------------------

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        ActualizarUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ActualizarUI();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // Botón visible solo para el nuevo master
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        ActualizarUI();
    }

    public void AbrirEstadoPanel(bool estado)
    {
        panel.SetActive(estado);
        
        
    }
    */
    

}
