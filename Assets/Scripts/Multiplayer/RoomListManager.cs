using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomListManager : MonoBehaviourPunCallbacks
{
    [Header("Referencias UI")]
    public GameObject roomItemPrefab;
    public Transform content; // Donde se instancian las salas
    public Button refreshButton;

    void Start()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshList);

        if (!PhotonNetwork.InLobby)
        {
            Debug.Log("No estás en el lobby, intentando unir...");
            PhotonNetwork.JoinLobby();
        }
        else
        {
            // Ya estás en el lobby, pinta lo que haya en el cache
            RefreshList();
        }
    }

    public void RefreshList()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogWarning("RoomListManager: NetworkManager.Instance es null. No se puede leer RoomCache.");
            return;
        }

        var cache = NetworkManager.RoomCache;
        Debug.Log($"[RoomListManager] Actualizar salas. Cache={cache.Count}");

        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (RoomInfo info in cache.Values)
        {
            if (info.RemovedFromList) continue;

            GameObject item = Instantiate(roomItemPrefab, content);
            item.transform.Find("RoomName").GetComponent<TMP_Text>().text = info.Name;
            item.transform.Find("PlayerCount").GetComponent<TMP_Text>().text = $"{info.PlayerCount}/{info.MaxPlayers}";
            Button joinButton = item.transform.Find("JoinButton").GetComponent<Button>();

            string roomName = info.Name; // capturar en variable local
            joinButton.onClick.AddListener(() =>
            {
                Debug.Log($"[RoomListManager] Unirse a sala: {roomName}");
                PhotonNetwork.JoinRoom(roomName);
            });
        }

        Debug.Log("[RoomListManager] Actualización completada.");
    }

    // Si quieres que se refresque automáticamente en esta escena también:
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        // El NetworkManager ya actualizó RoomCache; aquí solo repintamos UI
        RefreshList();
    }
}
